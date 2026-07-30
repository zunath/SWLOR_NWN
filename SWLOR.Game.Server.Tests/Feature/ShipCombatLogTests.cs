using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Feature;

public class ShipCombatLogTests
{
    [Test]
    public void ShipCombatLogs_AreRenderedPerReceiver()
    {
        var root = RepoPaths.FindRepositoryRoot();
        var shipModuleDirectory = new DirectoryInfo(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "ShipModuleDefinition"));

        foreach (var file in shipModuleDirectory.GetFiles("*.cs"))
        {
            var source = File.ReadAllText(file.FullName);

            source.Should().NotContain(
                "var combatLogMessage = Combat.BuildCombatLogMessage",
                $"{file.Name} should not broadcast one observer's combat-log text to every nearby player");
            source.Should().NotContain(
                "SendMessageNearbyToPlayers(target, combatLogMessage",
                $"{file.Name} should render combat logs for each receiver");
            source.Should().NotContain(
                "SendMessageNearbyToPlayers(nearbyTarget, combatLogMessage",
                $"{file.Name} should render combat logs for each receiver");
        }
    }

    [Test]
    public void CombatLogBuilder_RequiresExplicitObserver()
    {
        var root = RepoPaths.FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Combat.cs"));

        source.Should().NotContain("return BuildCombatLogMessage(attacker, attacker, defender");
        source.Should().Contain("public static string BuildCombatLogMessage(");
        source.Should().Contain("uint observer");
        source.Should().MatchRegex(
            @"public\s+static\s+string\s+BuildCombatLogMessage\s*\(\s*uint\s+observer\s*,\s*uint\s+attacker\s*,\s*uint\s+defender\s*,",
            "BuildCombatLogMessage should receive the observer before attacker and defender");
        source.Should().NotMatchRegex(
            @"public\s+static\s+string\s+BuildAbilityCombatLogMessage\s*\(\s*uint\s+attacker\s*,\s*uint\s+defender\s*,",
            "ability combat logs should require an explicit observer");
        source.Should().NotMatchRegex(
            @"public\s+static\s+string\s+BuildAbilityNoTargetCombatLogMessage\s*\(\s*uint\s+attacker\s*,\s*string\s+abilityName",
            "no-target ability combat logs should require an explicit observer");
        source.Should().NotMatchRegex(
            @"public\s+static\s+string\s+BuildCombatLogMessageNative\s*\(\s*CNWSCreature\s+attacker\s*,\s*CNWSCreature\s+defender",
            "native combat logs should require an explicit observer");
    }

}
