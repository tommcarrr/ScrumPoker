using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ScrumPoker.Client.Services;
using MudBlazor.Services;

namespace ScrumPoker.Client.Tests.Components;

// Tests: T051, T052, T053
public class SessionBoardComponentTests : TestContext
{
    public SessionBoardComponentTests()
    {
        // Ignore MudBlazor JS calls that aren't needed for logic assertions in tests
        JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true);
        JSInterop.SetupVoid("mudKeyInterceptor.disconnect", _ => true);
        // MudInput triggers this on first render; mock to avoid Bunit unhandled invocation exceptions.
        JSInterop.SetupVoid("mudElementRef.addOnBlurEvent", _ => true);
    }
    private class DummyNav : Microsoft.AspNetCore.Components.NavigationManager
    {
        public DummyNav() { Initialize("http://localhost/", "http://localhost/"); }
        protected override void NavigateToCore(string uri, bool forceLoad) { /* no-op */ }
    }

    private SessionState ConfigureServicesAndGetState()
    {
        Services.AddSingleton<Microsoft.AspNetCore.Components.NavigationManager, DummyNav>();
        Services.AddSingleton<SessionHubClient>();
        Services.AddSingleton<SessionState>();
        // Add MudBlazor + localization + HttpClient dependencies used by component
        Services.AddLocalization();
        Services.AddMudServices();
        Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri("http://localhost") });
        return Services.GetRequiredService<SessionState>();
    }

    private static SessionHubClient.SessionSnapshotDto CreateSnapshot(
        string code,
        IList<string>? deck = null,
        IList<SessionHubClient.ParticipantDto>? participants = null,
        IList<SessionHubClient.WorkItemDto>? workItems = null)
    {
        deck ??= new List<string>{"0","1","2","3","5","8"};
        participants ??= new List<SessionHubClient.ParticipantDto>();
        workItems ??= new List<SessionHubClient.WorkItemDto>();
        return new SessionHubClient.SessionSnapshotDto(code, deck.ToList(), DateTime.UtcNow, participants.ToList(), workItems.ToList());
    }

    [Fact(DisplayName = "T051 - Deck renders all allowed values + '?' chip")]
    public void T051_Deck_Renders_All_Values()
    {
        var state = ConfigureServicesAndGetState();
        var deck = new List<string>{"0","1","2","3","5","8","13"};
        var wi = new SessionHubClient.WorkItemDto(Guid.NewGuid(), "Story X", DateTime.UtcNow, "ActiveEstimating", null, null, null, Enumerable.Empty<object>());
        var snap = CreateSnapshot("ABC123", deck: deck, workItems: new List<SessionHubClient.WorkItemDto>{ wi });

        var cut = RenderComponent<ScrumPoker.Client.Pages.SessionBoard>();
        state.SetSnapshot(snap); // triggers event and re-render with active work item so deck displays

        cut.WaitForAssertion(() =>
        {
            // Collect chip texts (estimate panel chips)
            var chipTexts = cut.FindAll("span.mud-chip-content").Select(e => e.TextContent.Trim()).ToList();
            foreach(var v in deck)
                chipTexts.Should().Contain(v, $"deck value '{v}' should be rendered");
            chipTexts.Should().Contain("?", "wildcard '?' chip should be rendered");
        });
    }

    [Fact(DisplayName = "T052 - Reveal banner hidden before reveal then visible after state update")]
    public void T052_Reveal_Banner_Toggles()
    {
        var state = ConfigureServicesAndGetState();

        var wiId = Guid.NewGuid();
        var active = new SessionHubClient.WorkItemDto(wiId, "Story A", DateTime.UtcNow, "ActiveEstimating", null, null, null, Enumerable.Empty<object>());
        var snap1 = CreateSnapshot("CODE1", workItems: new List<SessionHubClient.WorkItemDto>{ active });

        var cut = RenderComponent<ScrumPoker.Client.Pages.SessionBoard>();
        state.SetSnapshot(snap1);
        cut.WaitForAssertion(() => cut.Markup.Contains("Story A"));
        cut.Markup.Should().NotContain("Estimates Revealed");

        var estimates = new List<object>{ new { participantId = Guid.NewGuid(), value = "3" }, new { participantId = Guid.NewGuid(), value = "5" } };
        var revealed = new SessionHubClient.WorkItemDto(wiId, "Story A", active.CreatedUtc, "Revealed", DateTime.UtcNow, null, null, estimates);
        var snap2 = CreateSnapshot("CODE1", workItems: new List<SessionHubClient.WorkItemDto>{ revealed });
        state.SetSnapshot(snap2);

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Estimates Revealed"));
    }

    [Fact(DisplayName = "T053 - Work item list updates after adding item")]
    public void T053_WorkItem_List_Updates()
    {
        var state = ConfigureServicesAndGetState();
        var emptySnap = CreateSnapshot("CODE2");
        var cut = RenderComponent<ScrumPoker.Client.Pages.SessionBoard>();
        state.SetSnapshot(emptySnap);
        cut.WaitForAssertion(() => cut.Markup.Contains("None yet"));

        var wi = new SessionHubClient.WorkItemDto(Guid.NewGuid(), "New Task", DateTime.UtcNow, "ActiveEstimating", null, null, null, Enumerable.Empty<object>());
        var snap2 = CreateSnapshot("CODE2", workItems: new List<SessionHubClient.WorkItemDto>{ wi });
        state.SetSnapshot(snap2);

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("New Task"));
    }
}
