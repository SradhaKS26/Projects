using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceManagement.Domain.Constants;
using ServiceManagement.Domain.Entities;
using ServiceManagement.Infrastructure.Persistence;

namespace ServiceManagement.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // EF's in-memory provider keys stores by name globally, so a shared constant would
    // let parallel test classes seed into the same store and duplicate the seed users.
    private readonly string _databaseName = $"ServiceManagementTests_{Guid.NewGuid():N}";
    private readonly SemaphoreSlim _seedLock = new(1, 1);
    private bool _seeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(AppDbContext));

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }

    public async Task EnsureSeededAsync()
    {
        if (_seeded)
        {
            return;
        }

        await _seedLock.WaitAsync();
        try
        {
            if (_seeded)
            {
                return;
            }

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            await db.Database.EnsureCreatedAsync();
            await SeedTestDataAsync(db, userManager, roleManager);
            _seeded = true;
        }
        finally
        {
            _seedLock.Release();
        }
    }

    private static async Task SeedTestDataAsync(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        foreach (var permissionName in Permissions.All)
        {
            if (!await db.Permissions.AnyAsync(p => p.Name == permissionName))
            {
                db.Permissions.Add(new Permission { Name = permissionName, Description = permissionName });
            }
        }

        await db.SaveChangesAsync();

        foreach (var roleName in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant(),
                    Description = roleName,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        var adminRole = await roleManager.FindByNameAsync(Roles.Administrator);
        if (adminRole is not null)
        {
            var permissions = await db.Permissions.ToListAsync();
            foreach (var permission in permissions)
            {
                if (!await db.RolePermissions.AnyAsync(rp => rp.RoleId == adminRole.Id && rp.PermissionId == permission.Id))
                {
                    db.RolePermissions.Add(new RolePermission
                    {
                        RoleId = adminRole.Id,
                        PermissionId = permission.Id
                    });
                }
            }

            await db.SaveChangesAsync();
        }

        var adminEmail = "admin@test.local";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "Test",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await userManager.CreateAsync(admin, "TestAdmin!123");
            await userManager.AddToRoleAsync(admin, Roles.Administrator);
        }
    }
}
