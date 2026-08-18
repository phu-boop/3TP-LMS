namespace Aig.Lms.Modules.Tenancy.Application.Tenants.Dtos;

public sealed record TenantDto(
    Guid Id,
    string Code,
    string Name,
    string Subdomain,
    string? LogoUrl,
    string? AvatarUrl,
    string? Color,
    string? Description,
    string? WatermarkSettings,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);
