using Aspire.Hosting.Publishing;

namespace Agentic.GraphRag.AppHost.ParameterDefaults;

internal sealed class EmptyStringParameterDefault : ParameterDefault
{
    public override string GetDefaultValue()
    {
        return string.Empty;
    }

    public override void WriteToManifest(ManifestPublishingContext context)
    {
    }
}
