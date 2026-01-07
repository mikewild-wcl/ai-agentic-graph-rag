using Agentic.GraphRag.Shared.Configuration;
using k8s.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OllamaSharp.Models;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Agentic.GraphRag.AppHost.Extensions;

[SuppressMessage("Minor Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "False positive in extensions class")]
[SuppressMessage("Maintainability", "S3398:\"private\" methods called only by inner classes should be moved to those classe", Justification = "False positive in extensions class")]
internal static class AIMultiModelExtensions
{
    extension(IDistributedApplicationBuilder builder)
    {
        public (IResourceBuilder<IResourceWithConnectionString>, IResourceBuilder<IResourceWithConnectionString>) AddAIModels(
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
                //AIProvider.AzureOpenAI => ConfigureAzureOpenAI(builder, name, settings),
                AIProvider.Ollama => ConfigureOllama(builder, name, settings),
                //AIProvider.GitHubModels => ConfigureGitHubModels(builder, settings),
                //AIProvider.AzureAIFoundry or AIProvider.AzureLocalFoundry => ConfigureAzureAIFoundry(builder, name, settings),
                _ => throw new InvalidOperationException(
                    $"Unsupported AI provider: {settings.Provider}")
            };
        }

        //public IResourceBuilder<IResourceWithConnectionString> AddAIEmbeddingModels(
        //    string name = "ai-service")
        //{
        //    var settings = builder.Configuration.GetSection(AISettings.SectionName).Get<AISettings>();

        //    return settings.Provider switch
        //    {
        //        AIProvider.AzureOpenAI => ConfigureAzureOpenAI(builder, name, settings),
        //        AIProvider.Ollama => ConfigureOllama(builder, name, settings),
        //        AIProvider.GitHubModels => ConfigureGitHubModels(builder, settings),
        //        AIProvider.AzureAIFoundry or AIProvider.AzureLocalFoundry => ConfigureAzureAIFoundry(builder, name, settings),
        //        _ => throw new InvalidOperationException($"Unsupported AI provider: {settings.Provider}")
        //    };
        //}

        private ILogger? CreateLogger() =>
            builder.Services
                ?.BuildServiceProvider()
                ?.GetService<ILoggerFactory>()
                ?.CreateLogger($"{typeof(AIModelExtensions).Namespace}.{nameof(AIModelExtensions)}");
    }

    extension(IResourceBuilder<ProjectResource> builder)
    {
        public IResourceBuilder<ProjectResource> WithAISettingsEnvironment()
        {
            var settings = builder.ApplicationBuilder.Configuration.GetSection(AISettings.SectionName).Get<AISettings>();

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

            return builder;
        }

        //public IResourceBuilder<ProjectResource> WithAIModels(
        //    IResourceBuilder<IResourceWithConnectionString> aiService,
        //    string deploymentName = "chat",
        //    string? embeddingDeploymentName = null)
        //{
        //    var settings = aiService.ApplicationBuilder.Configuration.GetSection(AISettings.SectionName).Get<AISettings>();

        //    builder
        //        .WithEnvironment("AI:Provider", settings!.Provider.ToString().ToLowerInvariant())
        //        .WithEnvironment("AI:DeploymentName", deploymentName)
        //        .WithEnvironment("AI:Model", settings.Model);

        //    if (!string.IsNullOrEmpty(settings.EmbeddingDeploymentName))
        //    {
        //        builder
        //            .WithEnvironment("AI:EmbeddingDeploymentName", embeddingDeploymentName)
        //            .WithEnvironment("AI:EmbeddingModel", settings.EmbeddingModel);
        //    }

        //    return ConnectToAIService(builder, aiService, settings.Provider);
        //}

        public IResourceBuilder<ProjectResource> WithAIService(
            IResourceBuilder<IResourceWithConnectionString> aiService)
        {
            var settings = aiService.ApplicationBuilder.Configuration.GetSection(AISettings.SectionName).Get<AISettings>();

            builder
                .WithEnvironment("AI:Provider", settings!.Provider.ToString().ToLowerInvariant());

            return ConnectToAIService(builder, aiService, settings.Provider);
        }

        public IResourceBuilder<ProjectResource> WithAIModel(
            IResourceBuilder<IResourceWithConnectionString> aiService,
            string deploymentName = "chat")
        {
            var settings = aiService.ApplicationBuilder.Configuration.GetSection(AISettings.SectionName).Get<AISettings>();

            return builder
                .WithEnvironment("AI:Provider", settings!.Provider.ToString().ToLowerInvariant())
                .WithEnvironment("AI:DeploymentName", deploymentName)
                .WithEnvironment("AI:Model", settings.Model);
        }

        public IResourceBuilder<ProjectResource> WithAIEmbeddingModel(
            IResourceBuilder<IResourceWithConnectionString> aiService,
            string deploymentName = "embedding")
        {
            var settings = aiService.ApplicationBuilder.Configuration.GetSection(AISettings.SectionName).Get<AISettings>();

            return builder
                .WithEnvironment("AI:EmbeddingDeploymentName", deploymentName)
                .WithEnvironment("AI:EmbeddingModel", settings.EmbeddingModel);
        }
    }

    private static IResourceBuilder<IResourceWithConnectionString> ConfigureAzureOpenAI(
        IDistributedApplicationBuilder builder,
        string name,
        AISettings settings)
    {
        var config = builder.Configuration;
        var modelVersion = config["AI:ModelVersion"] ?? "2024-11-20";
        var skuName = config["AI:SkuName"] ?? "GlobalStandard";
        var skuCapacity = config.GetValue<int?>("AI:SkuCapacity") ?? 150;

        var openai = builder.AddAzureOpenAI(name);
        openai.AddDeployment(settings.DeploymentName, settings.Model, modelVersion);

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

        return openai;
    }

    private static IResourceBuilder<IResourceWithConnectionString> ConfigureGitHubModels(
        IDistributedApplicationBuilder builder,
        AISettings settings)
    {
        //var githubToken = builder.Configuration["GITHUB_TOKEN"] ??
        //         builder.Configuration["ConnectionStrings:GitHubModels"];

        //builder.Services.AddSingleton<IChatClient>(serviceProvider =>
        //{
        //    var openAIClient = new OpenAIClient(
        //        new System.ClientModel.ApiKeyCredential(githubToken),
        //        new OpenAIClientOptions { Endpoint = new Uri("https://models.inference.ai.azure.com") }
        //    );
        //    return openAIClient.GetChatClient(aiSettings.Model).AsIChatClient();
        //});

        //Expects a key with $"{name}-gh-apikey" or a GITHUB_TOKEN environment variable
        var model = builder.AddGitHubModel(settings.DeploymentName, settings.Model)
            .WithHealthCheck();

        //Validation: EmbeddingDeploymentName cannot be DeploymentName here
        var embeddingModel = (!string.IsNullOrEmpty(settings.EmbeddingDeploymentName) && !string.IsNullOrEmpty(settings.EmbeddingModel))
            ? builder.AddGitHubModel(settings.EmbeddingDeploymentName, settings.EmbeddingModel)
                .WithHealthCheck()
            : null;

        return model;
    }

    private static (IResourceBuilder<IResourceWithConnectionString>, IResourceBuilder<IResourceWithConnectionString>) ConfigureOllama(
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

        //TODO: Allow for null, or return a list of models
        var embeddingModel = ollama.AddModel(settings.EmbeddingDeploymentName!, settings.EmbeddingModel!);
        //var embeddingModel = (!string.IsNullOrEmpty(settings.EmbeddingDeploymentName))
        //    ? ollama.AddModel(settings.EmbeddingDeploymentName, settings.EmbeddingModel)
        //    : null;

        return (model, embeddingModel);
    }

    private static IResourceBuilder<IResourceWithConnectionString> ConfigureOllamaEmbedding(
        IDistributedApplicationBuilder builder,
        string name,
        AISettings settings)
    {
        var config = builder.Configuration;
        var configuredVendor = config["AI:OllamaGpuVendor"];

        var ollama = builder.AddOllama(name)
            .WithDataVolume()
            .WithOpenWebUI();

        if (configuredVendor is not null && Enum.TryParse<OllamaGpuVendor>(configuredVendor, out var gpuVendor))
        {
            ollama!.WithGPUSupport(gpuVendor);
        }

        //var model = ollama.AddModel(settings.DeploymentName, settings.Model);
        var embeddingModel = (!string.IsNullOrEmpty(settings.EmbeddingDeploymentName))
            ? ollama.AddModel(settings.EmbeddingDeploymentName, settings.EmbeddingModel)
            : null;

        //return model;
        return ollama;
    }

    //private static IResourceBuilder<IResourceWithConnectionString> ConfigureGitHubModels(
    //    IDistributedApplicationBuilder builder,
    //    AISettings settings)
    //{
    //    return builder.AddGitHubModel(settings.DeploymentName, settings.Model)
    //                  .WithHealthCheck();
    //}

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

    private static IResourceBuilder<ProjectResource> ConnectToAIService(
        IResourceBuilder<ProjectResource> projectBuilder,
        IResourceBuilder<IResourceWithConnectionString> aiService,
        AIProvider provider)
    {
        return provider switch
        {
            AIProvider.AzureOpenAI => projectBuilder.WithReference(aiService).WaitFor(aiService),
            AIProvider.GitHubModels => projectBuilder.WithReference(aiService).WaitFor(aiService),
            AIProvider.AzureAIFoundry => projectBuilder.WithReference(aiService).WaitFor(aiService),
            AIProvider.AzureLocalFoundry => projectBuilder.WithReference(aiService).WaitFor(aiService),
            AIProvider.Ollama => projectBuilder
                .WithReference(aiService, connectionName: aiService.Resource.Name)
                .WaitFor(aiService),
            _ => throw new InvalidOperationException($"Unknown provider: {provider}")
        };
    }
}
