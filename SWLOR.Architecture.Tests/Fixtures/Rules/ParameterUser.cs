namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>Takes the container itself as a constructor parameter, which hides its real dependencies.</summary>
public sealed class ParameterUser
{
    public ParameterUser(IServiceProvider provider)
    {
    }
}
