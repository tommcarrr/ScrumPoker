using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ScrumPoker.Tests.Contract;

public class EstimatesContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public EstimatesContractTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    private async Task<(string code, string workItemId, string participantId)> ArrangeSessionWithWorkItemAsync()
    {
        // create session
        var createResp = await _client.PostAsync("/api/sessions", new StringContent("{}", Encoding.UTF8, "application/json"));
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var session = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var code = session.GetProperty("code").GetString()!;
        // add participant to get participantId conceptually (will come from returned snapshot later when implemented)
        var joinPayload = new { displayName = "Eve" };
        await _client.PostAsync($"/api/sessions/{code}/participants", new StringContent(JsonSerializer.Serialize(joinPayload), Encoding.UTF8, "application/json"));
        // add work item
        var wiPayload = new { title = "API endpoint" };
        await _client.PostAsync($"/api/sessions/{code}/work-items", new StringContent(JsonSerializer.Serialize(wiPayload), Encoding.UTF8, "application/json"));
        // We cannot extract real ids yet (not implemented) so we use placeholders to drive failure; tests will refine once IDs exist.
        return (code, "00000000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-000000000000");
    }

    [Fact]
    public async Task Post_Estimates_AcceptsValidDeckValue_Returns200()
    {
        var (code, workItemId, participantId) = await ArrangeSessionWithWorkItemAsync();
        var payload = new { participantId, value = "3" };
        var resp = await _client.PostAsync($"/api/sessions/{code}/work-items/{workItemId}/estimates", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Post_Estimates_InvalidDeckValue_Returns400()
    {
        var (code, workItemId, participantId) = await ArrangeSessionWithWorkItemAsync();
        var payload = new { participantId, value = "999" }; // invalid
        var resp = await _client.PostAsync($"/api/sessions/{code}/work-items/{workItemId}/estimates", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
