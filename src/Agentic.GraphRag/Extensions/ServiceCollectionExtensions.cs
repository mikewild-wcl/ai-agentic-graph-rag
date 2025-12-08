using Agentic.GraphRag.Application.Chunkers;
using Agentic.GraphRag.Application.Chunkers.Interfaces;
using Agentic.GraphRag.Application.EinsteinQuery;
using Agentic.GraphRag.Application.EinsteinQuery.Interfaces;
using Agentic.GraphRag.Application.Movies;
using Agentic.GraphRag.Application.Movies.Interfaces;
using Agentic.GraphRag.Application.Services;
using Agentic.GraphRag.Application.Services.Interfaces;
using Agentic.GraphRag.Application.Settings;
using Agentic.GraphRag.Components;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using System.ClientModel;

namespace Agentic.GraphRag.Extensions;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection ConfigureOptions(this IServiceCollection services, IConfiguration configuration) =>
        services
            .Configure<AzureOpenAISettings>(configuration.GetSection(AzureOpenAISettings.SectionName))
            .Configure<DownloadSettings>(configuration.GetSection(DownloadSettings.SectionName))
            .Configure<EinsteinQuerySettings>(configuration.GetSection(EinsteinQuerySettings.SectionName))
            .Configure<GraphDatabaseSettings>(configuration.GetSection(GraphDatabaseSettings.SectionName))
            ;

    internal static IServiceCollection RegisterAIAgentServices(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IOptions<AzureOpenAISettings>>().Value;
            return new AzureOpenAIClient(
                new Uri(config.Endpoint),
                new ApiKeyCredential(config.ApiKey));
        });

        services.AddSingleton(
            new ChatClientAgentOptions
            {
                Name = "Joker",
                ChatOptions = new() { Instructions = "You are good at telling jokes." }
            });

        services.AddKeyedChatClient(ServiceKeys.AzureOpenAIChatClient, sp =>
        {
            var config = sp.GetRequiredService<IOptions<AzureOpenAISettings>>().Value;
            var client = sp.GetRequiredService<AzureOpenAIClient>();

            return client
                .GetChatClient(config.DeploymentName)                
                .AsIChatClient();
        });

        services.AddScoped(sp =>
        {
            var config = sp.GetRequiredService<IOptions<AzureOpenAISettings>>().Value;
            var client = sp.GetRequiredService<AzureOpenAIClient>();

            return client.GetEmbeddingClient(config.EmbeddingDeploymentName).AsIEmbeddingGenerator();
        });

        services.AddSingleton<AIAgent>(sp => new ChatClientAgent(
           chatClient: sp.GetRequiredKeyedService<IChatClient>("AzureOpenAI"),
           options: sp.GetRequiredService<ChatClientAgentOptions>()));
        
        return services;
    }

    internal static IServiceCollection RegisterServices(this IServiceCollection services) =>
        services
            .AddScoped<IMoviesDataAccess, MoviesDataAccess>()
            .AddScoped<IMoviesQueryService, MoviesQueryService>()
            .AddScoped<IEinsteinDataIngestionService, EinsteinDataIngestionService>()
            .AddScoped<IEinsteinQueryService, EinsteinQueryService>()        
            .AddScoped<IEinsteinQueryDataAccess, EinsteinDataAccess>()
            .AddTransient<IDocumentChunker, PdfDocumentChunker>()
            ;

    internal static IServiceCollection RegisterHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient<IDownloadService, DownloadService>(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "Agentic.GraphRag-Downloader");
            client.Timeout = Timeout.InfiniteTimeSpan; /* Timeout is handled by resilience policies */
        })
            .AddStandardResilienceHandler()
            .Configure((options, sp) =>
            {
                var settings = sp.GetRequiredService<IOptions<DownloadSettings>>().Value;
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(settings.Timeout);
            });

        return services;
    }

    internal static IServiceCollection RegisterBlazorPersistenceServices(this IServiceCollection services) =>
        services /* Add services to persist state across navigations (per circuit/session) */
            .AddScoped<CounterState>()
            .AddScoped<MoviesState>()
            .AddScoped<EinsteinState>();

    internal static IServiceCollection RegisterGraphDatabase(this IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<GraphDatabaseSettings>>().Value;

        using var loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole());
        var buildLogger = loggerFactory.CreateLogger<Program>();

#pragma warning disable CA1848 // Use the LoggerMessage delegates
        buildLogger.LogInformation("Configuring Graph database with Connection: {Connection}, User: {User}",
            options.Connection,
            options.User);

        if (options.Connection is null)
        {
            buildLogger.LogInformation("Graph database connection string is not configured. It should be set up in GraphDatabase:Connection");
            return services; // or throw a configuration exception
        }
#pragma warning restore CA1848 // Use the LoggerMessage delegates

        services.AddSingleton(sp =>
        {
            return GraphDatabase.Driver(options.Connection, AuthTokens.Basic(options.User, options.Password));
        });

        return services;
    }
}
