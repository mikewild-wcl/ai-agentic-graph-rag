using Aspire.Hosting.Publishing;

namespace Agentic.GraphRag.AppHost.ParameterDefaults;

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
