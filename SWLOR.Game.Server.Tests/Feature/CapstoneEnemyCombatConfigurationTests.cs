using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class CapstoneEnemyCombatConfigurationTests
{
    private static DirectoryInfo RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module")))
            directory = directory.Parent;
        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private static IEnumerable<string> Enemies()
    {
        var blueprints = Directory.GetFiles(Path.Combine(RepositoryRoot().FullName, "Module", "utc"), "cp_*.utc.json")
            .Select(path => Path.GetFileName(path)[..^".utc.json".Length])
            .OrderBy(resref => resref, StringComparer.Ordinal)
            .ToList();
        blueprints.Should().NotBeEmpty();
        return blueprints;
    }

    [TestCaseSource(nameof(Enemies))]
    public void CapstoneEnemies_CanMoveAndRespondToCombat(string resref)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(RepositoryRoot().FullName, "Module", "utc", $"{resref}.utc.json")));
        var creature = document.RootElement;

        creature.GetProperty("WalkRate").GetProperty("value").GetInt32()
            .Should().NotBe((int)MovementRate.Immobile, $"{resref} must be able to pursue combat targets");
        creature.GetProperty("Interruptable").GetProperty("value").GetInt32().Should().Be(1);
        foreach (var field in new[] { "ScriptHeartbeat", "ScriptOnNotice", "ScriptAttacked",
                     "ScriptDamaged", "ScriptEndRound", "ScriptOnBlocked", "ScriptSpawn", "ScriptDeath" })
        {
            creature.GetProperty(field).GetProperty("value").GetString()
                .Should().NotBeNullOrWhiteSpace($"{resref} needs its {field} event to respond to combat");
        }
    }
}
