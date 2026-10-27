using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>
/// Decodes a signature (field, method, local, generic instantiation) into the types of other assemblies it mentions,
/// as full names without generic arity. The counterpart of <see cref="DefinedTypeCollector"/>, which keeps the types
/// of the scanned assembly: generic arguments and element types are included, so <c>List&lt;IServiceProvider&gt;</c>
/// mentions both.
/// </summary>
internal sealed class ExternalTypeCollector : ISignatureTypeProvider<string[], object?>
{
    private static readonly string[] None = [];

    public string[] GetPrimitiveType(PrimitiveTypeCode typeCode) => None;

    public string[] GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => None;

    public string[] GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) =>
        [FullName(reader, handle)];

    public string[] GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) =>
        reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

    public string[] GetArrayType(string[] elementType, ArrayShape shape) => elementType;

    public string[] GetByReferenceType(string[] elementType) => elementType;

    public string[] GetPointerType(string[] elementType) => elementType;

    public string[] GetSZArrayType(string[] elementType) => elementType;

    public string[] GetPinnedType(string[] elementType) => elementType;

    public string[] GetModifiedType(string[] modifier, string[] unmodifiedType, bool isRequired) => Concat(unmodifiedType, modifier);

    public string[] GetGenericInstantiation(string[] genericType, ImmutableArray<string[]> typeArguments)
    {
        var all = genericType;
        foreach (var argument in typeArguments)
            all = Concat(all, argument);
        return all;
    }

    public string[] GetFunctionPointerType(MethodSignature<string[]> signature)
    {
        var all = signature.ReturnType;
        foreach (var parameter in signature.ParameterTypes)
            all = Concat(all, parameter);
        return all;
    }

    public string[] GetGenericMethodParameter(object? genericContext, int index) => None;

    public string[] GetGenericTypeParameter(object? genericContext, int index) => None;

    /// <summary>The full name of a type of another assembly, <c>Namespace.Outer+Nested</c>, without generic arity.</summary>
    public static string FullName(MetadataReader reader, TypeReferenceHandle handle)
    {
        var reference = reader.GetTypeReference(handle);
        var name = reader.GetString(reference.Name);
        var tick = name.IndexOf('`');
        if (tick >= 0)
            name = name[..tick];

        if (reference.ResolutionScope.Kind == HandleKind.TypeReference)
            return FullName(reader, (TypeReferenceHandle)reference.ResolutionScope) + "+" + name;

        var @namespace = reader.GetString(reference.Namespace);
        return @namespace.Length == 0 ? name : @namespace + "." + name;
    }

    private static string[] Concat(string[] first, string[] second)
    {
        if (second.Length == 0)
            return first;
        if (first.Length == 0)
            return second;
        return [.. first, .. second];
    }
}
