using Agentic.GraphRag.Application.Settings;
using Agentic.GraphRag.Shared.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace Agentic.GraphRag.Extensions;

[SuppressMessage("Minor Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "False positive in extensions class")]
[SuppressMessage("Maintainability", "S3398:\"private\" methods called only by inner classes should be moved to those classe", Justification = "False positive in extensions class")]
       internal static class HostApplicationBuilderExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        internal IServiceCollection ConfigureOptions() =>
         builder.Services
            .Configure<AISettings>(builder.Configuration.GetSection(AISettings.SectionName))
            .Configure<AzureOpenAISettings>(builder.Configuration.GetSection(AzureOpenAISettings.SectionName))
            .Configure<DownloadSettings>(builder.Configuration.GetSection(DownloadSettings.SectionName))
            .Configure<EinsteinQuerySettings>(builder.Configuration.GetSection(EinsteinQuerySettings.SectionName))
            .Configure<GraphDatabaseSettings>(builder.Configuration.GetSection(GraphDatabaseSettings.SectionName))
            ;
    }
}
