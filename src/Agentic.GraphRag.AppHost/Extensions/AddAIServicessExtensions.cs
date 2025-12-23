using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Agentic.GraphRag.AppHost.Extensions;

internal static class AddAIServicessExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "<Pending>")]
        internal IHostApplicationBuilder AddAIServices()
        {
            // Note for github token:https://github.com/codebytes/aspire/blob/147fadf3985ab8a2a1f70c6138717a7a762b2ade/tools/ReleaseNotes/data/whats-new-95.md?plain=1#L480
            //  - add token to secrets
            return builder;
        }
    }
}
