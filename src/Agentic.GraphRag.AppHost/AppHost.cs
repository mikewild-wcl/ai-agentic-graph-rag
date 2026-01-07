using Agentic.GraphRag.AppHost.Extensions;
using Agentic.GraphRag.AppHost.ParameterDefaults;
using Agentic.GraphRag.Shared;

var builder = DistributedApplication.CreateBuilder(args);

var graphDBProvider = builder.AddParameter($"{ResourceNames.GraphDatabaseSection}-{ResourceNames.Provider}");
var graphDBConnection = builder.AddParameter($"{ResourceNames.GraphDatabaseSection}-{ResourceNames.Connection}");
var createGraphDBInDocker = builder.AddParameter($"{ResourceNames.GraphDatabaseSection}-{ResourceNames.CreateInDocker}");
var graphDBDockerContainerName = builder.AddParameter($"{ResourceNames.GraphDatabaseSection}-{ResourceNames.DockerContainerName}", value: new EmptyStringParameterDefault());
var graphDBUser = builder.AddParameter($"{ResourceNames.GraphDatabaseSection}-{ResourceNames.User}");
var graphDBPassword = builder.AddParameter($"{ResourceNames.GraphDatabaseSection}-{ResourceNames.Password}", secret: true);
var einsteinVectorDb = builder.AddParameter($"{ResourceNames.GraphDatabaseSection}-{ResourceNames.EinsteinVectorDb}");
var moviesDb = builder.AddParameter($"{ResourceNames.GraphDatabaseSection}-{ResourceNames.MoviesDb}");
var ufoDb = builder.AddParameter($"{ResourceNames.GraphDatabaseSection}-{ResourceNames.UfoDb}");

var azureOpenAIEndpoint = builder.AddParameter($"{ResourceNames.AzureOpenAISection}-{ResourceNames.Endpoint}");
var azureOpenAIApiKey = builder.AddParameter($"{ResourceNames.AzureOpenAISection}-{ResourceNames.ApiKey}", secret: true);
var azureOpenAIDeploymentName = builder.AddParameter($"{ResourceNames.AzureOpenAISection}-{ResourceNames.DeploymentName}");
var azureOpenAIEmbeddingDeploymentName = builder.AddParameter($"{ResourceNames.AzureOpenAISection}-{ResourceNames.EmbeddingDeploymentName}");
var azureOpenAITimeout = builder.AddParameter($"{ResourceNames.AzureOpenAISection}-{ResourceNames.Timeout}");

var downloadDirectory = builder.AddParameter($"{ResourceNames.DownloadSection}-{ResourceNames.DownloadDirectory}");
var einsteinDocumentFileName = builder.AddParameter($"{ResourceNames.EinsteinQuerySection}-{ResourceNames.DocumentFileName}");
var einsteinDocumentUri = builder.AddParameter($"{ResourceNames.EinsteinQuerySection}-{ResourceNames.DocumentUri}");

var addDockerContainers =
    bool.TryParse(createGraphDBInDocker.GetValue(), out var createInDocker)
    && createInDocker;

if (addDockerContainers && graphDBProvider.GetValue() == "neo4j")
{
    var neo4jContainer = builder.AddDockerfile(
        "neo4j", "./", "Dockerfile")      
        .WithEndpoint(7474, scheme: "http", targetPort: 7474)
        .WithEndpoint(7687, scheme: "bolt", targetPort: 7687)
        .WithEnvironment("NEO4J_AUTH", $"{graphDBUser.GetValue()}/{graphDBPassword.GetValue()}")
        .WithLifetime(ContainerLifetime.Persistent);

    var containerName = graphDBDockerContainerName.GetValue();
    if (!string.IsNullOrEmpty(containerName))
    {
        neo4jContainer.WithContainerName(containerName);
    }
}
else if (addDockerContainers && graphDBProvider.GetValue() == "memgraph")
{
    // Compose memgraph 
}

var (chatModel, embeddingModel) = builder.AddAIModels("ai-service");
//var aiService = builder.AddAIModels("ai-service");

builder.AddProject<Projects.Agentic_GraphRag>(ProjectNames.GraphRagBlazorApp)
    //.WithAIModels(aiService, "chat", "embedding")
    .WithAISettingsEnvironment()
    .WithReference(chatModel)
    .WithReference(embeddingModel)
    .WithEnvironment($"{ResourceNames.GraphDatabaseSection}:{ResourceNames.Provider}", graphDBProvider)
    .WithEnvironment($"{ResourceNames.GraphDatabaseSection}:{ResourceNames.Connection}", graphDBConnection)
    .WithEnvironment($"{ResourceNames.GraphDatabaseSection}:{ResourceNames.User}", graphDBUser)
    .WithEnvironment($"{ResourceNames.GraphDatabaseSection}:{ResourceNames.Password}", graphDBPassword)
    .WithEnvironment($"{ResourceNames.GraphDatabaseSection}:{ResourceNames.EinsteinVectorDb}", einsteinVectorDb)
    .WithEnvironment($"{ResourceNames.GraphDatabaseSection}:{ResourceNames.MoviesDb}", moviesDb)
    .WithEnvironment($"{ResourceNames.GraphDatabaseSection}:{ResourceNames.UfoDb}", ufoDb)
    //.WithEnvironment($"{ResourceNames.AzureOpenAISection}:{ResourceNames.Endpoint}", azureOpenAIEndpoint)
    //.WithEnvironment($"{ResourceNames.AzureOpenAISection}:{ResourceNames.ApiKey}", azureOpenAIApiKey)
    //.WithEnvironment($"{ResourceNames.AzureOpenAISection}:{ResourceNames.DeploymentName}", azureOpenAIDeploymentName)
    //.WithEnvironment($"{ResourceNames.AzureOpenAISection}:{ResourceNames.EmbeddingDeploymentName}", azureOpenAIEmbeddingDeploymentName)
    //.WithEnvironment($"{ResourceNames.AzureOpenAISection}:{ResourceNames.Timeout}", azureOpenAITimeout)
    .WithEnvironment($"{ResourceNames.DownloadSection}:{ResourceNames.DownloadDirectory}", downloadDirectory)
    .WithEnvironment($"{ResourceNames.EinsteinQuerySection}:{ResourceNames.DocumentFileName}", einsteinDocumentFileName)
    .WithEnvironment($"{ResourceNames.EinsteinQuerySection}:{ResourceNames.DocumentUri}", einsteinDocumentUri)
    ;

await builder.Build().RunAsync().ConfigureAwait(false);