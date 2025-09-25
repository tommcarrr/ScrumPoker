# Feature Specification: Scrum Poker Estimation Sessions
## Clarifications

### Session

- Host disconnect: session continues; no automatic host transfer; host must rejoin.
- Session end: Sessions persist until manually ended by host; ended sessions become read-only; cannot be reopened.
- Expiration: No automatic expiration; indefinite retention.
- Participant cap: Soft cap 20; UI warns at >20 joins.
- Concurrent sessions: Unlimited (isolation by session code).

### Participants

- Display name uniqueness enforced; duplicate entry rejected with prompt.
- Late joiners: See only final estimates for completed items; individual historical votes visible only for active or newly revealed items after they join.
- Multiple tabs: Treated as same participant (last connection wins); prior connection invalidated.

### Work Items

- Ordering: Insertion order; no reordering MVP.
- Duplicate titles allowed.
- Skipping: Not supported in MVP (host proceeds linearly); can remove before reveal.
- Removal: Allowed only before reveal; after reveal item must be finalized (cannot be deleted).
- Notes: Optional plain text, max 500 chars, no markdown rendering.

### Estimation Mechanics

- Deck: {0,1,2,3,5,8,13,21,34,?} only.
- Reveal with missing votes allowed.
- Final estimate must be one of deck values (except '?' which cannot be final).
- Revote after reveal: Not allowed; host can restart which clears previous estimates.
- Restart round: Clears hidden and revealed estimates; item returns to Active Estimating.
- Statistical helpers: Deferred (not in MVP).

### Visibility & Privacy

- Pre-reveal: Only participation status (has voted) visible.
- Post-finalization: Individual estimates for that item visible to all participants.

### Persistence & Retention

- Indefinite retention; no archival policy MVP.
- Soft delete only at system level (not exposed in UI) for compliance if needed later.

### Real-time & UI

- SignalR single group per session.
- Mobile/adaptive: Layout collapses participant grid into vertical list under 600px width.
- Accessibility: Keyboard navigation for deck; focus outline retained.
- Theme: MudBlazor default theme.

### Error Handling

- Network drop mid-vote: On reconnect previous submitted estimate (if any) re-fetched from server; unsent selection lost.
- Session code format: 6 uppercase alphanumeric (e.g., A1B2C3).

### Security / Abuse

- No auth; rely on ephemeral display name; future mitigation possible.
- No explicit rate limiting MVP; review if spam emerges.

All prior [NEEDS CLARIFICATION] markers are now resolved by these defaults.
As a team facilitator (host), I want to create an estimation session and invite team members via a sharable URL so that we can collaboratively estimate a list of work items using a Fibonacci-based scale and record the final agreed estimate for each item.

### Acceptance Scenarios

1. **Given** a host creates a new session **When** they receive a session code/URL **Then** they can share it and others joining see an empty item list and their name prompt.
2. **Given** participants have joined a session **When** the host adds a new work item with a title **Then** all participants see the item in the "Pending" state.
3. **Given** an active work item awaiting estimates **When** a participant selects a Fibonacci value **Then** their choice is recorded but hidden from others until reveal.
4. **Given** all (or some) participants have selected values **When** the host clicks "Reveal" **Then** all individual estimates become visible simultaneously.
5. **Given** estimates are revealed for an item **When** the host enters a final agreed estimate and confirms **Then** the item status becomes "Estimated" and the next item (if any) becomes active.
6. **Given** historical items exist **When** the session page loads **Then** previously estimated items display with title, final estimate, and (if authorized) the individual estimates.
7. **Given** a participant refreshes the browser **When** the session reloads **Then** their prior estimates (submitted before reveal) persist for unrevealed items.
8. **Given** an unrevealed round **When** the host chooses to restart that item's estimation **Then** all participants' selections for that item are cleared.

### Edge Cases

- What happens if the host disconnects? [NEEDS CLARIFICATION: Does another participant become host or is session locked?]
- Participant attempts to vote after reveal but before final estimate stored. [NEEDS CLARIFICATION: Allow revotes post reveal?]
- Duplicate work item titles entered. [NEEDS CLARIFICATION: Is uniqueness required or allowed duplicates?]
- Late joiner after some items estimated: Should they see historical individual estimates? [NEEDS CLARIFICATION]
- Large team (e.g., > 20 participants). [NEEDS CLARIFICATION: Any participant cap?]
- Idle participant never votes: Can host force reveal with missing votes? (Assumed YES.)
- Session expiration / inactivity timeout. [NEEDS CLARIFICATION]
- Handling misspelling "fibonachi" → Use standard Planning Poker Fibonacci deck.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow a user to create a new estimation session and return a session code + join URL.
- **FR-002**: System MUST allow additional users to join an existing session using the session code or join URL.
- **FR-003**: System MUST prompt each joining user to supply a display name (unique within session) before participating. [NEEDS CLARIFICATION: Enforce uniqueness or auto-suffix duplicates?]
- **FR-004**: System MUST allow the host to add work items with a required title (non-empty, trimmed).
- **FR-005**: System MUST track each work item through states: Pending → Active Estimating → Revealed → Finalized.
- **FR-006**: System MUST present participants with a Fibonacci estimation deck: {0, 1, 2, 3, 5, 8, 13, 21, 34, ?}. [NEEDS CLARIFICATION: Include 40 / 100 / coffee / infinity / skip?]
- **FR-007**: System MUST allow participants to select exactly one estimate per active item prior to reveal; selections remain hidden from other participants and host until reveal.
- **FR-008**: System MUST allow the host to trigger a reveal that simultaneously displays all individual estimates.
- **FR-009**: System MUST allow the host to record a final estimate (chosen value not restricted to previously selected values) after reveal.
- **FR-010**: System MUST persist each work item with: id, title, per-participant estimates, revealed flag, final estimate (nullable until set), timestamps.
- **FR-011**: System MUST display all items and their final estimates in a table ordered by creation time (latest at bottom or specified sort). [NEEDS CLARIFICATION: Sort order preference]
- **FR-012**: System MUST prevent estimation submissions after reveal unless a new round is started for that item. [NEEDS CLARIFICATION: Is restarting allowed?]
- **FR-013**: System SHOULD allow the host to skip to another item without finalizing (state change logged). [NEEDS CLARIFICATION]
- **FR-014**: System MUST handle participants leaving/rejoining without losing already submitted (unrevealed) estimates.
- **FR-015**: System MUST indicate visually which participants have submitted an estimate (without revealing the value) before reveal.
- **FR-016**: System MUST allow host to remove an item prior to reveal if created in error. [NEEDS CLARIFICATION: Removal after reveal / after finalization?]
- **FR-017**: System MUST log final decision timestamp per item.
- **FR-018**: System MUST restrict host-only actions (activate item, reveal, finalize) to the original session creator. [NEEDS CLARIFICATION: Host transfer rules]
- **FR-019**: System MUST handle invalid / expired session code with a user-friendly error.
- **FR-020**: System MUST prevent joining a session that has been marked ended by the host (if session end concept exists). [NEEDS CLARIFICATION: Do sessions end?]
- **FR-021**: System SHOULD display statistical helpers post reveal (e.g., min, max, average, outlier highlight). [NEEDS CLARIFICATION]
- **FR-022**: System MUST allow optional notes or rationale per item. [NEEDS CLARIFICATION: Mandatory?]
- **FR-023**: System MUST support concurrent sessions (isolation by session code).
- **FR-024**: System MUST sanitize all user-entered text to prevent injection or layout breaking characters.
- **FR-025**: System MUST persist data durably so that page refresh does not lose session state.

*Ambiguity Examples Marked Above—resolution required before implementation planning.*

### Key Entities *(include if feature involves data)*

- **Session**: Represents an estimation container; attributes: sessionId, hostId, code, createdAt, status [Active, Ended?], participants[]
- **Participant**: participantId, sessionId, displayName, joinedAt, lastSeenAt, isHost (bool)
- **WorkItem**: workItemId, sessionId, title, orderIndex, state, createdAt, activatedAt, revealedAt, finalizedAt, finalEstimate, notes?
- **Estimate**: workItemId, participantId, value (string or numeric deck token), submittedAt
- **Deck** (conceptual, not persisted per estimate): standard Fibonacci tokens used to validate selections

---

## Review & Acceptance Checklist

Gate: Automated checks run during main() execution

### Content Quality

- [ ] No implementation details (languages, frameworks, APIs)
- [ ] Focused on user value and business needs
- [ ] Written for non-technical stakeholders
- [ ] All mandatory sections completed

### Requirement Completeness

- [ ] No [NEEDS CLARIFICATION] markers remain
- [ ] Requirements are testable and unambiguous  
- [ ] Success criteria are measurable
- [ ] Scope is clearly bounded
- [ ] Dependencies and assumptions identified

---

## Execution Status

Updated by main() during processing

- [ ] User description parsed
- [ ] Key concepts extracted
- [ ] Ambiguities marked
- [ ] User scenarios defined
- [ ] Requirements generated
- [ ] Entities identified
- [ ] Review checklist passed

---
## Clarifications

### Session
- Host disconnect: session continues; no automatic host transfer; host must rejoin.
- Session end: Sessions persist until manually ended by host; ended sessions become read-only; cannot be reopened.
- Expiration: No automatic expiration; indefinite retention.
- Participant cap: Soft cap 20; UI warns at >20 joins.
- Concurrent sessions: Unlimited (isolation by session code).

### Participants
- Display name uniqueness enforced; duplicate entry rejected with prompt.
- Late joiners: See only final estimates for completed items; individual historical votes visible only for active or newly revealed items after they join.
- Multiple tabs: Treated as same participant (last connection wins); prior connection invalidated.

### Work Items
- Ordering: Insertion order; no reordering MVP.
- Duplicate titles allowed.
- Skipping: Not supported in MVP (host proceeds linearly); can remove before reveal.
- Removal: Allowed only before reveal; after reveal item must be finalized (cannot be deleted).
- Notes: Optional plain text, max 500 chars, no markdown rendering.

### Estimation Mechanics
- Deck: {0,1,2,3,5,8,13,21,34,?} only.
- Reveal with missing votes allowed.
- Final estimate must be one of deck values (except '?' which cannot be final).
- Revote after reveal: Not allowed; host can restart which clears previous estimates.
- Restart round: Clears hidden and revealed estimates; item returns to Active Estimating.
- Statistical helpers: Deferred (not in MVP).

### Visibility & Privacy
- Pre-reveal: Only participation status (has voted) visible.
- Post-finalization: Individual estimates for that item visible to all participants.

### Persistence & Retention
- Indefinite retention; no archival policy MVP.
- Soft delete only at system level (not exposed in UI) for compliance if needed later.

### Real-time & UI
- SignalR single group per session.
- Mobile/adaptive: Layout collapses participant grid into vertical list under 600px width.
- Accessibility: Keyboard navigation for deck; focus outline retained.
- Theme: MudBlazor default theme.

### Error Handling
- Network drop mid-vote: On reconnect previous submitted estimate (if any) re-fetched from server; unsent selection lost.
- Session code format: 6 uppercase alphanumeric (e.g., A1B2C3).

### Security / Abuse
- No auth; rely on ephemeral display name; future mitigation possible.
- No explicit rate limiting MVP; review if spam emerges.

All prior [NEEDS CLARIFICATION] markers are now resolved by these defaults.

