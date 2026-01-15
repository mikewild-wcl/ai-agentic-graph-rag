# ai-agentic-graph-rag
A space for learning GraphRAG with Semantic Kernel neo4j.

## Aspire Parameters

Parameters in the Aspire appsettings.json file `Parameters` section
follow a convention of `SectionName-SettingName` with the hyphen acting as a separator.
The same pattern is used when passing the resources , with : used as the seperator.

## AI settings

AI is configured to use one of several providers. This is based on https://chris-ayers.com/2025/07/06/aspire-with-lots-of-ai/ 
and the corresponding git repository https://github.com/codebytes/build-with-aspire.

See also [AI integrations compatibility matrix](https://aspire.dev/integrations/cloud/azure/ai-compatibility-matrix/). 

To set this up:

- Add model integration nuget packages to the AppHost project.
  - Aspire.Hosting.GitHub.Models
  - Aspire.Hosting.Azure.AIFoundry
  - Aspire.Hosting.Azure.CognitiveServices
  - CommunityToolkit.Aspire.Hosting.Ollama

- Add appSettings.<environment>.json files with the relevant settings for each provider. appsettings.Development.json can be used as the default.
  - appsettings.AzureOpenAI.json
  - appsettings.Development.json
  - appsettings.GitHubModels.json
  - appsettings.Ollama.json

- Change Properties\launchSettings.json to include profiles for each AI provider. Those profiles can be copied from the existing https profile,
  and the ASPNETCORE_ENVIRONMENT and DOTNET_ENVIRONMENT variables changed to match the <environment> on the appSettings files. 
  This will allow the project to be run in Visual Studio using different AI providers by selecting the relevant profile.

- Add an extension class AIModelExtensions to the AppHost project. This will load the relevant AI model based on the configured provider.
- Modify Program.cs in the AppHost project to use the AIModelExtensions class to add the AI model to the service collection.
- ```
    var aiService = builder.AddAIModel();
    var aiService = builder.AddAIEmbeddingModel();
    var (aiModel, aiEmbeddingModel) = builder.AddAIEmbeddingModel(); 
  ```
 
- For Github Models, make sure to add your GitHub API key either as a parameter in user secrets `{name}-gh-apikey` where `name` is 
  the name of the deployment(s) in your configuration (e.g. chat-gh-apikey embedding-gh-apikey), 
  or in the GITHUB_TOKEN environment variable.

- On the client side, add the following nuget packages:
    - Aspire.Azure.AI.OpenAI
    - Aspire.Azure.AI.Inference
    - CommunityToolkit.Aspire.Client.Ollama

## Resilience

Timeouts can occur occur with Ollama models because generating responses or loading large models can take 
longer than the default 10-second resilience timeout in .NET Aspire or the 5-minute default model unloading time in Ollama.
To address this a new policy extension `AddOllamaResilienceHandler` has been added and applied in 
`AIServiceExtensions.AddAIProvider` when Ollama is used. 

See this [issue](https://github.com/dotnet/extensions/issues/6331).

For GitHub models, 429 (too many requests) responses can occur when rate limits are exceeded. EinsteinDataIngestionService has been 
extended to use a resilience pipeline when generating embeddings.

## Graph database - Neo4j

How to get started with Neo4j see [Build applications with Neo4j and .NET](https://neo4j.com/docs/dotnet-manual/current/). 
For a suggested approach to dependency injection see [Neo4j Data Access for Your .NET Core C# Microservice](https://neo4j.com/blog/developer/neo4j-data-access-for-your-dot-net-core-c-microservice/).

Neo4j can be run locally using Docker. See https://neo4j.com/docs/operations-manual/current/docker/introduction/

A dockerfile is included in the AppHost project and this will start Neo4j when the application starts.

To start Neo4j in docker from a command line use the following (*backticks used for multi-line commands in Windows Terminal*):
```
docker volume create neo4j_data `
  && docker volume create neo4j_logs `
  && docker volume create neo4j_import `
  && docker volume create neo4j_plugins

docker run -d --name neo4j-agentic-graphrag `
  -p 7474:7474 -p 7687:7687 `
  -v neo4j_data:/data `
  -v neo4j_logs:/logs `
  -v neo4j_import:/import `
  -v neo4j_plugins:/plugins `
  -e NEO4J_AUTH=neo4j/password `
  -e 'NEO4J_PLUGINS=["apoc", "apoc-extended", "graph-data-science"]' `
  -e apoc.import.file.enabled=true `
  -e apoc.import.file.use_neo4j_config=false `
  neo4j:2025.09.0
```

The Neo4j browser can be accessed at http://localhost:7474/browser/. Use the username `neo4j` and default password `password`.

Delete data by running this in the Neo4j data browser:
```
MATCH (n) DETACH DELETE n;
CALL apoc.schema.assert({},{},true) YIELD label, key RETURN *
```

Some of the code in this project was inspired by the book Essential GraphRAG. The code can be found at https://github.com/tomasonjo/kg-rag. 
Datasets for the author's earlier book on graphs can be found in https://github.com/tomasonjo/graphs-network-science.

#### Setting up Movies data

Movies data can be added by opening the `ai-agentic-graph-rag\notebooks` folder in vs code and running notebook `Neo4j load movies.dib`.

Note that the first step in the notebook copies files into the docker container; if the copy fails check that the container is running and the name of the container in the `cp` command is correct.
The container name can be set in the appSettings parameters - for development this is `"GraphDatabase-DockerContainerName": "neo4j-agentic-graphrag"`; if no value is set the AppHost will create a default name.

### Cypher

To set a parameter in Cypher in the neo4j browser:
```
:param name => 'Tom Hanks'
```

You can then use the parameter in a query:
```
MATCH (a:Person {name: $name})-[:ACTED_IN]->(m:Movie)
RETURN m.title AS movieTitle
```

## Graph database - Memgraph

If using Memgraph as a replacement for Neo4j, see:

- https://stackoverflow.com/questions/74528361/how-can-i-connect-to-memgraph-from-my-c-sharp-application
- https://memgraph.com/blog/how-to-build-a-flight-network-analysis-graph-asp-net-application-with-memgraph-c-sharp-and-d3-js

## Docker

See [Add Dockerfiles](https://learn.microsoft.com/en-us/dotnet/aspire/app-host/withdockerfile#add-a-dockerfile-to-the-app-model) 
for details on using Docker files with the AppHost project.

When the project was first run the dockerfile failed to build. A simple file was added to see if it would build and run; this
file has been renamed to `Dockerfile_simple_python` and it loads and runs a simple python script `main.py`.

https://neo4j.com/docs/operations-manual/current/docker/introduction/ 

## Code style and analysis

The ServiceDefaults project was created by the Aspire template and there are a number of code analysis warnings in the project.
Those warnings have been suppressed in the project file.


## Einstein sample
        
Inspired by the book Essential GraphRAG. Some code was adapted and translated from  the book code repository:

- [chapter 2](https://github.com/tomasonjo/kg-rag/blob/main/notebooks/ch02.ipynb)
- [chapter 3](https://github.com/tomasonjo/kg-rag/blob/main/notebooks/ch03.ipynb).

## TODO

- add more unit tests
- make the chunking IAsyncEnumerable based
    - [link text](https://stackoverflow.com/questions/21136753/read-a-very-large-file-by-chunks-and-not-line-by-line/21137097#21137097)
- add a service to the project that uses the chunking
- implement the repository pattern for Neo4j access
    - See 
- Look at extracting and using UFO dataset
    - See chapter 5 of [sql_book](https://github.com/cathytanimura/sql_book)
    - [SQL book to R](https://iangow.github.io/sql_book/)
    - [Import csv](https://stackoverflow.com/questions/15242757/import-csv-file-into-sql-server)
    
