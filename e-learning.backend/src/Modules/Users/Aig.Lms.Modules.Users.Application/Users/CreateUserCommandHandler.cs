using Aig.Lms.BuildingBlocks.Application.Abstractions;
using Aig.Lms.Modules.Users.Domain;

namespace Aig.Lms.Modules.Users.Application.Users;

public sealed class CreateUserCommandHandler
{
    private readonly IUsersRepository _repository;
    private readonly IUserRoleAssignmentService _roleAssignment;
    private readonly IAuditLogService _auditLog;

    public CreateUserCommandHandler(
        IUsersRepository repository,
        IUserRoleAssignmentService roleAssignment,
        IAuditLogService auditLog)
    {
        _repository     = repository;
        _roleAssignment = roleAssignment;
        _auditLog       = auditLog;
    }

    public async Task<CreateUserResult> HandleAsync(CreateUserCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Username))
            throw new ArgumentException("Username là bắt buộc.");

        if (string.IsNullOrWhiteSpace(command.Password))
            throw new ArgumentException("Mật khẩu là bắt buộc.");

        if (string.IsNullOrWhiteSpace(command.FullName))
            throw new ArgumentException("Họ và tên là bắt buộc.");

        if (string.IsNullOrWhiteSpace(command.AccountType))
            throw new ArgumentException("Vai trò là bắt buộc.");

        var exists = await _repository.ExistsAsync(command.SchoolId, command.Username, ct);
        if (exists)
        {
            var message = command.SchoolId.HasValue
                ? $"Username '{command.Username}' đã tồn tại trong trường học này."
                : $"Username '{command.Username}' đã tồn tại.";
            throw new InvalidOperationException(message);
        }

        if (!string.IsNullOrWhiteSpace(command.Email))
        {
            var emailExists = await _repository.EmailExistsAsync(command.Email, ct);
            if (emailExists)
                throw new InvalidOperationException($"Địa chỉ email đã được sử dụng. Vui lòng sử dụng địa chỉ email khác.");
        }

        var user = new User
        {
            Id          = Guid.NewGuid(),
            SchoolId    = command.SchoolId,
            Username    = command.Username,
            Email       = command.Email,
            FullName    = command.FullName,
            AvatarUrl   = command.AvatarUrl,
            AccountType = command.AccountType,
            Status      = "ACTIVE"
        };

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(command.Password);
        await _repository.CreateAsync(user, passwordHash, ct);

        // Assign initial role if explicitly provided
        if (command.RoleId.HasValue && command.TenantIds is { Count: > 0 })
        {
            foreach (var tenantId in command.TenantIds.Distinct())
            {
                await _roleAssignment.AssignRoleAsync(user.Id, command.RoleId.Value, tenantId, ct);
            }
        }

        // Auto-inherit tenants from school's active contracts
        if (user.SchoolId.HasValue)
        {
            await _roleAssignment.InheritSchoolTenantsAsync(
                user.Id, user.SchoolId.Value, user.AccountType, ct);
        }

        // Audit log
        await _auditLog.LogAsync(new AuditLogEntry(
            Action:      "USER_CREATED",
            EntityType:  "user_account",
            EntityId:    user.Id,
            TenantId:    command.TenantIds?.FirstOrDefault(),
            SchoolId:    command.SchoolId,
            ActorUserId: command.ActorUserId,
            IpAddress:   command.IpAddress,
            UserAgent:   command.UserAgent), ct);

        return new CreateUserResult(user.Id, user.Username, user.FullName, user.Email, user.AvatarUrl, user.AccountType);
    }
}
