using Aig.Lms.Modules.Tickets.Application;
using Aig.Lms.Modules.Tickets.Application.Abstractions;
using Aig.Lms.Modules.Tickets.Domain.Repositories;
using Aig.Lms.Modules.Tickets.Infrastructure.Persistence;
using Aig.Lms.Modules.Tickets.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aig.Lms.Modules.Tickets.Infrastructure;

public static class TicketsModule
{
    public static IServiceCollection AddTicketsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Application services
        services.AddTicketsApplication();

        // Persistence
        services.AddSingleton<TicketsDbContext>();

        // Repositories
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<ITicketReadRepository, TicketReadRepository>();

        // Application handlers
        services.AddTicketsApplication();

        return services;
    }
}
