using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using SWLOR.Game.Server.Service.GuiService;
using static SWLOR.Game.Server.Tests.Service.Prospecting.ProspectingTestData;

namespace SWLOR.Game.Server.Tests.Service.Prospecting;

/// <summary>Covers the Prospecting window's layout, its first-use help and the words its buttons show.</summary>
public class ProspectingWindowTests
{
    [Test]
    public void TheWindowBuildsWithoutLayoutFindings()
    {
        using var validationBuild = GuiLayoutValidator.BeginValidationOnlyBuild();

        var window = new ProspectingDefinition().BuildWindow();

        window.Type.Should().Be(GuiWindowType.Prospecting);
        window.LayoutFindings.Should().BeEmpty();
    }

    [Test]
    public void TheWindowHasItsOwnId_AndNoOtherWindowSharesIt()
    {
        ((int)GuiWindowType.Prospecting).Should().Be(90);
        Enum.GetValues<GuiWindowType>().Count(window => window == GuiWindowType.Prospecting).Should().Be(1);
        Enum.GetValues<GuiWindowType>().Select(window => (int)window).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void TheWindowHasAnActionRowForEveryActionOfTheEngine_AndARowForEachSeam()
    {
        ProspectingViewModel.ActionCount.Should().Be(new ProspectingEngine(new ScriptedMinigameRandom(), Setup(Node(2))).Actions.Count);
        ProspectingViewModel.SeamRows.Should().Be(ProspectingRules.MaxSeams);
        foreach (var name in new[]
                 {
                     ProspectingViewModel.ActionTextProperty(4), ProspectingViewModel.ActionSummaryProperty(4),
                     ProspectingViewModel.ActionTooltipProperty(4), ProspectingViewModel.ActionColorProperty(4),
                     ProspectingViewModel.ActionEnabledProperty(4), ProspectingViewModel.SeamTextProperty(2),
                     ProspectingViewModel.SeamColorProperty(2)
                 })
        {
            typeof(ProspectingViewModel).GetProperty(name).Should().NotBeNull(name);
        }
    }

    [Test]
    public void TheHelpPageOpensOnTheFirstSessionOnly()
    {
        var player = new Player("p1");

        CraftMinigameHelp.ShouldOpenOnFirstUse(player, CraftMinigameType.Prospecting).Should().BeTrue();
        CraftMinigameHelp.TryRecordFirstUse(player, CraftMinigameType.Prospecting, DateTime.UtcNow).Should().BeTrue();
        CraftMinigameHelp.TryRecordFirstUse(player, CraftMinigameType.Prospecting, DateTime.UtcNow).Should().BeFalse();
        CraftMinigameHelp.ShouldOpenOnFirstUse(player, CraftMinigameType.Forge).Should().BeTrue("each minigame has its own first use");
    }

    [Test]
    public void AButtonShowsTheExactResult_AndItsTooltipWhatTheActionDoes()
    {
        var engine = Engine("rg", pool: 4);

        var survey = ProspectingActionText.Describe(engine, engine.Preview(ProspectingActionIds.Survey));
        var strike = ProspectingActionText.Describe(engine, engine.Preview(ProspectingActionIds.Strike));
        var heavy = ProspectingActionText.Describe(engine, engine.Preview(ProspectingActionIds.HeavyStrike));
        var leave = ProspectingActionText.Describe(engine, engine.Preview(ProspectingActionIds.Leave));

        survey.Summary.Should().Be("Reveals 1 hidden seam if there is one. Stability 100 -> 88", "a Survey costs Stability and no charge");
        strike.Summary.Should().Be("Takes 1 unit (70%) or 2 units (30%). Stability 100 -> 80, Charges 20 -> 19");
        heavy.Summary.Should().Be(
            "Takes 2 units (25%) or 3 units (50%) or 4 units (25%). 20% chance to shatter a gem pocket you have not surveyed. Stability 100 -> 64, Charges 20 -> 18. 25% MINES OUT THE NODE");
        leave.Summary.Should().Be("Leaves the rest of the node for others");
        survey.Kind.Should().Be(ProspectingActionKind.Normal);
        strike.Kind.Should().Be(ProspectingActionKind.Normal);
        heavy.Kind.Should().Be(ProspectingActionKind.Ends);
        leave.Kind.Should().Be(ProspectingActionKind.Normal, "Leave always ends the session, so it is not a warning");
        strike.Tooltip.Should().StartWith("Takes 1 or 2 units");
        heavy.Tooltip.Should().Contain("whether or not a pocket is hidden here");
    }

    [Test]
    public void TheActionThatMinesTheNodeOutSaysSo_AndTheOneThatCollapsesItSaysThat()
    {
        var mining = Engine("", pool: 1);
        var collapsing = Engine("", pool: 4, stability: ProspectingRules.StrikeStability);
        var partly = Engine("", pool: 2);

        var mine = ProspectingActionText.Describe(mining, mining.Preview(ProspectingActionIds.Strike));
        var collapse = ProspectingActionText.Describe(collapsing, collapsing.Preview(ProspectingActionIds.Strike));
        var maybe = ProspectingActionText.Describe(partly, partly.Preview(ProspectingActionIds.Strike));

        mine.Kind.Should().Be(ProspectingActionKind.Ends);
        mine.Summary.Should().EndWith(ProspectingActionText.MinesOut);
        collapse.Kind.Should().Be(ProspectingActionKind.Collapses);
        collapse.Summary.Should().EndWith(ProspectingActionText.Collapses);
        maybe.Kind.Should().Be(ProspectingActionKind.Ends);
        maybe.Summary.Should().EndWith($"30% {ProspectingActionText.MinesOut}", "only the roll that takes both units mines it out");
    }

    [Test]
    public void ExtractNamesTheSeamItClaims_AndADisabledActionShowsWhyInPlaceOfTheResult()
    {
        var engine = Engine("RG", pool: 4);
        var plain = Engine("r", pool: 4, charges: 1);

        var extract = ProspectingActionText.Describe(engine, engine.Preview(ProspectingActionIds.Extract));
        var noSeam = ProspectingActionText.Describe(plain, plain.Preview(ProspectingActionIds.Extract));
        var noCharges = ProspectingActionText.Describe(plain, plain.Preview(ProspectingActionIds.HeavyStrike));

        extract.Summary.Should().StartWith("Claims a gem pocket for 1 gem");
        noSeam.Kind.Should().Be(ProspectingActionKind.Unavailable);
        noSeam.Summary.Should().Be(ProspectingEngine.NoRevealedSeam);
        noCharges.Summary.Should().Be(ProspectingEngine.NotEnoughCharges);
    }

    [Test]
    public void ThePerformedActionIsDescribedByItsOutcomeAndCost()
    {
        var engine = Engine("R", pool: 4, rolls: [RichBonus]);

        var result = engine.Perform(ProspectingActionIds.Extract);

        ProspectingActionText.OutcomeText(result.Outcome).Should().Be(
            "Claims a rich seam and finds an extra ore roll. Stability 100 -> 76, Charges 20 -> 18");
    }

    [Test]
    public void SeamsAreListedAsTheWindowShowsThem()
    {
        ProspectingActionText.SeamText(new ProspectingSeam(ProspectingSeamKind.GemPocket, true))
            .Should().Be("Gem pocket: Extract for 1 gem in place of its unit");
        ProspectingActionText.SeamText(new ProspectingSeam(ProspectingSeamKind.Rich, true))
            .Should().Be("Rich seam: Extract for its unit and a 25% chance of an extra ore roll");
    }

    [Test]
    public void EveryTextTheWindowShowsIsAsciiPunctuation()
    {
        var engine = Engine("RG", pool: 4);
        var texts = engine.PreviewAll().SelectMany(preview => new[]
        {
            ProspectingActionText.SummaryText(engine, preview), ProspectingActionText.Describe(engine, preview).Tooltip,
            preview.Action.Description, preview.Action.Name
        }).Concat(engine.RevealedSeams.Select(ProspectingActionText.SeamText))
          .Concat(new[]
          {
              ProspectingActionText.MinesOut, ProspectingActionText.Collapses, ProspectingActionText.UsesUp,
              ProspectingEngine.NoRevealedSeam, ProspectingEngine.NotEnoughCharges, ProspectingEngine.NodeSpent,
              SWLOR.Game.Server.Service.ProspectingService.ProspectingLifecycle.InUseMessage,
              SWLOR.Game.Server.Service.ProspectingService.ProspectingLifecycle.SpentMessage,
              SWLOR.Game.Server.Service.ProspectingService.ProspectingLifecycle.NoChargesMessage,
              SWLOR.Game.Server.Service.ProspectingService.ProspectingLifecycle.MinedOutMessage,
              SWLOR.Game.Server.Service.ProspectingService.ProspectingLifecycle.CollapsedMessage,
              SWLOR.Game.Server.Service.ProspectingService.ProspectingLifecycle.LeftMessage,
              SWLOR.Game.Server.Service.ProspectingService.ProspectingLifecycle.StillWorkingMessage,
              SWLOR.Game.Server.Service.ProspectingService.ProspectingLifecycle.QuickHarvestTooLateMessage
          });

        texts.Should().OnlyContain(text => text.All(character => character < 128));
    }
}
