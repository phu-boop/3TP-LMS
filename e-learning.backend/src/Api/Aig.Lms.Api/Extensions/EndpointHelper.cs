using System.Security.Claims;
using Aig.Lms.BuildingBlocks.Contracts.Tenancy;

namespace Aig.Lms.Api.Extensions;

/// <summary>
/// Shared helper methods for endpoint handlers.
/// Eliminates duplicated ResolveTenantId / GetUserId across endpoint files.
/// </summary>
public static class EndpointHelper
{
    /// <summary>
    /// Resolves the tenant ID from <see cref="ICurrentTenant"/> first, then falls back
    /// to the "tenant_id" claim. Returns null if neither is available.
    /// </summary>
    public static Guid? ResolveTenantId(ICurrentTenant currentTenant, ClaimsPrincipal user)
    {
        if (currentTenant.TenantId.HasValue)
            return currentTenant.TenantId.Value;

        var tenantClaim = user.FindFirst("tenant_id")?.Value;
        if (Guid.TryParse(tenantClaim, out var tenantIdFromClaim))
            return tenantIdFromClaim;

        return null;
    }

    /// <summary>
    /// Extracts the user ID from the "sub" or ClaimTypes.NameIdentifier claim.
    /// </summary>
    public static Guid? GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var userId) ? userId : null;
    }
}
