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

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Platform",
                LastName = "Admin",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(admin, adminPassword);
            if (!createResult.Succeeded)
            {
                logger.LogError("Failed to create seed admin: {Errors}",
                    string.Join("; ", createResult.Errors.Select(e => e.Description)));
            }
            else
            {
                await userManager.AddToRoleAsync(admin, Roles.Administrator);
                logger.LogInformation("Seeded administrator account {Email}", adminEmail);
            }
        }
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
