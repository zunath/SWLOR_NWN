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
    [TestCase("Service/CombatService/WeaponRepositioning.cs", "_lockouts.Remove(actor);",
        "if (_moving.Remove(actor, out var operation)) operation.OnArrival(false, 0f);")]
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
            .Should().Contain("if (PendingHands.Remove(actor)) StatusEffect.ForgetDestroyedNativeAttackSwing(actor);");
        var statusEffect = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Service", "StatusEffect.cs"))
            .Replace("\r\n", "\n");
        var start = statusEffect.IndexOf("public static void ForgetDestroyedNativeAttackSwing(", StringComparison.Ordinal);
        var body = statusEffect.Substring(start, statusEffect.IndexOf("\n        }", start, StringComparison.Ordinal) - start);
        body.Should().Contain("_nativeAttackSwingDepth.Remove(attacker);");
        body.Should().Contain("_deferredNativeAttackStatusEffects.Remove(attacker);");
        body.Should().NotContain("Invoke").And.NotContain("();", "deferred effects for a destroyed attacker are discarded");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
