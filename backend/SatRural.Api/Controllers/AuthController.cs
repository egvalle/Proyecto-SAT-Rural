using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SatRural.Domain.Entities;
using SatRural.Infrastructure.Persistence;

namespace SatRural.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthController(
        AppDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
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
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(subject, out var userId) &&
            await _dbContext.Users.AnyAsync(candidate =>
                candidate.Id == userId &&
                candidate.IsActive &&
                candidate.RolId == Rol.AdminId);
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
}

public sealed record RegisterRequest(string Username, string Password, string FullName, int RoleId);

public sealed record LoginRequest(string Username, string Password);

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