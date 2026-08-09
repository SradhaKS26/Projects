namespace ServiceManagement.Domain.Constants;

/// <summary>
/// Central permission catalog. Authorization checks should reference these constants,
/// not hardcode string literals throughout the codebase.
/// </summary>
public static class Permissions
{
    public const string ManageUsers = "ManageUsers";
    public const string ManageServices = "ManageServices";
    public const string ManageProviders = "ManageProviders";
    public const string ManageRequests = "ManageRequests";
    public const string ManagePricing = "ManagePricing";
    public const string AssignProviders = "AssignProviders";
    public const string ViewReports = "ViewReports";
    public const string ManageRoles = "ManageRoles";
    public const string ManagePermissions = "ManagePermissions";
    public const string ManagePlatformConfig = "ManagePlatformConfig";

    public static readonly string[] All =
    [
        ManageUsers,
        ManageServices,
        ManageProviders,
        ManageRequests,
        ManagePricing,
        AssignProviders,
        ViewReports,
        ManageRoles,
        ManagePermissions,
        ManagePlatformConfig
    ];

    public static readonly string[] AdministratorDefaults = All;

    public static readonly string[] ServiceProviderDefaults =
    [
        ManageRequests
    ];

    public static readonly string[] CommonUserDefaults = [];
}
