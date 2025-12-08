using Agentic.GraphRag.Components;
using Agentic.GraphRag.Extensions;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services
    .ConfigureOptions(builder.Configuration)
    .RegisterServices()
    .RegisterBlazorPersistenceServices()
    .RegisterHttpClients()
    .RegisterGraphDatabase()
    .RegisterAIAgentServices();

builder.Services.AddHsts(options =>
{
    options.Preload = false;
    options.MaxAge = TimeSpan.FromDays(60);
});

var app = builder.Build();

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync().ConfigureAwait(false);
