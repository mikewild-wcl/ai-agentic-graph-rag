using Agentic.GraphRag.Shared.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace Agentic.GraphRag.Extensions;

internal static class AIServiceExtensions
{
    internal static IHostApplicationBuilder AddAIServices(this IHostApplicationBuilder builder)
    {
        var aiSettings = builder.Configuration.GetSection(AISettings.SectionName).Get<AISettings>();

        if (aiSettings is null)
        {
            throw new InvalidOperationException("AI settings are not configured properly.");
        }

        builder.Services.AddSingleton(aiSettings);

        builder.AddAIProvider(aiSettings);

        builder.Services.AddHostedService<AIStartupLogger>();

        return builder;
    }

    private static IHostApplicationBuilder AddAIProvider(
        this IHostApplicationBuilder builder,
        AISettings aiSettings)
    {
        switch (aiSettings.Provider)
        {
            case AIProvider.Ollama:
                builder.AddOllamaApiClient(aiSettings.DeploymentName)
                    .AddChatClient();
                builder.AddOllamaApiClient(aiSettings.EmbeddingDeploymentName)
                    .AddEmbeddingGenerator();
                break;

            case AIProvider.AzureOpenAI:
                var azureClient = builder.AddAzureOpenAIClient("ai-service");
                azureClient.AddChatClient();
                azureClient.AddEmbeddingGenerator();
                break;

            case AIProvider.GitHubModels:
                var gitHubClient = builder.AddOpenAIClient(aiSettings.DeploymentName);
                gitHubClient.AddChatClient(aiSettings.DeploymentName);
                gitHubClient.AddEmbeddingGenerator(aiSettings.EmbeddingDeploymentName);
                break;

            case AIProvider.AzureAIFoundry:
            case AIProvider.AzureLocalFoundry:
                var foundryClient = builder.AddAzureChatCompletionsClient(aiSettings.DeploymentName);
                foundryClient.AddChatClient();
                //foundryClient.AddEmbeddingGenerator(); //Not available yet
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported AI provider: {aiSettings.Provider}");
        }

        return builder;
    }
}

/// <summary>
/// Logs AI configuration at startup.
/// </summary>
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
