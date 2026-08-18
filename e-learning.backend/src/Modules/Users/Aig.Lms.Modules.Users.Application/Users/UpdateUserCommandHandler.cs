using Aig.Lms.BuildingBlocks.Application.Abstractions;

namespace Aig.Lms.Modules.Users.Application.Users;

public sealed class UpdateUserCommandHandler
{
    private readonly IUsersRepository _repository;
    private readonly IUserRoleAssignmentService _roleAssignment;
    private readonly IAuditLogService _auditLog;

    public UpdateUserCommandHandler(IUsersRepository repository, IUserRoleAssignmentService roleAssignment, IAuditLogService auditLog)
    {
        _repository     = repository;
        _roleAssignment = roleAssignment;
        _auditLog       = auditLog;
    }

    public async Task<UpdateUserResult?> HandleAsync(UpdateUserCommand command, CancellationToken ct = default)
    {
        var existingUser = await _repository.GetByIdAsync(command.UserId, ct);
        if (existingUser is null)
            return null;

        var shouldClearSchool = string.Equals(command.AccountType, "LMS_ADMIN", StringComparison.OrdinalIgnoreCase)
            || string.Equals(command.AccountType, "TENANT_ADMIN", StringComparison.OrdinalIgnoreCase)
            || (command.RoleId.HasValue && await _repository.IsRoleCodeAsync(command.RoleId.Value, "LMS_ADMIN", ct))
            || (command.RoleId.HasValue && await _repository.IsRoleCodeAsync(command.RoleId.Value, "TENANT_ADMIN", ct));

        var repositoryCommand = shouldClearSchool
            ? command with { SchoolId = null, ClearSchoolId = true }
            : command;

        var result = await _repository.UpdateAsync(repositoryCommand, ct);
        if (result is null)
            return null;

        await _repository.RemoveAllUserTenantsAsync(command.UserId, ct);

        if (command.RoleId.HasValue && command.TenantIds is { Count: > 0 })
        {
            foreach (var tenantId in command.TenantIds.Distinct())
            {
                await _roleAssignment.AssignRoleAsync(command.UserId, command.RoleId.Value, tenantId, ct);
            }
        }

        if (command.SchoolId.HasValue)
        {
            await _roleAssignment.InheritSchoolTenantsAsync(command.UserId, command.SchoolId.Value, command.AccountType, ct);
        }

        await _auditLog.LogAsync(new AuditLogEntry(
            Action:      "USER_UPDATED",
            EntityType:  "user_account",
            EntityId:    command.UserId,
            ActorUserId: command.ActorUserId,
            IpAddress:   command.IpAddress,
            UserAgent:   command.UserAgent), ct);

        return result;
    }
}
