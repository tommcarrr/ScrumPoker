using System;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ScrumPoker.Tests.Contract;

public class SessionsContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SessionsContractTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_Sessions_Creates_Session_201_And_ContractShape()
    {
        // Arrange - per OpenAPI draft no body required (could extend later)
        var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/sessions", content);

        // Assert (will currently fail - endpoint not implemented yet)
        response.StatusCode.Should().Be(HttpStatusCode.Created, "contract requires 201 on create new session");
        response.Headers.Location.Should().NotBeNull();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.TryGetProperty("code", out var codeProp).Should().BeTrue();
        codeProp.GetString().Should().MatchRegex("^[A-Z0-9]{6}$");
        json.TryGetProperty("deck", out var deckProp).Should().BeTrue();
        deckProp.ValueKind.Should().Be(JsonValueKind.Array);
        deckProp.EnumerateArray().Should().NotBeEmpty();
        json.TryGetProperty("createdUtc", out var createdUtcProp).Should().BeTrue();
        DateTime.TryParse(createdUtcProp.GetString(), out _).Should().BeTrue();
    }
}
