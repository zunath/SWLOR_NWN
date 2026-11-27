namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The source of the native weapon roll hooks wherever it lives, for the tests that guard a rule by reading the code.
/// <c>Native/ResolveAttackRoll.cs</c> and <c>Native/GetDamageRoll.cs</c> were split into types by responsibility
/// (<c>Native/AttackRoll*.cs</c>, <c>Native/DamageRoll*.cs</c>, <c>Native/WeaponDamageProfile.cs</c>); a test reads the
/// combined text of a roll and keeps its needles. Each roll's files are combined hook file first, then in the order the
/// roll runs them, so a needle that orders two calls of one moved method still compares them in that method.
/// </summary>
public static class NativeCombatSource
{
    /// <summary>The attack roll: the hook and its flow, then accuracy, defense, critical, settlement and feedback.</summary>
    public static readonly IReadOnlyList<string> AttackRollFiles =
    [
        "ResolveAttackRoll.cs",
        "AttackRollAccuracy.cs",
        "AttackRollDefense.cs",
        "AttackRollCritical.cs",
        "AttackRollSettlement.cs",
        "AttackRollFeedback.cs"
    ];

    /// <summary>The damage roll: the hook and its flow, then the weapon, the creature formula, mitigation and hit effects.</summary>
    public static readonly IReadOnlyList<string> DamageRollFiles =
    [
        "GetDamageRoll.cs",
        "DamageRollWeapon.cs",
        "DamageRollCreature.cs",
        "DamageRollMitigation.cs",
        "DamageRollHitEffects.cs",
        "WeaponDamageProfile.cs"
    ];

    /// <summary>The combined text of the attack roll's files.</summary>
    public static string AttackRoll() => Combine(AttackRollFiles);

    /// <summary>The combined text of the damage roll's files.</summary>
    public static string DamageRoll() => Combine(DamageRollFiles);

    /// <summary>The text of one file of <c>SWLOR.Game.Server/Native</c>.</summary>
    public static string Read(string fileName) => File.ReadAllText(PathOf(fileName));

    /// <summary>The full path of one file of <c>SWLOR.Game.Server/Native</c>.</summary>
    public static string PathOf(string fileName) =>
        Path.Combine(FindRepositoryRoot(), "SWLOR.Game.Server", "Native", fileName);

    private static string Combine(IEnumerable<string> fileNames) =>
        string.Join(Environment.NewLine, fileNames.Select(Read));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate SWLOR.Game.Server.sln from the test directory.");
    }
}
