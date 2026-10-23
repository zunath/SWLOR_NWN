using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using static SWLOR.Game.Server.Tests.Service.Prospecting.ProspectingTestData;

namespace SWLOR.Game.Server.Tests.Service.Prospecting;

/// <summary>
/// Covers every action of the Prospecting engine, what its preview shows against what the result does, the seams (hidden, surveyed,
/// claimed and shattered), the ways a session ends and the budget of charges.
/// </summary>
public class ProspectingEngineTests
{
    private static MinigameMeterView Meter(ProspectingEngine engine, string id) => engine.DescribeMeters().Single(meter => meter.Id == id);

    [Test]
    public void ASessionBeginsWithTheNodeAsItIs()
    {
        var engine = Engine("rG", pool: 3, charges: 12, stability: 70);

        engine.Type.Should().Be(CraftMinigameType.Prospecting);
        engine.Status.Should().Be(MinigameStatus.InProgress);
        engine.Ending.Should().Be(ProspectingEnding.InProgress);
        engine.Pool.Should().Be(3);
        engine.PoolAtStart.Should().Be(3);
        engine.Stability.Should().Be(70);
        engine.Charges.Should().Be(12);
        engine.ChargesAtStart.Should().Be(12);
        engine.Items.Should().Be(0);
        engine.RevealedSeams.Should().ContainSingle().Which.Kind.Should().Be(ProspectingSeamKind.GemPocket);
        engine.Actions.Select(action => action.Id).Should().Equal(
            ProspectingActionIds.Survey, ProspectingActionIds.Strike, ProspectingActionIds.HeavyStrike, ProspectingActionIds.Extract,
            ProspectingActionIds.Leave);
        engine.ToNode().Should().Be(Node(3, "rG", 70));
    }

    [Test]
    public void TheMetersAreTheSameInEveryStateAndNeverShowHowManySeamsAreHidden()
    {
        var withSeams = Engine("rgg", pool: 4);
        var without = Engine("", pool: 4);

        withSeams.DescribeMeters().Select(meter => (meter.Id, meter.Label, meter.Max))
            .Should().Equal(without.DescribeMeters().Select(meter => (meter.Id, meter.Label, meter.Max)));
        withSeams.DescribeMeters().Select(meter => meter.Id).Should().Equal(
            MinigameMeterIds.Progress, MinigameMeterIds.Integrity, MinigameMeterIds.Cp, ProspectingEngine.GatheredMeterId);
        Meter(withSeams, ProspectingEngine.GatheredMeterId).Max.Should().Be(4 + ProspectingRules.MaxSeams, "the most a pool of 4 and any node's seams can give");
    }

    [Test]
    public void ThePreviewOfAnActionDoesNotRevealWhatIsHidden()
    {
        // Two nodes that differ only in their hidden seams show the same previews for every action.
        var rich = Engine("r", pool: 3);
        var pockets = Engine("gg", pool: 3);

        foreach (var action in rich.Actions.Select(a => a.Id).Where(id => id != ProspectingActionIds.Extract))
        {
            var a = rich.Preview(action);
            var b = pockets.Preview(action);
            a.Outcomes.Select(o => (o.Label, o.ChancePercent, o.Changes.Select(c => (c.Id, c.Before, c.After)).ToList()))
                .Should().BeEquivalentTo(b.Outcomes.Select(o => (o.Label, o.ChancePercent, o.Changes.Select(c => (c.Id, c.Before, c.After)).ToList())),
                    $"{action} must not reveal which seams are hidden");
        }
    }

    // Survey.

    [Test]
    public void Survey_RevealsOneHiddenSeam_AndSpendsItsChargesAndStability()
    {
        var engine = Engine("rg", pool: 4);

        var preview = engine.Preview(ProspectingActionIds.Survey);
        var result = engine.Perform(ProspectingActionIds.Survey);

        preview.Enabled.Should().BeTrue();
        preview.CpCost.Should().Be(ProspectingRules.SurveyCharges);
        preview.IntegrityCost.Should().Be(ProspectingRules.SurveyStability);
        preview.Outcomes.Should().ContainSingle().Which.ChancePercent.Should().Be(100);
        result.Performed.Should().BeTrue();
        engine.RevealedSeams.Should().ContainSingle().Which.Should().Be(new ProspectingSeam(ProspectingSeamKind.Rich, true));
        engine.Charges.Should().Be(20 - ProspectingRules.SurveyCharges);
        engine.Stability.Should().Be(100 - ProspectingRules.SurveyStability);
        engine.Pool.Should().Be(4, "a Survey takes nothing from the pool");
        engine.Items.Should().Be(0);
        engine.ToNode().Seams.Should().Equal(new ProspectingSeam(ProspectingSeamKind.Rich, true), new ProspectingSeam(ProspectingSeamKind.GemPocket, false));
    }

    [Test]
    public void Survey_FindsTheNextSeamEachTime_AndNothingOnceAllAreFound()
    {
        var engine = Engine("rg", pool: 4);

        engine.Perform(ProspectingActionIds.Survey);
        engine.Perform(ProspectingActionIds.Survey);
        engine.RevealedSeams.Should().HaveCount(2);

        var third = engine.Perform(ProspectingActionIds.Survey);
        third.Performed.Should().BeTrue("a Survey with nothing left to find is allowed, and costs what it costs");
        engine.RevealedSeams.Should().HaveCount(2);
        engine.Stability.Should().Be(100 - 3 * ProspectingRules.SurveyStability);
    }

    [TestCase(0, 1)]
    [TestCase(24, 1)]
    [TestCase(25, 2)]
    [TestCase(49, 2)]
    [TestCase(50, 3)]
    public void GatheringRankMilestonesRevealMoreSeamsWithASurvey(int rank, int reveals)
    {
        var engine = Engine(Node(4, "rrr"), rank: rank);

        engine.Setup.SurveyReveals.Should().Be(reveals);
        engine.Perform(ProspectingActionIds.Survey);
        engine.RevealedSeams.Should().HaveCount(reveals);
    }

    // Strike.

    [Test]
    public void Strike_TakesOneOrTwoUnits_AtTheChancesItShows()
    {
        var engine = Engine("", pool: 4);

        var preview = engine.Preview(ProspectingActionIds.Strike);

        preview.Outcomes.Select(o => (o.Label, o.ChancePercent)).Should().Equal(("Takes 1 unit", 60), ("Takes 2 units", 40));
        preview.CpCost.Should().Be(ProspectingRules.StrikeCharges);
        preview.IntegrityCost.Should().Be(ProspectingRules.StrikeStability);
        preview.FinishesItem.Should().BeFalse();
    }

    [TestCase(StrikeOneUnit, 1)]
    [TestCase(StrikeTwoUnits, 2)]
    public void Strike_TakesTheUnitsTheRollPicks_AndGathersThemAsOreRolls(int roll, int units)
    {
        var engine = Engine("", pool: 4, rolls: [roll]);

        var result = engine.Perform(ProspectingActionIds.Strike);

        result.Performed.Should().BeTrue();
        engine.Pool.Should().Be(4 - units);
        engine.Units.Should().Be(units);
        engine.Items.Should().Be(units);
        engine.Charges.Should().Be(20 - ProspectingRules.StrikeCharges);
        engine.Stability.Should().Be(100 - ProspectingRules.StrikeStability);
        Meter(engine, ProspectingEngine.GatheredMeterId).Value.Should().Be(units);
        Meter(engine, MinigameMeterIds.Progress).Value.Should().Be(4 - units);
    }

    [Test]
    public void Strike_CannotTakeMoreThanThePoolHolds_SoTheTwoRollsAreOneResult()
    {
        var engine = Engine("", pool: 1);

        var preview = engine.Preview(ProspectingActionIds.Strike);

        preview.Outcomes.Should().ContainSingle();
        preview.Outcomes[0].Label.Should().Be("Takes 1 unit");
        preview.Outcomes[0].ChancePercent.Should().Be(100);
        preview.Outcomes[0].FinishesItem.Should().BeTrue("the last unit spends the node");
        preview.FinishesItem.Should().BeTrue();
    }

    [Test]
    public void Strike_TakesTheLooseUnitsFirst_AndBreaksIntoASeamOnlyWhenNoneIsLeft()
    {
        // Three units, two sealed in hidden seams: one is loose.
        var engine = Engine("rg", pool: 3, rolls: [StrikeOneUnit, StrikeOneUnit, StrikeOneUnit]);

        engine.Perform(ProspectingActionIds.Strike);
        engine.ToNode().Seams.Should().HaveCount(2, "the loose unit went first");
        engine.ToNode().LooseUnits.Should().Be(0);

        engine.Perform(ProspectingActionIds.Strike);
        engine.ToNode().Seams.Should().ContainSingle("the next strike broke into the first hidden seam");
        engine.Units.Should().Be(2, "a unit taken from a seam by a strike is plain ore");
        engine.Gems.Should().Be(0);
        engine.RichRolls.Should().Be(0);
    }

    [Test]
    public void Strike_BreaksHiddenSeamsBeforeRevealedOnes()
    {
        var engine = Engine("Rg", pool: 2, rolls: [StrikeOneUnit]);

        engine.Perform(ProspectingActionIds.Strike);

        engine.ToNode().Seams.Should().ContainSingle().Which.Should().Be(new ProspectingSeam(ProspectingSeamKind.Rich, true));
    }

    [Test]
    public void AStrikeThatTakesTheLastUnitSpendsTheNode()
    {
        var engine = Engine("", pool: 1);

        var result = engine.Perform(ProspectingActionIds.Strike);

        result.Status.Should().Be(MinigameStatus.Succeeded);
        engine.Ending.Should().Be(ProspectingEnding.MinedOut);
        engine.Pool.Should().Be(0);
        engine.Preview(ProspectingActionIds.Strike).Enabled.Should().BeFalse();
    }

    [TestCase(0, 60, 40)]
    [TestCase(14, 60, 40)]
    [TestCase(15, 55, 45)]
    [TestCase(30, 50, 50)]
    [TestCase(45, 45, 55)]
    [TestCase(50, 45, 55)]
    public void GatheringRankMilestonesAddYieldToAStrike(int rank, int oneUnit, int twoUnits)
    {
        var engine = Engine(Node(4), rank: rank);

        engine.Preview(ProspectingActionIds.Strike).Outcomes.Select(o => o.ChancePercent).Should().Equal(oneUnit, twoUnits);
    }

    // Heavy Strike.

    [Test]
    public void HeavyStrike_TakesTwoToFourUnits_AndShowsTheChanceOfShatteringAPocket()
    {
        var engine = Engine("", pool: 4);

        var preview = engine.Preview(ProspectingActionIds.HeavyStrike);

        preview.CpCost.Should().Be(ProspectingRules.HeavyStrikeCharges);
        preview.IntegrityCost.Should().Be(ProspectingRules.HeavyStrikeStability);
        preview.IntegrityCost.Should().BeGreaterThan(ProspectingRules.StrikeStability);
        preview.Outcomes.Select(o => o.ChancePercent).Should().Equal(20, 5, 40, 10, 20, 5);
        preview.Outcomes.Sum(o => o.ChancePercent).Should().Be(100);
        preview.Outcomes.Where(o => o.Label.Contains("shatters")).Sum(o => o.ChancePercent).Should().Be(ProspectingRules.ShatterPercent);
        preview.Outcomes.Select(o => -o.Change(MinigameMeterIds.Progress).Delta).Distinct().OrderBy(units => units).Should().Equal(2, 3, 4);
    }

    [Test]
    public void HeavyStrike_ShattersAnUnsurveyedGemPocket_AndItsUnitStaysAsPlainOre()
    {
        var engine = Engine("g", pool: 4, rolls: [HeavyThreeShatters]);

        engine.Perform(ProspectingActionIds.HeavyStrike);

        engine.Shattered.Should().Be(1);
        engine.ToNode().Seams.Should().BeEmpty();
        engine.Units.Should().Be(3);
        engine.Gems.Should().Be(0);
        engine.Pool.Should().Be(1);
    }

    [Test]
    public void HeavyStrike_NeverShattersASurveyedPocket_OrARichSeam_OrWhenTheRollDoesNotShatter()
    {
        var surveyed = Engine("G", pool: 4, rolls: [HeavyThreeShatters]);
        surveyed.Perform(ProspectingActionIds.HeavyStrike);
        surveyed.Shattered.Should().Be(0);
        surveyed.ToNode().Seams.Should().ContainSingle();

        var rich = Engine("r", pool: 4, rolls: [HeavyThreeShatters]);
        rich.Perform(ProspectingActionIds.HeavyStrike);
        rich.Shattered.Should().Be(0);

        var clean = Engine("g", pool: 4, rolls: [HeavyThreeClean]);
        clean.Perform(ProspectingActionIds.HeavyStrike);
        clean.Shattered.Should().Be(0);
        clean.ToNode().Seams.Should().ContainSingle();
    }

    [Test]
    public void HeavyStrike_ShattersOnlyOnePocketAtATime_TheFirstHiddenOne()
    {
        var engine = Engine("gg", pool: 4, rolls: [HeavyTwoShatters]);

        engine.Perform(ProspectingActionIds.HeavyStrike);

        engine.Shattered.Should().Be(1);
        engine.ToNode().Seams.Should().ContainSingle();
    }

    [TestCase(HeavyTwoClean, 2)]
    [TestCase(HeavyThreeClean, 3)]
    [TestCase(HeavyFourClean, 4)]
    public void HeavyStrike_TakesTheUnitsTheRollPicks(int roll, int units)
    {
        var engine = Engine("", pool: 4, rolls: [roll]);

        engine.Perform(ProspectingActionIds.HeavyStrike);

        engine.Units.Should().Be(units);
        engine.Pool.Should().Be(4 - units);
    }

    [Test]
    public void HeavyStrike_IsCappedByThePool()
    {
        var engine = Engine("", pool: 2);

        var preview = engine.Preview(ProspectingActionIds.HeavyStrike);

        preview.Outcomes.Select(o => (o.Label, o.ChancePercent)).Should().Equal(
            ("Takes 2 units", 80), ("Takes 2 units and shatters a gem pocket if one is hidden", 20));
        preview.FinishesItem.Should().BeTrue();
    }

    [TestCase(0, 25, 25)]
    [TestCase(15, 20, 30)]
    [TestCase(30, 15, 35)]
    [TestCase(45, 10, 40)]
    public void GatheringRankMilestonesAddYieldToAHeavyStrike(int rank, int twoUnits, int fourUnits)
    {
        var engine = Engine(Node(4), rank: rank);

        var preview = engine.Preview(ProspectingActionIds.HeavyStrike);

        preview.Outcomes.Where(o => -o.Change(MinigameMeterIds.Progress).Delta == 2).Sum(o => o.ChancePercent).Should().Be(twoUnits);
        preview.Outcomes.Where(o => -o.Change(MinigameMeterIds.Progress).Delta == 4).Sum(o => o.ChancePercent).Should().Be(fourUnits);
    }

    // Extract.

    [Test]
    public void Extract_NeedsASeamThatHasBeenFound()
    {
        var engine = Engine("rg", pool: 4);

        var preview = engine.Preview(ProspectingActionIds.Extract);

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be(ProspectingEngine.NoRevealedSeam);
        engine.Perform(ProspectingActionIds.Extract).Performed.Should().BeFalse();
        engine.Charges.Should().Be(20, "a refused action changes nothing");
        engine.Turn.Should().Be(0);
    }

    [Test]
    public void Extract_ClaimsARichSeam_ItsUnitAndSometimesAnExtraOreRoll()
    {
        var engine = Engine("R", pool: 4, rolls: [RichBonus]);

        var preview = engine.Preview(ProspectingActionIds.Extract);
        engine.Perform(ProspectingActionIds.Extract);

        preview.CpCost.Should().Be(ProspectingRules.ExtractCharges);
        preview.IntegrityCost.Should().Be(ProspectingRules.ExtractStability);
        preview.CpCost.Should().BeGreaterThan(ProspectingRules.StrikeCharges, "a seam is claimed for extra charges");
        preview.Outcomes.Select(o => o.ChancePercent).Should().Equal(ProspectingRules.RichSeamBonusPercent, 100 - ProspectingRules.RichSeamBonusPercent);
        engine.Units.Should().Be(1);
        engine.RichRolls.Should().Be(1);
        engine.Items.Should().Be(2);
        engine.Gems.Should().Be(0);
        engine.Pool.Should().Be(3, "the seal's unit left the pool");
        engine.RevealedSeams.Should().BeEmpty();
        engine.ToNode().Seams.Should().BeEmpty();
    }

    [Test]
    public void Extract_ARichSeamWithoutItsBonusGivesJustItsUnit()
    {
        var engine = Engine("R", pool: 4, rolls: [RichNoBonus]);

        engine.Perform(ProspectingActionIds.Extract);

        engine.Units.Should().Be(1);
        engine.RichRolls.Should().Be(0);
        engine.Items.Should().Be(1);
        engine.Pool.Should().Be(3);
    }

    [Test]
    public void Extract_ClaimsAGemPocket_AGemInPlaceOfItsUnit()
    {
        var engine = Engine("G", pool: 4);

        var preview = engine.Preview(ProspectingActionIds.Extract);
        engine.Perform(ProspectingActionIds.Extract);

        preview.Outcomes.Should().ContainSingle().Which.ChancePercent.Should().Be(100);
        engine.Gems.Should().Be(ProspectingRules.GemPocketGems);
        engine.Units.Should().Be(0, "the gem takes the place of the unit");
        engine.RichRolls.Should().Be(0);
        engine.Items.Should().Be(1);
        engine.Pool.Should().Be(3);
    }

    [Test]
    public void Extract_ClaimsAGemPocketBeforeARichSeam()
    {
        var engine = Engine("RG", pool: 4);

        engine.NextExtract.Should().Be(new ProspectingSeam(ProspectingSeamKind.GemPocket, true));
        engine.RevealedSeams.Select(seam => seam.Kind).Should().Equal(ProspectingSeamKind.GemPocket, ProspectingSeamKind.Rich);

        engine.Perform(ProspectingActionIds.Extract);

        engine.Gems.Should().Be(1);
        engine.NextExtract.Should().Be(new ProspectingSeam(ProspectingSeamKind.Rich, true));
    }

    [Test]
    public void ExtractingTheLastUnitSpendsTheNode()
    {
        var engine = Engine("G", pool: 1);

        var preview = engine.Preview(ProspectingActionIds.Extract);
        var result = engine.Perform(ProspectingActionIds.Extract);

        preview.FinishesItem.Should().BeTrue();
        result.Status.Should().Be(MinigameStatus.Succeeded);
        engine.Ending.Should().Be(ProspectingEnding.MinedOut);
        engine.Gems.Should().Be(1);
    }

    // Leave.

    [Test]
    public void Leave_EndsTheSessionAtOnce_FreeAndLeavesTheRestOfTheNode()
    {
        var engine = Engine("rG", pool: 3, rolls: [StrikeOneUnit]);
        engine.Perform(ProspectingActionIds.Strike);

        var preview = engine.Preview(ProspectingActionIds.Leave);
        var result = engine.Perform(ProspectingActionIds.Leave);

        preview.CpCost.Should().Be(0);
        preview.IntegrityCost.Should().Be(0);
        result.Status.Should().Be(MinigameStatus.Succeeded);
        engine.Ending.Should().Be(ProspectingEnding.Left);
        ProspectingRules.SecondsFor(ProspectingActionIds.Leave).Should().Be(0);
        engine.ToNode().Pool.Should().Be(engine.Pool);
        engine.ToNode().Seams.Should().HaveCount(2, "the seams found and the ones not found both stay in the node");
        engine.Perform(ProspectingActionIds.Strike).Performed.Should().BeFalse("the session is over");
    }

    // Collapse, mining out and charges.

    [Test]
    public void ANodeCollapsesWhenStabilityReachesZero_AndWhatWasGatheredIsKept()
    {
        var engine = Engine("", pool: 4, stability: ProspectingRules.StrikeStability, rolls: [StrikeOneUnit]);

        var preview = engine.Preview(ProspectingActionIds.Strike);
        var result = engine.Perform(ProspectingActionIds.Strike);

        preview.Outcomes.Should().OnlyContain(o => o.BreaksItem);
        preview.CanBreakItem.Should().BeTrue();
        result.Status.Should().Be(MinigameStatus.Failed);
        engine.Ending.Should().Be(ProspectingEnding.Collapsed);
        engine.Stability.Should().Be(0);
        engine.Units.Should().Be(1, "the strike that collapsed the node still took its unit");
        engine.Pool.Should().Be(3, "what was left is lost with the node");
    }

    [Test]
    public void TakingTheLastUnitAndCollapsingTogetherIsMinedOut()
    {
        var engine = Engine("", pool: 1, stability: ProspectingRules.StrikeStability);

        engine.Perform(ProspectingActionIds.Strike);

        engine.Ending.Should().Be(ProspectingEnding.MinedOut);
        engine.Status.Should().Be(MinigameStatus.Succeeded);
    }

    [Test]
    public void ASurveyOrAnExtractCanCollapseTheNodeToo()
    {
        var survey = Engine("r", pool: 4, stability: ProspectingRules.SurveyStability);
        survey.Perform(ProspectingActionIds.Survey);
        survey.Ending.Should().Be(ProspectingEnding.Collapsed);

        var extract = Engine("G", pool: 4, stability: ProspectingRules.ExtractStability);
        extract.Preview(ProspectingActionIds.Extract).CanBreakItem.Should().BeTrue();
        extract.Perform(ProspectingActionIds.Extract);
        extract.Ending.Should().Be(ProspectingEnding.Collapsed);
        extract.Gems.Should().Be(1, "the gem is gathered before the node collapses");
    }

    [Test]
    public void EveryActionSpendsItsChargesAndAnActionYouCannotPayForIsRefused()
    {
        var engine = Engine("RG", pool: 4, charges: 1);

        engine.Preview(ProspectingActionIds.Strike).Enabled.Should().BeTrue();
        foreach (var id in new[] { ProspectingActionIds.HeavyStrike, ProspectingActionIds.Extract })
        {
            var preview = engine.Preview(id);
            preview.Enabled.Should().BeFalse(id);
            preview.DisabledReason.Should().Be(ProspectingEngine.NotEnoughCharges);
            engine.Perform(id).Performed.Should().BeFalse(id);
        }

        engine.Charges.Should().Be(1);
    }

    [Test]
    public void TheLastChargeEndsTheSession_AndTheNodeKeepsWhatIsLeft()
    {
        var engine = Engine("r", pool: 4, charges: ProspectingRules.StrikeCharges, rolls: [StrikeOneUnit]);

        engine.Perform(ProspectingActionIds.Strike);

        engine.Charges.Should().Be(0);
        engine.Ending.Should().Be(ProspectingEnding.OutOfCharges);
        engine.Status.Should().Be(MinigameStatus.Succeeded);
        engine.ToNode().Pool.Should().Be(3);
        engine.ToNode().Seams.Should().ContainSingle();
    }

    [Test]
    public void ChargesPerActionAreTheCostsTheRulesName()
    {
        var engine = Engine("rg", pool: 6, charges: 20, rolls: [StrikeOneUnit, HeavyThreeClean, RichNoBonus]);

        engine.Perform(ProspectingActionIds.Survey);
        engine.Perform(ProspectingActionIds.Strike);
        engine.Perform(ProspectingActionIds.HeavyStrike);
        engine.Perform(ProspectingActionIds.Extract);

        engine.Charges.Should().Be(
            20 - ProspectingRules.SurveyCharges - ProspectingRules.StrikeCharges - ProspectingRules.HeavyStrikeCharges - ProspectingRules.ExtractCharges);
        engine.Stability.Should().Be(
            100 - ProspectingRules.SurveyStability - ProspectingRules.StrikeStability - ProspectingRules.HeavyStrikeStability - ProspectingRules.ExtractStability);
    }

    // Previews against results.

    [Test]
    public void ThePreviewOfEveryOutcomeIsWhatTheResultDoes()
    {
        foreach (var setup in new[]
                 {
                     (Seams: "", Pool: 4, Action: ProspectingActionIds.Strike),
                     (Seams: "", Pool: 1, Action: ProspectingActionIds.Strike),
                     (Seams: "rg", Pool: 4, Action: ProspectingActionIds.Strike),
                     (Seams: "g", Pool: 4, Action: ProspectingActionIds.HeavyStrike),
                     (Seams: "", Pool: 2, Action: ProspectingActionIds.HeavyStrike),
                     (Seams: "rG", Pool: 4, Action: ProspectingActionIds.Extract),
                     (Seams: "R", Pool: 4, Action: ProspectingActionIds.Extract),
                     (Seams: "rg", Pool: 4, Action: ProspectingActionIds.Survey),
                     (Seams: "rg", Pool: 4, Action: ProspectingActionIds.Leave)
                 })
        {
            var preview = Engine(setup.Seams, setup.Pool).Preview(setup.Action);
            var before = 0;
            foreach (var outcome in preview.Outcomes)
            {
                var engine = Engine(setup.Seams, setup.Pool, rolls: [before + 1]);
                var result = engine.Perform(setup.Action);

                result.Performed.Should().BeTrue($"{setup.Action} on {setup.Seams}/{setup.Pool}");
                result.Outcome.Label.Should().Be(outcome.Label);
                result.Outcome.ChancePercent.Should().Be(outcome.ChancePercent);
                result.MetersAfter.Select(meter => (meter.Id, meter.Value)).Should().Equal(
                    outcome.Changes.Select(change => (change.Id, change.After)),
                    $"{setup.Action} on {setup.Seams}/{setup.Pool}: {outcome.Label}");
                result.Status.Should().Be(outcome.ResultingStatus);
                before += outcome.ChancePercent;
            }

            before.Should().Be(100);
        }
    }

    [Test]
    public void APreviewShowsEveryMeterMoving()
    {
        var engine = Engine("", pool: 4);

        var changes = engine.Preview(ProspectingActionIds.Strike).Outcomes[1].Changes.ToDictionary(change => change.Id);

        changes[MinigameMeterIds.Progress].Should().BeEquivalentTo(new MinigameMeterChange(MinigameMeterIds.Progress, "Yield", 4, 2));
        changes[MinigameMeterIds.Integrity].After.Should().Be(100 - ProspectingRules.StrikeStability);
        changes[MinigameMeterIds.Cp].After.Should().Be(20 - ProspectingRules.StrikeCharges);
        changes[ProspectingEngine.GatheredMeterId].After.Should().Be(2);
    }

    // Everything else.

    [Test]
    public void AnUnknownActionIsAnError_AndAnActionAfterTheEndIsRefused()
    {
        var engine = Engine("", pool: 1);

        var unknown = () => engine.Preview("dynamite");
        unknown.Should().Throw<ArgumentException>();

        engine.Perform(ProspectingActionIds.Strike);
        engine.Preview(ProspectingActionIds.Survey).Enabled.Should().BeFalse();
        engine.Perform(ProspectingActionIds.Survey).Performed.Should().BeFalse();
        engine.PreviewAll().Should().OnlyContain(preview => !preview.Enabled, "every action is refused once the session is over");
    }

    [Test]
    public void AForkContinuesFromTheSameSessionAndNeverTouchesTheOriginal()
    {
        var engine = Engine("rg", pool: 4, rolls: [StrikeTwoUnits]);
        engine.Perform(ProspectingActionIds.Survey);

        var fork = (ProspectingEngine)engine.Fork(new ScriptedMinigameRandom(StrikeOneUnit, RichNoBonus));
        fork.Perform(ProspectingActionIds.Strike);
        fork.Perform(ProspectingActionIds.Extract);

        engine.Units.Should().Be(0);
        engine.Charges.Should().Be(20 - ProspectingRules.SurveyCharges);
        engine.RevealedSeams.Should().HaveCount(1);
        fork.Units.Should().BeGreaterThan(0);
        fork.RevealedSeams.Should().BeEmpty("the fork claimed it");
    }

    [Test]
    public void ASetupNeedsAtLeastOneChargeAndInRangeMilestones()
    {
        var node = Node(2);

        ((Action)(() => new ProspectingSetup(node, 0))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => new ProspectingSetup(node, 5, surveyReveals: 0))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => new ProspectingSetup(node, 5, surveyReveals: 4))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => new ProspectingSetup(node, 5, yieldTier: 4))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => new ProspectingSetup(node, 5, nodeTier: 0))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => new ProspectingSetup(Node(1).Take(1), 5))).Should().Throw<ArgumentException>("a spent node cannot be worked");
    }

    [Test]
    public void TheSetupReadsTheRankMilestonesOfTheGatheringRank()
    {
        var setup = ProspectingSetup.ForNode(Node(3, "rr"), 10, gatheringRank: 47, nodeTier: 5);

        setup.SurveyReveals.Should().Be(CraftRankMilestones.ProspectingSurveyRevealsForRank(47)).And.Be(2);
        setup.YieldTier.Should().Be(CraftRankMilestones.ProspectingYieldTierForRank(47)).And.Be(3);
        setup.VeinLevel.Should().Be(45);
        setup.Rank.Should().Be(47);
    }

    [Test]
    public void AYieldTableAddsUpToOneHundredAtEveryTier()
    {
        for (var tier = 0; tier <= CraftRankMilestones.MaxProspectingYieldTier; tier++)
        {
            ProspectingRules.StrikeYield(tier).Sum(entry => entry.Chance).Should().Be(100, $"Strike at tier {tier}");
            ProspectingRules.HeavyStrikeYield(tier).Sum(entry => entry.Chance).Should().Be(100, $"Heavy Strike at tier {tier}");
            ProspectingRules.StrikeYield(tier).Should().OnlyContain(entry => entry.Chance > 0);
            ProspectingRules.HeavyStrikeYield(tier).Should().OnlyContain(entry => entry.Chance > 0);
            (ProspectingRules.HeavyStrikeYield(tier).Select(e => e.Chance * (100 - ProspectingRules.ShatterPercent) % 100)
                .Concat(ProspectingRules.HeavyStrikeYield(tier).Select(e => e.Chance * ProspectingRules.ShatterPercent % 100)))
                .Should().OnlyContain(remainder => remainder == 0, "a Heavy Strike's outcomes must be whole percentages");
        }

        ProspectingRules.ExpectedUnits(ProspectingRules.StrikeYield(0)).Should().BeApproximately(1.4, 1e-9);
        ProspectingRules.ExpectedUnits(ProspectingRules.StrikeYield(3)).Should().BeGreaterThan(ProspectingRules.ExpectedUnits(ProspectingRules.StrikeYield(0)));
    }
}
