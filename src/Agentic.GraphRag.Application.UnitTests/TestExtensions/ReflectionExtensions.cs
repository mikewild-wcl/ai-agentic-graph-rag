using Neo4j.Driver;
using System.Reflection;

namespace Agentic.GraphRag.Application.UnitTests.TestExtensions;

internal static class ReflectionExtensions
{
    public static EagerResult<IReadOnlyList<IRecord>> ConstructEagerResult(
    this IReadOnlyList<IRecord> records,
    IResultSummary? summary = null,
    string[]? keys = null)
    {
        var type = typeof(EagerResult<IReadOnlyList<IRecord>>);
        var ctor = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(IReadOnlyList<IRecord>), typeof(IResultSummary), typeof(string[])],
            modifiers: null
        ) ?? throw new InvalidOperationException("EagerResult constructor not found.");

        return (EagerResult<IReadOnlyList<IRecord>>)ctor.Invoke(
            [records, summary, keys ?? []]);
    }

    public static string? GetPrivateField<T>(this T instance, string fieldName) 
        where T: class
    {
        var type = instance.GetType();
        var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? type.BaseType?.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

        field.ShouldNotBeNull($"expected a private field named '{fieldName}' on the type or its base type");

        return field!.GetValue(instance) as string;
    }
}
