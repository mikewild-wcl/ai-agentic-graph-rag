namespace Agentic.GraphRag.AppHost.Extensions;

internal static class ResourceBuilderExtensions
{
    extension(IResourceBuilder<ParameterResource> parameter)
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "<Pending>")]
        internal string? GetValue() => parameter.Resource
            .GetValueAsync(default)
            .AsTask()
            .GetAwaiter()
            .GetResult();
    }
}
