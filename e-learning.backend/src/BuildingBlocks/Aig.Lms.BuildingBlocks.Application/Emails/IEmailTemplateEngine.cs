namespace Aig.Lms.BuildingBlocks.Application.Emails;

public interface IEmailTemplateEngine
{
    Task<string> RenderAsync(string templateName, string payloadJson);
}
