using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>
/// Decodes a signature (field, method, local, generic instantiation) into the types of the scanned assembly it
/// mentions. Types of other assemblies are ignored; generic arguments and element types are included, so
/// <c>List&lt;Perk&gt;</c> and <c>Perk[]</c> both mention <c>Perk</c>.
/// </summary>
internal sealed class DefinedTypeCollector : ISignatureTypeProvider<TypeDefinitionHandle[], object?>
{
    private static readonly TypeDefinitionHandle[] None = [];

    public TypeDefinitionHandle[] GetPrimitiveType(PrimitiveTypeCode typeCode) => None;

    public TypeDefinitionHandle[] GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
        [handle];

    public TypeDefinitionHandle[] GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => None;

    public TypeDefinitionHandle[] GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) =>
        reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

    public TypeDefinitionHandle[] GetArrayType(TypeDefinitionHandle[] elementType, ArrayShape shape) => elementType;

    public TypeDefinitionHandle[] GetByReferenceType(TypeDefinitionHandle[] elementType) => elementType;

    public TypeDefinitionHandle[] GetPointerType(TypeDefinitionHandle[] elementType) => elementType;

    public TypeDefinitionHandle[] GetSZArrayType(TypeDefinitionHandle[] elementType) => elementType;

    public TypeDefinitionHandle[] GetPinnedType(TypeDefinitionHandle[] elementType) => elementType;

    public TypeDefinitionHandle[] GetModifiedType(TypeDefinitionHandle[] modifier, TypeDefinitionHandle[] unmodifiedType, bool isRequired) =>
        Concat(unmodifiedType, modifier);

    public TypeDefinitionHandle[] GetGenericInstantiation(TypeDefinitionHandle[] genericType, ImmutableArray<TypeDefinitionHandle[]> typeArguments)
    {
        var all = genericType;
        foreach (var argument in typeArguments)
            all = Concat(all, argument);
        return all;
    }

    public TypeDefinitionHandle[] GetFunctionPointerType(MethodSignature<TypeDefinitionHandle[]> signature)
    {
        var all = signature.ReturnType;
        foreach (var parameter in signature.ParameterTypes)
            all = Concat(all, parameter);
        return all;
    }

    public TypeDefinitionHandle[] GetGenericMethodParameter(object? genericContext, int index) => None;

    public TypeDefinitionHandle[] GetGenericTypeParameter(object? genericContext, int index) => None;

    private static TypeDefinitionHandle[] Concat(TypeDefinitionHandle[] first, TypeDefinitionHandle[] second)
    {
        if (second.Length == 0)
            return first;
        if (first.Length == 0)
            return second;
        return [.. first, .. second];
    }
}
