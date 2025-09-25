# ScrumPoker

Lightweight Scrum Poker estimation application:

* Blazor WebAssembly (global Interactive WebAssembly – no server circuits)
* ASP.NET Core Minimal API backend + SignalR real‑time hub
* Azure Table Storage (Azurite locally) persistence
* Layered architecture (Domain / Contracts / Application / Infrastructure / Server / Client)

> Latest structural change: Project tree flattened (removed deep `src/ScrumPoker/ScrumPoker/ScrumPoker*` nesting). Legacy nested folders are slated for deletion once consumers migrate. All code now lives under `src/<Layer>` (e.g. `src/Domain`, `src/Server`, `src/Client`).

> Recent routing & E2E hardening: Removed separate `Routes.razor` root component in favor of inlining the `<Router>` inside `App.razor` (fixes publish-time root component resolution error). Added Playwright E2E test (docker driven) that fails on Blazor bootstrap exceptions or the prior missing root component error.

## Rendering Mode

The application is configured for **global Interactive WebAssembly** rendering (no server Blazor circuit). Components are delivered as WASM and interactivity is established fully on the client. If you want to revert to a hybrid model (server + WASM) reintroduce:

```csharp
builder.Services.AddRazorComponents()
  .AddInteractiveServerComponents()
  .AddInteractiveWebAssemblyComponents();

app.MapRazorComponents<App>()
  .AddInteractiveServerRenderMode()
  .AddInteractiveWebAssemblyRenderMode();
```

Current configuration keeps only `AddInteractiveWebAssemblyComponents` + `AddInteractiveWebAssemblyRenderMode` for simpler hosting and fewer server resources per user.

### MudBlazor Assets

MudBlazor static assets are explicitly referenced in the host page (`Components/App.razor`):

```html
<link rel="stylesheet" href="_content/MudBlazor/MudBlazor.min.css" />
<script src="_content/MudBlazor/MudBlazor.min.js"></script>
```

Integration tests assert these assets are both referenced and served (see `MudBlazorAssetsIntegrationTests`). A MIME flexibility assertion allows `application/javascript` or `text/javascript` variants depending on hosting environment.

### Routing Simplification & Root Component

Originally an intermediate `Routes.razor` component was used and referenced via `<Routes />` in `App.razor`. In container / published builds Blazor reported:

```text
AggregateException ... Root component type 'ScrumPoker.Components.Routes' could not be found in the assembly 'ScrumPoker'.
```

Even with a matching `@namespace`, the indirection caused inconsistency (likely due to trimming/assembly load ordering for an otherwise markup-only component). The fix: delete `Routes.razor` and inline the `<Router>` markup directly into `App.razor`:

```razor
<Router AppAssembly="typeof(Program).Assembly" AdditionalAssemblies="new[] { typeof(ScrumPoker.Client._Imports).Assembly }" @rendermode="InteractiveWebAssembly">
  <Found Context="routeData">
    <RouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)" />
    <FocusOnNavigate RouteData="routeData" Selector="h1" />
  </Found>
</Router>
```

The Playwright test (`e2e/tests/app.spec.ts`) asserts the absence of that specific console error.

## Frontend E2E Tests (Playwright)

A lightweight Playwright setup validates the root page loads without Blazor console errors (especially the `Root component type '...Routes' could not be found` issue) and waits for basic layout elements.

### Run (Full Compose)

```powershell
docker compose -f docker-compose.e2e.yml up --build --exit-code-from tests --abort-on-container-exit
```

### What it does

* Builds and starts the app container (exposes 5000 internally / 5080 locally).
* Waits for the `/health` endpoint to report healthy.
* Launches Playwright (Chromium) and navigates to `/`.
* Captures all browser console errors; fails the test if a Blazor root component resolution error appears.

### Interpreting Failures

If you see an error like:

```text
Encountered Blazor root component error: ManagedError: AggregateException_ctor_DefaultMessage (Root component type 'ScrumPoker.Components.Routes' could not be found in the assembly 'ScrumPoker'.)
```

Then either:

* (Historical) A removed `Routes.razor` file is still referenced (clean build / ensure it is deleted), or
* The component namespace differs from runtime expectation (check `Components/_Imports.razor` for the `@namespace ScrumPoker.Components` directive), or
* A stale WASM / DLL bundle is cached by the browser. Try a hard reload / cache clear or open in an incognito window.

### Local Iteration Outside Compose

You can run only the tests (re-using an already running container on port 5080) by executing inside `e2e`:

```powershell
npm install
npx playwright test --reporter=line
```

(Requires Node + browsers locally; compose route avoids this.) If you modify only test code, re-running compose without `--build` is sufficient.

### E2E Artifacts

On failure Playwright stores:

* Trace: `test-results/*/trace.zip` (view with `npx playwright show-trace <path>`)
* Screenshot: `test-results/*/test-failed-1.png`

These artifacts are ephemeral inside the container run; to preserve them mount an output volume.

## Running Locally

Prerequisites:

* .NET 9 SDK
* Docker Desktop

### Option 1: Docker Compose (API + Azurite)

From repository root:

```powershell
docker compose up --build
```

API will listen on `http://localhost:5000` (health endpoint: `/health`). Azurite on ports 10000-10002.

Build Notes: The API Dockerfile uses the repository root as build context (see `docker-compose.yml`) and copies only necessary source subfolders for faster incremental rebuilds. A root `.dockerignore` significantly reduces context size (excludes bin/ obj/ tests/ docs). If you add new top-level folders under `src/ScrumPoker/ScrumPoker`, update the COPY list in the Dockerfile accordingly.

Healthcheck: The runtime image installs a minimal `curl` so the compose health probe (`/health`) can function. HTTPS redirection is conditional—enabled only if an HTTPS binding exists or environment is Production—to avoid redirect loops / false negatives in health checks when only HTTP is exposed inside containers.

To stop:

```powershell
docker compose down
```

### Option 2: Manual (see Quickstart)

Follow the step-by-step instructions in `specs/001-build-an-application/quickstart.md` for running Azurite and the projects individually.

### Environment Variables

```text
AZURE_STORAGE_CONNECTION_STRING=UseDevelopmentStorage=true
```

## Project Structure (Flattened)

```text
src/
  Domain/                # Pure domain model (Session, WorkItem, Participant, etc.)
  Contracts/             # External/API & client DTO contracts
  Application/           # Use cases + coordination (SessionService, SignalR hub)
    RealTime/            # SessionHub (broadcasts snapshots)
  Infrastructure/        # Persistence (Azure Table + InMemory repositories, mappers)
  Server/                # ASP.NET Core host (Minimal APIs, endpoints, Program.cs, Razor components for hosting shell)
    Components/          # App.razor, Layouts, Pages (host-side shell for WASM)
    wwwroot/             # Static assets (app.css, MudBlazor references, bootstrap placeholder)
  Client/                # Blazor WebAssembly client (Program.cs, hub client, state)
    Services/            # SessionHubClient + SessionState

tests/
  ScrumPoker.Tests/          # Domain/Application/Infrastructure + integration tests
  ScrumPoker.Client.Tests/   # bUnit / component tests

e2e/                      # Playwright configuration + browser tests (container driven)
docker-compose.yml        # App + Azurite
docker-compose.e2e.yml    # App + Azurite + Playwright test runner
```

### Legacy (Pending Removal)

The previous nested structure (`src/ScrumPoker/ScrumPoker/ScrumPoker` & `.../ScrumPoker.Client`) remains temporarily for comparison and fallback. No builds/tests reference it anymore. It will be deleted after a final E2E verification pass.

## Architecture (Layered Flow)

```text
┌────────────────┐   HTTP (REST)   ┌───────────────────┐
│  WASM Client    │ ──────────────▶│  Server Host       │
│ (Blazor + UI)   │                │ (Minimal APIs)     │
└──────┬──────────┘                ├────────┬──────────┤
       │ SignalR (WebSockets)      │        │          │
       └──────────────────────────▶│ SessionHub        │
                        (Application/RealTime)         │
                                    ├────────┴─────────┤
                                    │ Application Layer │ (SessionService orchestrates)
                                    ├────────┬─────────┤
                                    │ Domain Model      │ (Entities, invariants)
                                    ├────────┴─────────┤
                                    │ Infrastructure    │ (Azure Table / InMemory)
                                    └────────┬─────────┘
                                          Azure Table
                                            Storage
                                           (Azurite)
```

Notable points:

* Hub moved into Application (`RealTime/SessionHub.cs`) to allow broadcast logic near use cases.
* Domain remains persistence & transport agnostic; hydration methods surfaced for cross-layer mapping.
* Infrastructure provides repository implementations + entity mappers (TableEntity <-> domain).
* Contracts isolate wire-format DTOs (keeps Application layer shielded from accidental transport coupling).
* Server hosts the Razor shell only (global interactive WASM), not server-side rendering circuits.

Key Principles:

* Domain independent of SignalR (hub is an adapter broadcasting snapshots).
* Optimistic concurrency via ETags when updating entities.
* No caching layer (explicit deferral in Constitution).
* ProblemDetails middleware centralizes error/validation responses.
* Retry policy (exponential backoff) wraps Azure Table operations.

## Tasks & Progress

Active implementation tasks live in `specs/001-build-an-application/tasks.md` (TDD-first). Mark completed tasks with [X].

Completed highlights:

* Flattened project layout (removed deep nesting – legacy pending deletion)
* Routing simplification (inline Router)
* Playwright E2E bootstrap regression guard
* SignalR hub relocation (Application layer)
* Explicit MudBlazor asset inclusion + integration tests
* Layered segregation (Domain / Contracts / Application / Infrastructure)

Planned / upcoming:

* Delete legacy nested folders after final E2E run
* Expand static asset strategy (CDN or LibMan) instead of placeholder bootstrap
* Broaden E2E coverage (multi-user estimation flow over SignalR)

## Load Test Placeholder (T058)

Light baseline performance smoke (goal: p95 < 250ms for create/join/add work item under modest load).

### k6

Prerequisites: install [k6](https://k6.io/)

Run against running API (adjust BASE_URL if not compose default):

```powershell
k6 run --env BASE_URL=http://localhost:5000 .\loadtest\k6-script.js
```

Key threshold configured in script: `http_req_duration: p(95)<250`.

### Bombardier (alternative quick burst)

```powershell
bombardier -c 20 -n 500 -m POST -H "Content-Type: application/json" -f body.json http://localhost:5000/api/sessions
```

Where `body.json` contains `{}`. This only stresses create-session endpoint.

These are placeholders; refine scenarios once full real-time flow (SignalR interactions) is included.

## Cleanup Checklist (Post-Refactor)

Before removing the legacy nested folders ensure:

1. `dotnet build` (solution) succeeds without referencing old paths.
2. All tests pass (`tests/*`).
3. Playwright E2E run passes using `docker-compose.e2e.yml`.
4. No Dockerfile or compose COPY instructions point at legacy folders.
5. Git history captured (commit before deletion for diff reference).

After confirmation, delete:

```text
src/ScrumPoker/ScrumPoker/ScrumPoker/
src/ScrumPoker/ScrumPoker/ScrumPoker.Client/
```

Then run tests + E2E again.

## License

TBD
