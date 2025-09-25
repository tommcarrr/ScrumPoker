using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ScrumPoker.Tests.Contract;

public class WorkItemsContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public WorkItemsContractTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    private async Task<string> CreateSessionAsync()
    {
        var resp = await _client.PostAsync("/api/sessions", new StringContent("{}", Encoding.UTF8, "application/json"));
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("code").GetString()!;
    }

    [Fact]
    public async Task Post_WorkItems_AddsWorkItem_Returns200AndSessionShape()
    {
        var code = await CreateSessionAsync();
        var payload = new { title = "Login page" };
        var resp = await _client.PostAsync($"/api/sessions/{code}/work-items", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("workItems").EnumerateArray().Any(w => w.GetProperty("title").GetString()=="Login page").Should().BeTrue();
    }

    [Fact]
    public async Task Post_WorkItems_InvalidTitle_Empty_Returns400()
    {
        var code = await CreateSessionAsync();
        var payload = new { title = "" };
        var resp = await _client.PostAsync($"/api/sessions/{code}/work-items", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
