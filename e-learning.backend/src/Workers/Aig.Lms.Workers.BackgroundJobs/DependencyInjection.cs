using Microsoft.Extensions.DependencyInjection;

namespace Aig.Lms.Workers.BackgroundJobs;

public static class DependencyInjection
{
    public static IServiceCollection AddEmailBackgroundJobs(this IServiceCollection services)
    {
        services.AddHostedService<EmailOutboxProcessorJob>();
        return services;
    }
}
