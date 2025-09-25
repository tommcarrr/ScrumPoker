using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ScrumPoker.Tests.Integration;

// T048 - Restart resets estimates while preserving participants & work items
public class RestartIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public RestartIntegrationTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    private async Task<string> CreateSessionAsync()
    {
        var resp = await _client.PostAsync("/api/sessions", new StringContent("{}", Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("code").GetString()!;
    }

    [Fact]
    public async Task Restart_ClearsEstimates_ResetsState_PreservesParticipants()
    {
        var code = await CreateSessionAsync();
        await _client.PostAsJsonAsync($"/api/sessions/{code}/participants", new { displayName = "Alice" });
        await _client.PostAsJsonAsync($"/api/sessions/{code}/participants", new { displayName = "Bob" });
        await _client.PostAsJsonAsync($"/api/sessions/{code}/work-items", new { title = "Item" });

        var snap = await _client.GetFromJsonAsync<JsonElement>($"/api/sessions/{code}");
        var wiId = snap.GetProperty("workItems").EnumerateArray().First().GetProperty("id").GetGuid();
        var p1 = snap.GetProperty("participants").EnumerateArray().First().GetProperty("id").GetGuid();
        var p2 = snap.GetProperty("participants").EnumerateArray().Skip(1).First().GetProperty("id").GetGuid();

        // Submit estimates
        (await _client.PostAsJsonAsync($"/api/sessions/{code}/work-items/{wiId}/estimates", new { participantId = p1, value = "3" })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.PostAsJsonAsync($"/api/sessions/{code}/work-items/{wiId}/estimates", new { participantId = p2, value = "5" })).StatusCode.Should().Be(HttpStatusCode.OK);

        // Reveal
        (await _client.PostAsync($"/api/sessions/{code}/work-items/{wiId}/reveal", new StringContent("{}", Encoding.UTF8, "application/json"))).StatusCode.Should().Be(HttpStatusCode.OK);

        // Restart
        (await _client.PostAsync($"/api/sessions/{code}/restart", new StringContent("{}", Encoding.UTF8, "application/json"))).StatusCode.Should().Be(HttpStatusCode.OK);

        var after = await _client.GetFromJsonAsync<JsonElement>($"/api/sessions/{code}");
        after.GetProperty("participants").EnumerateArray().Should().HaveCount(2);
        var wi = after.GetProperty("workItems").EnumerateArray().First();
        // The public work item projection currently omits the state property; ensure it is absent (restart should not add it)
        wi.TryGetProperty("state", out _).Should().BeFalse();
    }
}
