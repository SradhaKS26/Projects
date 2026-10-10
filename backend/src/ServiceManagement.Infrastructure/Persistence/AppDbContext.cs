using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ServiceManagement.Domain.Entities;

namespace ServiceManagement.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceProviderProfile> ServiceProviderProfiles => Set<ServiceProviderProfile>();
    public DbSet<ProviderService> ProviderServices => Set<ProviderService>();
    public DbSet<CategoryDocumentRequirement> CategoryDocumentRequirements => Set<CategoryDocumentRequirement>();
    public DbSet<ProviderDocument> ProviderDocuments => Set<ProviderDocument>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<ServiceRequestStatusHistory> ServiceRequestStatusHistories => Set<ServiceRequestStatusHistory>();
    public DbSet<ServiceRequestAttachment> ServiceRequestAttachments => Set<ServiceRequestAttachment>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ProfileImageUrl).HasMaxLength(1024);
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(x => x.Description).HasMaxLength(512);
        });

        builder.Entity<Permission>(entity =>
        {
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(512);
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(x => new { x.RoleId, x.PermissionId });
            entity.HasOne(x => x.Role)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Permission)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(x => x.Token).IsUnique();
            entity.Property(x => x.Token).HasMaxLength(512).IsRequired();
            entity.HasOne(x => x.User)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ServiceCategory>(entity =>
        {
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.ImageUrl).HasMaxLength(1024);
        });

        builder.Entity<Service>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.BasePrice).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.CategoryId, x.Name }).IsUnique();
            entity.HasOne(x => x.Category)
                .WithMany(x => x.Services)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ServiceProviderProfile>(entity =>
        {
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.HasIndex(x => x.ApprovalStatus);
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.ReviewReason).HasMaxLength(2000);
            entity.Property(x => x.Rating).HasPrecision(3, 2);
            entity.HasOne(x => x.User)
                .WithOne(x => x.ProviderProfile)
                .HasForeignKey<ServiceProviderProfile>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ReviewedByUser)
                .WithMany()
                .HasForeignKey(x => x.ReviewedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ProviderService>(entity =>
        {
            entity.HasKey(x => new { x.ProviderProfileId, x.ServiceId });
            entity.HasOne(x => x.ProviderProfile)
                .WithMany(x => x.ProviderServices)
                .HasForeignKey(x => x.ProviderProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Service)
                .WithMany(x => x.ProviderServices)
                .HasForeignKey(x => x.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CategoryDocumentRequirement>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.HasIndex(x => new { x.CategoryId, x.Name }).IsUnique();
            entity.HasOne(x => x.Category)
                .WithMany(x => x.DocumentRequirements)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ProviderDocument>(entity =>
        {
            entity.Property(x => x.OriginalFileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
            entity.HasIndex(x => new { x.ProviderProfileId, x.DocumentRequirementId }).IsUnique();
            entity.HasIndex(x => x.StorageKey).IsUnique();
            entity.HasOne(x => x.ProviderProfile)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.ProviderProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.DocumentRequirement)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.DocumentRequirementId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CustomerAddress>(entity =>
        {
            entity.Property(x => x.AddressLine1).HasMaxLength(250).IsRequired();
            entity.Property(x => x.AddressLine2).HasMaxLength(250);
            entity.Property(x => x.City).HasMaxLength(100).IsRequired();
            entity.Property(x => x.State).HasMaxLength(100).IsRequired();
            entity.Property(x => x.PostalCode).HasMaxLength(20).IsRequired();
            entity.HasOne(x => x.User)
                .WithMany(x => x.Addresses)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ServiceRequest>(entity =>
        {
            entity.Property(x => x.Notes).HasMaxLength(4000);
            entity.Property(x => x.Price).HasPrecision(18, 2);
            entity.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AssignedProvider)
                .WithMany()
                .HasForeignKey(x => x.AssignedProviderId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Service)
                .WithMany(x => x.ServiceRequests)
                .HasForeignKey(x => x.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Address)
                .WithMany()
                .HasForeignKey(x => x.AddressId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ServiceRequestStatusHistory>(entity =>
        {
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.HasOne(x => x.ServiceRequest)
                .WithMany(x => x.StatusHistory)
                .HasForeignKey(x => x.ServiceRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ChangedByUser)
                .WithMany()
                .HasForeignKey(x => x.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ServiceRequestAttachment>(entity =>
        {
            entity.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.FileUrl).HasMaxLength(1024).IsRequired();
            entity.Property(x => x.FileType).HasMaxLength(100).IsRequired();
            entity.HasOne(x => x.ServiceRequest)
                .WithMany(x => x.Attachments)
                .HasForeignKey(x => x.ServiceRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.UploadedByUser)
                .WithMany()
                .HasForeignKey(x => x.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Review>(entity =>
        {
            entity.HasIndex(x => x.ServiceRequestId).IsUnique();
            entity.Property(x => x.Comment).HasMaxLength(2000);
            entity.HasOne(x => x.ServiceRequest)
                .WithOne(x => x.Review)
                .HasForeignKey<Review>(x => x.ServiceRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Provider)
                .WithMany()
                .HasForeignKey(x => x.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
    }
}
