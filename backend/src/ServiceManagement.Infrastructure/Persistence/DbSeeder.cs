using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceManagement.Domain.Constants;
using ServiceManagement.Domain.Entities;

namespace ServiceManagement.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
        var db = sp.GetRequiredService<AppDbContext>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();
        var config = sp.GetRequiredService<IConfiguration>();

        await db.Database.MigrateAsync();

        foreach (var permissionName in Permissions.All)
        {
            if (!await db.Permissions.AnyAsync(p => p.Name == permissionName))
            {
                db.Permissions.Add(new Permission
                {
                    Name = permissionName,
                    Description = permissionName
                });
            }
        }

        await db.SaveChangesAsync();

        await EnsureRoleAsync(roleManager, db, Roles.Administrator, "Platform administrator", Permissions.AdministratorDefaults);
        await EnsureRoleAsync(roleManager, db, Roles.CommonUser, "Customer / common user", Permissions.CommonUserDefaults);
        await EnsureRoleAsync(roleManager, db, Roles.ServiceProvider, "Service provider", Permissions.ServiceProviderDefaults);

        var adminEmail = config["Seed:AdminEmail"] ?? "admin@servicemanagement.local";
        var adminPassword = config["Seed:AdminPassword"] ?? "ChangeMe!Admin123";
        await EnsureUserAsync(
            userManager,
            logger,
            email: adminEmail,
            password: adminPassword,
            firstName: "Platform",
            lastName: "Admin",
            role: Roles.Administrator);

        var customerEmail = config["Seed:CustomerEmail"] ?? "customer@test.local";
        var customerPassword = config["Seed:CustomerPassword"] ?? "Customer!123";
        await EnsureUserAsync(
            userManager,
            logger,
            email: customerEmail,
            password: customerPassword,
            firstName: "Sara",
            lastName: "Customer",
            role: Roles.CommonUser);

        var providerEmail = config["Seed:ProviderEmail"] ?? "provider@test.local";
        var providerPassword = config["Seed:ProviderPassword"] ?? "Provider!123";
        var provider = await EnsureUserAsync(
            userManager,
            logger,
            email: providerEmail,
            password: providerPassword,
            firstName: "John",
            lastName: "Provider",
            role: Roles.ServiceProvider);

        if (provider is not null &&
            !await db.ServiceProviderProfiles.AnyAsync(p => p.UserId == provider.Id))
        {
            db.ServiceProviderProfiles.Add(new ServiceProviderProfile
            {
                UserId = provider.Id,
                AvailabilityStatus = Domain.Enums.AvailabilityStatus.Available,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }
    }

    private static async Task<ApplicationUser?> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        string email,
        string password,
        string firstName,
        string lastName,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            return user;
        }

        user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            logger.LogError(
                "Failed to create seed user {Email}: {Errors}",
                email,
                string.Join("; ", createResult.Errors.Select(e => e.Description)));
            return null;
        }

        await userManager.AddToRoleAsync(user, role);
        logger.LogInformation("Seeded {Role} account {Email}", role, email);
        return user;
    }

    private static async Task EnsureRoleAsync(
        RoleManager<ApplicationRole> roleManager,
        AppDbContext db,
        string roleName,
        string description,
        IEnumerable<string> permissionNames)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            role = new ApplicationRole
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant(),
                Description = description,
                CreatedAt = DateTime.UtcNow
            };
            await roleManager.CreateAsync(role);
        }

        var desired = permissionNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existing = await db.RolePermissions
            .Include(rp => rp.Permission)
            .Where(rp => rp.RoleId == role.Id)
            .ToListAsync();

        var existingNames = existing.Select(rp => rp.Permission.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var permissions = await db.Permissions.Where(p => desired.Contains(p.Name)).ToListAsync();

        foreach (var permission in permissions.Where(p => !existingNames.Contains(p.Name)))
        {
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id
            });
        }

        await db.SaveChangesAsync();
    }
}
