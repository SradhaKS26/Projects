namespace ServiceManagement.Domain.Constants;

public static class Roles
{
    public const string Administrator = "Administrator";
    public const string CommonUser = "CommonUser";
    public const string ServiceProvider = "ServiceProvider";

    public static readonly string[] All = [Administrator, CommonUser, ServiceProvider];
}
