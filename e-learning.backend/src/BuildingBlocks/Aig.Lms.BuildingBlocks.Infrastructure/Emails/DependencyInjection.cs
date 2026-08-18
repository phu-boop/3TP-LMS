using Aig.Lms.BuildingBlocks.Application.Emails;
using FluentEmail.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using RazorLight;

namespace Aig.Lms.BuildingBlocks.Infrastructure.Emails;

public static class DependencyInjection
{
    public static IServiceCollection AddEmailFoundation(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.AddHttpClient();

        services.AddTransient<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<IEmailTokenProvider, OAuth2EmailTokenProvider>();
            services.AddTransient<IEmailTemplateEngine, RazorEmailTemplateEngine>();
            services.AddSingleton<IRazorLightEngine>(_ => new RazorLightEngineBuilder()
                .UseFileSystemProject(AppContext.BaseDirectory)
                .UseMemoryCachingProvider()
                .Build());
        services.AddTransient<IEmailOutboxRepository, EmailOutboxRepository>();

        // Facade — modules chỉ cần inject IEmailOutboxService và gọi EnqueueAsync()
        services.AddTransient<IEmailOutboxService, EmailOutboxService>();

        // Register FluentEmail
        services.AddFluentEmail("noreply@daihoc.io.vn")
                .AddRazorRenderer();

        return services;
    }
}
