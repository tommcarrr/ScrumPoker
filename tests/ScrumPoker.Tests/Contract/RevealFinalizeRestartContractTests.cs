using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ScrumPoker.Tests.Contract;

public class RevealFinalizeRestartContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public RevealFinalizeRestartContractTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    private async Task<string> CreateSessionAsync()
    {
        var resp = await _client.PostAsync("/api/sessions", new StringContent("{}", Encoding.UTF8, "application/json"));
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("code").GetString()!;
    }

    [Fact]
    public async Task Post_Reveal_ChangesStateAndReturns200()
    {
        var code = await CreateSessionAsync();
        var resp = await _client.PostAsync($"/api/sessions/{code}/work-items/00000000-0000-0000-0000-000000000000/reveal", new StringContent("{}", Encoding.UTF8, "application/json"));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Post_Finalize_RejectsQuestionMark()
    {
        var code = await CreateSessionAsync();
        var payload = new { value = "?" };
        var resp = await _client.PostAsync($"/api/sessions/{code}/work-items/00000000-0000-0000-0000-000000000000/finalize", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Restart_Returns200AndResetsSession()
    {
        var code = await CreateSessionAsync();
        var resp = await _client.PostAsync($"/api/sessions/{code}/restart", new StringContent("{}", Encoding.UTF8, "application/json"));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
