using SWLOR.Architecture.Tests.Fixtures.Targets;

namespace SWLOR.Architecture.Tests.Fixtures.Sources;

/// <summary>Reaches targets only through attributes on parameters, return values and generic parameters.</summary>
public class SignatureAttributeSource<[TypeArgument(typeof(TypeGenericAttributeTarget))] T>
{
    public void Parameter([TypeArgument(typeof(ParameterAttributeTarget))] int value)
    {
    }

    [return: TypeArgument(typeof(ReturnAttributeTarget))]
    public int Returning() => 0;

    public void Generic<[TypeArgument(typeof(MethodGenericAttributeTarget))] TMethod>()
    {
    }
}
