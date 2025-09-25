using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ScrumPoker.Tests.Integration;

// T050 - Invalid deck value rejected on submit
public class InvalidEstimateIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public InvalidEstimateIntegrationTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    private async Task<(string code, Guid workItemId, Guid participantId)> SetupAsync()
    {
        var create = await _client.PostAsync("/api/sessions", new StringContent("{}", Encoding.UTF8, "application/json"));
        create.EnsureSuccessStatusCode();
        var json = await create.Content.ReadFromJsonAsync<JsonElement>();
        var code = json.GetProperty("code").GetString()!;
        await _client.PostAsJsonAsync($"/api/sessions/{code}/participants", new { displayName = "Alice" });
        await _client.PostAsJsonAsync($"/api/sessions/{code}/work-items", new { title = "Item" });
        var snap = await _client.GetFromJsonAsync<JsonElement>($"/api/sessions/{code}");
        var pid = snap.GetProperty("participants").EnumerateArray().First().GetProperty("id").GetGuid();
        var wid = snap.GetProperty("workItems").EnumerateArray().First().GetProperty("id").GetGuid();
        return (code, wid, pid);
    }

    [Fact]
    public async Task InvalidEstimate_ShouldReturn400()
    {
        var (code, wid, pid) = await SetupAsync();
        var resp = await _client.PostAsJsonAsync($"/api/sessions/{code}/work-items/{wid}/estimates", new { participantId = pid, value = "999" });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
