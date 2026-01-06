using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;

namespace Agentic.GraphRag.AppHost.Extensions;

[SuppressMessage("Minor Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "False positive in extensions class")]
internal static class AddAIServicesExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        internal IHostApplicationBuilder AddAIServices()
        {
            // Note for github token:https://github.com/codebytes/aspire/blob/147fadf3985ab8a2a1f70c6138717a7a762b2ade/tools/ReleaseNotes/data/whats-new-95.md?plain=1#L480
            //  - add token to secrets
            return builder;
        }
    }
}
