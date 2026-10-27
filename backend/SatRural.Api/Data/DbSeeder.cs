using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SatRural.Domain.Entities;
using SatRural.Infrastructure.Persistence;

namespace SatRural.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        var dbContext = services.GetRequiredService<AuthDbContext>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher<User>>();

        var username = configuration["InitialAdmin:Username"]
                       ?? "adminangi@gmail.com";

        var password = configuration["InitialAdmin:Password"]
                       ?? "adminangi123*";

        var fullName = configuration["InitialAdmin:FullName"]
                       ?? "Administrador";

        username = username.Trim().ToLowerInvariant();

        var existingUser = await dbContext.Users
            .SingleOrDefaultAsync(user => user.Username == username);

        if (existingUser is not null)
        {
            return;
        }

        var adminRole = await dbContext.Roles
            .SingleOrDefaultAsync(role => role.Id == Rol.AdminId);

        if (adminRole is null)
        {
            throw new InvalidOperationException(
                "El rol ADMIN no existe en la base de datos.");
        }

        var user = new User
        {
            Username = username,
            FullName = fullName,
            RolId = Rol.AdminId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(
            user,
            password);

        dbContext.Users.Add(user);

        await dbContext.SaveChangesAsync();
    }
}