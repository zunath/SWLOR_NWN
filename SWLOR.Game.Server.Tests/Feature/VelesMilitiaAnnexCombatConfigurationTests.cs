using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class VelesMilitiaAnnexCombatConfigurationTests
{
    private static IEnumerable<string> Enemies()
    {
        foreach (var line in new[] { "invinc", "vitrupt", "sysshut" })
        foreach (var tier in new[] { "ad", "sp", "wd", "ic", "ms" })
            yield return $"cp_{line}_{tier}";

        yield return "invictus";
        yield return "ruptorvane";
        yield return "blackoutwrd";
    }

    [TestCaseSource(nameof(Enemies))]
    public void AnnexEnemies_CanMoveAndRespondToCombat(string resref)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module")))
            directory = directory.Parent;
        var root = directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
        using var document = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(root.FullName, "Module", "utc", $"{resref}.utc.json")));
        var creature = document.RootElement;

        creature.GetProperty("WalkRate").GetProperty("value").GetInt32()
            .Should().Be((int)MovementRate.Default, $"{resref} must be able to pursue combat targets");
        creature.GetProperty("Interruptable").GetProperty("value").GetInt32().Should().Be(1);
        foreach (var field in new[] { "ScriptHeartbeat", "ScriptOnNotice", "ScriptAttacked",
                     "ScriptDamaged", "ScriptEndRound", "ScriptOnBlocked", "ScriptSpawn" })
        {
            creature.GetProperty(field).GetProperty("value").GetString()
                .Should().NotBeNullOrWhiteSpace($"{resref} needs its {field} event to respond to combat");
        }
    }
}
