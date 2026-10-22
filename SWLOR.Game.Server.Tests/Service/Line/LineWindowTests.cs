using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.GuiService;
using static SWLOR.Game.Server.Tests.Service.Line.LineTestData;

namespace SWLOR.Game.Server.Tests.Service.Line;

/// <summary>Covers the Line window's layout, its first-use help and the words its buttons show.</summary>
public class LineWindowTests
{
    [Test]
    public void TheWindowBuildsWithoutLayoutFindings()
    {
        using var validationBuild = GuiLayoutValidator.BeginValidationOnlyBuild();

        var window = new LineDefinition().BuildWindow();

        window.Type.Should().Be(GuiWindowType.Line);
        window.LayoutFindings.Should().BeEmpty();
    }

    [Test]
    public void TheWindowHasItsOwnId_AndNoOtherWindowSharesIt()
    {
        ((int)GuiWindowType.Line).Should().Be(87);
        Enum.GetValues<GuiWindowType>().Count(window => window == GuiWindowType.Line).Should().Be(1);
        Enum.GetValues<GuiWindowType>().Select(window => (int)window).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void TheWindowHasFourActionRows()
    {
        LineViewModel.ActionCount.Should().Be(4);
        foreach (var name in new[]
                 {
                     LineViewModel.ActionTextProperty(3), LineViewModel.ActionSummaryProperty(3), LineViewModel.ActionTooltipProperty(3),
                     LineViewModel.ActionColorProperty(3), LineViewModel.ActionEnabledProperty(3)
                 })
        {
            typeof(LineViewModel).GetProperty(name).Should().NotBeNull(name);
        }
    }

    [Test]
    public void TheHelpPageOpensOnTheFirstFightOnly()
    {
        var player = new Player("p1");

        CraftMinigameHelp.ShouldOpenOnFirstUse(player, CraftMinigameType.Line).Should().BeTrue();
        CraftMinigameHelp.TryRecordFirstUse(player, CraftMinigameType.Line, DateTime.UtcNow).Should().BeTrue();
        CraftMinigameHelp.TryRecordFirstUse(player, CraftMinigameType.Line, DateTime.UtcNow).Should().BeFalse();
        CraftMinigameHelp.ShouldOpenOnFirstUse(player, CraftMinigameType.Forge).Should().BeTrue("each minigame has its own first use");
    }

    [Test]
    public void AButtonShowsTheExactResult_AndItsTooltipTheFishsMove()
    {
        var engine = Engine(Setup(), FishMove.Run);

        var reel = LineActionText.Describe(engine, engine.Preview(LineActionIds.Reel));
        var give = LineActionText.Describe(engine, engine.Preview(LineActionIds.GiveLine));
        var hold = LineActionText.Describe(engine, engine.Preview(LineActionIds.Hold));
        var hook = LineActionText.Describe(engine, engine.Preview(LineActionIds.SetHook));

        reel.Summary.Should().Be("Distance 16 -> 10, Tension 0 -> 10");
        give.Summary.Should().Be("Distance 16 -> 24");
        hold.Summary.Should().Be("Distance 16 -> 18, Tension 0 -> 4, Stamina 16 -> 12");
        hook.Summary.Should().Be("Tension 0 -> 2, Stamina 16 -> 10");
        reel.Kind.Should().Be(LineActionKind.Normal);
        reel.Tooltip.Should().StartWith("Closes Distance and adds Tension");
        reel.Tooltip.Should().Contain("reeling now adds 6 extra Tension");
    }

    [Test]
    public void TheActionThatLandsTheFish_SaysSo_AndTheOneThatSnapsTheLineSaysThat()
    {
        var landing = Engine(Setup(start: 6, max: 16), FishMove.Tire);
        var snapping = Engine(Setup(cap: 4), FishMove.Run);
        var escaping = Engine(Setup(start: 29, pull: 1), FishMove.Run);

        var land = LineActionText.Describe(landing, landing.Preview(LineActionIds.Reel));
        var snap = LineActionText.Describe(snapping, snapping.Preview(LineActionIds.Reel));
        var escape = LineActionText.Describe(escaping, escaping.Preview(LineActionIds.GiveLine));

        land.Kind.Should().Be(LineActionKind.LandsFish);
        land.Summary.Should().EndWith(LineActionText.LandsTheFish);
        snap.Kind.Should().Be(LineActionKind.LosesFish);
        snap.Summary.Should().EndWith(LineActionText.SnapsTheLine);
        escape.Kind.Should().Be(LineActionKind.LosesFish);
        escape.Summary.Should().EndWith(LineActionText.FishEscapes);
    }

    [Test]
    public void ADisabledAction_ShowsWhyInPlaceOfTheResult()
    {
        var engine = Engine(Setup(), FishMove.Run, FishMove.Tire);
        engine.Perform(LineActionIds.GiveLine);

        var hook = LineActionText.Describe(engine, engine.Preview(LineActionIds.SetHook));

        hook.Kind.Should().Be(LineActionKind.Unavailable);
        hook.Summary.Should().Be("Only on the first turn.");
    }

    [Test]
    public void TheMoveHints_NameTheNumbersOfThisFish()
    {
        var setup = Setup(pull: 3);

        LineActionText.MoveHint(setup, FishMove.Run).Should().Contain("adds 5 extra Tension");
        LineActionText.MoveHint(setup, FishMove.Dive).Should().Contain("takes 3 Tension");
        LineActionText.MoveHint(setup, FishMove.Thrash).Should().Contain("takes 5 Tension").And.Contain("only Hold");
        LineActionText.MoveHint(setup, FishMove.Tire).Should().Contain("loses 2 Stamina");
        LineActionText.ForecastText(new[] { FishMove.Run, FishMove.Thrash }).Should().Be("RUN, then THRASH");
        LineActionText.MeterText("Tension", 3, 10, "the line snaps at 10").Should().Be("Tension: 3 of 10 (the line snaps at 10)");
    }

    [Test]
    public void EveryTextTheWindowShowsIsAsciiPunctuation()
    {
        var engine = Engine(Setup(), FishMove.Thrash);
        var texts = engine.PreviewAll().SelectMany(preview => new[]
        {
            LineActionText.ResultText(engine, preview), LineActionText.Describe(engine, preview).Tooltip, preview.Action.Description,
            preview.Action.Name
        }).Concat(Enum.GetValues<FishMove>().SelectMany(move => new[] { LineActionText.MoveName(move), LineActionText.MoveHint(engine.Setup, move) }));

        texts.Should().OnlyContain(text => text.All(character => character < 128));
    }
}
