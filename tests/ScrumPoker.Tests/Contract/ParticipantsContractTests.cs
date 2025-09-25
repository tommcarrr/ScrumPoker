using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ScrumPoker.Tests.Contract;

public class ParticipantsContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ParticipantsContractTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> CreateSessionAsync()
    {
        var resp = await _client.PostAsync("/api/sessions", new StringContent("{}", Encoding.UTF8, "application/json"));
        resp.StatusCode.Should().Be(HttpStatusCode.Created, "session creation is a prerequisite for participant tests");
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("code").GetString()!;
    }

    [Fact]
    public async Task Post_Participants_AddsParticipant_Returns200AndParticipantShape()
    {
        var code = await CreateSessionAsync();
        var payload = new { displayName = "Alice" };
        var resp = await _client.PostAsync($"/api/sessions/{code}/participants", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("participants").EnumerateArray().Any(p => p.GetProperty("displayName").GetString()=="Alice").Should().BeTrue();
    }

    [Fact]
    public async Task Post_Participants_DuplicateName_IsRejected()
    {
        var code = await CreateSessionAsync();
        var payload = new { displayName = "Bob" };
        var first = await _client.PostAsync($"/api/sessions/{code}/participants", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var second = await _client.PostAsync($"/api/sessions/{code}/participants", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest, "duplicate participant names must be rejected");
    }
}
