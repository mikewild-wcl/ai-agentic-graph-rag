using Agentic.GraphRag.Extensions;
using Agentic.GraphRag.Shared.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace Agentic.GraphRag.Logging;

internal sealed class AIStartupLogger(
    ILogger<AIStartupLogger> logger,
    AISettings settings,
    IConfiguration configuration) : IHostedService
{
    [SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "This method is only called at startup")]
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (settings.Provider == AIProvider.AzureOpenAI)
        {
            var hasConnection = !string.IsNullOrEmpty(configuration.GetConnectionString("ai-service"));
            logger.LogInformation(
                "AI: {Provider}, Deployment: {Deployment}, Model: {Model}, Connected: {HasConnection}",
                settings.Provider, settings.DeploymentName, settings.Model, hasConnection);
        }
        else
        {
            logger.LogInformation(
                "AI: {Provider}, Deployment: {Deployment}, Model: {Model}",
                settings.Provider, settings.DeploymentName, settings.Model);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
