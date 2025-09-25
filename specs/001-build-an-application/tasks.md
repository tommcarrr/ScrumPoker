# Tasks: Build an application to allow a team to play scrum poker

Feature Directory: `specs/001-build-an-application`
Plan: `specs/001-build-an-application/plan.md`
Contracts: `specs/001-build-an-application/contracts/openapi.yaml`
Data Model: `specs/001-build-an-application/data-model.md`
Quickstart: `specs/001-build-an-application/quickstart.md`
Research: `specs/001-build-an-application/research.md`

> TDD Principle: Each production change must correspond to at least one failing test added or modified first.

Legend: [P] = Parallelizable (independent files / no shared sequencing risk)

## Ordering Rationale

1. Repo hygiene & environment reproducibility
2. Contract tests before server implementation (contract-first)
3. Domain models & validation before endpoints using them
4. Endpoint implementation before SignalR broadcast integration
5. Client integration & UI after stable API + domain invariants
6. Observability & hardening after core flow works
7. Final polish & documentation alignment

## Task List

### Foundation & Environment
 
[X] T001 - Create `docker-compose.yml` with services: `api` (ScrumPoker server project), `azurite` (mcr.microsoft.com/azure-storage/azurite) mapping ports 10000-10002. Healthcheck for API (HTTP 200 on root or /health). Add instructions section to `quickstart.md`.  
[X] T002 - Add root `README.md` section "Running Locally" referencing Quickstart and docker compose usage.  
[X] T003 - Add Directory structure for tests: `tests/contract`, `tests/integration`, `tests/unit`, `tests/component` if not existing; add placeholder `.gitkeep` files. [P]  
[X] T004 - Add test project(s): create `ScrumPoker.Tests` (xUnit) referencing server project; configure folders mapping to structure (use Directory.Build.props if desired).  
[X] T005 - Add package references to test project: `xunit`, `xunit.runner.visualstudio`, `FluentAssertions`, `Microsoft.AspNetCore.Mvc.Testing`, `Bogus` (for data generation) (all as PrivateAssets=all).  
[X] T006 - Add bUnit test project `ScrumPoker.Client.Tests` referencing `ScrumPoker.Client` with packages: `bunit`, `FluentAssertions`. [P]

### Contract Tests (One per Endpoint)
 
[X] T007 - Contract test: POST /api/sessions (validate 201, schema fields: code pattern, deck array, createdUtc date-time).  
[X] T008 - Contract test: GET /api/sessions/{code} (404 for unknown; 200 shape on existing). [P]  
[X] T009 - Contract test: POST /api/sessions/{code}/participants (validate 200 returns participant added, duplicate name rejected). [P]  
[X] T010 - Contract test: POST /api/sessions/{code}/work-items (validate title validation, optional description). [P]  
[X] T011 - Contract test: POST /api/sessions/{code}/work-items/{workItemId}/estimates (accept deck values; reject invalid). [P]  
[X] T012 - Contract test: POST /api/sessions/{code}/work-items/{workItemId}/reveal (estimates hidden before, visible after). [P]  
[X] T013 - Contract test: POST /api/sessions/{code}/work-items/{workItemId}/finalize (finalEstimate cannot be '?'). [P]  
[X] T014 - Contract test: POST /api/sessions/{code}/restart (clears non-finalized progress / resets state). [P]

### Domain & Validation (Entities)
 
[X] T015 - Implement domain model `Session` with invariants (single host, status rules). [P]  
[X] T016 - Implement domain model `Participant` with display name normalization & uniqueness logic (in-memory collection for tests). [P]  
[X] T017 - Implement domain model `WorkItem` with state machine transitions, restart clearing estimates. [P]  
[X] T018 - Implement `Estimate` value object with deck validation (no '?' as final). [P]  
[X] T019 - Implement deck abstraction (list of allowed values central constant). [P]

### Persistence Layer (Azure Table Storage)
 
[#] T020 - Add Azure.Data.Tables to server project; create Table client factory (read from connection string).  
[X] T021 - Create Table entities + mappers (SessionEntity, ParticipantEntity, WorkItemEntity, EstimateEntity) matching partition/row key strategy. [P]  
[#] T022 - Repository/service for Session aggregation load: load all partition rows -> assemble snapshot (avoid premature caching).  
[X] T023 - Implement persistence for create session + join + add work item + submit estimate (write patterns).  
[X] T024 - Implement persistence for reveal, finalize, restart flows ensuring atomicity (multi-entity update sequencing).  
[X] T025 - Add optimistic concurrency (ETag check on updates) with retry loop (limited attempts). (Note: integration test scenarios for concurrent updates deferred; add in integration phase).  

### Application Services
 
[X] T026 - Service method layer (SessionService) orchestrating domain + repository calls (create/join/add/estimate/reveal/finalize/restart) returning session snapshot.  
[X] T027 - Validation/result model consolidation (return ProblemDetails on validation failures).  
[X] T028 - SignalR hub skeleton (`SessionHub`) with groups by session code (no client yet). [P]

### API Endpoints (Minimal APIs)
 
[X] T029 - Wire endpoints from OpenAPI: POST create session -> service; map validation errors; set Location header.  
[X] T030 - GET session snapshot -> service. [Depends: T022,T026]  
[X] T031 - POST join participant -> service; return updated snapshot.  
[X] T032 - POST add work item -> service.  
[X] T033 - POST submit/update estimate -> service.  
[X] T034 - POST reveal estimates -> service.  
[X] T035 - POST finalize -> service (enforce final estimate not '?').  
[X] T036 - POST restart -> service (clear ephemeral state).  
[X] T037 - Generate OpenAPI document on startup (Swashbuckle) and serve `/swagger` (dev only).  

### Real-time Integration
 
[X] T038 - Broadcast session snapshot changes via SignalR after each mutating operation (create/join/add/estimate/reveal/finalize/restart).  
[X] T039 - Client: Add SignalR client service in `ScrumPoker.Client` to subscribe to session updates.  
[X] T040 - Client: Implement state store (singleton) updating UI on hub messages.  

### Client UI (MudBlazor)
 
[X] T041 - Install MudBlazor NuGet in client; add MudTheme & basic layout integration.  
T042 - Page: Create/Join session UI (enter display name, create or join by code) with navigation.  
[X] T042 - Page: Create/Join session UI (enter display name, create or join by code) with navigation.  
[X] T043 - Page: Session board listing participants, active work item, add work item form.  
[X] T044 - Component: Estimation card deck (click -> send estimate). [P]  
[X] T045 - Component: Work item list with state indicators & reveal/finalize actions. [P]  
[X] T046 - Component: Estimate reveal banner (shows after reveal). [P]

### Integration Tests (Scenarios from Quickstart)
 
[X] T047 - Integration: Full session lifecycle (create → join x2 → add item → submit estimates → reveal → finalize).  
[X] T048 - Integration: Restart resets estimates but preserves participants & work item metadata. [P]  
[X] T049 - Integration: Duplicate participant name rejected (case-insensitive). [P]  
[X] T050 - Integration: Invalid deck value rejected on submit. [P]

### Component Tests (bUnit)
 
[X] T051 - Component test: Estimation card deck renders all allowed values (deck constant). [P]  
[X] T052 - Component test: Reveal banner hidden before reveal & visible after state update. [P]  
[X] T053 - Component test: Work item list shows new item after add. [P]

### Observability & Hardening
 
[X] T054 - Add structured logging (Serilog or built-in ILogger) for each endpoint action + correlation id.  
[X] T055 - Add minimal health endpoint `/health` for compose healthcheck. [P]  
[X] T056 - Add ProblemDetails middleware mapping domain validation exceptions.  
[X] T057 - Add rudimentary performance timing (middleware) logging p95 metrics approximation.  

### Performance / Safety Nets
 
[X] T058 - Load test script placeholder (k6 or Bombardier) documented in `README.md` (manual step).  
[X] T059 - Table Storage retry policy (exponential backoff) configuration.  

### Polishing & Docs
 
[X] T060 - Update Quickstart with SignalR instructions & compose usage once implemented.  
[X] T061 - Update README with architecture diagram (ASCII) summarizing flow.  
[X] T062 - Add CHANGELOG.md entry for initial MVP scope.  
[X] T063 - Post-Design Constitution Check verification & mark progress tracking in `plan.md`.  
[X] T064 - Remove any TODO markers left or convert to backlog items.

## Parallel Execution Guidance
 
- Contract tests T008–T014 can run in parallel once test project exists (after T004/T005).
- Domain model tasks T015–T019 parallelizable (pure model files) before persistence.
- Persistence entity mapping (T021) can run while repository/service skeleton (T022/T026) drafted, but coordinate field names.
- UI components (T044–T046) parallel after state store (T040) shape known; can stub events.
- Integration tests (T048–T050) parallel after base lifecycle test (T047) establishes harness.

## Agent Command Examples

```bash
# Example agent invocation for a parallel contract test
/tasks run T008

# Example for domain model
/tasks run T017
```

## Dependency Notes
 
- All tests rely on docker compose environment (T001) except pure domain/component tests.
- Service & endpoint tasks require domain + persistence (T015–T024) before full correctness; may stub initially under TDD.
- SignalR tasks depend on endpoints; avoid coupling domain logic to hub.

---
*Generated on 2025-09-24 based on Constitution v1.0.0 and current design artifacts.*
