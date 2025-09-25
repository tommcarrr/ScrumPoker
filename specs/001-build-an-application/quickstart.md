# Quickstart: Scrum Poker (MVP)

> Goal: Run backend API + Azurite + (optionally) Blazor WASM client locally, exercise core session lifecycle via HTTP, then connect client for real-time SignalR updates.

## Prerequisites

- .NET SDK 9.0 installed
- Docker Desktop running
- (Optional) VS Code C# Dev Kit or JetBrains Rider

## 1. Clone & Restore

```powershell
git clone <repo-url>
cd ScrumPoker
# Restore solution
 dotnet restore .\src\ScrumPoker\ScrumPoker.sln
```

## 2. Run Backend + Azurite via Docker Compose (Preferred)

One command spins up Azurite and the API (with health checks) using the root `docker-compose.yml`:

```powershell
docker compose up --build
```

API base URL: `http://localhost:5000`  |  Health: `/health`  |  Azurite: `http://localhost:10000/devstoreaccount1`

To stop:

```powershell
docker compose down
```

Skip to step 4 to smoke test the API. If you prefer manual startup, follow the manual Azurite instructions below.

## 3. (Manual Alternative) Run Azurite Only (Table + Queue + Blob)

Use Docker (preferred):

```powershell
docker run --name azurite -p 10000:10000 -p 10001:10001 -p 10002:10002 -d mcr.microsoft.com/azure-storage/azurite
```
Environment variables for local emulator (these will be set in compose later):

```text
AZURE_STORAGE_CONNECTION_STRING=UseDevelopmentStorage=true
```


## 4. Run Backend API (Manual Path)

From server project directory:

```powershell
cd .\src\ScrumPoker\ScrumPoker\ScrumPoker
# (If not already) dotnet restore
 dotnet run --project ScrumPoker.csproj
```
Expected console output: Kestrel listening on `http://localhost:5000` (adjust if `launchSettings.json` differs).


## 5. (Optional) Run WASM Client

In a new terminal:

```powershell
cd .\src\ScrumPoker\ScrumPoker\ScrumPoker.Client
 dotnet run --project ScrumPoker.Client.csproj
```
Default port may differ (e.g., 5002). The client will call the backend API at configured base URL (to be wired in a later task via configuration).

## 6. Smoke Test API (Manual HTTP)

Create Session:

```powershell
$session = Invoke-RestMethod -Method POST http://localhost:5000/api/sessions -ContentType 'application/json' -Body '{}' 
$code = $session.code
$code
```

Join Session:

```powershell
Invoke-RestMethod -Method POST "http://localhost:5000/api/sessions/$code/participants" -ContentType 'application/json' -Body '{"displayName":"Alice"}' | Out-Null
Invoke-RestMethod -Method POST "http://localhost:5000/api/sessions/$code/participants" -ContentType 'application/json' -Body '{"displayName":"Bob"}' | Out-Null
```

Add Work Item:

```powershell
Invoke-RestMethod -Method POST "http://localhost:5000/api/sessions/$code/work-items" -ContentType 'application/json' -Body '{"title":"API endpoint skeleton"}' | Out-Null
```

Submit Estimates (need workItemId from snapshot):

```powershell
$snapshot = Invoke-RestMethod -Method GET "http://localhost:5000/api/sessions/$code"
$workItemId = $snapshot.workItems[0].id
Invoke-RestMethod -Method POST "http://localhost:5000/api/sessions/$code/work-items/$workItemId/estimates" -ContentType 'application/json' -Body (@{ participantId = $snapshot.participants[0].id; value = '5' } | ConvertTo-Json)
Invoke-RestMethod -Method POST "http://localhost:5000/api/sessions/$code/work-items/$workItemId/estimates" -ContentType 'application/json' -Body (@{ participantId = $snapshot.participants[1].id; value = '8' } | ConvertTo-Json)
```

Reveal:

```powershell
Invoke-RestMethod -Method POST "http://localhost:5000/api/sessions/$code/work-items/$workItemId/reveal"
```

Finalize:

```powershell
Invoke-RestMethod -Method POST "http://localhost:5000/api/sessions/$code/work-items/$workItemId/finalize" -ContentType 'application/json' -Body '{"value":"8"}'
```

Restart Session (optional):

```powershell
Invoke-RestMethod -Method POST "http://localhost:5000/api/sessions/$code/restart"
```


## 7. Real-time Updates (SignalR)

After performing mutating operations (join, add work item, estimate, reveal, finalize, restart) the server broadcasts the full session snapshot to all clients connected to the SignalR hub at:

```text
/hubs/session
```

The client (`ScrumPoker.Client`) automatically connects (see `SessionHubClient`) and listens for the `sessionUpdated` message. Each payload includes:

```json
{
	"reason": "estimateSubmitted | participantJoined | workItemAdded | revealed | finalized | restarted | created",
	"session": { "code": "ABCD", "deck": ["1","2", "3"], "participants": [], "workItems": [] }
}
```

If you open two browser windows pointed at the client and perform actions in one, you should see the other update within < 1 second.

### Manual Hub Smoke (Optional)

You can inspect hub traffic with a WebSocket client:

1. Create a session via HTTP (as above) and note the code.
2. Open browser dev tools → Network → filter WebSocket → confirm connection to `/hubs/session`.
3. Perform an action (e.g., add work item) → observe `sessionUpdated` frame.

## 8. Performance Smoke (Optional)

Run the provided k6 script (requires API running):

```powershell
k6 run --env BASE_URL=http://localhost:5000 .\loadtest\k6-script.js
```

Threshold: p(95) < 250ms (placeholder baseline).

## 9. Troubleshooting

## 10. Troubleshooting

| Issue | Symptom | Fix |
|-------|---------|-----|
| Azurite not reachable | Connection refused | Ensure container running: `docker ps` |
| Wrong backend port | 404 from client | Check launchSettings.json & adjust client base URL or `docker compose ps` |
| CORS blocked | Browser console errors | Add AllowAnyOrigin dev policy (will be in tasks) |
| Estimates not revealed | Values still hidden | Ensure reveal endpoint called; check work item status |

## 11. Change Log Reference

See `CHANGELOG.md` for release notes (initial MVP 0.1.0).

---
*This Quickstart is covered by integration & contract tests in the solution.*
