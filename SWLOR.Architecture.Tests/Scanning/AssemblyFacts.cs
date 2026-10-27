namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>
/// What the architecture rules need to know about a compiled assembly besides the references between its own types
/// (<see cref="TypeReferenceGraph"/>): its top-level types, static fields, event handler methods and uses of types
/// from other assemblies. Read from metadata and IL; the assembly is never loaded.
/// </summary>
internal sealed class AssemblyFacts
{
    public AssemblyFacts(
        IReadOnlyList<TypeFact> types,
        IReadOnlyList<StaticFieldFact> staticFields,
        IReadOnlyList<HandlerFact> handlers,
        IReadOnlyList<ExternalReference> externalReferences)
    {
        Types = types;
        StaticFields = staticFields;
        Handlers = handlers;
        ExternalReferences = externalReferences;
    }

    /// <summary>Every top-level type, compiler-generated ones included.</summary>
    public IReadOnlyList<TypeFact> Types { get; }

    /// <summary>Every static field written in the source, <c>const</c> fields included.</summary>
    public IReadOnlyList<StaticFieldFact> StaticFields { get; }

    /// <summary>Every method that carries <c>[NWNEventHandler]</c>.</summary>
    public IReadOnlyList<HandlerFact> Handlers { get; }

    /// <summary>Every use of a type from another assembly, grouped by the type that makes it.</summary>
    public IReadOnlyList<ExternalReference> ExternalReferences { get; }
}
