# Research (Phase 0)

## Overview
This document captures decisions, rationale, and alternatives for the Scrum Poker estimation feature prior to design artifact generation.

## Decisions

### D1: UI Framework
- Decision: Use MudBlazor components for all UI elements (cards, tables, dialogs, buttons) instead of custom JS/CSS.
- Rationale: Consistency, accessibility, responsive grid system, reduces custom styling effort.
- Alternatives: Raw Bootstrap (higher styling overhead), Tailwind (extra dependency footprint), Custom components (longer dev time).

### D2: Real-time Transport
- Decision: SignalR (WebSockets preferred) for session and estimation state sync.
- Rationale: Tight integration with ASP.NET Core, built-in group management, fallback support.
- Alternatives: Polling (higher latency), gRPC streams (overkill for browser + WASM in this context), WebRTC (complex signaling).

### D3: Persistence Store
- Decision: Azure Table Storage via Azure.Data.Tables SDK (AzURite locally) for simplicity and low cost.
- Rationale: Fits key-based access patterns (Session + WorkItems), minimal operational complexity.
- Alternatives: Cosmos DB (higher complexity/cost), SQL (schema migrations overhead), In-memory only (no durability).

### D4: Entities & Partition Strategy
- PartitionKey strategy:
  - Session: PartitionKey = "SESSION", RowKey = sessionCode
  - Participant: PartitionKey = sessionCode, RowKey = participantId
  - WorkItem: PartitionKey = sessionCode, RowKey = workItemId (ordered via a sortable prefix or createdAt ticks)
  - Estimate: PartitionKey = sessionCode, RowKey = workItemId:participantId (composite)
- Rationale: Enables efficient session-scoped queries. Avoids cross-partition fan-out for core operations.
- Alternatives: Single partition (scales poorly), per-user partition (complicates retrieval by session).

### D5: State Machine (Work Item)
- Decision: States = Pending -> ActiveEstimating -> Revealed -> Finalized; transitions controlled by host actions.
- Rationale: Clear progression, easy to validate allowed actions.
- Alternatives: Combined Revealed+Finalized step (less explicit), Adding Skipped (deferred for simplicity).

### D6: Final Estimate Constraint
- Decision: Final estimate must be one of allowed deck values except '?'.
- Rationale: Avoids ambiguous values; maintains shared scale.
- Alternatives: Arbitrary numeric values (introduces friction, complicates consensus display).

### D7: Deck Scope
- Decision: {0,1,2,3,5,8,13,21,34,?}
- Rationale: Balanced coverage for early feature complexity. Additional tokens (40, 100, ∞, Coffee) deferred.

### D8: Visibility Rules
- Decision: Individual estimates hidden until reveal; after finalization visible to all.
- Rationale: Encourages independent estimation; transparency post-consensus.

### D9: Restart Behavior
- Decision: Restart clears all prior estimates for the item (no historical retention MVP).
- Rationale: Simplicity; reduce edge cases; can extend later with history log.

### D10: Notes Field
- Decision: Optional plain text, max 500 characters, stored on WorkItem.
- Rationale: Lightweight context capture.

### D11: Session Code Format
- Decision: 6-character uppercase alphanumeric (regex: ^[A-Z0-9]{6}$)
- Rationale: Easy to read/communicate verbally; adequate key space (36^6 ~ 2.18B combinations).

### D12: Performance Target
- Decision: p95 latency for core operations (<250ms) and near-real-time broadcast (<200ms end-to-end) under 20 participants.

### D13: Testing Strategy
- Decision: TDD with failing tests: contract (API endpoints), integration (session flow), component tests (MudBlazor interactions), domain unit tests.

### D14: Caching
- Decision: No server-side caching layer MVP (Table access expected low volume). Add IMemoryCache only if metrics show hotspots.

## Unresolved (Deferred)
- Statistical summary helpers (min/max/outliers) - Phase 2+ candidate.
- Session archival / export (CSV) - Future feature.
- Host transfer - Future resilience enhancement.

## Risks & Mitigations
| Risk | Impact | Mitigation |
|------|--------|------------|
| High concurrent sessions scaling Table Storage partitions | Elevated latency | Monitor partition RU metrics; introduce caching if needed |
| Browser reconnect churn | State divergence | On reconnect, server re-sends authoritative session snapshot |
| Large item lists degrade client render | UX lag | Virtualized table or paging if >200 items |
| Estimate collusion (external comms) | Reduced estimation quality | Out of scope (process issue) |

## Glossary
- **Reveal**: Action where host makes all estimates visible.
- **Finalization**: Host commits final consensus value.
- **Restart**: Clears estimates; returns to ActiveEstimating.

## Summary
All prior ambiguities resolved with defaults. Ready to proceed to Phase 1 design generation.
