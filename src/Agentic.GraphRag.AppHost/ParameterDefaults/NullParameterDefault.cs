using Aspire.Hosting.Publishing;
using System.Diagnostics.CodeAnalysis;

namespace Agentic.GraphRag.AppHost.ParameterDefaults;

[SuppressMessage("Performance", "CA1812", Justification = "Available for future use")]
internal sealed class NullParameterDefault : ParameterDefault
{
    public override string GetDefaultValue()
    {
        return null!;
    }

    public override void WriteToManifest(ManifestPublishingContext context)
    {
    }
}
