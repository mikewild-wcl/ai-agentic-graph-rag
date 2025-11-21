namespace Agentic.GraphRag.AppHost.Extensions;

internal static class ResourceBuilderExtensions
{
    public static string? GetValue(this IResourceBuilder<ParameterResource> parameter) =>
        parameter.Resource.GetValueAsync(default).AsTask().GetAwaiter().GetResult();

    // Fails with CA1515 - SDK bug https://github.com/dotnet/sdk/issues/51683
    //extension(IResourceBuilder<ParameterResource> parameter)
    //{
    //    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "<Pending>")]
    //    internal string? GetValue() =>
    //        parameter.Resource.GetValueAsync(default).AsTask().GetAwaiter().GetResult();
    //}
}
