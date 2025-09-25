# Data Model

## Overview

Domain entities for Scrum Poker estimation with Azure Table Storage mapping and validation rules.

## Entity: Session

| Field | Type | Notes | Validation |
|-------|------|-------|------------|
| sessionCode | string (PK part) | 6-char uppercase code | Regex ^[A-Z0-9]{6}$ |
| hostParticipantId | string | Participant designated as host | Non-empty |
| createdAt | DateTimeUtc | Creation timestamp | Set server-side |
| status | enum {Active, Ended} | Current lifecycle | Defaults Active |
| endedAt | DateTimeUtc? | When session ended | Null until Ended |

PartitionKey: "SESSION"  
RowKey: sessionCode

## Entity: Participant

| Field | Type | Notes | Validation |
|-------|------|-------|------------|
| participantId | string (GUID) | Unique per session | GUID format |
| sessionCode | string | FK to Session | Exists in Session |
| displayName | string | Unique within session | 1..40 chars; trimmed |
| joinedAt | DateTimeUtc | Join time | Server set |
| lastSeenAt | DateTimeUtc | Heartbeat / activity | Updated periodically |
| isHost | bool | Host flag | True matches Session.hostParticipantId |

PartitionKey: sessionCode  
RowKey: participantId

Unique Constraint (logical): (sessionCode, displayName)

## Entity: WorkItem

| Field | Type | Notes | Validation |
|-------|------|-------|------------|
| workItemId | string (GUID) | Identifier | GUID |
| sessionCode | string | FK | Exists in Session |
| title | string | User provided | 1..120 chars; trimmed |
| orderIndex | int | Insertion ordering | Incremental starting at 0 |
| state | enum {Pending, ActiveEstimating, Revealed, Finalized} | Workflow state | Enforced transitions |
| createdAt | DateTimeUtc | Creation timestamp | Server set |
| activatedAt | DateTimeUtc? | When moved to ActiveEstimating | Nullable |
| revealedAt | DateTimeUtc? | When estimates revealed | Nullable |
| finalizedAt | DateTimeUtc? | Finalization timestamp | Nullable |
| finalEstimate | string? | One of deck (not '?') | Null until Finalized |
| notes | string? | Plain text | <=500 chars |

PartitionKey: sessionCode  
RowKey: workItemId (optionally prefix with orderIndex padded for scanning: `0001_{guid}` if ordering needed via query)

## Entity: Estimate

| Field | Type | Notes | Validation |
|-------|------|-------|------------|
| workItemId | string | FK to WorkItem | Exists |
| participantId | string | FK to Participant | Exists |
| sessionCode | string | Redundant for partition | Matches WorkItem.sessionCode |
| value | string | Deck token | In {0,1,2,3,5,8,13,21,34,?} |
| submittedAt | DateTimeUtc | Submission time | Server set |

PartitionKey: sessionCode  
RowKey: workItemId:participantId (concatenate)

Unique Constraint: (workItemId, participantId)

## Derived / Projection Models

- **WorkItemSummary**: workItemId, title, state, finalEstimate, participantVoteCounts, hasAllVotes (bool)
- **SessionSnapshot**: sessionCode, status, participants[], activeWorkItemId, workItems[] (lightweight summaries)

## State Transitions (WorkItem)

```text
Pending -> ActiveEstimating -> Revealed -> Finalized
                ^               |
                | (Restart) ----
```
Restart allowed only from Revealed back to ActiveEstimating (clears estimates, revealedAt, finalEstimate).

## Validation Rules

- Display name trimmed; reject if duplicate (case-insensitive) within session.
- Title trimmed; reject blank or >120 chars.
- Notes stripped of control chars; enforce length.
- Deck token '?' cannot be finalEstimate.

## Consistency Invariants

- A session has exactly one host participant.
- A WorkItem in ActiveEstimating must not have revealedAt/finalizedAt set.
- A WorkItem in Finalized MUST have finalEstimate != null && revealedAt != null.
- Estimates only exist for state ∈ {ActiveEstimating, Revealed}; all removed on restart.

## Query Patterns

- Load session snapshot: Partition scan on sessionCode for Participants + WorkItems + (optionally) Estimates grouped by workItemId.
- Active work item retrieval: Filter WorkItems where state = ActiveEstimating (at most one) OR last with state Revealed awaiting finalization.

## Index & Performance Considerations

- Table Storage query cost minimized via single-partition design per session.
- For large numbers of work items, orderIndex padding supports lexicographic scans.
- Estimates volume: (#participants × #workItems). For 20 participants and 200 items → 4000 rows manageable.

## Future Extensions (Not MVP)

- Skipped state
- Statistical aggregates entity
- Audit trail entity (state transitions)

