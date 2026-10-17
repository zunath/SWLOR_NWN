using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;
using SWLOR.Game.Server.Feature.AbilityDefinition.TwinPistol;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Tests.Engine;

[TestFixture]
public class TwinPistolTests
{
    private AbilityCatalog _catalog;
    [OneTimeSetUp] public void Load() => _catalog = AbilityCatalog.Load();

    [Test]
    public void Catalog_ReadsAllTwentyAttackRanksAndBothStancesFromProduction()
    {
        var twin = _catalog.Abilities.Values.Where(info => info.TwinPistol != null).ToArray();
        twin.Should().HaveCount(20);
        _catalog.Get(FeatType.ControlledBurst2).TwinPistol.WeaponPowerPercent.Should().Be(75);
        _catalog.Get(FeatType.FinalSalvo).TwinPistol.MomentumPowerPerCharge.Should().Be(6);
        _catalog.Get(FeatType.AkimboStance).CastSeconds.Should().Be(2);
        _catalog.Get(FeatType.CrossfireStance).CastSeconds.Should().Be(2);
    }

    [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
    public void CompleteLine_HasTenPerksSixtyPointsAndOnlyOneHandling(bool crossfire, bool rhythm)
    {
        var spec = TwinPistolScenarios.Create(crossfire, rhythm).Teams[0].Members[0];
        spec.Perks.Should().HaveCount(10);
        _catalog.Perks.GetPurchaseCost(spec.Perks).Should().Be(60);
        spec.Perks.Count(perk => perk.StartsWith("MatchedRhythm") || perk.StartsWith("PairedHandling")).Should().Be(1);
        new Simulation(TwinPistolScenarios.Create(crossfire, rhythm), RuleSets.Current(), _catalog, 1).Spawn();
    }

    [Test]
    public void MutuallyExclusiveHandling_IsRejectedBeforeSimulation()
    {
        var scenario = TwinPistolScenarios.Create(false, true);
        scenario.Teams[0].Members[0].Perks.Add("PairedHandling:3");
        Action spawn = () => new Simulation(scenario, RuleSets.Current(), _catalog, 1).Spawn();
        spawn.Should().Throw<InvalidDataException>().WithMessage("*mutually exclusive*");
    }

    [TestCase(false)] [TestCase(true)]
    public void BothLines_RunFiniteStaminaRotationAndTheirCapstones(bool crossfire)
    {
        var scenario = TwinPistolScenarios.Create(crossfire, true);
        var result = new Simulation(scenario, RuleSets.Current(), _catalog, 47).Run();
        var actor = result.Combatants[0];
        actor.Stats.AbilityUses.Should().HaveCount(5);
        actor.Stats.AbilityHits.Should().BeGreaterThan(0);
        actor.Stats.OrdinaryHits.Should().BeGreaterThan(0);
        actor.Stats.StmSpent.Should().BeGreaterThan(actor.Stats.StmRefunded);
        actor.Stm.Should().BeInRange(0, 61);
        actor.Stats.DamageDealt.Should().Be(actor.Stats.AbilityDamage + actor.Stats.OrdinaryDamage);
        if (crossfire) actor.Stats.FlowSpent.Should().BeGreaterThan(0);
    }

    [Test]
    public void NoMovementOpportunity_DoesNotFabricateFlowOrMovementDamage()
    {
        var scenario = TwinPistolScenarios.Create(true, true, movementAvailable: false);
        var actor = new Simulation(scenario, RuleSets.Current(), _catalog, 47).Run().Combatants[0];
        actor.Stats.AbilityUses.Keys.Should().Equal("CrossfireStance");
        actor.Stats.FlowSpent.Should().Be(0);
        actor.Stats.AbilityDamage.Should().Be(0);
    }

    [Test]
    public void TwinSuns_BonusIsSixAttemptsIncludingMisses_NotSixCycles()
    {
        var (sim, actor) = SpawnOne("TwinSuns", "TwinSuns");
        var target = sim.Combatants[1];
        target.Spec.WeaponAvoidancePercent = 100;
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 1, 10, "Twin Suns completes");
        actor.WeaponTraits.Paired.BonusAttempts.Should().Be(6);
        actor.NextActionAt = double.PositiveInfinity;
        actor.NextAttackAt = sim.Now;
        SimulationClock.RunUntil(sim, 11);
        actor.WeaponTraits.Paired.BonusAttempts.Should().Be(0);
        actor.Stats.OrdinaryHits.Should().Be(0);
    }

    [Test]
    public void Rush_StopsAtThreeTargetsAndGrantsOneFlowPerActivation()
    {
        var scenario = TwinPistolScenarios.Create(true, false, targets: 5);
        var spec = scenario.Teams[0].Members[0];
        spec.Abilities = new List<AbilityUseSpec> { new() { Feat = "PistolRush2", MovementMeters = 3, MovementSeconds = 0.7f, LineTargets = 5, MinIntervalSeconds = 600 } };
        var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, 1);
        var actor = sim.Combatants[0];
        actor.NextAttackAt = double.PositiveInfinity;
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilityImpacts > 0, 10, "Rush impacts");
        actor.Stats.AbilityImpacts.Should().Be(6);
        actor.WeaponTraits.Paired.Momentum.Should().Be(1);
    }

    [Test]
    public void ControlledBurst_RefundsOnceEvenWhenBothWeaponsHit()
    {
        var (sim, actor) = SpawnOne("ControlledBurst:2", "ControlledBurst2");
        actor.Spec.WeaponLoadout.Accuracy = 10000;
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 1, 10, "Controlled Burst");
        actor.Stats.AbilityImpacts.Should().Be(2);
        actor.Stats.StmSpent.Should().Be(9);
        actor.Stats.StmRefunded.Should().Be(3);
    }

    [Test]
    public void ShieldOrSingleWeaponProfile_ContributesOnlyOneAbilityImpact()
    {
        var (sim, actor) = SpawnOne("PairedVolley:4", "PairedVolley4");
        actor.Spec.WeaponLoadout.OffhandDamage = 0;
        actor.Spec.WeaponLoadout.OffhandDelay = 0;
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 1, 10, "Paired Volley");
        actor.Stats.AbilityImpacts.Should().Be(1);
    }

    [TestCase("AkimboStance", 30)] [TestCase("CrossfireStance", 20)]
    public void StancePenalty_AppliesToSingleWeaponAutoattacks(string stance, int penalty)
    {
        var (sim, actor) = SpawnOne(stance, stance);
        actor.Spec.WeaponLoadout.OffhandDamage = 0;
        actor.Spec.WeaponLoadout.OffhandDelay = 0;
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 1, 10, "stance");
        actor.GetStat(StatType.OrdinaryAttackDamagePercentAdjustment).Should().Be(-penalty);
        actor.GetStat(StatType.AttackDelayReductionPercent).Should().Be(10);
    }

    [Test]
    public void PvpDamage_UsesTargetRampAndDoesNotReduceStaminaRefunds()
    {
        var (sim, actor) = SpawnOne("ControlledBurst:2", "ControlledBurst2", pvp: true);
        actor.Spec.WeaponLoadout.Accuracy = 10000;
        var target = sim.Combatants[1];
        target.PvpCombatStartedAt = -200;
        target.LastPvpActivityAt = 0;
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 1, 10, "PvP Controlled Burst");
        actor.Stats.StmRefunded.Should().Be(3);
        actor.Stats.AbilityDamage.Should().BeGreaterThan(100);
    }

    [Test]
    public void CoolUnderFire_ExpiredDiscountIsRecheckedWhenTheCastPays()
    {
        var scenario = TwinPistolScenarios.Create(false, true);
        var spec = scenario.Teams[0].Members[0];
        spec.Perks = new List<string> { "TwinSuns", "CoolUnderFire" };
        spec.Abilities = new List<AbilityUseSpec> { new() { Feat = "TwinSuns", MinIntervalSeconds = 600 } };
        var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, 47);
        var actor = sim.Combatants[0];
        actor.NextAttackAt = double.PositiveInfinity;
        actor.NextActionAt = 9;
        sim.ApplyDamage(sim.Combatants[1], actor, 1);
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 1, 14, "Twin Suns after the ten-second discount expires");
        actor.Stats.StmSpent.Should().Be(15);
    }

    [Test]
    public void CoolUnderFire_DiscountIsOneChargeAndNeverAnAbilityRefundLoop()
    {
        var scenario = TwinPistolScenarios.Create(false, true);
        var spec = scenario.Teams[0].Members[0];
        spec.Perks = new List<string> { "ControlledBurst:2", "CoolUnderFire" };
        spec.Abilities = new List<AbilityUseSpec> { new() { Feat = "ControlledBurst2", MinIntervalSeconds = 600 } };
        var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, 47);
        var actor = sim.Combatants[0];
        actor.Spec.WeaponLoadout.Accuracy = 10000;
        actor.NextAttackAt = double.PositiveInfinity;
        sim.ApplyDamage(sim.Combatants[1], actor, 1);
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 1, 3, "discounted Controlled Burst");
        actor.Stats.StmSpent.Should().Be(7);
        actor.Stats.StmRefunded.Should().Be(3);
        actor.WeaponTraits.Paired.GetAbilityStaminaCostAdjustment(DateTime.UnixEpoch.AddSeconds(sim.Now), WeaponTraitState.Configuration(actor)).Should().Be(0);
    }

    [Test]
    public void IncomingAccuracyMisses_ProduceCappedFlowAndSecondWindCooldown()
    {
        var scenario = TwinPistolScenarios.Create(true, false, incomingPressure: true);
        var spec = scenario.Teams[0].Members[0];
        spec.Evasion = 10000;
        spec.Abilities.Clear();
        var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, 47);
        var actor = sim.Combatants[0];
        actor.NextAttackAt = double.PositiveInfinity;
        var enemy = sim.Combatants[1];
        enemy.Spec.WeaponLoadout.MainDelay = 110;
        SimulationClock.RunUntil(sim, () => actor.WeaponTraits.Paired.Momentum == 1, 8, "the first evaded attack");
        var firstGrantAt = sim.Now;
        SimulationClock.RunUntil(sim, firstGrantAt + 9);
        actor.WeaponTraits.Paired.Momentum.Should().Be(1);
        SimulationClock.RunUntil(sim, () => actor.WeaponTraits.Paired.Momentum == 3, 40, "three separately gated evasion grants");
        actor.WeaponTraits.Paired.Momentum.Should().Be(3);
        sim.Now.Should().BeGreaterThanOrEqualTo(firstGrantAt + 19.8, "three grants require two ten-second cooldowns, allowing one clock step of tolerance");
    }

    [Test]
    public void FollowThrough_SpendsCapturedFlowAndRefundsOnceDespiteTwoHits()
    {
        var scenario = TwinPistolScenarios.Create(true, false);
        var spec = scenario.Teams[0].Members[0];
        spec.Abilities = new List<AbilityUseSpec> { new() { Feat = "FinalSalvo", MinIntervalSeconds = 600 } };
        var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, 47);
        var actor = sim.Combatants[0];
        actor.NextAttackAt = double.PositiveInfinity;
        actor.Spec.WeaponLoadout.Accuracy = 10000;
        var config = WeaponTraitState.Configuration(actor);
        actor.WeaponTraits.Paired.RecordAccuracyEvasionMiss(DateTime.UnixEpoch.AddSeconds(-20), config);
        actor.WeaponTraits.Paired.RecordAccuracyEvasionMiss(DateTime.UnixEpoch.AddSeconds(-10), config);
        actor.WeaponTraits.Paired.RecordAccuracyEvasionMiss(DateTime.UnixEpoch, config);
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilityImpacts > 0, 10, "Final Salvo");
        actor.Stats.FlowSpent.Should().Be(3);
        actor.Stats.StmRefunded.Should().Be(3);
        actor.WeaponTraits.Paired.Momentum.Should().Be(0);
    }

    [Test]
    public void MovementBuff_IsCapturedByTheNextCrossfireAbility()
    {
        var scenario = TwinPistolScenarios.Create(true, false);
        var spec = scenario.Teams[0].Members[0];
        spec.Abilities = new List<AbilityUseSpec> { new() { Feat = "BlasterSlide4", MovementMeters = 4, MovementSeconds = .5f, MinIntervalSeconds = 600 } };
        var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, 47);
        var actor = sim.Combatants[0];
        actor.NextAttackAt = double.PositiveInfinity;
        actor.Spec.WeaponLoadout.Accuracy = 10000;
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilityImpacts > 0, 10, "Slide");
        var next = actor.WeaponTraits.Paired.CaptureAbility(DateTime.UnixEpoch.AddSeconds(sim.Now), WeaponTraitState.Configuration(actor), true, true);
        next.MomentumSpent.Should().Be(1);
        next.AccuracyAdjustment.Should().Be(5);
    }

    [Test]
    public void AkimboRampsOnlyOnOrdinaryHits_AndSameTargetHitRefreshesAtCap()
    {
        var (sim, actor) = SpawnOne("AkimboStance", "AkimboStance");
        actor.Spec.WeaponLoadout.Accuracy = 10000;
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 1, 10, "Akimbo");
        actor.WeaponTraits.Haste.Stacks.Should().Be(0);
        actor.Spec.WeaponLoadout.MainDelay = 110;
        actor.Spec.WeaponLoadout.OffhandDelay = 110;
        actor.NextAttackAt = sim.Now;
        SimulationClock.RunUntil(sim, 8);
        actor.WeaponTraits.Haste.Stacks.Should().BeLessThanOrEqualTo(3);
        SimulationClock.RunUntil(sim, 20);
        actor.WeaponTraits.Haste.Stacks.Should().Be(5);
        actor.WeaponTraits.Haste.Expires.Should().BeAfter(DateTime.UnixEpoch.AddSeconds(sim.Now));
        actor.NextAttackAt = double.PositiveInfinity;
        SimulationClock.RunUntil(sim, 27);
        actor.WeaponTraits.Haste.Reconcile(DateTime.UnixEpoch.AddSeconds(sim.Now), WeaponTraitState.HasteConfiguration(actor));
        actor.WeaponTraits.Haste.Stacks.Should().Be(0);
    }

    private (Simulation Sim, Combatant Actor) SpawnOne(string perk, string feat, bool pvp = false)
    {
        var scenario = TwinPistolScenarios.Create(false, true, pvp: pvp);
        var spec = scenario.Teams[0].Members[0];
        spec.Perks = new List<string> { perk };
        spec.StmRegenPer30Seconds = 0;
        spec.Abilities = new List<AbilityUseSpec> { new() { Feat = feat, Target = feat.EndsWith("Stance") ? AbilityTarget.Self : AbilityTarget.Enemy, MinIntervalSeconds = 600 } };
        var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, 47);
        sim.Combatants[0].NextAttackAt = double.PositiveInfinity;
        return (sim, sim.Combatants[0]);
    }

    [Test]
    public void DamageRoll_UsesNativeFloatTruncation_NotInclusiveIntegerSampling()
    {
        var random = new Random(47);
        Enumerable.Range(0, 1000).Select(_ => Simulation.SampleWeaponDamage(random, 1, 2)).Should().OnlyContain(damage => damage == 1);
        Enumerable.Range(0, 1000).Select(_ => Simulation.SampleWeaponDamage(random, 10, 15)).Should().OnlyContain(damage => damage >= 10 && damage < 15);
        Simulation.SampleWeaponDamage(random, 10, 10).Should().Be(10);
    }

    [TestCase(0f)] [TestCase(.005f)]
    public void GrappleAlreadyInsideStoppingRange_DoesNotWaitForMovementLockout(float distance)
    {
        var scenario = TwinPistolScenarios.Create(true, false);
        scenario.Teams[0].Members[0].Abilities = new List<AbilityUseSpec>
        {
            new() { Feat = "GrappleShot3", MovementMeters = distance, MovementSeconds = 1, MinIntervalSeconds = 600 }
        };
        var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, 47);
        var actor = sim.Combatants[0];
        actor.WeaponTraits.MovementReadyAt = 2;
        actor.NextActionAt = 0;
        actor.NextAttackAt = double.PositiveInfinity;
        sim.Step(.1);
        actor.Stats.AbilitiesUsed.Should().Be(1);
        actor.Stats.AbilityImpacts.Should().Be(2);
    }
}
