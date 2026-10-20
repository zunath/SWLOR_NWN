using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Dismissed droids and despawned NPCs are destroyed without death or exit events, and their object IDs can be
/// reused. Owner-keyed combat state therefore needs an OnObjectDestroyed path that never touches the destroyed object.
/// </summary>
public class DestroyedOwnerCleanupTests
{
    [TestCase("Feature/OrdinaryAttackCombatLifecycle.cs", "OrdinaryAttackTraits.ForgetDestroyed(OBJECT_SELF)")]
    [TestCase("Feature/UsePerkFeat.cs", "_queuedWeaponTraits.Remove(creature);", "_queuedWeaponAttacks.Remove(creature);")]
    [TestCase("Service/CombatService/WeaponRepositioning.cs", "_moving.Remove(actor);", "_lockouts.Remove(actor);")]
    [TestCase("Feature/TwinPistolCombatLifecycle.cs", "TwinPistolAbility.ForgetDestroyed(actor);",
        "PairedRangedAttackCycle.ForgetDestroyed(actor);", "PairedRangedPresentation.Release(actor);")]
    public void OwnerKeyedStateIsDroppedWhenTheOwnerIsDestroyed(string file, params string[] expected)
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot().FullName, "SWLOR.Game.Server", file))
            .Replace("\r\n", "\n");
        var start = source.IndexOf("[NWNEventHandler(ScriptName.OnObjectDestroyed)]", StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"{file} needs an OnObjectDestroyed handler");
        var end = source.IndexOf("\n    }", start + 1, StringComparison.Ordinal);
        var handler = source.Substring(start, (end < 0 ? source.Length : end) - start);
        foreach (var call in expected)
            handler.Should().Contain(call);
    }

    [Test]
    public void DestroyedOwnerCleanupOnlyRemovesState()
    {
        var root = FindRepositoryRoot().FullName;
        File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Service", "OrdinaryAttackTraits.cs"))
            .Should().Contain("public static void ForgetDestroyed(uint actor) => States.Remove(actor);");
        File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Native", "PairedRangedAttackCycle.cs"))
            .Should().Contain("public static void ForgetDestroyed(uint actor) => PendingHands.Remove(actor);");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
