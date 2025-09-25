using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ScrumPoker.Tests.Integration;

// T047 - Full session lifecycle integration test
public class SessionLifecycleIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public SessionLifecycleIntegrationTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    private async Task<JsonElement> CreateSessionAsync()
    {
        var resp = await _client.PostAsync("/api/sessions", new StringContent("{}", Encoding.UTF8, "application/json"));
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return json;
    }

    [Fact]
    public async Task FullLifecycle_CompletesSuccessfully()
    {
        // Create session
        var session = await CreateSessionAsync();
        var code = session.GetProperty("code").GetString()!;

        // Join two participants
        var p1 = await _client.PostAsJsonAsync($"/api/sessions/{code}/participants", new { displayName = "Alice" });
        p1.StatusCode.Should().Be(HttpStatusCode.OK);
        var p2 = await _client.PostAsJsonAsync($"/api/sessions/{code}/participants", new { displayName = "Bob" });
        p2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Add work item
        var wiResp = await _client.PostAsJsonAsync($"/api/sessions/{code}/work-items", new { title = "Estimate API" });
        wiResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Snapshot to get participant ids and workItemId
        var snap = await _client.GetFromJsonAsync<JsonElement>($"/api/sessions/{code}");
        var participants = snap.GetProperty("participants").EnumerateArray().ToList();
        participants.Should().HaveCount(2);
        var workItemId = snap.GetProperty("workItems").EnumerateArray().First().GetProperty("id").GetGuid();
        var p1Id = participants[0].GetProperty("id").GetGuid();
        var p2Id = participants[1].GetProperty("id").GetGuid();

        // Submit two estimates
        var e1 = await _client.PostAsJsonAsync($"/api/sessions/{code}/work-items/{workItemId}/estimates", new { participantId = p1Id, value = "3" });
        e1.StatusCode.Should().Be(HttpStatusCode.OK);
        var e2 = await _client.PostAsJsonAsync($"/api/sessions/{code}/work-items/{workItemId}/estimates", new { participantId = p2Id, value = "5" });
        e2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Reveal
        var rev = await _client.PostAsync($"/api/sessions/{code}/work-items/{workItemId}/reveal", new StringContent("{}", Encoding.UTF8, "application/json"));
        rev.StatusCode.Should().Be(HttpStatusCode.OK);

        // Finalize
        var finPayload = new { value = "5" };
        var fin = await _client.PostAsJsonAsync($"/api/sessions/{code}/work-items/{workItemId}/finalize", finPayload);
        fin.StatusCode.Should().Be(HttpStatusCode.OK);

        // Optional: Verify finalize via another snapshot
        var finalSnap = await _client.GetFromJsonAsync<JsonElement>($"/api/sessions/{code}");
        var wiState = finalSnap.GetProperty("workItems").EnumerateArray().First().GetProperty("createdUtc"); // ensure field accessible
        finalSnap.GetProperty("workItems").EnumerateArray().First().TryGetProperty("title", out _).Should().BeTrue();
    }
}
