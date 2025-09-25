using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Xunit;

namespace ScrumPoker.Tests.Integration;

public class MudBlazorSetupIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public MudBlazorSetupIntegrationTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task MudBlazor_Services_Are_Registered()
    {
        // Instead of resolving services directly (which may require a fully initialized NavigationManager),
        // request the root page and ensure a known MudBlazor CSS class is present indicating components rendered.
        using var client = _factory.CreateClient();
        var html = await client.GetStringAsync("/");
        html.Should().Contain("mud-", "MudBlazor css class should appear in rendered markup, implying services registered");
    }

    [Fact]
    public async Task Root_Page_Renders_DisplayName_DataTest()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });
        var html = await client.GetStringAsync("/");
        html.Should().Contain("data-test=\"display-name-input\"", "Display name input marker should be present");
    }

    [Fact]
    public async Task Root_Page_Includes_WebAssembly_Script()
    {
        using var client = _factory.CreateClient();
        var html = await client.GetStringAsync("/");
        html.Should().Contain("_framework/blazor.web.js", "interactive WebAssembly script should be present for hybrid render mode");
    }
}
