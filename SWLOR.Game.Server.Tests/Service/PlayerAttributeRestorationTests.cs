using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class PlayerAttributeRestorationTests
{
    [Test]
    public void OlderCharacterFileRecoversEverySavedAttributeIncludingCappedInvestments()
    {
        var player = new Player { TotalAPAcquired = 23, UnallocatedAP = 7, RacialStat = AbilityType.Perception };
        var nativeScores = player.BaseStats.Keys.ToDictionary(ability => ability, _ => 10);
        player.BaseStats[AbilityType.Might] = 15;
        player.BaseStats[AbilityType.Perception] = 15;
        player.BaseStats[AbilityType.Vitality] = 10;
        player.BaseStats[AbilityType.Agility] = 10;
        player.BaseStats[AbilityType.Willpower] = 15;
        player.BaseStats[AbilityType.Social] = 10;
        player.UpgradedStats[AbilityType.Might] = 11;
        player.UpgradedStats[AbilityType.Perception] = 5;
        nativeScores[AbilityType.Might] = 15;
        nativeScores[AbilityType.Perception] = 15;

        Restore(player, (ability, score) => nativeScores[ability] = score);

        nativeScores.Should().BeEquivalentTo(new Dictionary<AbilityType, int>
        {
            [AbilityType.Might] = 26,
            [AbilityType.Perception] = 21,
            [AbilityType.Vitality] = 10,
            [AbilityType.Agility] = 10,
            [AbilityType.Willpower] = 15,
            [AbilityType.Social] = 10
        });
        player.UnallocatedAP.Should().Be(7);
    }

    [TestCase(AbilityType.Invalid)]
    [TestCase(AbilityType.Might)]
    [TestCase(AbilityType.Perception)]
    [TestCase(AbilityType.Vitality)]
    [TestCase(AbilityType.Agility)]
    [TestCase(AbilityType.Willpower)]
    [TestCase(AbilityType.Social)]
    public void ReconnectingAssignsSavedTotalsWithoutChangingAPOrStackingRacialBonuses(AbilityType racialStat)
    {
        var player = new Player { TotalAPAcquired = 40, UnallocatedAP = 4, RacialStat = racialStat };
        foreach (var ability in player.BaseStats.Keys)
        {
            player.BaseStats[ability] = 15;
            player.UpgradedStats[ability] = 6;
        }
        var savedRecord = JsonConvert.SerializeObject(player);
        var nativeScores = player.BaseStats.Keys.ToDictionary(ability => ability, _ => 40);

        for (var reconnect = 0; reconnect < 3; reconnect++)
            Restore(player, (ability, score) => nativeScores[ability] = score);

        foreach (var ability in player.BaseStats.Keys)
            nativeScores[ability].Should().Be(ability == racialStat ? 22 : 21);
        JsonConvert.SerializeObject(player).Should().Be(savedRecord);
    }

    [Test]
    public void LoginRestoresFinalAttributesEvenWhenNoMigrationIsPending()
    {
        var source = ReadSource("Service", "Migration.cs");
        var method = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>().Single(node => node.Identifier.Text == "RunPlayerMigrations");
        var restore = method.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Single(node => node.Expression.ToString() == "Stat.RestorePlayerAttributes");
        var migrationLoop = method.DescendantNodes().OfType<ForEachStatementSyntax>().Single();
        restore.Ancestors().Should().NotContain(migrationLoop, "players without pending migrations still need restoration");
        restore.SpanStart.Should().BeGreaterThan(migrationLoop.Span.End);
        restore.SpanStart.Should().BeLessThan(method.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Single(node => node.Expression.ToString() == "Perk.RestorePlayerFeats").SpanStart);
    }

    [Test]
    public void AttributePurchasesApplySavedTotalInsteadOfIncrementingStaleCharacterScore()
    {
        var source = ReadSource("Feature", "GuiDefinition", "ViewModel", "CharacterSheetViewModel.cs");
        source.Should().NotContain("ModifyRawAbilityScore");
        source.Should().Contain("DB.Set(dbPlayer);\n                Stat.ApplyPlayerStat(dbPlayer, _target, ability);");
    }

    private static void Restore(Player player, Action<AbilityType, int> setScore)
    {
        typeof(Stat).GetMethod("RestorePlayerAttributes", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { player, setScore });
    }

    private static string ReadSource(params string[] path)
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server")))
            root = root.Parent;
        root.Should().NotBeNull();
        return File.ReadAllText(Path.Combine(new[] { root!.FullName, "SWLOR.Game.Server" }.Concat(path).ToArray()))
            .Replace("\r\n", "\n");
    }
}
