<!--
Sync Impact Report
Version change: (none) → 1.0.0
Modified principles: (initial creation)
Added sections: Core Principles; Engineering Constraints & Stack Standards; Development Workflow & Quality Gates; Governance
Removed sections: None
Templates requiring updates:
 - .specify/templates/plan-template.md ✅ updated
 - .specify/templates/spec-template.md ✅ no changes needed (still aligned)
 - .specify/templates/tasks-template.md ✅ updated for .NET/Blazor context
Follow-up TODOs:
 - None (all placeholders resolved). If original ratification predates 2025-09-24 replace date accordingly.
-->

# ScrumPoker Constitution

## Core Principles

### 1. Test-Driven Development (NON-NEGOTIABLE)
Tests MUST be written first and MUST fail before any production code is authored (Red → Green → Refactor). Every bug fix requires a failing regression test first. No code is merged without: (a) failing test existed, (b) implementation made it pass, (c) refactor pass retaining coverage. High-level flow tests (API/UI) precede lower-level abstractions when uncertainty exists. Snapshot and golden-file tests require explicit approval and justification.
Rationale: Ensures correctness, prevents regressions, drives design clarity, and institutionalizes fast feedback.

### 2. Architectural Integrity (SOLID, DRY, YAGNI, KISS)
Code MUST observe: Single Responsibility per class/component; Explicit interfaces for cross-layer contracts; Dependency Inversion via DI container; No duplication exceeding the Rule of Three; No speculative features—every abstraction justified by current, not hypothetical, requirements; Simplicity favored over cleverness; Public surface minimal & versioned. Cross-cutting concerns (logging, caching, validation) centralized. Circular dependencies forbidden and blocked in review.
Rationale: Maintains evolvability, reduces cognitive load, and prevents architecture erosion.

### 3. Reproducible Environment & Run-Anywhere
The entire system (Blazor WASM client + ASP.NET Core backend + Azurite emulation + supporting services) MUST start locally with a single `docker compose up` producing a usable site. Local and CI environments MUST be functionally equivalent (excluding production-only secrets). Configuration is 12‑factor: environment variables first, no secrets committed. A failing compose startup blocks merge. Health endpoints and readiness probes required for each container.
Rationale: Eliminates “works on my machine”; speeds onboarding; supports deterministic CI.

### 4. Data Persistence & Caching Discipline
Azure Table Storage is the authoritative persistence layer. Access MUST go through repository/service boundaries with asynchronous APIs. PartitionKey/RowKey design documented for every entity. Caching (in-memory or distributed) MUST be: (a) explicit, (b) measurable (cache hit metrics), (c) invalidation rules documented. No premature caching—only after an observed performance need or latency SLO risk. Writes are idempotent where possible. Migrations (schema evolution / table shape changes) require backward-compatible rollout notes.
Rationale: Ensures predictable data performance, correctness, and traceability.

### 5. Observability, Performance, Security & Compliance
Every feature supplies structured logging (correlated via trace/span IDs), metrics (request latency, error rate, cache hits, Table Storage RU approximations), and trace instrumentation (OpenTelemetry). P0 APIs have latency SLOs (p95 < 250ms under nominal load) and error budget tracking. Security: input validation at boundaries, zero trust for user input, least privilege credentials, secrets in managed store (never in code), third‑party dependency scanning automated. Privacy: only necessary data stored; retention & deletion policies implemented where applicable. Any performance optimization MUST include before/after measurements committed in PR notes.
Rationale: Makes the system operable, trustworthy, and cost-aware.

## Engineering Constraints & Stack Standards
Technology Stack: Blazor WebAssembly front-end (`ScrumPoker.Client`), ASP.NET Core backend (`ScrumPoker`) exposing a versioned REST/JSON API. Persistence: Azure Table Storage (emulated locally by Azurite in Docker). Optional in-memory caching via `IMemoryCache`; consider distributed cache if multi-instance scaling emerges. Target Framework: .NET 9.0 (update via explicit governance approval). Dependency Injection: built-in container; avoid service locator. Configuration binding validated at startup; misconfig = fail fast.

Source Structure: Keep solution modular but lean—avoid microservice fragmentation until required by load or domain boundaries. Shared contracts in a dedicated project if (and only if) duplication pressure exists. UI components isolated by feature folder. Backend layering: `Api` (minimal endpoints) → `Application` (handlers / orchestrators) → `Domain` (entities/value objects) → `Infrastructure` (Table Storage, caching, external integrations). No domain logic in controllers or Razor components.

API Standards: All endpoints versioned (URL or header). Breaking changes require a parallel live version + deprecation notice (minimum 1 minor release overlap). Error responses standardized (traceId, code, message, details). Validation failures return 400 with field-level errors.

Quality Gates Automation: Static analysis (analyzers, nullable reference types warnings treated as errors), security scanning, and test coverage threshold (initial 70% lines / 80% critical services—ratchet upward, never downward). Code formatting enforced via `dotnet format` in CI.

Performance & Capacity: Table storage partition strategies documented. Any operation expected to scan > 1,000 entities must use continuation tokens and be profiled. Memory growth > 5% per release requires investigation notes.

## Development Workflow & Quality Gates
Branching: `main` always deployable; feature branches named `feat/<slug>`; fixes `fix/<slug>`. All PRs: (1) link Issue/Feature spec, (2) include test evidence (screenshots for UI flows), (3) list architectural decisions touched, (4) note any caching strategy changes. Squash merge default.

TDD Pipeline: New feature: write failing integration/API/UI tests → failing unit tests → implement smallest passing code → refactor for SOLID/DRY → add observability. Bug fix: reproduce with failing test first. A PR without at least one new or updated test is rejected unless explicitly classified as pure refactor (and validated by unchanged behavior tests).

Quality Gates (all MUST pass pre-merge):

1. `docker compose up` healthy (readiness probes green)
2. All tests green (unit, integration, contract, UI where applicable)
3. Lint/analyzers no errors; warnings triaged
4. Coverage thresholds met / improved
5. No open [NEEDS CLARIFICATION] markers in related spec/plan
6. Security & license scan clean (or approved exceptions)
7. Constitution Check: explicit checklist in plan.md satisfied

Documentation: Each feature supplies or updates: spec, plan, data model (if domain changes), quickstart snippet, and ADR if introducing/altering architectural decisions.

Release & Versioning: Semantic Versioning applied to application API & protocol contracts. MAJOR for breaking API/domain changes; MINOR for backward-compatible features or new principles; PATCH for bug fixes/observability-only changes.

## Governance

Authority: This Constitution supersedes ad hoc conventions. Conflicts resolved in its favor until amended.

Amendments: Proposed via PR referencing an issue labeled `governance`. Requires: impact analysis, version bump rationale (Patch/Minor/Major), migration considerations, and updated Constitution Check examples. At least two maintainers must approve (or one maintainer + automated policy bot once implemented). `LAST_AMENDED_DATE` updated only on merge.

Versioning Policy (Governance Document Itself):

- MAJOR: Remove or redefine a principle / introduce backward-incompatible workflow change.
- MINOR: Add a new principle, expand scope materially, or introduce new mandatory gate.
- PATCH: Clarify wording, fix typos, tighten language without altering intent.

Compliance & Review Cadence: Quarterly (minimum) governance review ensures principles remain necessary and lean. Metrics gathered: build stability, mean PR cycle time, escaped defect count, performance SLO adherence, cache hit ratios. Persistent violations require an ADR or refactor task in next sprint.

Violation Handling: A documented violation blocks merge unless a temporary waiver issue (with expiry date) is created. Waivers tracked and reported in review meetings.

Sunsetting / Deprecation: Deprecated practices tagged; removal scheduled with clear migration window & tasks list.

Escalation: If a principle impedes urgent security fix, a maintainer may authorize a narrow exception—must be followed by remediation PR within 48h.

**Version**: 1.0.0 | **Ratified**: 2025-09-24 | **Last Amended**: 2025-09-24

---
All contributors are expected to internalize and apply these principles. "I didn’t know" is not an acceptable justification—tooling and reviews enforce compliance.
