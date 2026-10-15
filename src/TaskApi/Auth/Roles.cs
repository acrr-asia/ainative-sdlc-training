namespace TaskApi.Auth;

public static class Roles
{
    public const string Member = "Member";
    public const string Admin = "Admin";

    public static readonly string[] All = [Member, Admin];
}

public static class Policies
{
    public const string AdminOnly = nameof(AdminOnly);
    public const string TaskOwnerOrAdmin = nameof(TaskOwnerOrAdmin);
}
