using System;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ScrumPoker.Tests.Contract;

public class GetSessionContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GetSessionContractTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_Unknown_Session_Returns_404()
    {
        var resp = await _client.GetAsync("/api/sessions/NOPE99");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_Then_Get_Session_Returns_200_And_Shape()
    {
        // create
        var createResp = await _client.PostAsync("/api/sessions", new StringContent("{}", Encoding.UTF8, "application/json"));
        createResp.StatusCode.Should().Be(HttpStatusCode.Created, "precondition for GET contract");
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var code = created.GetProperty("code").GetString();

        // get
        var getResp = await _client.GetAsync($"/api/sessions/{code}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await getResp.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("code").GetString().Should().Be(code);
        json.GetProperty("deck").ValueKind.Should().Be(JsonValueKind.Array);
        json.GetProperty("createdUtc").GetString().Should().NotBeNull();
    }
}
