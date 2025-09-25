using System.Net;
using System.Net.Http.Json;
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
        // Arrange
        var request = new { }; // no body expected per draft contract

        // Act
        var response = await _client.PostAsync("/api/sessions", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.TryGetProperty("code", out var codeProp).Should().BeTrue();
        codeProp.GetString().Should().MatchRegex("^[A-Z0-9]{6}$");
        json.TryGetProperty("deck", out var deckProp).Should().BeTrue();
        deckProp.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Array);
        deckProp.EnumerateArray().Should().NotBeEmpty();
        json.TryGetProperty("createdUtc", out var createdUtcProp).Should().BeTrue();
        DateTime.TryParse(createdUtcProp.GetString(), out _).Should().BeTrue();
    }
}
