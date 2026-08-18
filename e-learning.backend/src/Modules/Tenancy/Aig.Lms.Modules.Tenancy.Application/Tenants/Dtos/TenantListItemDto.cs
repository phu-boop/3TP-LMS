namespace Aig.Lms.Modules.Tenancy.Application.Tenants.Dtos;

public sealed record TenantListItemDto(
    Guid Id,
    string Code,
    string Name,
    string Subdomain,
    string? LogoUrl,
    string? Color,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);
