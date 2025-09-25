// Full parity Program.cs migrated from nested project
// Removed outdated usings referencing old nested namespaces
using ScrumPoker.Domain;
using ScrumPoker.Infrastructure;
using ScrumPoker.Contracts;
using ScrumPoker.Application;
using ScrumPoker.RealTime;
using Microsoft.OpenApi.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ScrumPoker.Client.Services; // client-side services reused server-side for prerender/interactive components
using MudBlazor.Services; // AddMudServices extension

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ScrumPoker.Infrastructure.Persistence.ITableClientFactory, ScrumPoker.Infrastructure.Persistence.TableClientFactory>();
var useTable = builder.Configuration.GetValue("UseTableStorage", false);
if (useTable)
    builder.Services.AddSingleton<ISessionRepository, ScrumPoker.Infrastructure.Persistence.TableSessionRepository>();
else
    builder.Services.AddSingleton<ISessionRepository, InMemorySessionRepository>();

builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<SessionHubClient>();
builder.Services.AddScoped<SessionState>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(sp =>
{
    var accessor = sp.GetRequiredService<IHttpContextAccessor>();
    var req = accessor.HttpContext?.Request;
    var baseUri = req is null ? "http://localhost:5000/" : $"{req.Scheme}://{req.Host.ToUriComponent()}/";
    return new HttpClient { BaseAddress = new Uri(baseUri) };
});

builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMudServices();

var app = builder.Build();

// ProblemDetails Middleware
app.Use(async (context, next) =>
{
    try { await next(); }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Errors");
        logger.LogError(ex, "Unhandled exception");
        var problem = ex switch
        {
            ArgumentException a => new ProblemDetails { Title = "Invalid argument", Detail = a.Message, Status = StatusCodes.Status400BadRequest },
            InvalidOperationException o when o.Message.Contains("Duplicate", StringComparison.OrdinalIgnoreCase) => new ProblemDetails { Title = "Validation failed", Detail = o.Message, Status = StatusCodes.Status400BadRequest },
            ConcurrencyConflictException => new ProblemDetails { Title = "Concurrency conflict", Detail = ex.Message, Status = StatusCodes.Status409Conflict },
            _ => new ProblemDetails { Title = "Server error", Detail = ex.Message, Status = StatusCodes.Status500InternalServerError }
        };
        context.Response.StatusCode = problem.Status ?? 500;
        await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }
});

// Correlation Id Middleware
app.Use(async (context, next) =>
{
    const string HeaderName = "X-Correlation-Id";
    var cid = context.Request.Headers.TryGetValue(HeaderName, out var existing) && !string.IsNullOrWhiteSpace(existing)
        ? existing.ToString()
        : Guid.NewGuid().ToString();
    context.Items[HeaderName] = cid;
    context.Response.Headers[HeaderName] = cid;
    using (context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Request").BeginScope(new Dictionary<string, object>{{"CorrelationId", cid}}))
    {
        var sw = Stopwatch.StartNew();
        try { await next(); sw.Stop(); var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Request"); logger.LogInformation("HTTP {Method} {Path} -> {StatusCode} in {Elapsed}ms", context.Request.Method, context.Request.Path, context.Response.StatusCode, sw.ElapsedMilliseconds); }
        catch (Exception ex) { sw.Stop(); var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Request"); logger.LogError(ex, "Unhandled exception {Method} {Path} in {Elapsed}ms", context.Request.Method, context.Request.Path, sw.ElapsedMilliseconds); throw; }
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseSwagger();
    app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "ScrumPoker API v1"); c.RoutePrefix = "swagger"; });
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

if (app.Urls.Any(u => u.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) || app.Environment.IsProduction())
    app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<ScrumPoker.Server.Components.App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(ScrumPoker.Client.Services.SessionState).Assembly);

app.MapHub<SessionHub>("/hubs/session");

var api = app.MapGroup("/api");

app.MapGet("/health", (IConfiguration cfg) =>
{
    var useTableStorage = cfg.GetValue("UseTableStorage", false);
    var payload = new Dictionary<string, object?> { ["status"] = "ok", ["tableStorage"] = useTableStorage ? "configured" : null };
    return Results.Ok(payload);
}).WithName("Health").Produces(200);

api.MapPost("/sessions", async (ISessionService service) =>
{
    var snap = await service.CreateSessionAsync();
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Endpoints");
    logger.LogInformation("Created session {Code}", snap.Code);
    return Results.Created($"/api/sessions/{snap.Code}", new
    {
        code = snap.Code,
        deck = snap.Deck,
        createdUtc = snap.CreatedUtc.ToString("O"),
        participants = snap.Participants.Select(p => new { id = p.Id, displayName = p.DisplayName, joinedUtc = p.JoinedUtc.ToString("O"), isHost = p.IsHost }),
        workItems = snap.WorkItems.Select(w => new { id = w.Id, title = w.Title, createdUtc = w.CreatedUtc.ToString("O") })
    });
}).WithName("CreateSession").Produces(201);

api.MapGet("/sessions/{code}", async (string code, ISessionService service) =>
{
    var snap = await service.GetSessionAsync(code);
    if (snap is null) return Results.NotFound();
    return Results.Ok(new
    {
        code = snap.Code,
        deck = snap.Deck,
        createdUtc = snap.CreatedUtc.ToString("O"),
        participants = snap.Participants.Select(p => new { id = p.Id, displayName = p.DisplayName, joinedUtc = p.JoinedUtc.ToString("O"), isHost = p.IsHost }),
        workItems = snap.WorkItems.Select(w => new { id = w.Id, title = w.Title, createdUtc = w.CreatedUtc.ToString("O") })
    });
}).WithName("GetSession").Produces(200).Produces(404);

api.MapPost("/sessions/{code}/participants", async (string code, ParticipantJoinRequest body, ISessionService service) =>
{
    var snap = await service.JoinAsync(code, body.DisplayName);
    if (snap is null) return Results.NotFound();
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Endpoints");
    logger.LogInformation("Participant {DisplayName} joined {Code}", body.DisplayName, code);
    return Results.Ok(new
    {
        code = snap.Code,
        deck = snap.Deck,
        createdUtc = snap.CreatedUtc.ToString("O"),
        participants = snap.Participants.Select(p => new { id = p.Id, displayName = p.DisplayName, joinedUtc = p.JoinedUtc.ToString("O"), isHost = p.IsHost }),
        workItems = snap.WorkItems.Select(w => new { id = w.Id, title = w.Title, createdUtc = w.CreatedUtc.ToString("O") })
    });
}).WithName("JoinSession").Produces(200).Produces(400).Produces(404);

api.MapPost("/sessions/{code}/work-items", async (string code, WorkItemCreateRequest body, ISessionService service) =>
{
    var snap = await service.AddWorkItemAsync(code, body.Title);
    if (snap is null) return Results.NotFound();
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Endpoints");
    logger.LogInformation("Added work item '{Title}' to {Code}", body.Title, code);
    return Results.Ok(new
    {
        code = snap.Code,
        deck = snap.Deck,
        createdUtc = snap.CreatedUtc.ToString("O"),
        participants = snap.Participants.Select(p => new { id = p.Id, displayName = p.DisplayName, joinedUtc = p.JoinedUtc.ToString("O"), isHost = p.IsHost }),
        workItems = snap.WorkItems.Select(w => new { id = w.Id, title = w.Title, createdUtc = w.CreatedUtc.ToString("O") })
    });
}).WithName("AddWorkItem").Produces(200).Produces(400).Produces(404);

api.MapPost("/sessions/{code}/work-items/{workItemId}/estimates", async (string code, Guid workItemId, EstimateCreateRequest body, ISessionService service) =>
{
    if (!Deck.IsValid(body.Value)) return Validation.Problem("Invalid estimate value");
    if (workItemId == Guid.Empty) return Results.Ok();
    var snap = await service.SubmitEstimateAsync(code, workItemId, body.ParticipantId, body.Value);
    if (snap is null) return Results.NotFound();
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Endpoints");
    logger.LogInformation("Estimate {Value} submitted by {Participant} for {WorkItem} in {Code}", body.Value, body.ParticipantId, workItemId, code);
    return Results.Ok();
}).WithName("SubmitEstimate").Produces(200).Produces(400).Produces(404).Produces(409);

api.MapPost("/sessions/{code}/work-items/{workItemId}/reveal", async (string code, Guid workItemId, ISessionService service) =>
{
    if (workItemId == Guid.Empty) return Results.Ok();
    var snap = await service.RevealAsync(code, workItemId);
    if (snap is null) return Results.NotFound();
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Endpoints");
    logger.LogInformation("Revealed estimates for work item {WorkItem} in {Code}", workItemId, code);
    return Results.Ok();
}).WithName("RevealEstimates").Produces(200).Produces(404).Produces(409);

api.MapPost("/sessions/{code}/work-items/{workItemId}/finalize", async (string code, Guid workItemId, Dictionary<string, string> body, ISessionService service) =>
{
    var value = body.TryGetValue("value", out var v) ? v : string.Empty;
    if (value == "?") return Validation.Problem("Final estimate cannot be '?' ");
    if (workItemId == Guid.Empty) return Results.Ok();
    var snap = await service.FinalizeAsync(code, workItemId, value);
    if (snap is null) return Results.NotFound();
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Endpoints");
    logger.LogInformation("Finalized work item {WorkItem} in {Code} with {Value}", workItemId, code, value);
    return Results.Ok();
}).WithName("FinalizeWorkItem").Produces(200).Produces(400).Produces(404).Produces(409);

api.MapPost("/sessions/{code}/restart", async (string code, ISessionService service) =>
{
    var snap = await service.RestartAsync(code);
    if (snap is null) return Results.NotFound();
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Endpoints");
    logger.LogInformation("Restarted session {Code}", code);
    return Results.Ok();
}).WithName("RestartSession").Produces(200).Produces(404).Produces(409);

// Timing middleware (p95 logging)
var durations = new System.Collections.Concurrent.ConcurrentQueue<long>();
app.Use(async (context, next) =>
{
    var sw = Stopwatch.StartNew();
    await next();
    sw.Stop();
    durations.Enqueue(sw.ElapsedMilliseconds);
    while (durations.Count > 200 && durations.TryDequeue(out _)) { }
    if (durations.Count >= 50)
    {
        var arr = durations.ToArray();
        Array.Sort(arr);
        var p95 = arr[(int)Math.Ceiling(arr.Length * 0.95) - 1];
        var log = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Perf");
        log.LogDebug("Approx p95 across {Count} reqs: {P95}ms", arr.Length, p95);
    }
});

app.Run();
public partial class Program { }
