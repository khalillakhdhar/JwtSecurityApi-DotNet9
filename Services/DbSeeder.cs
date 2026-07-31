using JwtSecurityApi.Constants;
using JwtSecurityApi.Data;
using JwtSecurityApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JwtSecurityApi.Services;

public sealed class DbSeeder(
    ApplicationDbContext dbContext,
    IPasswordHasher<AppUser> passwordHasher,
    IConfiguration configuration,
    ILogger<DbSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var email = configuration["SeedAdmin:Email"]?.Trim().ToLowerInvariant();
        var password = configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation(
                "Administrateur non créé : SeedAdmin:Email ou SeedAdmin:Password absent.");
            return;
        }

        var normalizedEmail = email.ToUpperInvariant();

        if (await dbContext.Users.AnyAsync(
                user => user.NormalizedEmail == normalizedEmail,
                cancellationToken))
        {
            return;
        }

        var admin = new AppUser
        {
            FullName = "Administrateur",
            Email = email,
            NormalizedEmail = normalizedEmail,
            Role = Roles.Admin
        };

        admin.PasswordHash = passwordHasher.HashPassword(admin, password);

        dbContext.Users.Add(admin);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Administrateur initial créé pour {Email}.", email);
    }
}
