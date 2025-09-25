# Changelog

All notable changes to this project will be documented in this file.

The format loosely follows Keep a Changelog and Semantic Versioning (0.y.z while in MVP evolution).

## [0.1.0] - 2025-09-24

### Added

- Initial Scrum Poker MVP: session creation, participant join, work item add, estimate submit, reveal, finalize, restart.
- Azure Table Storage persistence (Azurite for local development) with partition strategy per session and optimistic concurrency via ETags.
- Retry policy (exponential backoff) for Table Storage operations (configurable).
- SignalR real-time broadcasting of full session snapshot to connected clients.
- Blazor WebAssembly client with session board UI (participants, work items, estimates deck, reveal/finalize actions).
- Contract tests (Minimal APIs) and integration tests covering full lifecycle & validation rules.
- Component tests (bUnit) for key UI behaviors (deck render, reveal banner, work item list updating).
- ProblemDetails middleware centralizing domain & validation error responses.
- Correlation Id + rudimentary performance timing middleware (approximate p95 logging baseline).
- Health endpoint `/health` for container orchestration and compose health checks.
- Load test placeholder (k6 script with p95 < 250ms threshold goal) and Bombardier example.
- Docker Compose for API + Azurite.

### Changed

- Set global render mode to Interactive WebAssembly only (removed server circuit mode) and explicitly referenced MudBlazor static assets in host page. Added integration tests validating asset availability and references.

### Deferred / Not in MVP

- Authentication & authorization.
- Persistent user identity beyond display name.
- OpenTelemetry tracing & metrics (placeholder only via logs).
- Advanced performance tuning / caching layer.
- UI polish (themes, responsive layout refinements) beyond basics.

### Notes

- API surface intentionally minimal and versioned implicitly as 0.x (breaking changes allowed without major bump during MVP).
- Future releases will begin semantic versioning once API stabilizes.

---
