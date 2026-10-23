using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Pins how a crafter's selected style outlives its status effect: the field on the player record, the rules for what is restored,
/// and that every path that has to record, restore or clear it does. The engine half is <c>CraftingStyleSelectionEngineTests</c>.
/// </summary>
public class CraftingStyleSelectionTests
{
    private static readonly IReadOnlyList<CraftingStyleDefinition> Definitions = CraftingStyleSelection.Definitions;

    private static Player Record(params PerkType[] ownedPerks)
    {
        var record = new Player("style-test");
        foreach (var perk in ownedPerks)
            record.Perks[perk] = 1;
        return record;
    }

    [Test]
    public void TheCircuitStylesAreRegisteredWithTheirSkillPerkAndStatusEffect()
    {
        Definitions.Should().Equal(
            new CraftingStyleDefinition(SkillType.Engineering, PerkType.OverclockStyle, typeof(OverclockStatusEffect)),
            new CraftingStyleDefinition(SkillType.Engineering, PerkType.SafeModeStyle, typeof(SafeModeStatusEffect)));
    }

    [Test]
    public void ARecordFromBeforeStylesWereRememberedLoadsWithNoSelection()
    {
        var legacy = $"{{\"Id\":\"old\",\"Version\":23,\"Name\":\"Old Timer\",\"Perks\":{{\"{(int)PerkType.OverclockStyle}\":1}}}}";
        var player = JsonConvert.DeserializeObject<Player>(legacy)!;

        player.SelectedCraftingStyles.Should().NotBeNull().And.BeEmpty();
        CraftingStyleSelection.Selected(player, SkillType.Engineering).Should().Be(PerkType.Invalid);
        CraftingStyleSelection.Reconcile(player, Definitions, out var active).Should().BeFalse("there is nothing to drop");
        active.Should().BeEmpty();
    }

    [Test]
    public void ARecordThatSavedNullForTheSelectionIsTreatedAsEmpty()
    {
        var player = JsonConvert.DeserializeObject<Player>("{\"Id\":\"x\",\"SelectedCraftingStyles\":null}")!;

        CraftingStyleSelection.Selected(player, SkillType.Engineering).Should().Be(PerkType.Invalid);
        CraftingStyleSelection.Set(player, SkillType.Engineering, PerkType.SafeModeStyle).Should().BeTrue();
        player.SelectedCraftingStyles.Should().ContainKey(SkillType.Engineering);
    }

    [Test]
    public void TheSelectionRoundTripsThroughTheSavedRecord()
    {
        var player = Record(PerkType.OverclockStyle);
        CraftingStyleSelection.Set(player, SkillType.Engineering, PerkType.OverclockStyle);

        var restored = JsonConvert.DeserializeObject<Player>(JsonConvert.SerializeObject(player))!;

        CraftingStyleSelection.Selected(restored, SkillType.Engineering).Should().Be(PerkType.OverclockStyle);
    }

    [Test]
    public void SelectingAStyleReplacesTheSkillsPreviousOne_AndLeavesOtherSkillsAlone()
    {
        var player = Record();
        CraftingStyleSelection.Set(player, SkillType.Engineering, PerkType.OverclockStyle).Should().BeTrue();
        CraftingStyleSelection.Set(player, SkillType.Engineering, PerkType.OverclockStyle).Should().BeFalse("nothing changed");
        CraftingStyleSelection.Set(player, SkillType.Smithery, PerkType.SafeModeStyle).Should().BeTrue();

        CraftingStyleSelection.Set(player, SkillType.Engineering, PerkType.SafeModeStyle).Should().BeTrue();

        CraftingStyleSelection.Selected(player, SkillType.Engineering).Should().Be(PerkType.SafeModeStyle, "one style per skill");
        CraftingStyleSelection.Selected(player, SkillType.Smithery).Should().Be(PerkType.SafeModeStyle);
        player.SelectedCraftingStyles.Should().HaveCount(2);
    }

    [Test]
    public void SelectingNoStyleClearsTheSkillsEntry()
    {
        var player = Record();
        CraftingStyleSelection.Set(player, SkillType.Engineering, PerkType.OverclockStyle);

        CraftingStyleSelection.Set(player, SkillType.Engineering, PerkType.Invalid).Should().BeTrue();
        CraftingStyleSelection.Set(player, SkillType.Engineering, PerkType.Invalid).Should().BeFalse("it was already clear");

        CraftingStyleSelection.Selected(player, SkillType.Engineering).Should().Be(PerkType.Invalid);
        player.SelectedCraftingStyles.Should().BeEmpty();
    }

    [Test]
    public void AnOwnedSelectedStyleIsRestored()
    {
        var player = Record(PerkType.OverclockStyle, PerkType.SafeModeStyle);
        CraftingStyleSelection.Set(player, SkillType.Engineering, PerkType.SafeModeStyle);

        CraftingStyleSelection.Reconcile(player, Definitions, out var active).Should().BeFalse();

        active.Should().ContainSingle("only the selected style comes back, never both")
            .Which.StatusEffectType.Should().Be(typeof(SafeModeStatusEffect));
        CraftingStyleSelection.Selected(player, SkillType.Engineering).Should().Be(PerkType.SafeModeStyle);
    }

    [Test]
    public void AStyleWhosePerkWasRefundedIsNotRestored_AndTheEntryIsDropped()
    {
        var player = Record(PerkType.SafeModeStyle);
        CraftingStyleSelection.Set(player, SkillType.Engineering, PerkType.OverclockStyle);

        CraftingStyleSelection.Reconcile(player, Definitions, out var active).Should().BeTrue();

        active.Should().BeEmpty("the crafter no longer owns Overclock");
        player.SelectedCraftingStyles.Should().BeEmpty();
    }

    [Test]
    public void APerkAtLevelZeroDoesNotCountAsOwned()
    {
        var player = Record();
        player.Perks[PerkType.OverclockStyle] = 0;
        CraftingStyleSelection.Set(player, SkillType.Engineering, PerkType.OverclockStyle);

        CraftingStyleSelection.Reconcile(player, Definitions, out var active).Should().BeTrue();

        active.Should().BeEmpty();
    }

    [Test]
    public void AnEntryThatNamesNoStyleOfItsSkillIsDropped()
    {
        var player = Record(PerkType.OverclockStyle);
        CraftingStyleSelection.Set(player, SkillType.Smithery, PerkType.OverclockStyle);
        CraftingStyleSelection.Set(player, SkillType.Engineering, PerkType.Tame);
        player.Perks[PerkType.Tame] = 1;

        CraftingStyleSelection.Reconcile(player, Definitions, out var active).Should().BeTrue();

        active.Should().BeEmpty("neither entry is a registered style of its skill");
        player.SelectedCraftingStyles.Should().BeEmpty();
    }

    [Test]
    public void AFullRebuildClearsEverySelection()
    {
        var player = Record(PerkType.OverclockStyle);
        CraftingStyleSelection.Set(player, SkillType.Engineering, PerkType.OverclockStyle);
        CraftingStyleSelection.Set(player, SkillType.Smithery, PerkType.SafeModeStyle);

        CraftingStyleSelection.ClearAll(player).Should().BeTrue();
        CraftingStyleSelection.ClearAll(player).Should().BeFalse("nothing was left");

        player.SelectedCraftingStyles.Should().BeEmpty();
    }

    // The effect is lost on death and on logout, so these are the paths that have to bring it back or keep the record right.

    [Test]
    public void LoginRestoresTheSelectedStyles()
    {
        var handler = Method(Source("Service", "CraftService", "CraftingStyleSelection.cs"), "OnPlayerEnter");

        handler.AttributeLists.ToString().Should().Contain("NWNEventHandler(ScriptName.OnModuleEnter)");
        Squash(handler).Should().Contain("Restore(GetEnteringObject())");
    }

    [Test]
    public void EveryReviveRestoresTheSelectedStyles_ThroughTheOneHookEveryReviveCalls()
    {
        Squash(Method(Source("Feature", "PlayerTemporaryEffects.cs"), "RestoreGhostAfterRevive"))
            .Should().Contain("CraftingStyleSelection.Restore(creature)");

        var gameServer = Path.Combine(FindRepositoryRoot().FullName, "SWLOR.Game.Server");
        var revives = Directory.EnumerateFiles(gameServer, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                           !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(path => (Path: path, Text: File.ReadAllText(path)))
            .Where(file => file.Text.Contains("EffectResurrection()"))
            .ToList();

        revives.Should().NotBeEmpty();
        foreach (var file in revives)
        {
            Regex.Matches(file.Text, Regex.Escape("EffectResurrection()")).Count.Should().Be(
                Regex.Matches(file.Text, Regex.Escape("RestoreGhostAfterRevive(")).Count,
                $"every revive in {Path.GetFileName(file.Path)} must call RestoreGhostAfterRevive, which restores the crafting styles");
        }
    }

    [Test]
    public void ChangingTheStyleInTheWindowRecordsIt_AndARefundedStyleIsForgotten()
    {
        var styles = Source("Service", "CraftService", "Minigame", "Circuit", "CircuitStyles.cs");

        Squash(Method(styles, "Select")).Should().Contain("CraftingStyleSelection.Record(player,Skill,PerkOf(style))");
        Squash(Method(styles, "RemoveUnowned")).Should().Contain("CraftingStyleSelection.ClearIfSelected(player,Skill,PerkOf(style))");
    }

    [Test]
    public void EachStylePerkClearsItsSelectionWhenRefunded()
    {
        var engineering = Source("Feature", "PerkDefinition", "EngineeringPerkDefinition.cs");

        foreach (var perk in new[] { "OverclockStyle", "SafeModeStyle" })
        {
            Squash(Method(engineering, perk)).Should().Contain(".TriggerRefund(CircuitStyles.RemoveUnowned)", perk);
        }
    }

    [Test]
    public void TheFullRebuildClearsTheSelectionsWithTheRefundedPerks_BeforeSavingTheRecord()
    {
        var rebuild = Source("Feature", "GuiDefinition", "ViewModel", "CharacterFullRebuildViewModel.cs");
        var refund = rebuild.DescendantNodes().OfType<LocalFunctionStatementSyntax>()
            .Single(function => function.Identifier.ValueText == "RefundAllPerks");

        var calls = refund.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();
        var clear = calls.Single(call => call.Expression.ToString() == "CraftingStyleSelection.ClearAll");
        var save = calls.Last(call => call.Expression.ToString() == "DB.Set");

        clear.ArgumentList.Arguments.Single().ToString().Should().Be("dbPlayer");
        clear.Span.End.Should().BeLessThan(save.Span.Start, "the record that is saved must be the one the selections were cleared on");
    }

    [Test]
    public void TheCraftWindowRestoresTheRecordedStyleWhenItOpens()
    {
        var initialize = Method(Source("Feature", "GuiDefinition", "ViewModel", "CircuitCraftViewModel.cs"), "Initialize");

        Squash(initialize).Should().Contain("CraftingStyleSelection.Restore(Player)");
    }

    private static string Squash(SyntaxNode node) => Regex.Replace(node.ToString(), @"\s+", string.Empty);

    private static SyntaxNode Source(params string[] path)
    {
        var file = Path.Combine(new[] { FindRepositoryRoot().FullName, "SWLOR.Game.Server" }.Concat(path).ToArray());
        return CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot();
    }

    private static MethodDeclarationSyntax Method(SyntaxNode root, string name) =>
        root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == name);

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("repository root should be discoverable from the test directory");
        return directory!;
    }
}
