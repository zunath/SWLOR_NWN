using System.Reflection;
using System.Reflection.Metadata;

namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>
/// Decides from metadata whether a type is deeply immutable, so a <c>static readonly</c> field of it cannot carry
/// shared state. Accepted: primitives, <c>string</c>, enums of the scanned assembly, <c>Regex</c>, the frozen and
/// immutable collections of <c>System.Collections.Frozen</c> and <c>System.Collections.Immutable</c> when every
/// argument is accepted, <c>Nullable&lt;T&gt;</c> and value tuples of accepted types, the immutable value types <c>decimal</c>, <c>TimeSpan</c>,
/// <c>DateTime</c>, <c>DateTimeOffset</c>, <c>DateOnly</c>, <c>TimeOnly</c> and <c>Guid</c>, and sealed records and record structs whose
/// members are all init-only (or get-only) and of accepted types. Anything the check cannot prove (arrays, mutable
/// collections, classes with a settable member, types of other assemblies it does not know) is not accepted.
/// </summary>
internal sealed class ImmutabilityAnalyzer
{
    private const string EnumBase = "System.Enum";
    private const string BackingSuffix = ">k__BackingField";

    private static readonly HashSet<string> ImmutableExternal =
    [
        "System.Text.RegularExpressions.Regex",
        "System.String",
        "System.Decimal",
        "System.TimeSpan",
        "System.DateTime",
        "System.DateTimeOffset",
        "System.DateOnly",
        "System.TimeOnly",
        "System.Guid",
    ];

    private static readonly HashSet<string> ImmutableCollections =
    [
        "System.Collections.Frozen.FrozenDictionary",
        "System.Collections.Frozen.FrozenSet",
        "System.Collections.Immutable.ImmutableArray",
        "System.Collections.Immutable.ImmutableList",
        "System.Collections.Immutable.ImmutableDictionary",
        "System.Collections.Immutable.ImmutableHashSet",
        "System.Collections.Immutable.ImmutableSortedDictionary",
        "System.Collections.Immutable.ImmutableSortedSet",
        "System.Collections.Immutable.ImmutableQueue",
        "System.Collections.Immutable.ImmutableStack",
        "System.Nullable",
        "System.ValueTuple",
    ];

    private readonly MetadataReader _metadata;
    private readonly TypeShapeProvider _provider = new();
    private readonly Dictionary<TypeDefinitionHandle, bool> _verdicts = new();
    private readonly HashSet<TypeDefinitionHandle> _visiting = new();

    public ImmutabilityAnalyzer(MetadataReader metadata)
    {
        _metadata = metadata;
    }

    /// <summary>True when a value of the type can never change after it is built.</summary>
    public bool IsImmutable(TypeShape shape)
    {
        switch (shape.Kind)
        {
            case TypeShapeKind.Primitive:
                return true;
            case TypeShapeKind.External:
                if (shape.Name is null)
                    return false;
                if (ImmutableExternal.Contains(shape.Name))
                    return shape.Args.Count == 0;
                return ImmutableCollections.Contains(shape.Name) && shape.Args.Count > 0 && shape.Args.All(IsImmutable);
            case TypeShapeKind.Defined:
                return shape.Args.Count == 0 && IsImmutableDefinition(shape.Definition);
            default:
                return false;
        }
    }

    private bool IsImmutableDefinition(TypeDefinitionHandle handle)
    {
        if (_verdicts.TryGetValue(handle, out var known))
            return known;

        // A type that refers back to itself through its members: its other members decide the answer.
        if (!_visiting.Add(handle))
            return true;

        var verdict = Evaluate(_metadata.GetTypeDefinition(handle));
        _visiting.Remove(handle);

        // A "yes" reached while another type was still being decided could rest on that type's optimistic assumption.
        if (_visiting.Count == 0 || !verdict)
            _verdicts[handle] = verdict;
        return verdict;
    }

    private bool Evaluate(TypeDefinition definition)
    {
        var baseName = BaseTypeName(definition);
        if (baseName == EnumBase)
            return true;

        var isStruct = baseName == "System.ValueType";
        if (!isStruct && baseName != "System.Object")
            return false;
        if (!isStruct && (definition.Attributes & TypeAttributes.Sealed) == 0)
            return false;
        if (definition.GetGenericParameters().Count > 0)
            return false;

        var isRecord = definition.GetMethods().Any(handle => _metadata.GetString(_metadata.GetMethodDefinition(handle).Name) == "PrintMembers");
        if (!isRecord)
            return false;

        var initOnlyProperties = new HashSet<string>();
        foreach (var propertyHandle in definition.GetProperties())
        {
            var property = _metadata.GetPropertyDefinition(propertyHandle);
            var setter = property.GetAccessors().Setter;
            if (!setter.IsNil && !_metadata.GetMethodDefinition(setter).DecodeSignature(_provider, null).ReturnType.HasInitModifier)
                return false;

            initOnlyProperties.Add(_metadata.GetString(property.Name));
        }

        foreach (var fieldHandle in definition.GetFields())
        {
            var field = _metadata.GetFieldDefinition(fieldHandle);
            if ((field.Attributes & FieldAttributes.Static) != 0)
                continue;

            var name = _metadata.GetString(field.Name);
            var isInitBacking = name.StartsWith('<') && name.EndsWith(BackingSuffix, StringComparison.Ordinal)
                && initOnlyProperties.Contains(name.Substring(1, name.Length - 1 - BackingSuffix.Length));
            if ((field.Attributes & FieldAttributes.InitOnly) == 0 && !isInitBacking)
                return false;

            if (!IsImmutable(field.DecodeSignature(_provider, null)))
                return false;
        }

        return true;
    }

    private string BaseTypeName(TypeDefinition definition) =>
        definition.BaseType.Kind == HandleKind.TypeReference
            ? ExternalTypeCollector.FullName(_metadata, (TypeReferenceHandle)definition.BaseType)
            : string.Empty;
}
