using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ScrumPoker.Tests.Integration;

// T054 - Correlation Id middleware behavior
public class CorrelationIdIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private const string Header = "X-Correlation-Id";
    public CorrelationIdIntegrationTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Generates_CorrelationId_When_Missing()
    {
        var resp = await _client.PostAsync("/api/sessions", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        resp.Headers.Contains(Header).Should().BeTrue();
        var value = resp.Headers.GetValues(Header).First();
        Guid.TryParse(value, out _).Should().BeTrue();
    }

    [Fact]
    public async Task Echoes_Provided_CorrelationId()
    {
        var provided = Guid.NewGuid().ToString();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/sessions")
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add(Header, provided);
        var resp = await _client.SendAsync(request);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        resp.Headers.GetValues(Header).First().Should().Be(provided);
    }
}
