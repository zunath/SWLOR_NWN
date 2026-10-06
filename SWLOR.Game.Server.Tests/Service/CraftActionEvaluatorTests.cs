using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Tests.Service;

public class CraftActionEvaluatorTests
{
    private static CraftSession Session(int rank = 50) => CraftSession.CreateLegacy(
        rank, rank, new RecipeLevelDetail(10000, 10000, 80, 0), 30, 29, 100);

    private static CraftActionOutcome Act(CraftSession state, CraftActionType action, int roll = 1) =>
        CraftActionEvaluator.Resolve(state, new CraftActionRequest(state.Id, state.ActionCount, action), () => roll);

    [TestCase(CraftActionType.BasicSynthesis, 50, 0, 0, 90)]
    [TestCase(CraftActionType.RapidSynthesis, 70, 0, 6, 75)]
    [TestCase(CraftActionType.CarefulSynthesis, 120, 0, 10, 50)]
    [TestCase(CraftActionType.BasicTouch, 0, 146, 3, 90)]
    [TestCase(CraftActionType.StandardTouch, 0, 166, 6, 75)]
    [TestCase(CraftActionType.PreciseTouch, 0, 216, 10, 50)]
    public void EndgameGear_MatchesAuditedLegacyGainsAndCosts(
        CraftActionType action, int progress, int quality, int cp, int chance)
    {
        var state = Session();
        var preview = CraftActionEvaluator.Preview(state, action);
        var result = Act(state, action);
        Assert.Multiple(() =>
        {
            Assert.That(preview.ProgressGain, Is.EqualTo(progress));
            Assert.That(preview.QualityGain, Is.EqualTo(quality));
            Assert.That(preview.CPCost, Is.EqualTo(cp));
            Assert.That(preview.SuccessChance, Is.EqualTo(chance));
            Assert.That(result.Session.Progress, Is.EqualTo(progress));
            Assert.That(result.Session.Quality, Is.EqualTo(quality));
            Assert.That(result.Session.CP, Is.EqualTo(state.CP - cp));
            Assert.That(result.Session.Durability, Is.EqualTo(70));
        });
    }

    [TestCase(1, 2044)]
    [TestCase(100, 0)]
    public void AuditedProtectedFinishingRotation_MatchesLegacyAtBothRollExtremes(int touchRoll, int quality)
    {
        var state = CraftSession.CreateLegacy(50, 50, new RecipeLevelChart().GetByLevel(50), 30, 29, 37);
        Assert.That(state.MaxCP, Is.EqualTo(105));
        Assert.That(state.MaxProgress, Is.EqualTo(186));
        Assert.That(state.MaxQuality, Is.EqualTo(2641));
        for (var index = 0; index < 6; index++) state = Act(state, CraftActionType.BasicTouch, touchRoll).Session;
        state = Act(state, CraftActionType.MastersMend).Session;
        for (var block = 0; block < 2; block++)
        {
            state = Act(state, CraftActionType.WasteNot).Session;
            for (var index = 0; index < 4; index++) state = Act(state, CraftActionType.BasicTouch, touchRoll).Session;
        }
        state = Act(state, CraftActionType.WasteNot).Session;
        state = Act(state, CraftActionType.SteadyHand).Session;
        state = Act(state, CraftActionType.RapidSynthesis, 100).Session;
        state = Act(state, CraftActionType.SteadyHand).Session;
        state = Act(state, CraftActionType.CarefulSynthesis, 100).Session;
        Assert.Multiple(() =>
        {
            Assert.That(state.Status, Is.EqualTo(CraftSessionStatus.Succeeded));
            Assert.That(state.ActionCount, Is.EqualTo(22));
            Assert.That(state.CP, Is.EqualTo(1));
            Assert.That(state.Durability, Is.Zero);
            Assert.That(state.Quality, Is.EqualTo(quality));
        });
    }

    [TestCase(0, 1, 0, 0, 0, 23, 9, 0)]
    [TestCase(5, 5, 0, 0, 0, 33, 10, 10)]
    [TestCase(20, 20, 0, 0, 0, 74, 31, 10)]
    [TestCase(25, 25, 0, 0, 0, 89, 31, 10)]
    [TestCase(40, 40, 0, 0, 0, 138, 31, 125)]
    [TestCase(47, 50, 30, 29, 37, 325, 42, 124)]
    [TestCase(50, 40, 30, 29, 37, 172, 75, 146)]
    public void SkillAndRecipeScaling_MatchesLegacyFixtures(
        int rank, int level, int craftsmanship, int control, int cp,
        int maxProgress, int progress, int quality)
    {
        var state = CraftSession.CreateLegacy(rank, level, new RecipeLevelChart().GetByLevel(level), craftsmanship, control, cp);
        Assert.That(state.MaxProgress, Is.EqualTo(maxProgress));
        Assert.That(CraftActionEvaluator.Preview(state, CraftActionType.BasicSynthesis).ProgressGain, Is.EqualTo(progress));
        if (rank >= 5)
            Assert.That(CraftActionEvaluator.Preview(state, CraftActionType.BasicTouch).QualityGain, Is.EqualTo(quality));
    }

    [TestCase(CraftActionType.RapidSynthesis, 9)]
    [TestCase(CraftActionType.CarefulSynthesis, 29)]
    [TestCase(CraftActionType.BasicTouch, 4)]
    [TestCase(CraftActionType.StandardTouch, 14)]
    [TestCase(CraftActionType.PreciseTouch, 34)]
    [TestCase(CraftActionType.MastersMend, 9)]
    [TestCase(CraftActionType.SteadyHand, 19)]
    [TestCase(CraftActionType.MuscleMemory, 39)]
    [TestCase(CraftActionType.Veneration, 24)]
    [TestCase(CraftActionType.WasteNot, 7)]
    public void LockedActions_AreRejectedWithoutRollingOrSpending(CraftActionType action, int rank)
    {
        var state = Session(rank);
        var result = CraftActionEvaluator.Resolve(state, new CraftActionRequest(state.Id, 0, action),
            () => throw new AssertionException("Rejected actions must not roll."));
        Assert.That(result.Accepted, Is.False);
        Assert.That(result.Session, Is.SameAs(state));
        Assert.That(result.Reason, Does.Contain("Requires skill rank"));
        Assert.That(Act(Session(rank + 1), action).Accepted, Is.True);
    }

    [Test]
    public void UnaffordableSynthesis_DoesNotConsumeVenerationOrWasteNot()
    {
        var state = Session() with { CP = 2, VenerationCharges = 4, WasteNotCharges = 4 };
        var result = CraftActionEvaluator.Resolve(state, new CraftActionRequest(state.Id, 0, CraftActionType.RapidSynthesis),
            () => throw new AssertionException("Rejected actions must not roll."));
        Assert.That(result.Accepted, Is.False);
        Assert.That(result.Session, Is.SameAs(state));
        Assert.That(result.Preview.CPCost, Is.EqualTo(3));
    }

    [Test]
    public void LegacyBuffs_ConsumeOnlyEligibleChargesIncludingFailedAttempts()
    {
        var state = Act(Session(), CraftActionType.WasteNot).Session;
        state = Act(state, CraftActionType.Veneration).Session;
        Assert.That(state.Durability, Is.EqualTo(75));
        Assert.That(state.WasteNotCharges, Is.EqualTo(3));
        state = Act(state, CraftActionType.BasicSynthesis).Session;
        Assert.That(state.VenerationCharges, Is.EqualTo(4));
        Assert.That(state.WasteNotCharges, Is.EqualTo(2));
        var preview = CraftActionEvaluator.Preview(state, CraftActionType.RapidSynthesis);
        var failed = Act(state, CraftActionType.RapidSynthesis, 100);
        Assert.Multiple(() =>
        {
            Assert.That(failed.Succeeded, Is.False);
            Assert.That(failed.Session.VenerationCharges, Is.EqualTo(3));
            Assert.That(failed.Session.WasteNotCharges, Is.EqualTo(1));
            Assert.That(failed.Session.CP, Is.EqualTo(state.CP - preview.CPCost));
            Assert.That(failed.Session.Durability, Is.EqualTo(state.Durability - preview.DurabilityCost));
            Assert.That(failed.Session.Progress, Is.EqualTo(state.Progress));
        });
        var support = Act(failed.Session, CraftActionType.MuscleMemory).Session;
        Assert.That(support.VenerationCharges, Is.EqualTo(3));
        Assert.That(support.WasteNotCharges, Is.EqualTo(1));
        Assert.That(support.MuscleMemoryActive, Is.True);
    }

    [Test]
    public void LegacyGuarantees_PersistAcrossSupportAndAreConsumedBySuccessfulWork()
    {
        var state = Act(Session(), CraftActionType.SteadyHand).Session;
        state = Act(state, CraftActionType.MuscleMemory).Session;
        state = Act(state, CraftActionType.WasteNot).Session;
        state = Act(state, CraftActionType.CarefulSynthesis, 100).Session;
        Assert.That(state.SteadyHandActive, Is.False);
        Assert.That(state.MuscleMemoryActive, Is.True);
        state = Act(state, CraftActionType.PreciseTouch, 100).Session;
        Assert.That(state.MuscleMemoryActive, Is.False);
    }

    [Test]
    public void RepairPreview_ShowsActualRestorationAfterMaximumCap()
    {
        var state = Session() with { Durability = 75 };
        var preview = CraftActionEvaluator.Preview(state, CraftActionType.MastersMend);
        Assert.That(preview.DurabilityRestored, Is.EqualTo(5));
        Assert.That(Act(state, CraftActionType.MastersMend).Session.Durability, Is.EqualTo(80));
    }

    [Test]
    public void CompletionAtZeroDurability_IsSuccessButAFailedFinishingAttemptIsTerminal()
    {
        var state = Session() with { Progress = 9990, Durability = 10 };
        var preview = CraftActionEvaluator.Preview(state, CraftActionType.BasicSynthesis);
        Assert.That(preview.ProgressGain, Is.EqualTo(10));
        Assert.That(preview.FinishesOnSuccess, Is.True);
        Assert.That(preview.FailsOnSuccess, Is.False);
        Assert.That(preview.FailsOnFailure, Is.True);
        Assert.That(Act(state, CraftActionType.BasicSynthesis, 90).Session.Status, Is.EqualTo(CraftSessionStatus.Succeeded));
        Assert.That(Act(state, CraftActionType.BasicSynthesis, 91).Session.Status, Is.EqualTo(CraftSessionStatus.Failed));
    }

    [Test]
    public void QualityCapAndDurabilityFailure_AreBothVisibleInPreview()
    {
        var state = Session() with { Quality = 9999, Durability = 5 };
        var preview = CraftActionEvaluator.Preview(state, CraftActionType.BasicTouch);
        Assert.That(preview.QualityGain, Is.EqualTo(1));
        Assert.That(preview.FailsOnSuccess, Is.True);
        var result = Act(state, CraftActionType.BasicTouch);
        Assert.That(result.Session.Quality, Is.EqualTo(10000));
        Assert.That(result.Session.Durability, Is.Zero);
        Assert.That(result.Session.Status, Is.EqualTo(CraftSessionStatus.Failed));
    }

    [Test]
    public void Snapshot_DoesNotChangeWhenRecipeTargetsChangeAndIncludesEnhancementPenalty()
    {
        var targets = new RecipeLevelDetail(186, 2641, 80, 0);
        var state = CraftSession.CreateLegacy(50, 50, targets, 65, 294, 37, 150);
        targets.Progress = 9999;
        targets.Durability = 1;
        Assert.That(state.MaxProgress, Is.EqualTo(336));
        Assert.That(state.MaxDurability, Is.EqualTo(80));
        Assert.That(CraftActionEvaluator.Preview(state, CraftActionType.BasicTouch).QualityGain, Is.EqualTo(345));
    }
}
