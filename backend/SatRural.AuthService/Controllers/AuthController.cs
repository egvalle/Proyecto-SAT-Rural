using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SatRural.Domain.Entities;
using SatRural.Infrastructure.Persistence;

namespace SatRural.AuthService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthController(
        AuthDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        var fullName = request.FullName.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(fullName))
        {
            return BadRequest(new { message = "Username and full name are required." });
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return BadRequest(new { message = "Password must contain at least 8 characters." });
        }

        var role = await _dbContext.Roles
            .SingleOrDefaultAsync(role => role.Id == request.RoleId);
        if (role is null)
        {
            return BadRequest(new
            {
                message = $"RoleId must be {Rol.AdminId} (ADMIN), {Rol.UserId} (USER), or {Rol.UserConsultaId} (USERCONSULTA)."
            });
        }

        if (await _dbContext.Users.AnyAsync(user => user.Username == username))
        {
            return Conflict(new { message = "Username is already registered." });
        }

        var user = new User
        {
            Username = username,
            FullName = fullName,
            RolId = role.Id,
            IsActive = true
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return StatusCode(
            StatusCodes.Status201Created,
            CreateAuthResponse(user, role.Descripcion));
    }

    [Authorize]
    [HttpGet("admin/users")]
    public async Task<ActionResult<IReadOnlyList<AdminUserResponse>>> GetAdminUsers(
        CancellationToken cancellationToken,
        [FromQuery] int? roleId = null)
    {
        if (!await IsCurrentUserAdminAsync())
        {
            return Forbid();
        }

        if (roleId.HasValue &&
            !await _dbContext.Roles.AnyAsync(
                role => role.Id == roleId.Value,
                cancellationToken))
        {
            return BadRequest(new { message = "The specified roleId does not exist." });
        }

        var users = await (
            from user in _dbContext.Users.AsNoTracking()
            join role in _dbContext.Roles.AsNoTracking()
                on user.RolId equals role.Id
            where !roleId.HasValue || user.RolId == roleId.Value
            orderby user.Id
            select new AdminUserResponse(
                user.Id,
                user.Username,
                user.FullName,
                user.RolId,
                role.Descripcion,
                user.IsActive,
                user.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [Authorize]
    [HttpGet("admin/users/{id:int}")]
    public async Task<ActionResult<UserResponse>> GetAdminUser(int id)
    {
        if (!await IsCurrentUserAdminAsync())
        {
            return Forbid();
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        var roleDescription = await _dbContext.Roles
            .Where(role => role.Id == user.RolId)
            .Select(role => role.Descripcion)
            .SingleAsync();

        return Ok(ToUserResponse(user, roleDescription));
    }

    [Authorize]
    [HttpPut("admin/users/{id:int}")]
    public async Task<ActionResult<AdminUserResponse>> UpdateAdminUser(
        int id,
        UpdateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync())
        {
            return Forbid();
        }

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "User not found." });
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new { message = "Full name is required." });
        }

        if (request.Password is not null &&
            (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8))
        {
            return BadRequest(new
            {
                message = "Password must contain at least 8 characters."
            });
        }

        var role = await _dbContext.Roles
            .SingleOrDefaultAsync(candidate => candidate.Id == request.RoleId, cancellationToken);
        if (role is null)
        {
            return BadRequest(new { message = "The specified roleId does not exist." });
        }

        if (user.Id == GetCurrentUserId() && user.RolId != request.RoleId)
        {
            return BadRequest(new { message = "You cannot change your own role." });
        }

        user.FullName = request.FullName.Trim();
        user.RolId = role.Id;
        if (request.Password is not null)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToAdminUserResponse(user, role.Descripcion));
    }

    [Authorize]
    [HttpPatch("admin/users/{id:int}/status")]
    public async Task<ActionResult<AdminUserResponse>> ChangeAdminUserStatus(
        int id,
        ChangeAdminUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdminAsync())
        {
            return Forbid();
        }

        if (id == GetCurrentUserId() && !request.IsActive)
        {
            return BadRequest(new { message = "You cannot deactivate your own account." });
        }

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "User not found." });
        }

        user.IsActive = request.IsActive;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var roleDescription = await GetRoleDescriptionAsync(user.RolId);
        return Ok(ToAdminUserResponse(user, roleDescription));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(candidate => candidate.Username == username && candidate.IsActive);

        if (user is null ||
            _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) ==
            PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        var roleDescription = await GetRoleDescriptionAsync(user.RolId);
        return Ok(CreateAuthResponse(user, roleDescription));
    }

    [HttpPost("google")]
    public async Task<ActionResult<AuthResponse>> LoginWithGoogle(
        GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        var firebaseApiKey = _configuration["Firebase:ApiKey"];
        if (string.IsNullOrWhiteSpace(firebaseApiKey))
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = "El inicio de sesión con Google no está configurado." });
        }

        using var firebaseResponse = await _httpClientFactory
            .CreateClient()
            .PostAsJsonAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:lookup?key={firebaseApiKey}",
                new FirebaseTokenRequest(request.IdToken),
                cancellationToken);

        if (!firebaseResponse.IsSuccessStatusCode)
        {
            return Unauthorized(new
            {
                message = "La sesión de Google no es válida o ha expirado."
            });
        }

        var lookup = await firebaseResponse.Content
            .ReadFromJsonAsync<FirebaseAccountLookup>(cancellationToken);
        var account = lookup?.Users?.FirstOrDefault();

        if (account is null ||
            !account.EmailVerified ||
            string.IsNullOrWhiteSpace(account.Email) ||
            account.ProviderUserInfo?.Any(provider => provider.ProviderId == "google.com") != true)
        {
            return Unauthorized(new
            {
                message = "Google no devolvió una cuenta verificada válida."
            });
        }

        var username = account.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(
                candidate => candidate.Username == username && candidate.IsActive,
                cancellationToken);

        if (user is null)
        {
            return Unauthorized(new
            {
                message = "El correo Google no está asociado a un usuario SAT Rural activo."
            });
        }

        var roleDescription = await GetRoleDescriptionAsync(user.RolId);
        return Ok(CreateAuthResponse(user, roleDescription));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me()
    {
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(subject, out var userId))
        {
            return Unauthorized();
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == userId && candidate.IsActive);

        if (user is null)
        {
            return Unauthorized();
        }

        var roleDescription = await GetRoleDescriptionAsync(user.RolId);
        return Ok(ToUserResponse(user, roleDescription));
    }

    private async Task<bool> IsCurrentUserAdminAsync()
    {
        var userId = GetCurrentUserId();
        return userId.HasValue &&
            await _dbContext.Users.AnyAsync(candidate =>
                candidate.Id == userId.Value &&
                candidate.IsActive &&
                candidate.RolId == Rol.AdminId);
    }

    private int? GetCurrentUserId()
    {
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(subject, out var userId) ? userId : null;
    }

    private Task<string> GetRoleDescriptionAsync(int roleId) =>
        _dbContext.Roles
            .Where(role => role.Id == roleId)
            .Select(role => role.Descripcion)
            .SingleAsync();

    private AuthResponse CreateAuthResponse(User user, string roleDescription)
    {
        var key = _configuration["Jwt:Key"]!;
        var issuer = _configuration["Jwt:Issuer"]!;
        var audience = _configuration["Jwt:Audience"]!;
        var expiresAt = DateTime.UtcNow.AddMinutes(
            _configuration.GetValue("Jwt:ExpiresMinutes", 60));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, roleDescription),
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AuthResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            ToUserResponse(user, roleDescription));
    }

    private static UserResponse ToUserResponse(User user, string roleDescription) =>
        new(user.Id, user.Username, user.FullName, user.RolId, roleDescription);

    private static AdminUserResponse ToAdminUserResponse(User user, string roleDescription) =>
        new(
            user.Id,
            user.Username,
            user.FullName,
            user.RolId,
            roleDescription,
            user.IsActive,
            user.CreatedAt);
}

public sealed record RegisterRequest(string Username, string Password, string FullName, int RoleId);

public sealed record UpdateAdminUserRequest(
    string FullName,
    int RoleId,
    string? Password = null);

public sealed record ChangeAdminUserStatusRequest(bool IsActive);

public sealed record LoginRequest(string Username, string Password);

public sealed record GoogleLoginRequest(string IdToken);

public sealed record FirebaseTokenRequest(string IdToken);

public sealed record FirebaseAccountLookup(
    [property: JsonPropertyName("users")] FirebaseAccount[]? Users);

public sealed record FirebaseAccount(
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("emailVerified")] bool EmailVerified,
    [property: JsonPropertyName("providerUserInfo")] FirebaseProviderInfo[]? ProviderUserInfo);

public sealed record FirebaseProviderInfo(
    [property: JsonPropertyName("providerId")] string ProviderId);

public sealed record UserResponse(
    int Id,
    string Username,
    string FullName,
    int RoleId,
    string RoleDescription);

public sealed record AdminUserResponse(
    int Id,
    string Username,
    string FullName,
    int RoleId,
    string RoleDescription,
    bool IsActive,
    DateTime CreatedAt);

public sealed record AuthResponse(string Token, UserResponse User);