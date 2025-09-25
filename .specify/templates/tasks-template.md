# Tasks: [FEATURE NAME]

**Input**: Design documents from `/specs/[###-feature-name]/`
**Prerequisites**: plan.md (required), research.md, data-model.md, contracts/

## Execution Flow (main)

```text
1. Load plan.md from feature directory
   → If not found: ERROR "No implementation plan found"
   → Extract: tech stack, libraries, structure
2. Load optional design documents:
   → data-model.md: Extract entities → model tasks
   → contracts/: Each file → contract test task
   → research.md: Extract decisions → setup tasks
3. Generate tasks by category:
   → Setup: project init, dependencies, linting
   → Tests: contract tests, integration tests
   → Core: models, services, CLI commands
   → Integration: DB, middleware, logging
   → Polish: unit tests, performance, docs
4. Apply task rules:
   → Different files = mark [P] for parallel
   → Same file = sequential (no [P])
   → Tests before implementation (TDD)
5. Number tasks sequentially (T001, T002...)
6. Generate dependency graph
7. Create parallel execution examples
8. Validate task completeness:
   → All contracts have tests?
   → All entities have models?
   → All endpoints implemented?
9. Return: SUCCESS (tasks ready for execution)
```

## Format: `[ID] [P?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- Include exact file paths in descriptions

## Path Conventions

- **Single project**: `src/`, `tests/` at repository root
- **Web app**: `backend/src/`, `frontend/src/`
- **Mobile**: `api/src/`, `ios/src/` or `android/src/`
- Paths shown below assume single project - adjust based on plan.md structure

## Phase 3.1: Setup

- [ ] T001 Ensure solution structure (.sln) updated per plan (client + backend)
- [ ] T002 Add/verify required NuGet packages (e.g., Azure.Data.Tables, OpenTelemetry.*)
- [ ] T003 [P] Configure analyzers & formatting (stylecop/nullable + dotnet format in CI)
- [ ] T004 Docker compose service definitions updated (backend, client, azurite) & health endpoints

## Phase 3.2: Tests First (TDD) ⚠️ MUST COMPLETE BEFORE 3.3

**Critical:** These tests MUST be written and MUST FAIL before ANY implementation

- [ ] T005 [P] Contract test POST /api/users in tests/contract/Users_PostTests.cs
- [ ] T006 [P] Contract test GET /api/users/{id} in tests/contract/Users_GetByIdTests.cs
- [ ] T007 [P] Integration test user registration in tests/integration/UserRegistrationTests.cs
- [ ] T008 [P] Integration test auth flow in tests/integration/AuthFlowTests.cs

## Phase 3.3: Core Implementation (ONLY after tests are failing)

- [ ] T009 [P] User entity + partition strategy in Domain/User.cs
- [ ] T010 [P] Table storage repository in Infrastructure/Repositories/TableUserRepository.cs
- [ ] T011 [P] Application service (create/get) in Application/Users/UserService.cs
- [ ] T012 POST /api/users endpoint (Api/UsersController.cs)
- [ ] T013 GET /api/users/{id} endpoint (Api/UsersController.cs)
- [ ] T014 Input validation (FluentValidation or custom attributes)
- [ ] T015 Error handling middleware + problem details

## Phase 3.4: Integration

- [ ] T016 Wire repository & service DI registrations
- [ ] T017 Auth middleware (JWT or placeholder) & policy definitions
- [ ] T018 Structured logging + OpenTelemetry tracing
- [ ] T019 CORS and security headers hardened
- [ ] T020 Add caching layer (IMemoryCache) with metrics

## Phase 3.5: Polish

- [ ] T021 [P] Unit tests for validation in tests/unit/ValidationTests.cs
- [ ] T022 Performance test harness (ensure p95 < 250ms) in tests/perf/UsersPerfTests.cs
- [ ] T023 [P] Update docs/api.md with new endpoints & versioning
- [ ] T024 Remove duplication / refactor per SOLID
- [ ] T025 Manual exploratory script updated (manual-testing.md)

## Dependencies

- Tests (T005-T008) before implementation (T009-T015)
- T009 blocks T010-T011
- T011 blocks T012-T013
- T016 blocks T018-T020
- Implementation before polish (T021-T025)

## Parallel Example

```text
# Launch T005-T008 together:
Task: "Contract test POST /api/users in tests/contract/Users_PostTests.cs"
Task: "Contract test GET /api/users/{id} in tests/contract/Users_GetByIdTests.cs"
Task: "Integration test registration in tests/integration/UserRegistrationTests.cs"
Task: "Integration test auth in tests/integration/AuthFlowTests.cs"
```

## Notes

- [P] tasks = different files, no dependencies
- Verify tests fail before implementing
- Commit after each task
- Avoid: vague tasks, same file conflicts

## Task Generation Rules

Applied during main() execution:

1. **From Contracts**:
   - Each contract file → contract test task [P]
   - Each endpoint → implementation task
   
2. **From Data Model**:
   - Each entity → model creation task [P]
   - Relationships → service layer tasks
   
3. **From User Stories**:
   - Each story → integration test [P]
   - Quickstart scenarios → validation tasks

4. **Ordering**:
   - Setup → Tests → Models → Services → Endpoints → Polish
   - Dependencies block parallel execution

## Validation Checklist

Gate: Checked by main() before returning

- [ ] All contracts have corresponding tests
- [ ] All entities have model tasks
- [ ] All tests come before implementation
- [ ] Parallel tasks truly independent
- [ ] Each task specifies exact file path
- [ ] No task modifies same file as another [P] task
- [ ] No task modifies same file as another [P] task
