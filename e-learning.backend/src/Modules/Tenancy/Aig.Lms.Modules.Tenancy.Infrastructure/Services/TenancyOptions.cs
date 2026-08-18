namespace Aig.Lms.Modules.Tenancy.Infrastructure.Services;

public sealed class TenancyOptions
{
    public const string SectionName = "Tenancy";

    public string AdminSubdomain { get; set; } = string.Empty;
    public string[] AdminDomains { get; set; } = [];
    public string[] TenantBaseDomains { get; set; } = [];
    public string[] LocalHosts { get; set; } = ["localhost", "127.0.0.1", "::1"];
}