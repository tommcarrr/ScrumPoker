using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ScrumPoker.Tests.Integration;

// T049 - Duplicate participant name rejected (case-insensitive)
public class DuplicateParticipantIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public DuplicateParticipantIntegrationTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    private async Task<string> CreateSessionAsync()
    {
        var resp = await _client.PostAsync("/api/sessions", new StringContent("{}", Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("code").GetString()!;
    }

    [Fact]
    public async Task DuplicateName_ShouldReturn400()
    {
        var code = await CreateSessionAsync();
        (await _client.PostAsJsonAsync($"/api/sessions/{code}/participants", new { displayName = "Alice" })).StatusCode.Should().Be(HttpStatusCode.OK);
        var dup = await _client.PostAsJsonAsync($"/api/sessions/{code}/participants", new { displayName = "alice" });
        dup.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
