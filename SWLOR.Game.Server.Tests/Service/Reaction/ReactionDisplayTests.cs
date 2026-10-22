using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Reaction;

namespace SWLOR.Game.Server.Tests.Service.Reaction;

/// <summary>Covers the text the Reaction window shows for the engine's previews.</summary>
public class ReactionDisplayTests
{
    private static ReactionEngine EngineAt(int start, int rank = 30, int level = 30, int intensity = 2) =>
        new(
            new SeededMinigameRandom(1),
            new ReactionSettings(new ReactionParameters(ReactionCyclePattern.Brew, 6, intensity), rank, level),
            start);

    private static ReactionActionView View(ReactionEngine engine, string action) =>
        ReactionDisplay.DescribeAction(engine.Preview(action), engine.CurrentPhase, engine.PhaseAt);

    [Test]
    public void ANormalActionShowsEveryMeterChangeAndThePhaseThatFollows()
    {
        var engine = EngineAt(3);

        var view = View(engine, ReactionEngine.Charge);

        view.Kind.Should().Be(ReactionActionKind.Normal);
        view.Summary.Should().Contain($"Potency +{engine.Settings.ChargePotency * 2}");
        view.Summary.Should().Contain($"Stability -{engine.Settings.ActionStability + engine.Settings.PeakChargeStability}");
        view.Summary.Should().EndWith("Next: Settling");
        view.Tooltip.Should().Contain("Charge in Peak");
        view.Tooltip.Should().Contain("Cost:");
        view.Tooltip.Should().Contain("Result:");
    }

    [Test]
    public void DampenShowsThatThePhaseIsHeld()
    {
        var view = View(EngineAt(3), ReactionEngine.Dampen);

        view.Summary.Should().Contain($"CP -{ReactionRules.DampenCp}");
        view.Summary.Should().EndWith("Next: Peak");
    }

    [Test]
    public void TheActionThatFinishesTheItemSaysSoBeforeTheClick()
    {
        var engine = EngineAt(0, rank: 50, level: 3);
        for (var turn = 0; turn < 3; turn++)
            engine.Perform(ReactionEngine.Charge);

        var view = View(engine, ReactionEngine.Charge);

        view.Kind.Should().Be(ReactionActionKind.FinishesItem);
        view.Summary.Should().StartWith("Finishes the item");
        view.Summary.Should().NotContain("Next:", "the cycle does not advance once the item is finished");
        view.Tooltip.Should().Contain("Finishes the item");
    }

    [Test]
    public void TheActionThatCouldBreakThePieceSaysSoBeforeTheClick()
    {
        var engine = EngineAt(0);
        while (engine.Meters.Integrity.Value > engine.Settings.ActionStability)
            engine.Perform(ReactionEngine.Refine);

        var view = View(engine, ReactionEngine.Refine);

        view.Kind.Should().Be(ReactionActionKind.BreaksItem);
        view.Summary.Should().StartWith("May break the piece");
        view.Tooltip.Should().Contain("May break the piece");
    }

    [Test]
    public void AnActionThatCannotBeUsedGivesTheReason()
    {
        var engine = EngineAt(0);

        var full = View(engine, ReactionEngine.Stabilize);
        var noCp = View(EngineAt(0, rank: 0, level: 3), ReactionEngine.Dampen);
        var locked = View(EngineAt(0, rank: 5, level: 3), ReactionEngine.Stabilize);

        full.Kind.Should().Be(ReactionActionKind.Unavailable);
        full.Summary.Should().Be("Unavailable: Stability is already full.");
        noCp.Kind.Should().Be(ReactionActionKind.Unavailable);
        noCp.Summary.Should().Be("Unavailable: Not enough CP.");
        locked.Kind.Should().Be(ReactionActionKind.Unavailable);
        locked.Summary.Should().Be("Unavailable: Unlocks at Espionage rank 10.");
    }

    [Test]
    public void TheCycleStripBracketsTheCurrentPosition()
    {
        var cycle = new ReactionCycle(ReactionCyclePattern.Brew, 6);

        ReactionDisplay.CycleStrip(cycle, 3).Should().Be("C C R [P] S S");
        ReactionDisplay.CycleStrip(cycle, 0).Should().Be("[C] C R P S S");
        ReactionDisplay.CycleStrip(cycle, 5).Should().Be("C C R P S [S]");
    }

    [Test]
    public void ThePeakTextNamesTheIntensityAndWhatAPeakChargeCosts()
    {
        var settings = new ReactionSettings(new ReactionParameters(ReactionCyclePattern.Trigger, 5, 4), 20, 20);

        var text = ReactionDisplay.PeakText(settings);

        text.Should().Contain("intensity 4");
        text.Should().Contain((settings.ActionStability + ReactionRules.PeakChargeStability(4)).ToString());
    }

    [Test]
    public void AMeterWithNoMaximumShowsOnlyItsValue()
    {
        ReactionDisplay.MeterText("CP", new MinigameMeter(0, 0)).Should().Be("CP: 0");
        ReactionDisplay.MeterText("Potency", new MinigameMeter(12, 100)).Should().Be("Potency: 12/100");
    }

    [Test]
    public void EveryPhaseHasAnEffectLine()
    {
        foreach (var phase in Enum.GetValues<ReactionPhase>())
        {
            ReactionDisplay.PhaseName(phase).Should().Be(phase.ToString());
            ReactionDisplay.PhaseEffect(phase).Should().NotBeNullOrWhiteSpace();
        }
    }

    [Test]
    public void AllPlayerFacingTextIsPlainAscii()
    {
        var strings = new List<string>();
        foreach (var start in new[] { 0, 2, 3, 4 })
        {
            var engine = EngineAt(start);
            strings.AddRange(engine.Actions.SelectMany(action => new[] { action.Name, action.Description }));
            foreach (var action in engine.Actions)
            {
                var view = View(engine, action.Id);
                strings.Add(view.Summary);
                strings.Add(view.Tooltip);
            }

            strings.Add(ReactionDisplay.PhaseEffect(engine.CurrentPhase));
            strings.Add(ReactionDisplay.PeakText(engine.Settings));
            strings.Add(ReactionDisplay.CycleStrip(engine.Cycle, start));
        }

        strings.Add(ReactionDisplay.FinishesTheItem);
        strings.Add(ReactionDisplay.BreaksThePiece);

        strings.Should().OnlyContain(text => text.All(character => character == '\n' || (character >= ' ' && character < 127)));
    }
}
