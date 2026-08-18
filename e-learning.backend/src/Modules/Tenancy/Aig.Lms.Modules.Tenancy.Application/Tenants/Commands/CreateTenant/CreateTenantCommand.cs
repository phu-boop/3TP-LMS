namespace Aig.Lms.Modules.Tenancy.Application.Tenants.Commands.CreateTenant;

public sealed record CreateTenantCommand(
    string Name,
    string Code,
    string Subdomain,
    string? LogoUrl,
    string? AvatarUrl,
    string? Description,
    string? WatermarkSettings,
    string? Color);

public sealed record CreateTenantResult(
    Guid TenantId,
    string Code,
    string Subdomain,
    string Status,
    string? Color,
    string? LogoUrl);
