using Agentic.GraphRag.Logging;
using Agentic.GraphRag.Shared.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace Agentic.GraphRag.Extensions;

[SuppressMessage("Minor Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "False positive in extensions class")]
internal static class AIServiceExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        internal IHostApplicationBuilder AddAIServices()
        {
            var aiSettings = builder.Configuration.GetSection(AISettings.SectionName).Get<AISettings>() 
                ?? throw new InvalidOperationException("AI settings are not configured properly.");

            builder.Services.AddSingleton(aiSettings);

            builder.AddAIProvider(aiSettings);

            builder.Services.AddHostedService<AIStartupLogger>();

            return builder;
        }

        private IHostApplicationBuilder AddAIProvider(AISettings aiSettings)
        {
            switch (aiSettings.Provider)
            {
                case AIProvider.Ollama:
                    builder.AddOllamaApiClient(aiSettings.DeploymentName)
                        .AddChatClient();
                    if (!string.IsNullOrEmpty(aiSettings.EmbeddingDeploymentName))
                    {
                        builder.AddOllamaApiClient(aiSettings.EmbeddingDeploymentName)
                            .AddEmbeddingGenerator();
                    }
                    break;

                case AIProvider.AzureOpenAI:
                    var azureClient = builder.AddAzureOpenAIClient("ai-service");
                    azureClient.AddChatClient();
                    if (!string.IsNullOrEmpty(aiSettings.EmbeddingDeploymentName))
                    {
                        azureClient.AddEmbeddingGenerator();
                    }
                    break;

                case AIProvider.GitHubModels:
                    var gitHubClient = builder.AddOpenAIClient(aiSettings.DeploymentName);
                    gitHubClient.AddChatClient();
                    if (!string.IsNullOrEmpty(aiSettings.EmbeddingDeploymentName))
                    {
                        var gitHubEmbeddingClient = builder.AddOpenAIClient(aiSettings.EmbeddingDeploymentName);
                        gitHubEmbeddingClient.AddEmbeddingGenerator();
                    }
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
}
