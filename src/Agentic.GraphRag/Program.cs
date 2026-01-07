using Agentic.GraphRag.Components;
using Agentic.GraphRag.Extensions;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.ConfigureOptions()
    .DumpConfiguration();

builder.AddAIServices();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services
    .RegisterServices()
    .RegisterBlazorPersistenceServices()
    .RegisterHttpClients()
    .RegisterGraphDatabase();

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
