using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ScrumPoker.Tests.Integration;

// T056 - ProblemDetails middleware maps validation & domain exceptions
public class ProblemDetailsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public ProblemDetailsIntegrationTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    private async Task<string> CreateSessionAsync()
    {
        var resp = await _client.PostAsync("/api/sessions", new StringContent("{}", Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("code").GetString()!;
    }

    [Fact]
    public async Task DuplicateParticipant_ShouldReturnProblemDetails400()
    {
        var code = await CreateSessionAsync();
        (await _client.PostAsJsonAsync($"/api/sessions/{code}/participants", new { displayName = "Alice" })).StatusCode.Should().Be(HttpStatusCode.OK);
        var dup = await _client.PostAsJsonAsync($"/api/sessions/{code}/participants", new { displayName = "alice" });
        dup.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        dup.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await dup.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("title").GetString().Should().Be("Validation failed");
        problem.GetProperty("status").GetInt32().Should().Be(400);
        problem.GetProperty("detail").GetString()!.ToLowerInvariant().Should().Contain("duplicate");
    }

    [Fact]
    public async Task EmptyWorkItemTitle_ShouldReturnProblemDetails400()
    {
        var code = await CreateSessionAsync();
        var resp = await _client.PostAsJsonAsync($"/api/sessions/{code}/work-items", new { title = "" });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await resp.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("title").GetString().Should().Be("Invalid argument");
        problem.GetProperty("status").GetInt32().Should().Be(400);
        problem.GetProperty("detail").GetString()!.ToLowerInvariant().Should().Contain("title required");
    }
}
