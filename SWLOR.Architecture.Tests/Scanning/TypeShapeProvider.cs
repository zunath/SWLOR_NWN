using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>Decodes a signature into a <see cref="TypeShape"/>.</summary>
internal sealed class TypeShapeProvider : ISignatureTypeProvider<TypeShape, object?>
{
    private const string InitModifier = "System.Runtime.CompilerServices.IsExternalInit";
    private static readonly TypeShape Other = new(TypeShapeKind.Other);

    public TypeShape GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode switch
    {
        PrimitiveTypeCode.Object or PrimitiveTypeCode.TypedReference or PrimitiveTypeCode.Void => Other,
        _ => new TypeShape(TypeShapeKind.Primitive, typeCode.ToString()),
    };

    public TypeShape GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
        new(TypeShapeKind.Defined, Definition: handle);

    public TypeShape GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) =>
        new(TypeShapeKind.External, ExternalTypeCollector.FullName(reader, handle));

    public TypeShape GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) =>
        reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

    public TypeShape GetGenericInstantiation(TypeShape genericType, ImmutableArray<TypeShape> typeArguments) =>
        genericType with { Arguments = typeArguments };

    public TypeShape GetModifiedType(TypeShape modifier, TypeShape unmodifiedType, bool isRequired) =>
        modifier.Name == InitModifier ? unmodifiedType with { HasInitModifier = true } : unmodifiedType;

    public TypeShape GetArrayType(TypeShape elementType, ArrayShape shape) => Other;

    public TypeShape GetSZArrayType(TypeShape elementType) => Other;

    public TypeShape GetByReferenceType(TypeShape elementType) => Other;

    public TypeShape GetPointerType(TypeShape elementType) => Other;

    public TypeShape GetPinnedType(TypeShape elementType) => Other;

    public TypeShape GetFunctionPointerType(MethodSignature<TypeShape> signature) => Other;

    public TypeShape GetGenericMethodParameter(object? genericContext, int index) => Other;

    public TypeShape GetGenericTypeParameter(object? genericContext, int index) => Other;
}
