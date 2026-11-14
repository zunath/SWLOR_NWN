using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;

namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>
/// Decodes the arguments of a custom attribute into the types of the scanned assembly they mention: a
/// <c>typeof</c> argument (stored as a type name, not as a metadata token), a type inside a generic
/// <c>typeof</c>, and the enum type of an enum-valued argument.
/// </summary>
internal sealed partial class AttributeArgumentCollector : ICustomAttributeTypeProvider<AttributeArgumentType>
{
    private static readonly AttributeArgumentType None = new([], null, false);
    private static readonly AttributeArgumentType SystemType = new([], null, true);

    private readonly MetadataReader _metadata;
    private readonly IReadOnlyDictionary<string, TypeDefinitionHandle> _typesByName;

    /// <param name="typesByName">Every type of the assembly by its metadata name (<c>Namespace.Outer+Nested</c>).</param>
    public AttributeArgumentCollector(MetadataReader metadata, IReadOnlyDictionary<string, TypeDefinitionHandle> typesByName)
    {
        _metadata = metadata;
        _typesByName = typesByName;
    }

    public AttributeArgumentType GetSystemType() => SystemType;

    public bool IsSystemType(AttributeArgumentType type) => type.IsSystemType;

    public AttributeArgumentType GetPrimitiveType(PrimitiveTypeCode typeCode) => None;

    public AttributeArgumentType GetSZArrayType(AttributeArgumentType elementType) => elementType;

    public AttributeArgumentType GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
        new([handle], null, false);

    public AttributeArgumentType GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        var name = ExternalName(reader, handle);
        return name.StartsWith("System.Type,", StringComparison.Ordinal)
            ? SystemType
            : new AttributeArgumentType([], name, false);
    }

    /// <summary>Resolves a <c>typeof(...)</c> argument; a generic one also names the types between its brackets.</summary>
    public AttributeArgumentType GetTypeFromSerializedName(string name)
    {
        var found = new List<TypeDefinitionHandle>();
        foreach (Match token in TypeNameToken().Matches(name))
        {
            if (_typesByName.TryGetValue(token.Value, out var handle))
                found.Add(handle);
        }

        return new AttributeArgumentType(found, null, false);
    }

    public PrimitiveTypeCode GetUnderlyingEnumType(AttributeArgumentType type)
    {
        if (type.Defined.Count == 1)
            return UnderlyingTypeOfDefinedEnum(type.Defined[0]);

        if (type.ExternalName is not null)
            return UnderlyingTypeOfExternalEnum(type.ExternalName);

        throw new InvalidOperationException("A custom attribute argument has an enum type that cannot be identified.");
    }

    private PrimitiveTypeCode UnderlyingTypeOfDefinedEnum(TypeDefinitionHandle handle)
    {
        foreach (var fieldHandle in _metadata.GetTypeDefinition(handle).GetFields())
        {
            var field = _metadata.GetFieldDefinition(fieldHandle);
            if (_metadata.GetString(field.Name) == "value__")
                return field.DecodeSignature(PrimitiveOnly.Instance, null);
        }

        throw new InvalidOperationException($"{_metadata.GetString(_metadata.GetTypeDefinition(handle).Name)} is not an enum.");
    }

    // The blob stores an enum value in the width of its underlying type, which only the defining assembly knows.
    private static PrimitiveTypeCode UnderlyingTypeOfExternalEnum(string assemblyQualifiedName)
    {
        var type = Type.GetType(assemblyQualifiedName)
                   ?? throw new InvalidOperationException(
                       $"Cannot find the enum {assemblyQualifiedName} to decode a custom attribute argument. " +
                       "Its assembly must be loadable from the test output folder.");
        return Type.GetTypeCode(Enum.GetUnderlyingType(type)) switch
        {
            TypeCode.SByte => PrimitiveTypeCode.SByte,
            TypeCode.Byte => PrimitiveTypeCode.Byte,
            TypeCode.Int16 => PrimitiveTypeCode.Int16,
            TypeCode.UInt16 => PrimitiveTypeCode.UInt16,
            TypeCode.Int32 => PrimitiveTypeCode.Int32,
            TypeCode.UInt32 => PrimitiveTypeCode.UInt32,
            TypeCode.Int64 => PrimitiveTypeCode.Int64,
            TypeCode.UInt64 => PrimitiveTypeCode.UInt64,
            var other => throw new InvalidOperationException($"Unexpected enum underlying type {other}.")
        };
    }

    // "Namespace.Outer+Nested, Assembly" for a TypeReference (nested references name their outer type in the scope).
    private static string ExternalName(MetadataReader reader, TypeReferenceHandle handle)
    {
        var reference = reader.GetTypeReference(handle);
        var name = reader.GetString(reference.Name);
        var @namespace = reader.GetString(reference.Namespace);

        switch (reference.ResolutionScope.Kind)
        {
            case HandleKind.TypeReference:
                var outer = ExternalName(reader, (TypeReferenceHandle)reference.ResolutionScope);
                var comma = outer.LastIndexOf(", ", StringComparison.Ordinal);
                return $"{outer[..comma]}+{name}{outer[comma..]}";
            case HandleKind.AssemblyReference:
                var assembly = reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)reference.ResolutionScope).Name);
                return $"{(@namespace.Length == 0 ? name : @namespace + "." + name)}, {assembly}";
            default:
                return $"{(@namespace.Length == 0 ? name : @namespace + "." + name)}, ";
        }
    }

    // A type name inside a serialized generic name: Namespace.Outer+Nested`1 (assembly names and versions never match a type).
    [GeneratedRegex(@"[A-Za-z_][A-Za-z0-9_.+`]*")]
    private static partial Regex TypeNameToken();

    /// <summary>Reads the primitive type code of an enum's <c>value__</c> field.</summary>
    private sealed class PrimitiveOnly : ISignatureTypeProvider<PrimitiveTypeCode, object?>
    {
        public static readonly PrimitiveOnly Instance = new();

        public PrimitiveTypeCode GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode;

        public PrimitiveTypeCode GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => throw NotPrimitive();

        public PrimitiveTypeCode GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => throw NotPrimitive();

        public PrimitiveTypeCode GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => throw NotPrimitive();

        public PrimitiveTypeCode GetSZArrayType(PrimitiveTypeCode elementType) => throw NotPrimitive();

        public PrimitiveTypeCode GetArrayType(PrimitiveTypeCode elementType, ArrayShape shape) => throw NotPrimitive();

        public PrimitiveTypeCode GetByReferenceType(PrimitiveTypeCode elementType) => throw NotPrimitive();

        public PrimitiveTypeCode GetPointerType(PrimitiveTypeCode elementType) => throw NotPrimitive();

        public PrimitiveTypeCode GetPinnedType(PrimitiveTypeCode elementType) => throw NotPrimitive();

        public PrimitiveTypeCode GetModifiedType(PrimitiveTypeCode modifier, PrimitiveTypeCode unmodifiedType, bool isRequired) => throw NotPrimitive();

        public PrimitiveTypeCode GetGenericInstantiation(PrimitiveTypeCode genericType, ImmutableArray<PrimitiveTypeCode> typeArguments) => throw NotPrimitive();

        public PrimitiveTypeCode GetFunctionPointerType(MethodSignature<PrimitiveTypeCode> signature) => throw NotPrimitive();

        public PrimitiveTypeCode GetGenericMethodParameter(object? genericContext, int index) => throw NotPrimitive();

        public PrimitiveTypeCode GetGenericTypeParameter(object? genericContext, int index) => throw NotPrimitive();

        private static InvalidOperationException NotPrimitive() => new("An enum's value__ field must have a primitive type.");
    }
}
