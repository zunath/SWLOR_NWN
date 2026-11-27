namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The source of <c>DeathService</c> wherever it lives, for the tests that guard a rule by reading the code. The combat services split
/// (<c>Readmes/MaintainabilityRefactorCombatServicesSplit.md</c>) moved the respawn code of <c>Domain/Combat/DeathService.cs</c> into
/// <c>DeathRespawnService</c>, and <see cref="PartFiles"/> lists both files.
/// </summary>
public static class DeathSource
{
    /// <summary>The files that hold the code of <c>DeathService.cs</c> as it was before the split, repository-relative, in reading order.</summary>
    public static readonly IReadOnlyList<string> PartFiles = new[]
    {
        "SWLOR.Game.Server/Domain/Combat/DeathService.cs",
        "SWLOR.Game.Server/Domain/Combat/DeathRespawnService.cs",
    };

    /// <summary>The interfaces of the parts: a call through a field of one of these types was a call on the same instance before the move.</summary>
    public static readonly IReadOnlySet<string> PartInterfaces = new HashSet<string>(StringComparer.Ordinal)
    {
        "IDeathService",
        "IDeathRespawnService",
    };

    private static readonly PartSourceFiles Files = new(PartFiles, PartInterfaces);

    /// <summary>The combined source of <c>DeathService</c>.</summary>
    public static string Read() => Files.Read();

    /// <summary>The full paths of the files <see cref="Read"/> combines.</summary>
    public static IReadOnlyList<string> Paths() => Files.Paths();
}
