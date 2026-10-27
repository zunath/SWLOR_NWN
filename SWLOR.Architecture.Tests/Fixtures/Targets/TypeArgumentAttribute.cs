namespace SWLOR.Architecture.Tests.Fixtures.Targets;

[System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
public sealed class TypeArgumentAttribute : System.Attribute
{
    public TypeArgumentAttribute(System.Type type)
    {
    }

    public TypeArgumentAttribute(System.Type[] types)
    {
    }

    public System.Type? Named { get; set; }
}
