using Aig.Lms.BuildingBlocks.Application.Emails;
using FluentEmail.Core;
using Newtonsoft.Json;
using System.Dynamic;
using RazorLight;
using Microsoft.Extensions.Logging;

namespace Aig.Lms.BuildingBlocks.Infrastructure.Emails;

public class RazorEmailTemplateEngine : IEmailTemplateEngine
{
    private readonly IFluentEmailFactory _fluentEmailFactory;
    private readonly IRazorLightEngine _razorEngine;
    private readonly ILogger<RazorEmailTemplateEngine> _logger;

    public RazorEmailTemplateEngine(IFluentEmailFactory fluentEmailFactory, IRazorLightEngine razorEngine, ILogger<RazorEmailTemplateEngine> logger)
    {
        _fluentEmailFactory = fluentEmailFactory;
        _razorEngine = razorEngine;
        _logger = logger;
    }

    public async Task<string> RenderAsync(string templateName, string payloadJson)
    {
        var model = string.IsNullOrWhiteSpace(payloadJson) 
            ? new ExpandoObject() 
            : JsonConvert.DeserializeObject<ExpandoObject>(payloadJson);
            
        try
        {
            var templatePath = $"Templates/{templateName}.cshtml";
            var result = await _razorEngine.CompileRenderAsync(templatePath, model);
            return result ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render email template {Template}", templateName);
            throw;
        }
    }
}
