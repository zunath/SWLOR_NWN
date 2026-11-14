using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Debt;

/// <summary>A server type that holds static state or hands out a container service from a static member.</summary>
/// <param name="Type">The top-level type (nested types count toward it).</param>
/// <param name="Fields">Its static fields that can change or share a collection: not <c>const</c>, and not <c>static readonly</c> of a deeply immutable type.</param>
/// <param name="ContainerAccessors">Its static members that resolve a service from the container.</param>
/// <param name="IsContent">True for a type in <c>Feature</c>.</param>
internal sealed record StaticStateHolder(TypeKey Type, IReadOnlyList<StaticFieldFact> Fields, IReadOnlyList<string> ContainerAccessors, bool IsContent)
{
    public int Count => Fields.Count + ContainerAccessors.Count;

    /// <summary>The fields and accessors for the report; a field code writes after its initialization is marked with <c>*</c>.</summary>
    public string Describe(IReadOnlySet<StaticFieldFact> written) =>
        string.Join("; ", new[]
        {
            Fields.Count == 0 ? null : "fields: " + string.Join(", ", Fields.Select(held => (held.Declaring.Contains('+') ? $"{held.Declaring[(held.Declaring.IndexOf('+') + 1)..].Replace('+', '.')}.{held.Name}" : held.Name) + (written.Contains(held) ? "*" : string.Empty))),
            ContainerAccessors.Count == 0 ? null : "resolves from the container: " + string.Join(", ", ContainerAccessors)
        }.OfType<string>());
}
