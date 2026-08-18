namespace Aig.Lms.Modules.Users.Application.Users;

public sealed record UpdateUserCommand(
    Guid UserId,
    string FullName,
    string? Email,
    string Status,
    string AccountType,
    Guid? SchoolId = null,
    Guid? RoleId = null,
    IReadOnlyList<Guid>? TenantIds = null,
    string? AvatarUrl = null,
    Guid? ActorUserId = null,
    string? IpAddress = null,
    string? UserAgent = null,
    bool ClearSchoolId = false);

public sealed record UpdateUserResult(
    Guid UserId,
    string Username,
    string FullName,
    string? Email,
    string Status,
    string? AccountType = null,
    Guid? SchoolId = null,
    Guid? TenantId = null);
