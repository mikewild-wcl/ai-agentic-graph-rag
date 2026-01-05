namespace Agentic.GraphRag.AppHost.Extensions;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "False positive in extensions class")]
internal static class ResourceBuilderExtensions
{
    extension(IResourceBuilder<ParameterResource> parameter)
    {
        internal string? GetValue() => parameter.Resource
            .GetValueAsync(default)
            .AsTask()
            .GetAwaiter()
            .GetResult();
    }
}
