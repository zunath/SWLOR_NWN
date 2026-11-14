namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>Uses no container at all.</summary>
public sealed class CleanUser
{
    public int Add(int first, int second) => first + second;
}
