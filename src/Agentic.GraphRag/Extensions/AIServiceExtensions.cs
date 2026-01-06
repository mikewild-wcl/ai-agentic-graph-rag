using Agentic.GraphRag.Application.Settings;
using Agentic.GraphRag.Shared.Configuration;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.ClientModel;
using System.Diagnostics.CodeAnalysis;

namespace Agentic.GraphRag.Extensions;

internal static class AIServiceExtensions
{
    /// <summary>
    /// Configures AI services based on appsettings.json configuration.
    /// </summary>
    internal static IHostApplicationBuilder AddAIServices(this IHostApplicationBuilder builder)
    {
        //var aiSettings = AIConfiguration.GetSettings(builder.Configuration);
        var aiSettings = builder.Configuration.GetSection(AISettings.SectionName).Get<AISettings>();

        if (aiSettings is null)
        {
            throw new InvalidOperationException("AI settings are not configured properly.");
        }

        builder.Services.AddSingleton(aiSettings);

        builder.AddAIProvider(aiSettings);

        builder.Services.AddAIAgentServices();

        builder.Services.AddHostedService<AIStartupLogger>();

        return builder;
    }

    internal static IServiceCollection AddAIAgentServices(this IServiceCollection services)
    {
        //services.AddSingleton(sp =>
        //{
        //    var config = sp.GetRequiredService<IOptions<AzureOpenAISettings>>().Value;
        //    return new AzureOpenAIClient(
        //        new Uri(config.Endpoint),
        //        new ApiKeyCredential(config.ApiKey));
        //});

        //services.AddSingleton(
        //    new ChatClientAgentOptions
        //    {
        //        Name = "Joker",
        //        ChatOptions = new() { Instructions = "You are good at telling jokes." }
        //    });

        //services.AddKeyedChatClient(ServiceKeys.AzureOpenAIChatClient, sp =>
        //{
        //    var config = sp.GetRequiredService<IOptions<AzureOpenAISettings>>().Value;
        //    var client = sp.GetRequiredService<AzureOpenAIClient>();

        //    return client
        //        .GetChatClient(config.DeploymentName)
        //        .AsIChatClient();
        //});

        services.AddKeyedChatClient(ServiceKeys.DefaultAIChatClient, sp =>
        {
            var config = sp.GetRequiredService<IOptions<AISettings>>().Value;
            var client = sp.GetRequiredService<AzureOpenAIClient>();

            return client
                .GetChatClient(config.DeploymentName)
                .AsIChatClient();
        });

        services.AddScoped(sp =>
        {
            var config = sp.GetRequiredService<IOptions<AISettings>>().Value;
            var client = sp.GetRequiredService<AzureOpenAIClient>();

            return client.GetEmbeddingClient(config.EmbeddingDeploymentName).AsIEmbeddingGenerator();
        });

        //services.AddSingleton<AIAgent>(sp => new ChatClientAgent(
        //   chatClient: sp.GetRequiredKeyedService<IChatClient>("AzureOpenAI"),
        //   options: sp.GetRequiredService<ChatClientAgentOptions>()));

        return services;
    }

    private static IHostApplicationBuilder AddAIProvider(
        this IHostApplicationBuilder builder,
        AISettings aiSettings)
    {
        switch (aiSettings.Provider)
        {
            case AIProvider.Ollama:
                //var ollamaClient = builder.AddOllamaApiClient(aiSettings.DeploymentName);
                //ollamaClient.AddChatClient();
                //ollamaClient.AddEmbeddingGenerator();
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
                gitHubClient.AddChatClient();
                gitHubClient.AddEmbeddingGenerator();
                break;

            case AIProvider.AzureAIFoundry:
            case AIProvider.AzureLocalFoundry:
                var foundryClient = builder.AddAzureChatCompletionsClient(aiSettings.DeploymentName);
                foundryClient.AddChatClient();
                //foundryClient.AddEmbeddingGenerator();
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
