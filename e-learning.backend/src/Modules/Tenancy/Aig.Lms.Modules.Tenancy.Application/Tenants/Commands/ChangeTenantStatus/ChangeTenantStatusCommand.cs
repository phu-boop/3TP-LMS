using Aig.Lms.Modules.Tenancy.Domain.Entities;
using Aig.Lms.Modules.Tenancy.Domain.Repositories;

namespace Aig.Lms.Modules.Tenancy.Application.Tenants.Commands.ChangeTenantStatus;

public sealed record ChangeTenantStatusCommand(Guid Id, string Status);

public sealed class ChangeTenantStatusHandler
{
    private static readonly IReadOnlySet<string> AllowedStatuses = new HashSet<string>(StringComparer.Ordinal)
    {
        TenantStatus.Active,
        TenantStatus.Inactive,
        TenantStatus.Locked,
    };

    private readonly ITenantRepository _tenantRepository;

    public ChangeTenantStatusHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<bool> HandleAsync(ChangeTenantStatusCommand command, CancellationToken ct = default)
    {
        if (command.Id == Guid.Empty)
            throw new InvalidOperationException("Id is required.");

        if (string.IsNullOrWhiteSpace(command.Status))
            throw new InvalidOperationException("Status is required.");

        var normalizedStatus = command.Status.Trim().ToUpperInvariant();
        if (!AllowedStatuses.Contains(normalizedStatus))
            throw new InvalidOperationException("Status must be ACTIVE, INACTIVE, or LOCKED.");

        var tenant = await _tenantRepository.GetByIdAsync(command.Id, ct);
        if (tenant is null)
            return false;

        // Chỉ kiểm tra đối với trường hợp ACTIVE -> INACTIVE
        if (normalizedStatus == TenantStatus.Inactive && tenant.Status == TenantStatus.Active)
        {
            var IsActiveContract = await _tenantRepository.IsActiveContract(command.Id, ct);
            if(IsActiveContract)
                throw new InvalidOperationException("Không thể ngừng kích hoạt tenant đang có hợp đồng hoạt động.");
        }

        tenant.SetStatus(normalizedStatus);
        await _tenantRepository.UpdateAsync(tenant, ct);
        return true;
    }
}