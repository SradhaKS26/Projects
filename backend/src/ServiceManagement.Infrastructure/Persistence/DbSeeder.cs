using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceManagement.Domain.Constants;
using ServiceManagement.Domain.Entities;
using ServiceManagement.Domain.Enums;

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
                Description = "Demo provider awaiting review.",
                AvailabilityStatus = AvailabilityStatus.Unavailable,
                IsActive = true,
                ApprovalStatus = ProviderApprovalStatus.Pending
            });
            await db.SaveChangesAsync();
        }

        await SeedCatalogAsync(db, logger);
        await SeedWorkedExampleAsync(db, logger);
        await SeedDemoProviderApplicationAsync(db, provider, logger);
    }

    /// <summary>
    /// Seeds a small, deliberately generic catalog so a fresh environment is usable.
    /// Only runs when the catalog is empty; it never overwrites operator-managed data.
    /// </summary>
    private static async Task SeedCatalogAsync(AppDbContext db, ILogger logger)
    {
        if (await db.ServiceCategories.AnyAsync())
        {
            return;
        }

        var catalog = new (string Category, string? Description, (string Name, decimal Price, int Minutes, string Description)[] Services)[]
        {
            ("Home Cleaning", "Residential and deep cleaning services",
            [
                ("Standard Home Cleaning", 49.00m, 120, "General cleaning for an average home."),
                ("Deep Cleaning", 129.00m, 300, "Detailed top-to-bottom cleaning."),
                ("Kitchen Cleaning", 39.00m, 90, "Focused kitchen and appliance cleaning.")
            ]),
            ("Cooking", "Personal cooking and meal preparation",
            [
                ("Daily Meal Preparation", 25.00m, 90, "Home-cooked meals prepared at your place."),
                ("Party Catering Assistance", 199.00m, 480, "Cooking support for small gatherings.")
            ]),
            ("Delivery", "Pickup and delivery errands",
            [
                ("Local Package Delivery", 12.50m, 60, "Same-city pickup and drop-off."),
                ("Grocery Pickup", 15.00m, 75, "Shopping and delivery of a grocery list.")
            ]),
            ("Repairs & Maintenance", "Household repair and upkeep",
            [
                ("Plumbing Repair", 59.00m, 120, "Leak fixes and basic plumbing work."),
                ("Electrical Repair", 65.00m, 120, "Switches, sockets, and fixture repairs."),
                ("Appliance Servicing", 45.00m, 90, "Diagnostics and servicing of home appliances.")
            ])
        };

        foreach (var (categoryName, categoryDescription, services) in catalog)
        {
            var category = new ServiceCategory
            {
                Name = categoryName,
                Description = categoryDescription,
                IsActive = true
            };

            foreach (var (name, price, minutes, description) in services)
            {
                category.Services.Add(new Service
                {
                    Name = name,
                    Description = description,
                    BasePrice = price,
                    EstimatedDurationMinutes = minutes,
                    IsActive = true
                });
            }

            db.ServiceCategories.Add(category);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} sample service categories", catalog.Length);
    }

    /// <summary>
    /// Worked example used throughout onboarding docs and demos. Vehicle types are
    /// ordinary catalog services (not a separate domain concept). Seed data only —
    /// application code never branches on these names.
    /// </summary>
    private static async Task SeedWorkedExampleAsync(AppDbContext db, ILogger logger)
    {
        var taxi = await db.ServiceCategories
            .Include(c => c.Services)
            .Include(c => c.DocumentRequirements)
            .FirstOrDefaultAsync(c => c.Name == "Taxi");

        if (taxi is null)
        {
            taxi = new ServiceCategory
            {
                Name = "Taxi",
                Description = "On-demand passenger trips. Vehicle types are services with their own base price.",
                IsActive = true
            };

            taxi.Services.Add(new Service
            {
                Name = "Auto",
                Description = "Compact three-wheeler for short trips.",
                BasePrice = 8.00m,
                EstimatedDurationMinutes = 20,
                IsActive = true
            });
            taxi.Services.Add(new Service
            {
                Name = "Sedan",
                Description = "Standard four-door car.",
                BasePrice = 12.00m,
                EstimatedDurationMinutes = 25,
                IsActive = true
            });
            taxi.Services.Add(new Service
            {
                Name = "SUV",
                Description = "Larger vehicle for more passengers or luggage.",
                BasePrice = 18.00m,
                EstimatedDurationMinutes = 30,
                IsActive = true
            });

            db.ServiceCategories.Add(taxi);
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded Taxi worked-example category with Auto, Sedan, and SUV services");
        }

        await EnsureDocumentRequirementsAsync(
            db,
            taxi.Id,
            [
                ("Driving Licence", "Valid licence for the vehicle class being offered.", 0),
                ("Vehicle Registration", "Registration document for the vehicle that will fulfill trips.", 1),
                ("Insurance Certificate", "Current motor insurance covering passenger transport.", 2)
            ]);

        var cooking = await db.ServiceCategories.FirstOrDefaultAsync(c => c.Name == "Cooking");
        if (cooking is not null)
        {
            await EnsureDocumentRequirementsAsync(
                db,
                cooking.Id,
                [
                    ("Food Safety Certificate", "Proof of food-hygiene training required to prepare meals for customers.", 0)
                ]);
        }
    }

    private static async Task EnsureDocumentRequirementsAsync(
        AppDbContext db,
        Guid categoryId,
        IReadOnlyList<(string Name, string Description, int SortOrder)> requirements)
    {
        var existing = await db.CategoryDocumentRequirements
            .Where(r => r.CategoryId == categoryId)
            .Select(r => r.Name.ToLower())
            .ToListAsync();

        var existingSet = existing.ToHashSet();
        var added = false;

        foreach (var (name, description, sortOrder) in requirements)
        {
            if (existingSet.Contains(name.ToLowerInvariant()))
            {
                continue;
            }

            db.CategoryDocumentRequirements.Add(new CategoryDocumentRequirement
            {
                CategoryId = categoryId,
                Name = name,
                Description = description,
                IsRequired = true,
                SortOrder = sortOrder
            });
            added = true;
        }

        if (added)
        {
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedDemoProviderApplicationAsync(
        AppDbContext db,
        ApplicationUser? provider,
        ILogger logger)
    {
        if (provider is null)
        {
            return;
        }

        var profile = await db.ServiceProviderProfiles
            .Include(p => p.ProviderServices)
            .FirstOrDefaultAsync(p => p.UserId == provider.Id);
        if (profile is null || profile.ProviderServices.Count > 0)
        {
            return;
        }

        var services = await db.Services
            .Include(s => s.Category)
            .Where(s => s.Category.Name == "Taxi" && (s.Name == "Auto" || s.Name == "Sedan"))
            .ToListAsync();

        if (services.Count == 0)
        {
            return;
        }

        foreach (var service in services)
        {
            profile.ProviderServices.Add(new ProviderService
            {
                ProviderProfileId = profile.Id,
                ServiceId = service.Id,
                Status = ProviderServiceStatus.Pending,
                AppliedAt = DateTime.UtcNow
            });
        }

        profile.Description ??= "Demo provider awaiting review.";
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded a pending demo provider application");
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
