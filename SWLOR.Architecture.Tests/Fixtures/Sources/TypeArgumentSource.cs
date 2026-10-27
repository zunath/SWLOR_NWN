using SWLOR.Architecture.Tests.Fixtures.Targets;

namespace SWLOR.Architecture.Tests.Fixtures.Sources;

/// <summary>Reaches targets only through the arguments of custom attributes, which sit in the attribute blob.</summary>
[TypeArgument(typeof(AttributeTypeofTarget))]
[TypeArgument(typeof(string), Named = typeof(AttributeNamedTypeTarget))]
[TypeArgument(new[] { typeof(AttributeArrayTypeTarget) })]
[TypeArgument(typeof(List<AttributeGenericTypeTarget>))]
[EnumArgument(AttributeEnumTarget.One)]
public class TypeArgumentSource
{
}
