using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ScrumPoker.Tests.Integration;

public class MudBlazorAssetsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public MudBlazorAssetsIntegrationTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Theory]
    [InlineData("/_content/MudBlazor/MudBlazor.min.css", "text/css")]
    [InlineData("/_content/MudBlazor/MudBlazor.min.js", "javascript")] // accept text/javascript or application/javascript
    public async Task MudBlazor_Static_Asset_Serves_Successfully(string path, string expectedToken)
    {
        using var client = _factory.CreateClient();
        using var resp = await client.GetAsync(path);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var mediaType = resp.Content.Headers.ContentType!.MediaType!;
        mediaType.Should().Contain(expectedToken); // flexible across hosting / framework variations
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Root_Page_References_MudBlazor_Assets()
    {
        using var client = _factory.CreateClient();
        var html = await client.GetStringAsync("/");
        html.Should().Contain("_content/MudBlazor/MudBlazor.min.css");
        html.Should().Contain("_content/MudBlazor/MudBlazor.min.js");
    }
}
