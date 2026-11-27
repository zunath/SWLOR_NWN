using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.DuelService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The Dueling Ring's rules: which blows interfere, the protection window, draws, forfeits and results.
/// </summary>
public class DuelRulesTests
{
    private const uint Challenger = 10;
    private const uint Target = 20;
    private const uint Stranger = 30;
    private const uint Invalid = 0x7F000000;

    private static readonly DateTime Start = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    private static DuelRecord NewDuel() => new(Challenger, Target, 99, "city-1", Start);

    // The numbers the plan sets

    [Test]
    public void Protection_LastsSixSeconds_AndTheRingIsTenMetersWide()
    {
        DuelRules.ProtectionSeconds.Should().Be(6.0);
        DuelRules.RingRadiusMeters.Should().Be(10.0f);
    }

    // Who dealt the blow

    [TestCase(Challenger, DuelBlowType.Duel)]
    [TestCase(Target, DuelBlowType.Duel)]
    [TestCase(Stranger, DuelBlowType.Interference)]
    public void ClassifyBlow_JudgesTheSource(uint source, DuelBlowType expected)
    {
        DuelRules.ClassifyBlow(NewDuel(), true, source).Should().Be(expected);
    }

    [Test]
    public void ClassifyBlow_WithNoSource_IsNeitherDuelNorInterference()
    {
        DuelRules.ClassifyBlow(NewDuel(), false, Invalid).Should().Be(DuelBlowType.Unattributed);
    }

    [Test]
    public void ClassifyBlow_ADuelistsOwnBlowOnThemselvesIsPartOfTheDuel()
    {
        DuelRules.ClassifyBlow(NewDuel(), true, Challenger).Should().Be(DuelBlowType.Duel);
    }

    // Tracing a pet, a droid or an effect back to its owner

    private static uint Trace(uint attacker, Dictionary<uint, uint> controllers, Dictionary<uint, uint> masters) =>
        DuelRules.TraceSource(
            attacker,
            id => id != Invalid,
            id => controllers.GetValueOrDefault(id, Invalid),
            id => masters.GetValueOrDefault(id, Invalid));

    [Test]
    public void TraceSource_APlayerStandsForThemselves()
    {
        Trace(Challenger, new Dictionary<uint, uint> { [Challenger] = Challenger }, new Dictionary<uint, uint>())
            .Should().Be(Challenger);
    }

    [Test]
    public void TraceSource_APetOrDroidStandsForThePlayerItFightsFor()
    {
        const uint pet = 41;
        const uint droid = 42;
        var controllers = new Dictionary<uint, uint> { [pet] = Challenger, [droid] = Stranger };

        var duel = NewDuel();
        DuelRules.ClassifyBlow(duel, true, Trace(pet, controllers, new Dictionary<uint, uint>()))
            .Should().Be(DuelBlowType.Duel, "a duelist's own pet counts as the duelist");
        DuelRules.ClassifyBlow(duel, true, Trace(droid, controllers, new Dictionary<uint, uint>()))
            .Should().Be(DuelBlowType.Interference, "a third player's droid interferes");
    }

    [Test]
    public void TraceSource_AnNpcsPetStandsForItsMaster_AndALoneNpcForItself()
    {
        const uint npcPet = 51;
        const uint npc = 52;
        var masters = new Dictionary<uint, uint> { [npcPet] = npc };
        var none = new Dictionary<uint, uint>();

        Trace(npcPet, none, masters).Should().Be(npc);
        Trace(npc, none, masters).Should().Be(npc);
    }

    [Test]
    public void TraceSource_ADamagingEffectWhoseCreatorIsGoneHasNoSource()
    {
        var none = new Dictionary<uint, uint>();

        var source = Trace(Invalid, none, none);

        source.Should().Be(Invalid);
        DuelRules.ClassifyBlow(NewDuel(), source != Invalid, source).Should().Be(DuelBlowType.Unattributed);
    }

    // The ordering: interference, then protection, then the blow

    [Test]
    public void Interrupt_VoidsTheDuel_AndProtectsBothForSixSeconds()
    {
        var duel = NewDuel();

        DuelRules.Interrupt(duel, Start).Should().BeTrue();

        duel.State.Should().Be(DuelState.Interrupted);
        duel.ProtectedUntilUtc.Should().Be(Start.AddSeconds(6));
        duel.InterruptionNoticePending.Should().BeTrue();
    }

    [Test]
    public void Interrupt_AgainDoesNotStretchTheProtection()
    {
        var duel = NewDuel();
        DuelRules.Interrupt(duel, Start);

        DuelRules.Interrupt(duel, Start.AddSeconds(4)).Should().BeFalse();

        duel.ProtectedUntilUtc.Should().Be(Start.AddSeconds(6));
    }

    [Test]
    public void ALethalBlowFromAnActiveDuelSubdues_WhoeverLandsIt()
    {
        var duel = NewDuel();

        DuelRules.ShouldSubdue(duel, Start.AddMinutes(30)).Should().BeTrue("an Active duel protects until it ends");
    }

    [Test]
    public void TheBlowThatVoidedTheDuelStillSubdues_AndTheNextOneAfterSixSecondsDoesNot()
    {
        var duel = NewDuel();
        var hit = Start.AddSeconds(20);
        DuelRules.Interrupt(duel, hit);

        DuelRules.ShouldSubdue(duel, hit).Should().BeTrue("the voiding blow lands with the duel already protected");
        DuelRules.ShouldSubdue(duel, hit.AddSeconds(5.9)).Should().BeTrue();
        DuelRules.ShouldSubdue(duel, hit.AddSeconds(6)).Should().BeFalse("normal rules apply once the 6 seconds pass");
        DuelRules.ShouldSubdue(duel, hit.AddSeconds(60)).Should().BeFalse();
    }

    [Test]
    public void ANonLethalThirdPartyBlowStillStartsTheProtection()
    {
        var duel = NewDuel();

        // A scratch from a stranger: the duel is voided and a lethal hit from anyone four seconds later still subdues.
        DuelRules.ClassifyBlow(duel, true, Stranger).Should().Be(DuelBlowType.Interference);
        DuelRules.Interrupt(duel, Start.AddSeconds(10));

        DuelRules.ShouldSubdue(duel, Start.AddSeconds(14)).Should().BeTrue();
        DuelRules.IsOver(duel, Start.AddSeconds(14)).Should().BeFalse();
        DuelRules.IsOver(duel, Start.AddSeconds(16)).Should().BeTrue();
    }

    [Test]
    public void ADuelNobodyInterruptedIsNeverOver()
    {
        DuelRules.IsOver(NewDuel(), Start.AddDays(1)).Should().BeFalse();
    }

    [Test]
    public void ADamageOverTimeEffectFromAThirdPlayerVoidsTheDuelOnItsFirstTick()
    {
        var duel = NewDuel();

        // The effect's creator is the attacker the damage pipeline sees, and a tick is a blow like any other.
        var blow = DuelRules.ClassifyBlow(duel, true, Stranger);
        if (blow == DuelBlowType.Interference)
            DuelRules.Interrupt(duel, Start.AddSeconds(3));

        duel.State.Should().Be(DuelState.Interrupted);
        DuelRules.ShouldSubdue(duel, Start.AddSeconds(4)).Should().BeTrue();
    }

    // Falls

    [Test]
    public void OneDuelistFalling_GivesTheOtherTheVictory()
    {
        var duel = NewDuel();

        DuelRules.RecordFall(duel, DuelSide.Challenger, Start).Should().BeTrue();

        var outcome = DuelRules.GetOutcome(duel);
        outcome.Should().Be(new DuelOutcome(DuelOutcomeType.Victory, DuelSide.Target));
        outcome!.Value.Loser.Should().Be(DuelSide.Challenger);
    }

    [Test]
    public void BothFallingInTheSamePass_IsADraw()
    {
        var duel = NewDuel();

        DuelRules.RecordFall(duel, DuelSide.Challenger, Start).Should().BeTrue();
        DuelRules.RecordFall(duel, DuelSide.Target, Start.AddSeconds(0.4)).Should().BeFalse("only the first fall starts the clock");

        var outcome = DuelRules.GetOutcome(duel);
        outcome.Should().Be(new DuelOutcome(DuelOutcomeType.Draw, DuelSide.None));
        outcome!.Value.Loser.Should().Be(DuelSide.None);
    }

    [Test]
    public void ASecondFallAfterThePassHasNoEffectOnTheResult()
    {
        var duel = NewDuel();
        DuelRules.RecordFall(duel, DuelSide.Challenger, Start);

        DuelRules.RecordFall(duel, DuelSide.Target, Start.AddSeconds(DuelRules.SamePassSeconds + 0.5));

        duel.TargetFell.Should().BeFalse();
        DuelRules.GetOutcome(duel).Should().Be(new DuelOutcome(DuelOutcomeType.Victory, DuelSide.Target));
    }

    [Test]
    public void TheSamePassWindowIncludesItsEdge()
    {
        DuelRules.IsSamePass(Start, Start.AddSeconds(DuelRules.SamePassSeconds)).Should().BeTrue();
        DuelRules.IsSamePass(Start, Start.AddSeconds(DuelRules.SamePassSeconds + 0.01)).Should().BeFalse();
    }

    [Test]
    public void AStrangerIsNotADuelistAndCannotFall()
    {
        var duel = NewDuel();

        DuelRules.GetSide(duel, Stranger).Should().Be(DuelSide.None);
        DuelRules.RecordFall(duel, DuelSide.None, Start).Should().BeFalse();
        DuelRules.GetOutcome(duel).Should().BeNull();
    }

    [Test]
    public void ADuelNobodyHasLostHasNoResult()
    {
        DuelRules.GetOutcome(NewDuel()).Should().BeNull();
    }

    // Forfeits

    [TestCase(DuelSide.Challenger, DuelSide.Target)]
    [TestCase(DuelSide.Target, DuelSide.Challenger)]
    public void LeavingTheRingOrLoggingOut_ForfeitsToTheOtherDuelist(DuelSide leaver, DuelSide expectedWinner)
    {
        var duel = NewDuel();

        DuelRules.RecordForfeit(duel, leaver);

        DuelRules.GetOutcome(duel).Should().Be(new DuelOutcome(DuelOutcomeType.Forfeit, expectedWinner));
    }

    [Test]
    public void BothLeavingTogether_IsADraw()
    {
        var duel = NewDuel();
        DuelRules.RecordForfeit(duel, DuelSide.Challenger);
        DuelRules.RecordForfeit(duel, DuelSide.Target);

        DuelRules.GetOutcome(duel).Should().Be(new DuelOutcome(DuelOutcomeType.Draw, DuelSide.None));
    }

    [Test]
    public void OneFallingAndTheOtherLeavingInTheSamePass_IsADraw()
    {
        var duel = NewDuel();
        DuelRules.RecordFall(duel, DuelSide.Challenger, Start);
        DuelRules.RecordForfeit(duel, DuelSide.Target);

        DuelRules.GetOutcome(duel).Should().Be(new DuelOutcome(DuelOutcomeType.Draw, DuelSide.None));
    }

    [Test]
    public void ADuelistWhoFellAndThenLeftStillLostByTheFall()
    {
        var duel = NewDuel();
        DuelRules.RecordFall(duel, DuelSide.Challenger, Start);
        DuelRules.RecordForfeit(duel, DuelSide.Challenger);

        DuelRules.GetOutcome(duel).Should().Be(new DuelOutcome(DuelOutcomeType.Victory, DuelSide.Target));
    }

    // The ring

    [TestCase(0f, 0f, true)]
    [TestCase(10f, 0f, true)]
    [TestCase(0f, -10f, true)]
    [TestCase(7f, 7f, true)]
    [TestCase(10.01f, 0f, false)]
    [TestCase(8f, 8f, false)]
    public void IsInsideRing_UsesAnEdgeInclusiveCircleAroundTheTerminal(float x, float y, bool expected)
    {
        DuelRules.IsInsideRing(0f, 0f, x, y).Should().Be(expected);
    }

    [Test]
    public void IsInsideRing_MeasuresFromTheTerminalNotTheOrigin()
    {
        DuelRules.IsInsideRing(100f, 50f, 109f, 50f).Should().BeTrue();
        DuelRules.IsInsideRing(100f, 50f, 111f, 50f).Should().BeFalse();
        DuelRules.IsInsideRing(100f, 50f, 100f, 50f, radius: 3f).Should().BeTrue();
        DuelRules.IsInsideRing(100f, 50f, 104f, 50f, radius: 3f).Should().BeFalse();
    }

    // The Gazette line

    [Test]
    public void ResultText_NamesBothDuelists()
    {
        DuelRules.BuildResultText(new DuelOutcome(DuelOutcomeType.Victory, DuelSide.Target), "Hardy Twi'lek", "Quiet Human")
            .Should().Be("Quiet Human defeated Hardy Twi'lek in a duel at the Dueling Ring.");
        DuelRules.BuildResultText(new DuelOutcome(DuelOutcomeType.Forfeit, DuelSide.Challenger), "Hardy Twi'lek", "Quiet Human")
            .Should().Be("Quiet Human forfeited a duel against Hardy Twi'lek at the Dueling Ring.");
        DuelRules.BuildResultText(new DuelOutcome(DuelOutcomeType.Draw, DuelSide.None), "Hardy Twi'lek", "Quiet Human")
            .Should().Be("Hardy Twi'lek and Quiet Human fought a duel at the Dueling Ring and both fell: a draw.");
    }

    [Test]
    public void ResultText_StartsWithACapital_AndFillsInBlankNames()
    {
        DuelRules.BuildResultText(new DuelOutcome(DuelOutcomeType.Victory, DuelSide.Challenger), "the masked stranger", " ")
            .Should().Be("The masked stranger defeated Someone in a duel at the Dueling Ring.");
    }

    [TestCase(DuelOutcomeType.Victory)]
    [TestCase(DuelOutcomeType.Forfeit)]
    [TestCase(DuelOutcomeType.Draw)]
    public void ResultText_IsPlainAscii(DuelOutcomeType type)
    {
        var text = DuelRules.BuildResultText(new DuelOutcome(type, DuelSide.Target), "Hardy Twi'lek", "Quiet Human");

        text.Should().MatchRegex("^[\\x20-\\x7E]+$");
    }
}
