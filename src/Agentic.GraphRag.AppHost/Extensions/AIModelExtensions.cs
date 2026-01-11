using Agentic.GraphRag.Shared.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Agentic.GraphRag.AppHost.Extensions;

[SuppressMessage("Minor Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "False positive in extensions class")]
[SuppressMessage("Maintainability", "S3398:\"private\" methods called only by inner classes should be moved to those classe", Justification = "False positive in extensions class")]
internal static class AIModelExtensions
{
    public static void ConfiguringAI(this ILogger logger, AIProvider provider, string deployment, string model, string embeddingDeployment, string embeddingModel) =>
    _logConfiguringAI(logger, provider, deployment, model, embeddingDeployment, embeddingModel, default!);

    private static readonly Action<ILogger, AIProvider, string, string, string, string, Exception> _logConfiguringAI =
        LoggerMessage.Define<AIProvider, string, string, string, string>(
            LogLevel.Information,
            new EventId(1, nameof(AIModelExtensions)),
            """
            Configuring AI for provider: {Provider}, 
            Deployment: {DeploymentName}, Model: {Model}, 
            Embedding Deployment: {EmbeddingDeployment}, Model: {EmbeddingModel}.
            """);

    extension(IDistributedApplicationBuilder builder)
    {
        public (IResourceBuilder<IResourceWithConnectionString>, IResourceBuilder<IResourceWithConnectionString>?) AddAIModels(
            string name = "ai-service")
        {
            var settings = builder.Configuration.GetSection(AISettings.SectionName).Get<AISettings>();

            if (settings == null)
            {
                throw new InvalidOperationException("AI settings mot found. Please add AiSettings to the configuration.");
            }

            var logger = builder.CreateLogger();
            logger?.ConfiguringAI(
                settings.Provider,
                settings.DeploymentName,
                settings.Model,
                settings.EmbeddingDeploymentName ?? string.Empty,
                settings.EmbeddingModel ?? string.Empty);

            return settings.Provider switch
            {
                AIProvider.AzureOpenAI => ConfigureAzureOpenAI(builder, name, settings),
                AIProvider.Ollama => ConfigureOllama(builder, name, settings),
                AIProvider.GitHubModels => ConfigureGitHubModels(builder, settings),
                AIProvider.AzureAIFoundry or AIProvider.AzureLocalFoundry => new(ConfigureAzureAIFoundry(builder, name, settings), default!),
                _ => throw new InvalidOperationException(
                    $"Unsupported AI provider: {settings.Provider}")
            };
        }

        private ILogger? CreateLogger() =>
            builder.Services
                ?.BuildServiceProvider()
                ?.GetService<ILoggerFactory>()
                ?.CreateLogger($"{typeof(AIModelExtensions).Namespace}.{nameof(AIModelExtensions)}");
    }

    extension(IResourceBuilder<ProjectResource> builder)
    {
        public IResourceBuilder<ProjectResource> WithAIModels(
            IResourceBuilder<IResourceWithConnectionString> chat,
            IResourceBuilder<IResourceWithConnectionString>? embedding,
            string deploymentName = "chat",
            string embeddingDeploymentName = "embedding")
        {
            var settings = builder.ApplicationBuilder.Configuration.GetSection(AISettings.SectionName).Get<AISettings>();

            //TODO: Validate that the defaults of "chat" and "embedding" correspond to settings.DeploymentName and settings.EmbeddingDeploymentName
            builder
                .WithEnvironment("AI:Provider", settings!.Provider.ToString().ToLowerInvariant())
                .WithEnvironment("AI:DeploymentName", settings.DeploymentName)
                .WithEnvironment("AI:Model", settings.Model)
                .WithEnvironment("AI:EmbeddingDeploymentName", settings.EmbeddingDeploymentName)
                .WithEnvironment("AI:EmbeddingModel", settings.EmbeddingModel);

            if (settings.Timeout is not null)
            {
                builder
                    .WithEnvironment("AI:Timeout", settings.Timeout.Value.ToString(CultureInfo.InvariantCulture));
            }

            return settings.Provider switch
            {
                AIProvider.AzureLocalFoundry => builder.WithReference(chat).WaitFor(chat),
                AIProvider.AzureAIFoundry or
                AIProvider.AzureOpenAI or
                AIProvider.GitHubModels or
                AIProvider.Ollama => builder
                    .WithReference(chat)
                    .WaitFor(chat)
                    .ReferenceAndWaitForResourceIfNotNull(embedding),
                _ => throw new InvalidOperationException($"Unknown provider: {settings.Provider}")
            };
        }

        private IResourceBuilder<ProjectResource> ReferenceAndWaitForResourceIfNotNull(
            IResourceBuilder<IResourceWithConnectionString>? resource) =>
            resource is not null
                ? builder
                    .WithReference(resource)
                    .WaitFor(resource)
                : builder;
    }

    private static (IResourceBuilder<IResourceWithConnectionString>, IResourceBuilder<IResourceWithConnectionString>?) ConfigureAzureOpenAI(
        IDistributedApplicationBuilder builder,
        string name,
        AISettings settings)
    {
        var config = builder.Configuration;
        var modelVersion = config["AI:ModelVersion"] ?? "2024-11-20";
        var skuName = config["AI:SkuName"] ?? "GlobalStandard";
        var skuCapacity = config.GetValue<int?>("AI:SkuCapacity") ?? 150;


        var openai = builder.AddAzureOpenAI(name);
        var model = openai.AddDeployment(settings.DeploymentName, settings.Model, modelVersion);

        openai.ConfigureInfrastructure(infra =>
        {
            var deployments = infra.GetProvisionableResources()
                .OfType<Azure.Provisioning.CognitiveServices.CognitiveServicesAccountDeployment>();

            foreach (var deployment in deployments)
            {
                deployment.Sku = new Azure.Provisioning.CognitiveServices.CognitiveServicesSku
                {
                    Name = skuName,
                    Capacity = skuCapacity
                };
            }
        });

        IResourceBuilder<IResourceWithConnectionString>? embeddingModel = null;
        if (settings.HasEmbeddingDeployment)
        {
            embeddingModel = openai.AddDeployment(settings.EmbeddingDeploymentName!, settings.EmbeddingModel!, modelVersion)
                .WithProperties(p =>
                {
                    p.SkuName = skuName;
                    p.SkuCapacity = skuCapacity;
                });
        }

        return (model, embeddingModel);
    }

    private static (IResourceBuilder<IResourceWithConnectionString>, IResourceBuilder<IResourceWithConnectionString>?) ConfigureGitHubModels(
        IDistributedApplicationBuilder builder,
        AISettings settings)
    {
        //Expects a config value in secrets called $"{name}-gh-apikey" or a GITHUB_TOKEN environment variable
        var model = builder.AddGitHubModel(settings.DeploymentName, settings.Model)
            /* .WithHealthCheck() // Only include health checks when debugging connectivity issues; they are included in the rate limit */
            ;

        /* Validation: EmbeddingDeploymentName cannot be DeploymentName here */
        var embeddingModel = settings.HasEmbeddingDeployment
            ? builder.AddGitHubModel(settings.EmbeddingDeploymentName!, settings.EmbeddingModel!)
            : null;

        return (model, embeddingModel);
    }

    private static (IResourceBuilder<IResourceWithConnectionString>, IResourceBuilder<IResourceWithConnectionString>?) ConfigureOllama(
        IDistributedApplicationBuilder builder,
        string name,
        AISettings settings)
    {
        var config = builder.Configuration;
        var configuredVendor = config["AI:OllamaGpuVendor"];

        var ollama = builder.AddOllama(name)
            .WithDataVolume();

        if (configuredVendor is not null && Enum.TryParse<OllamaGpuVendor>(configuredVendor, out var gpuVendor))
        {
            ollama!.WithGPUSupport(gpuVendor);
        }

        ollama.WithOpenWebUI();

        var model = ollama.AddModel(settings.DeploymentName, settings.Model);

        var embeddingModel = settings.HasEmbeddingDeployment
            ? ollama.AddModel(settings.EmbeddingDeploymentName!, settings.EmbeddingModel!)
            : null;

        return (model, embeddingModel);
    }

    private static IResourceBuilder<IResourceWithConnectionString> ConfigureAzureAIFoundry(
        IDistributedApplicationBuilder builder,
        string name,
        AISettings settings)
    {
        var config = builder.Configuration;
        var isLocal = config["AI:Provider"]?.ToLowerInvariant() == "foundrylocal";
        var format = config["AI:ModelFormat"] ?? config["AI:ModelVendor"] ?? "Microsoft";
        var version = config["AI:ModelVersion"] ?? "1";
        var skuCapacity = config.GetValue<int?>("AI:SkuCapacity") ?? 20;

        Console.WriteLine(
            $"Azure AI Foundry: {(isLocal ? "Local" : "Cloud")}, " +
            $"Model: {settings.Model}, Version: {version}");

        var foundry = builder.AddAzureAIFoundry(name);
        if (isLocal)
        {
            foundry = foundry.RunAsFoundryLocal();
        }

        return foundry
            .AddDeployment(settings.DeploymentName, settings.Model, version, format)
            .WithProperties(p => p.SkuCapacity = skuCapacity);
    }

    extension(AISettings settings)
    {
        public bool HasEmbeddingDeployment =>
            !string.IsNullOrEmpty(settings.EmbeddingDeploymentName) &&
            !string.IsNullOrEmpty(settings.EmbeddingModel);
    }
}
