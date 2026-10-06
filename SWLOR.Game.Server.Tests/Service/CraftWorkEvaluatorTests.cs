using NUnit.Framework;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Service;

public class CraftWorkEvaluatorTests
{
    private static CraftSession Session(CraftCondition condition = CraftCondition.Normal, CraftProfile profile = CraftProfile.Calibrated,
        StatType stat = StatType.Invalid, int value = 0) => CraftSession.Create(50, 50,
        new RecipeLevelDetail(10000, 10000, 80, 0), 30, 29, 100, profile, CraftTechnique.None,
        stat == StatType.Invalid ? new Dictionary<StatType, int>() : new Dictionary<StatType, int> { [stat] = value }, _ => 0)
        with { Conditions = new[] { condition, CraftCondition.Workable, CraftCondition.Fine, CraftCondition.Normal, CraftCondition.Economical, CraftCondition.Reinforced } };
    private static CraftActionOutcome Act(CraftSession state, CraftActionType action, int roll = 1) =>
        CraftActionEvaluator.Resolve(state, new CraftActionRequest(state.Id, state.ActionCount, action), () => roll);

    [Test]
    public void Decks_RespectCompositionAndDroughtBoundAcrossRefills()
    {
        for (var seed = 0; seed < 1000; seed++)
        {
            var random = new Random(seed);
            Assert.That(CraftConditionDeck.IsValid(CraftConditionDeck.Create(random.Next, 4)), Is.True, $"Seed {seed}");
        }
        Assert.That(CraftConditionDeck.IsValid(CraftConditionDeck.Create(_ => 0)), Is.True);
        Assert.That(CraftConditionDeck.IsValid(CraftConditionDeck.Create(count => count - 1)), Is.True);
    }

    [Test]
    public void InvalidRandomIndices_UseAValidatedFallback()
    {
        Assert.That(CraftConditionDeck.IsValid(CraftConditionDeck.Create(_ => -1)), Is.True);
        Assert.That(CraftConditionDeck.IsValid(CraftConditionDeck.Create(count => count)), Is.True);
    }

    [Test]
    public void ResilientWork_PreviewsCompletionEvenWhenRapidFails()
    {
        var state = Session(stat: StatType.CraftingFailedSynthesisProgressPercent, value: 40) with { MaxProgress = 1 };
        var preview = CraftActionEvaluator.Preview(state, CraftActionType.RapidSynthesis);
        Assert.That(preview.FinishesOnFailure, Is.True);
        Assert.That(Act(state, CraftActionType.RapidSynthesis, 100).Session.Status, Is.EqualTo(CraftSessionStatus.Succeeded));
    }

    [TestCase(CraftActionType.BasicSynthesis, 50, 0, 10, 0, 100)]
    [TestCase(CraftActionType.RapidSynthesis, 90, 0, 15, 6, 75)]
    [TestCase(CraftActionType.CarefulSynthesis, 60, 0, 5, 10, 100)]
    [TestCase(CraftActionType.BasicTouch, 0, 101, 10, 3, 100)]
    [TestCase(CraftActionType.StandardTouch, 0, 151, 10, 7, 100)]
    [TestCase(CraftActionType.PreciseTouch, 0, 126, 5, 10, 100)]
    public void Actions_HaveDistinctReliableResourceRoles(CraftActionType action, int progress, int quality, int durability, int cp, int chance)
    {
        var state = Session(); var preview = CraftActionEvaluator.Preview(state, action); var result = Act(state, action);
        Assert.Multiple(() =>
        {
            Assert.That(preview.ProgressGain, Is.EqualTo(progress)); Assert.That(preview.QualityGain, Is.EqualTo(quality));
            Assert.That(preview.DurabilityCost, Is.EqualTo(durability)); Assert.That(preview.CPCost, Is.EqualTo(cp));
            Assert.That(preview.SuccessChance, Is.EqualTo(chance)); Assert.That(result.Session.Progress, Is.EqualTo(progress));
            Assert.That(result.Session.Quality, Is.EqualTo(quality)); Assert.That(result.Session.ConditionIndex, Is.EqualTo(1));
        });
    }

    [Test]
    public void Profiles_ModifyWorkWithoutInferringItemNames()
    {
        Assert.That(CraftActionEvaluator.Preview(Session(profile: CraftProfile.Sturdy), CraftActionType.BasicSynthesis).DurabilityCost, Is.EqualTo(8));
        Assert.That(CraftActionEvaluator.Preview(Session(profile: CraftProfile.Delicate), CraftActionType.RapidSynthesis).DurabilityCost, Is.EqualTo(20));
        var chained = Session() with { LastWork = CraftWorkKind.Synthesis, LastWorkSucceeded = true };
        Assert.That(CraftActionEvaluator.Preview(chained, CraftActionType.BasicTouch).QualityGain, Is.EqualTo(121));
        var failed = Act(chained, CraftActionType.RapidSynthesis, 100).Session;
        Assert.That(failed.LastWorkSucceeded, Is.False);
        Assert.That(CraftActionEvaluator.Preview(failed with { Conditions = new[] { CraftCondition.Normal } }, CraftActionType.BasicTouch).QualityGain, Is.EqualTo(101));
    }

    [Test]
    public void ExpenditureDiscounts_CapAtHalfAndRoundUp()
    {
        var state = Session(CraftCondition.Economical) with { Buffs = new Dictionary<CraftBuffType, CraftBuff> { [CraftBuffType.Veneration] = new(2, 3, 0) } };
        Assert.That(CraftActionEvaluator.Preview(state, CraftActionType.RapidSynthesis).CPCost, Is.EqualTo(3));
        state = state with { Conditions = new[] { CraftCondition.Reinforced }, Buffs = new Dictionary<CraftBuffType, CraftBuff> { [CraftBuffType.WasteNot] = new(2, 3, 0) } };
        Assert.That(CraftActionEvaluator.Preview(state, CraftActionType.RapidSynthesis).DurabilityCost, Is.EqualTo(8));
    }

    [Test]
    public void Preparation_AgesOnSupportAndFailureAndCannotBeReapplied()
    {
        var state = Act(Session(), CraftActionType.SteadyHand).Session;
        Assert.That(state.Buff(CraftBuffType.SteadyHand).ActionsRemaining, Is.EqualTo(2));
        Assert.That(CraftActionEvaluator.Preview(state, CraftActionType.SteadyHand).IsAvailable, Is.False);
        state = Act(state, CraftActionType.BasicTouch).Session;
        Assert.That(CraftActionEvaluator.Preview(state, CraftActionType.RapidSynthesis).SuccessChance, Is.EqualTo(100));
        state = Act(state, CraftActionType.RapidSynthesis, 100).Session;
        Assert.That(state.Buff(CraftBuffType.SteadyHand), Is.Null);
        state = Act(state, CraftActionType.SteadyHand).Session;
        state = Act(state, CraftActionType.BasicTouch).Session;
        state = Act(state, CraftActionType.BasicTouch).Session;
        Assert.That(state.Buff(CraftBuffType.SteadyHand), Is.Null);
        Assert.That(CraftActionEvaluator.Preview(state, CraftActionType.SteadyHand).IsAvailable, Is.False);
    }

    [Test]
    public void RejectedInput_DoesNotRollSpendAgeOrAdvance()
    {
        var state = Act(Session(), CraftActionType.WasteNot).Session;
        var result = CraftActionEvaluator.Resolve(state, new(state.Id, 0, CraftActionType.BasicTouch), () => throw new Exception("Must not roll"));
        Assert.That(result.Accepted, Is.False); Assert.That(result.Session, Is.SameAs(state));
        result = Act(state with { CP = 0 }, CraftActionType.MastersMend);
        Assert.That(result.Accepted, Is.False);
    }

    [TestCase(2)] [TestCase(3)]
    public void WorkDurabilityRestoration_IsCappedAndLimited(int amount)
    {
        var state = Session(CraftCondition.Workable, stat: StatType.CraftingWorkDurabilityRestore, value: amount);
        for (var i = 0; i < 3; i++) state = Act(state with { Conditions = new[] { CraftCondition.Workable }, ConditionIndex = 0 }, CraftActionType.BasicSynthesis).Session;
        Assert.That(state.Durability, Is.EqualTo(80 - 30 + 3 * amount));
        Assert.That(CraftActionEvaluator.Preview(state with { Conditions = new[] { CraftCondition.Workable }, ConditionIndex = 0 }, CraftActionType.BasicSynthesis).DurabilityRestored, Is.Zero);
    }

    [Test]
    public void FailureProgress_RemainsAFailedChainAndIsOneUse()
    {
        var state = Session(stat: StatType.CraftingFailedSynthesisProgressPercent, value: 40);
        var preview = CraftActionEvaluator.Preview(state, CraftActionType.RapidSynthesis);
        state = Act(state, CraftActionType.RapidSynthesis, 100).Session;
        Assert.That(preview.ProgressOnFailure, Is.EqualTo(36)); Assert.That(state.Progress, Is.EqualTo(36));
        Assert.That(state.LastWorkSucceeded, Is.False);
        Assert.That(CraftActionEvaluator.Preview(state, CraftActionType.RapidSynthesis).ProgressOnFailure, Is.Zero);
    }

    [Test]
    public void ChainedQuality_AndProductiveOpportunityAffectOnlyQualifyingWork()
    {
        var state = Session(CraftCondition.Reinforced, stat: StatType.CraftingChainedTouchQualityPercent, value: 25)
            with { LastWork = CraftWorkKind.Synthesis, LastWorkSucceeded = true };
        Assert.That(CraftActionEvaluator.Preview(state, CraftActionType.BasicTouch).QualityGain, Is.EqualTo(151));
        state = Session(CraftCondition.Workable, stat: StatType.CraftingWorkTouchOpportunityPercent, value: 30);
        state = Act(state, CraftActionType.BasicSynthesis).Session;
        Assert.That(state.Buff(CraftBuffType.TouchQuality).Magnitude, Is.EqualTo(30));
        state = Act(state, CraftActionType.BasicTouch).Session;
        Assert.That(state.Buff(CraftBuffType.TouchQuality), Is.Null);
    }

    [Test]
    public void EngineeringEffects_UseEffectiveCostsSuccessfulSwitchesAndExistingForecast()
    {
        var state = Session(stat: StatType.CraftingSwitchCPRestore, value: 2) with { CP = 20, LastWork = CraftWorkKind.Synthesis, LastWorkSucceeded = true };
        Assert.That(Act(state, CraftActionType.BasicTouch).Session.CP, Is.EqualTo(19));
        state = Session(CraftCondition.Fine, stat: StatType.CraftingFinePreciseQualityPercent, value: 25);
        Assert.That(CraftActionEvaluator.Preview(state, CraftActionType.PreciseTouch).QualityGain, Is.EqualTo(272));
        state = Session(stat: StatType.CraftingForecastLength, value: 3);
        Assert.That(state.Forecast, Is.EqualTo("Workable > Fine > Normal"));
        state = Session(CraftCondition.Economical, stat: StatType.CraftingEconomicalCPRefundPercent, value: 50) with { CP = 20 };
        var preview = CraftActionEvaluator.Preview(state, CraftActionType.StandardTouch);
        Assert.That(preview.CPCost, Is.EqualTo(6)); Assert.That(preview.CPRestored, Is.EqualTo(3));
        Assert.That(Act(state, CraftActionType.StandardTouch).Session.CP, Is.EqualTo(17));
    }

    [Test]
    public void FabricationEffects_BraceMendsExtendProtectionAndDiscountCarefulWork()
    {
        var state = Session(stat: StatType.CraftingMendDurabilityBonus, value: 10) with { Durability = 20 };
        Assert.That(CraftActionEvaluator.Preview(state, CraftActionType.MastersMend).DurabilityRestored, Is.EqualTo(40));
        state = Session(stat: StatType.CraftingWasteNotWindowBonus, value: 2);
        var buff = Act(state, CraftActionType.WasteNot).Session.Buff(CraftBuffType.WasteNot);
        Assert.That(buff.Charges, Is.EqualTo(4)); Assert.That(buff.ActionsRemaining, Is.EqualTo(5));
        state = Session(CraftCondition.Economical, stat: StatType.CraftingCarefulCPReduction, value: 2);
        Assert.That(CraftActionEvaluator.Preview(state, CraftActionType.CarefulSynthesis).CPCost, Is.EqualTo(6));
    }

    [Test]
    public void TerminalProtection_IsOnceAndCompletionWins()
    {
        var state = Session(stat: StatType.CraftingDurabilityFailureProtection, value: 1) with { Durability = 5 };
        var preview = CraftActionEvaluator.Preview(state, CraftActionType.BasicSynthesis);
        Assert.That(preview.FailureProtection, Is.True); Assert.That(preview.FailsOnSuccess, Is.False);
        var next = Act(state, CraftActionType.BasicSynthesis).Session;
        Assert.That(next.Durability, Is.EqualTo(1)); Assert.That(next.Status, Is.EqualTo(CraftSessionStatus.Active));
        Assert.That(Act(next, CraftActionType.BasicSynthesis).Session.Status, Is.EqualTo(CraftSessionStatus.Failed));
        next = Act(state with { MaxProgress = 50 }, CraftActionType.BasicSynthesis).Session;
        Assert.That(next.Status, Is.EqualTo(CraftSessionStatus.Succeeded)); Assert.That(next.Durability, Is.Zero);
        Assert.That(next.TriggerCount(StatType.CraftingDurabilityFailureProtection), Is.Zero);
    }

    [Test]
    public void AgricultureMixedProgress_IsPreviewedBeforeItFinishes()
    {
        var state = Session(CraftCondition.Fine, stat: StatType.CraftingFineTouchProgressPercent, value: 20) with { MaxProgress = 10 };
        var preview = CraftActionEvaluator.Preview(state, CraftActionType.BasicTouch);
        Assert.That(preview.ProgressGain, Is.EqualTo(10)); Assert.That(preview.FinishesOnSuccess, Is.True);
        Assert.That(Act(state, CraftActionType.BasicTouch).Session.Status, Is.EqualTo(CraftSessionStatus.Succeeded));
        state = Session(CraftCondition.Fine, stat: StatType.CraftingMaximumQualityProgress, value: 1) with { MaxQuality = 100, Quality = 90 };
        Assert.That(CraftActionEvaluator.Preview(state, CraftActionType.BasicTouch).ProgressGain, Is.EqualTo(50));
    }

    [Test]
    public void Preservation_RequiresARealRepairAndStillAgesPreparations()
    {
        var state = Session(CraftCondition.Fine, stat: StatType.CraftingFineMendPreservationUses, value: 2)
            with { Durability = 20, Buffs = new Dictionary<CraftBuffType, CraftBuff> { [CraftBuffType.MuscleMemory] = new(1, 2, 50) } };
        state = Act(state, CraftActionType.MastersMend).Session;
        Assert.That(state.ConditionIndex, Is.Zero); Assert.That(state.Buff(CraftBuffType.MuscleMemory).ActionsRemaining, Is.EqualTo(1));
        state = Act(state, CraftActionType.MastersMend).Session;
        Assert.That(state.ConditionIndex, Is.Zero); Assert.That(state.Buff(CraftBuffType.MuscleMemory), Is.Null);
        Assert.That(Act(state, CraftActionType.MastersMend).Accepted, Is.False);
        state = Session(CraftCondition.Fine, stat: StatType.CraftingFineMendPreservationUses, value: 2) with { Durability = 75 };
        Assert.That(Act(state, CraftActionType.MastersMend).Session.ConditionIndex, Is.EqualTo(1));
    }

    [Test]
    public void PreparedWorkDiscounts_ExpireAndConsumeOnTheIntendedAction()
    {
        var state = Session(stat: StatType.CraftingPreparedTouchCPReduction, value: 2);
        state = Act(state, CraftActionType.CarefulSynthesis).Session;
        Assert.That(CraftActionEvaluator.Preview(state, CraftActionType.BasicTouch).CPCost, Is.EqualTo(1));
        state = Act(state, CraftActionType.BasicTouch).Session;
        Assert.That(state.Buff(CraftBuffType.BasicTouchDiscount), Is.Null);
        state = Session(stat: StatType.CraftingPreparedRapidDurabilityReduction, value: 5) with { Technique = CraftTechnique.TrapAssembly };
        state = Act(state, CraftActionType.CarefulSynthesis).Session;
        Assert.That(CraftActionEvaluator.Preview(state, CraftActionType.RapidSynthesis).DurabilityCost, Is.EqualTo(10));
        state = Act(state, CraftActionType.RapidSynthesis, 100).Session;
        Assert.That(state.Buff(CraftBuffType.RapidDurabilityDiscount), Is.Null);
    }

    [Test]
    public void EspionageTechniqueEffects_RespectTagsAndReplaceWithoutRerolling()
    {
        var state = Session(CraftCondition.Fine, stat: StatType.CraftingPoisonTouchDurabilityRestore, value: 4);
        Assert.That(CraftActionEvaluator.Preview(state, CraftActionType.BasicTouch).DurabilityRestored, Is.Zero);
        Assert.That(CraftActionEvaluator.Preview(state with { Technique = CraftTechnique.PoisonMixing }, CraftActionType.BasicTouch).DurabilityRestored, Is.EqualTo(4));
        state = Session(CraftCondition.Economical, stat: StatType.CraftingEconomicalSynthesisQualityPercent, value: 20);
        Assert.That(CraftActionEvaluator.Preview(state, CraftActionType.CarefulSynthesis).QualityGain, Is.EqualTo(20));
        state = Session(stat: StatType.CraftingFailedSynthesisConditionRecovery, value: 1);
        var next = Act(state, CraftActionType.RapidSynthesis, 100).Session;
        Assert.That(next.CurrentCondition, Is.EqualTo(CraftCondition.Reinforced)); Assert.That(next.Conditions[2], Is.EqualTo(CraftCondition.Economical));
        Assert.That(next.Conditions.Skip(3), Is.EqualTo(state.Conditions.Skip(3)));
        next = Act(state with { Durability = 1 }, CraftActionType.RapidSynthesis, 100).Session;
        Assert.That(next.Status, Is.EqualTo(CraftSessionStatus.Failed)); Assert.That(next.Conditions, Is.SameAs(state.Conditions));
    }

    [Test]
    public void ProfessionTrees_HaveReplacementRanksScopedStatsAndAlternativeCapstoneRoutes()
    {
        IPerkListDefinition[] definitions = { new SmitheryCraftingPerkDefinition(), new EngineeringCraftingPerkDefinition(),
            new FabricationCraftingPerkDefinition(), new AgricultureCraftingPerkDefinition(), new EspionageCraftingPerkDefinition() };
        var skills = new[] { SkillType.Smithery, SkillType.Engineering, SkillType.Fabrication, SkillType.Agriculture, SkillType.Espionage };
        for (var i = 0; i < definitions.Length; i++)
        {
            var perks = definitions[i].BuildPerks(); Assert.That(perks.Count, Is.EqualTo(4));
            Assert.That(perks.Values.Sum(perk => perk.PerkLevels.Values.Sum(level => level.Price)), Is.EqualTo(20));
            foreach (var perk in perks.Values)
            {
                Assert.That(perk.PerkLevels.Values.SelectMany(level => level.StatBonuses).All(bonus => bonus.CraftingSkill == skills[i]), Is.True);
                Assert.That(perk.PerkLevels.Values.All(level => level.StatBonuses.Count == 1), Is.True);
                if (perk.PerkLevels.Count == 1)
                    Assert.That(perk.PerkLevels[1].Requirements.OfType<PerkRequirementAnyCompletedLine>().Single().Lines.Count, Is.EqualTo(3));
            }
        }
    }
}
