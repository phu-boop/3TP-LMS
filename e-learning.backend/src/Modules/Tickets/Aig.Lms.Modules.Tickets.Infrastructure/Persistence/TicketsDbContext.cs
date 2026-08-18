using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Aig.Lms.Modules.Tickets.Infrastructure.Persistence;

public sealed class TicketsDbContext
{
    private readonly string _connectionString;

    public TicketsDbContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
    }

    public NpgsqlConnection CreateConnection() => new(_connectionString);
}
