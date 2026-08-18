using System.Dynamic;
using Aig.Lms.BuildingBlocks.Infrastructure.Emails;
using FluentAssertions;
using FluentEmail.Core;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RazorLight;
using Xunit;

namespace Aig.Lms.UnitTests.Emails;

public class RazorEmailTemplateEngineTests
{
    private readonly IFluentEmailFactory _fluentEmailFactory = Substitute.For<IFluentEmailFactory>();
    private readonly IRazorLightEngine _razorEngine = Substitute.For<IRazorLightEngine>();
    private readonly ILogger<RazorEmailTemplateEngine> _logger = Substitute.For<ILogger<RazorEmailTemplateEngine>>();
    private readonly RazorEmailTemplateEngine _engine;

    public RazorEmailTemplateEngineTests()
    {
        _engine = new RazorEmailTemplateEngine(_fluentEmailFactory, _razorEngine, _logger);
    }

    [Fact]
    public async Task RenderAsync_ValidTemplateAndPayload_CallsCompileRenderAsync()
    {
        // Arrange
        var templateName = "WelcomeUser";
        var payloadJson = "{\"FullName\":\"Nguyen Van A\"}";
        var expectedHtml = "<h2>Chào mừng Nguyen Van A đến với AIG LMS!</h2>";

        _razorEngine.CompileRenderAsync(
            Arg.Is<string>(path => path == "Templates/WelcomeUser.cshtml"),
            Arg.Any<ExpandoObject>())
            .Returns(expectedHtml);

        // Act
        var result = await _engine.RenderAsync(templateName, payloadJson);

        // Assert
        result.Should().Be(expectedHtml);
        await _razorEngine.Received(1).CompileRenderAsync(
            "Templates/WelcomeUser.cshtml",
            Arg.Any<ExpandoObject>());
    }

    [Fact]
    public async Task RenderAsync_EmptyPayloadJson_PassesEmptyExpandoObject()
    {
        // Arrange
        var templateName = "WelcomeUser";
        var payloadJson = "";
        var expectedHtml = "<h2>Welcome</h2>";

        _razorEngine.CompileRenderAsync("Templates/WelcomeUser.cshtml", Arg.Any<ExpandoObject>())
            .Returns(expectedHtml);

        // Act
        var result = await _engine.RenderAsync(templateName, payloadJson);

        // Assert
        result.Should().Be(expectedHtml);
    }
}
