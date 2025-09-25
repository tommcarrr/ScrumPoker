using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ScrumPoker.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped<SessionHubClient>();
builder.Services.AddScoped<SessionState>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();