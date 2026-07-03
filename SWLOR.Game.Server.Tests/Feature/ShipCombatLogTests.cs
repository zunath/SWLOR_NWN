using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Tests;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Tests.Feature;

public class ShipCombatLogTests
{
    [Test]
    public void ShipCombatLogs_AreRenderedPerReceiver()
    {
        var root = FindRepositoryRoot();
        var shipModuleDirectory = new DirectoryInfo(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "ShipModuleDefinition"));

        foreach (var file in shipModuleDirectory.GetFiles("*.cs"))
        {
            var source = File.ReadAllText(file.FullName);

            source.Should().NotContain(
                "var combatLogMessage = CombatLog.BuildCombatLogMessage",
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
        var root = FindRepositoryRoot();
        var source = CombatSourceReader.Read(root);

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

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
