using Aig.Lms.Modules.Tickets.Application.Tickets.Commands;
using Aig.Lms.Modules.Tickets.Application.Tickets.Commands.ConfirmTicketUpload;
using Aig.Lms.Modules.Tickets.Application.Tickets.Commands.CreateTicket;
using Aig.Lms.Modules.Tickets.Application.Tickets.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Aig.Lms.Modules.Tickets.Application;

public static class TicketsApplicationModule
{
    public static IServiceCollection AddTicketsApplication(this IServiceCollection services)
    {
        services.AddScoped<GetMyTicketsHandler>();
        services.AddScoped<GetTicketDetailHandler>();
        services.AddScoped<GetTicketsQueryHandler>();
        services.AddScoped<GetTicketByIdHandler>();
        services.AddScoped<UpdateTicketStatusCommandHandler>();
        services.AddScoped<CreateTicketCommandHandler>();
        services.AddScoped<ConfirmTicketUploadCommandHandler>();

        return services;
    }
}
