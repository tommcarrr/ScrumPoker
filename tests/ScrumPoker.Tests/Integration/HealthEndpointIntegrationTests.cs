using System.Text.Json;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ScrumPoker.Tests.Integration;

// T055 - /health endpoint basic check
public class HealthEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public HealthEndpointIntegrationTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Health_Returns_Ok_Payload()
    {
        var json = await _client.GetFromJsonAsync<JsonElement>("/health");
        json.GetProperty("status").GetString().Should().Be("ok");
        // tableStorage field may be null depending on config; ensure it exists in projection
        json.TryGetProperty("tableStorage", out _).Should().BeTrue();
    }
}
