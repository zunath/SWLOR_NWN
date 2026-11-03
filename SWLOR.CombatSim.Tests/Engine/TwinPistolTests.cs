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
        _catalog.Get(FeatType.FinalSalvo).TwinPistol.CadencePowerPerCharge.Should().Be(4);
        _catalog.Get(FeatType.FinalSalvo).WeaponAbilityCadenceDamagePerCharge.Should().Be(4);
        _catalog.Get(FeatType.ClosingShot3).WeaponAbilityCadenceDamagePerCharge.Should().Be(2);
        _catalog.Get(FeatType.LeadShot4).WeaponAbilityCadenceOnHit.Should().Be(1);
        _catalog.Get(FeatType.TempoShot2).WeaponAbilityCadenceOnHit.Should().Be(2);
        _catalog.Get(FeatType.LeadShot1).KeepsWeaponAbilityCadence.Should().BeTrue();
        _catalog.Get(FeatType.ClosingShot1).KeepsWeaponAbilityCadence.Should().BeFalse();
        _catalog.Get(FeatType.LeadShot1).CadenceChainKey.Should().Be(_catalog.Get(FeatType.LeadShot4).CadenceChainKey,
            "ranks of one ability share a chain key");
        _catalog.Get(FeatType.LeadShot1).CadenceChainKey.Should().NotBe(_catalog.Get(FeatType.TempoShot1).CadenceChainKey);
        _catalog.Get(FeatType.AkimboStance).CastSeconds.Should().Be(2);
        _catalog.Get(FeatType.CrossfireStance).CastSeconds.Should().Be(2);
    }

    [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
    public void CompleteLine_HasTenPerksSixtyPointsAndOnlyOneHandling(bool crossfire, bool rhythm)
    {
        var spec = TwinPistolScenarios.Create(crossfire, rhythm).Teams[0].Members[0];
        spec.Perks.Should().HaveCount(10);
        _catalog.Perks.GetPurchaseCost(spec.Perks).Should().Be(60);
        spec.Perks.Count(perk => perk.StartsWith("MatchedRhythm") || perk.StartsWith("SteadyCadence")).Should().Be(1);
        new Simulation(TwinPistolScenarios.Create(crossfire, rhythm), RuleSets.Current(), _catalog, 1).Spawn();
    }

    [Test]
    public void BarrageWithoutMatchedRhythm_IsRefused_BecauseSteadyCadenceNeedsTheCadenceTrait()
    {
        Action create = () => TwinPistolScenarios.Create(false, false);
        create.Should().Throw<ArgumentException>().WithMessage("*Cadence trait*");
    }

    [Test]
    public void MatchedRhythmAndSteadyCadence_AreNoLongerMutuallyExclusive()
    {
        var scenario = TwinPistolScenarios.Create(true, false);
        scenario.Teams[0].Members[0].Perks.Add("MatchedRhythm:3");
        Action spawn = () => new Simulation(scenario, RuleSets.Current(), _catalog, 1).Spawn();
        spawn.Should().NotThrow();
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
        if (crossfire)
        {
            actor.Stats.CadenceSpent.Should().BeGreaterThan(0);
            actor.Stats.CadenceChains.Should().BeGreaterThan(0, "the Crossfire rotation chains Tempo Shot into Lead Shot");
        }
    }

    [Test]
    public void OrdinaryAttacksAlone_NeverGrantCadence()
    {
        var scenario = TwinPistolScenarios.Create(true, false, duration: 30);
        scenario.Teams[0].Members[0].Abilities.Clear();
        var actor = new Simulation(scenario, RuleSets.Current(), _catalog, 47).Run().Combatants[0];
        actor.Stats.OrdinaryHits.Should().BeGreaterThan(0);
        actor.WeaponTraits.Paired.Cadence.Should().Be(0);
        actor.Stats.CadenceSpent.Should().Be(0);
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
    public void TempoShot_HitsOnlyItsSelectedEnemy_AndGrantsTwoCadenceOncePerActivation()
    {
        var scenario = TwinPistolScenarios.Create(true, false, targets: 3);
        var spec = scenario.Teams[0].Members[0];
        spec.Abilities = new List<AbilityUseSpec> { new() { Feat = "TempoShot2", MinIntervalSeconds = 600 } };
        var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, 47);
        var actor = sim.Combatants[0];
        actor.Spec.WeaponLoadout.Accuracy = 10000;
        actor.NextAttackAt = double.PositiveInfinity;
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilityImpacts > 0, 10, "Tempo Shot impacts");
        actor.Stats.AbilityImpacts.Should().Be(2, "one shot per gun at the selected enemy");
        actor.Stats.AbilityHits.Should().Be(2);
        actor.WeaponTraits.Paired.Cadence.Should().Be(2, "both guns landing still grant the activation's two Cadence once");
        sim.Combatants.Skip(1).Count(target => target.Hp < target.Spec.MaxHp).Should().Be(1);
    }

    [Test]
    public void CrossfireChain_TempoLeadClosing_BuildsThreeThenSpendsAndChainsAgain()
    {
        var scenario = TwinPistolScenarios.Create(true, false);
        var spec = scenario.Teams[0].Members[0];
        spec.StmRegenPer30Seconds = 0;
        spec.Abilities = new List<AbilityUseSpec>
        {
            new() { Feat = "TempoShot2", MinIntervalSeconds = 600 },
            new() { Feat = "LeadShot4", MinIntervalSeconds = 600 },
            new() { Feat = "ClosingShot3", MinimumCadence = 3, MinIntervalSeconds = 600 }
        };
        var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, 47);
        var actor = sim.Combatants[0];
        actor.Spec.WeaponLoadout.Accuracy = 10000;
        actor.NextActionAt = 0;
        actor.NextAttackAt = double.PositiveInfinity;
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 1 && actor.Casting == null, 6, "Tempo Shot");
        actor.WeaponTraits.Paired.Cadence.Should().Be(2, "Tempo Shot grants two without a chain");
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 2 && actor.Casting == null, 6, "Lead Shot");
        actor.WeaponTraits.Paired.Cadence.Should().Be(3, "Lead Shot's chain and own grant do not stack; it adds one");
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 3 && actor.Casting == null, 6, "Closing Shot");
        actor.Stats.CadenceSpent.Should().Be(3);
        actor.Stats.CadenceChains.Should().Be(2);
        // Steady Cadence III adds +3 to the generic +2 per charge, once per gun.
        actor.Stats.CadenceDamagePowerApplied.Should().Be(3 * (2 + 3) * 2);
        actor.WeaponTraits.Paired.Cadence.Should().Be(1, "Closing Shot spends at payment, then its chained hit grants one");
    }

    [Test]
    public void WithoutTheCadenceTrait_ClosingShotSpendsLeadShotsCadenceAtItsOwnRate()
    {
        var scenario = TwinPistolScenarios.Create(true, false);
        var spec = scenario.Teams[0].Members[0];
        spec.Perks = new() { "LeadShot:4", "ClosingShot:3" };
        spec.StmRegenPer30Seconds = 0;
        spec.Abilities = new List<AbilityUseSpec>
        {
            new() { Feat = "LeadShot4", MinIntervalSeconds = 600 },
            new() { Feat = "ClosingShot3", MinimumCadence = 1, MinIntervalSeconds = 600 }
        };
        var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, 47);
        var actor = sim.Combatants[0];
        actor.Spec.WeaponLoadout.Accuracy = 10000;
        actor.NextActionAt = 0;
        actor.NextAttackAt = double.PositiveInfinity;
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 2 && actor.Casting == null, 12, "Lead then Closing Shot");
        actor.Stats.CadenceSpent.Should().Be(1, "Lead Shot's pool exists before the Cadence trait is bought");
        actor.Stats.CadenceDamagePowerApplied.Should().Be(1 * 2 * 2, "Closing Shot's own +2 per charge, once per gun");
        actor.Stats.CadenceChains.Should().Be(0, "chaining belongs to the Cadence trait");
        actor.WeaponTraits.Paired.Cadence.Should().Be(0);
    }

    [Test]
    public void RepeatingOneAbility_DoesNotContinueTheChain()
    {
        var scenario = TwinPistolScenarios.Create(true, false);
        var spec = scenario.Teams[0].Members[0];
        spec.Abilities = new List<AbilityUseSpec> { new() { Feat = "LeadShot4" } };
        var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, 47);
        var actor = sim.Combatants[0];
        actor.Spec.WeaponLoadout.Accuracy = 10000;
        actor.NextActionAt = 0;
        actor.NextAttackAt = double.PositiveInfinity;
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 1 && actor.Casting == null, 6, "first Lead Shot");
        actor.RecastReadyAt.Clear();
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 2 && actor.Casting == null, 6, "repeated Lead Shot");
        actor.Stats.CadenceChains.Should().Be(0);
        actor.WeaponTraits.Paired.Cadence.Should().Be(2, "each Lead Shot only adds its own single charge");
    }

    [Test]
    public void ASpenderAfterTheWindowExpires_DoesNotChain()
    {
        var scenario = TwinPistolScenarios.Create(true, false);
        var spec = scenario.Teams[0].Members[0];
        spec.Abilities = new List<AbilityUseSpec>
        {
            new() { Feat = "TempoShot2", MinIntervalSeconds = 600 },
            new() { Feat = "ClosingShot3", MinIntervalSeconds = 600 }
        };
        var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, 47);
        var actor = sim.Combatants[0];
        actor.Spec.WeaponLoadout.Accuracy = 10000;
        actor.NextActionAt = 0;
        actor.NextAttackAt = double.PositiveInfinity;
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 1 && actor.Casting == null, 6, "Tempo Shot");
        actor.NextActionAt = sim.Now + 6.5;
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 2 && actor.Casting == null, 10, "late Closing Shot");
        actor.Stats.CadenceSpent.Should().Be(2);
        actor.Stats.CadenceChains.Should().Be(0);
        actor.WeaponTraits.Paired.Cadence.Should().Be(0);
    }

    [TestCase(false, 3 * 4 * 2)] [TestCase(true, 3 * (4 + 3) * 2)]
    public void SteadyCadence_AddsToFinalSalvosOwnPerChargeDamage(bool steadyCadence, int expected)
    {
        var scenario = TwinPistolScenarios.Create(true, !steadyCadence);
        var spec = scenario.Teams[0].Members[0];
        spec.Abilities = new List<AbilityUseSpec> { new() { Feat = "FinalSalvo", MinIntervalSeconds = 600 } };
        var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, 47);
        var actor = sim.Combatants[0];
        actor.NextAttackAt = double.PositiveInfinity;
        actor.Spec.WeaponLoadout.Accuracy = 10000;
        GrantThreeCadence(actor);
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilityImpacts > 0, 10, "Final Salvo");
        actor.Stats.CadenceSpent.Should().Be(3);
        actor.Stats.CadenceDamagePowerApplied.Should().Be(expected);
    }

    [TestCase("TwinSuns")] [TestCase("FinalSalvo")]
    public void TargetDiesDuringCast_CancelsBeforeCostsCadenceDiscountRecastAndActivation(string feat)
    {
        var scenario = TwinPistolScenarios.Create(true, false);
        var spec = scenario.Teams[0].Members[0];
        spec.Perks.Add("CoolUnderFire");
        if (feat == "TwinSuns") spec.Perks.Add("TwinSuns");
        spec.StmRegenPer30Seconds = 0;
        spec.Abilities = new List<AbilityUseSpec> { new() { Feat = feat, MinIntervalSeconds = 600 } };
        var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, 47);
        var actor = sim.Combatants[0];
        actor.NextActionAt = 0;
        actor.NextAttackAt = double.PositiveInfinity;
        GrantThreeCadence(actor);
        sim.ApplyDamage(sim.Combatants[1], actor, 1);
        var stamina = actor.Stm;
        sim.Step(.1);
        actor.Casting.Should().NotBeNull();
        actor.Casting.Activated.Should().BeFalse();
        var recast = actor.Casting.Ability.RecastGroup;
        sim.ApplyDamage(actor, actor.Casting.Target, actor.Casting.Target.Hp);
        actor.NextActionAt = double.PositiveInfinity;

        SimulationClock.Set(sim, 2.1);
        sim.Step(.1);

        actor.Casting.Should().BeNull();
        actor.Stm.Should().Be(stamina);
        actor.Stats.StmSpent.Should().Be(0);
        actor.Stats.StmRefunded.Should().Be(0);
        actor.Stats.AbilitiesUsed.Should().Be(0);
        actor.Stats.AbilityUses.Should().BeEmpty();
        actor.Stats.AbilityImpacts.Should().Be(0);
        actor.Stats.CadenceSpent.Should().Be(0);
        actor.WeaponTraits.Paired.Cadence.Should().Be(3);
        actor.WeaponTraits.Paired.BonusAttempts.Should().Be(0);
        actor.WeaponTraits.Paired.GetAbilityStaminaCostAdjustment(DateTime.UnixEpoch.AddSeconds(sim.Now),
            WeaponTraitState.Configuration(actor)).Should().Be(-2);
        actor.RecastReadyAt.Should().NotContainKey(recast);
        actor.LastUsedAt.Should().NotContainKey(feat);
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

    [TestCase(false, 2)] [TestCase(true, 1)]
    public void ActivatedImpacts_CountDistinctItems_WhileDoubleWeaponsKeepBothOrdinaryEnds(bool sharesMainItem, int impacts)
    {
        var scenario = TwinPistolScenarios.Create(false, true);
        var spec = scenario.Teams[0].Members[0];
        spec.Perks = new List<string> { "PairedVolley:4" };
        spec.WeaponLoadout.OffhandSharesMainItem = sharesMainItem;
        spec.Abilities = new List<AbilityUseSpec> { new() { Feat = "PairedVolley4", MinIntervalSeconds = 600 } };
        var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, 47);
        var actor = sim.Combatants[0];
        actor.NextActionAt = 0;
        actor.NextAttackAt = double.PositiveInfinity;
        sim.Step(.1);

        actor.Stats.AbilityImpacts.Should().Be(impacts);
        actor.Stats.AbilityUses["PairedVolley4"].Should().Be(1);
        actor.NextActionAt = double.PositiveInfinity;
        actor.NextAttackAt = sim.Now;
        sim.Step(.1);
        actor.Stats.OrdinaryAttempts.Should().Be(2, "the ordinary off-hand end does not imply a second equipped item");
    }

    [TestCase(0, 0)] [TestCase(21, 250)] [TestCase(22, 200)]
    public void SharedItemOffhand_RejectsMissingOrConflictingItemProfiles(int damage, int delay)
    {
        var scenario = TwinPistolScenarios.Create(false, true);
        var weapon = scenario.Teams[0].Members[0].WeaponLoadout;
        weapon.OffhandSharesMainItem = true;
        weapon.OffhandDamage = damage;
        weapon.OffhandDelay = delay;
        Action spawn = () => TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, 47);
        spawn.Should().Throw<InvalidDataException>().WithMessage("*WeaponLoadout*");
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
    public void IncomingAccuracyMisses_ProduceCappedCadenceAndSecondWindCooldown()
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
        SimulationClock.RunUntil(sim, () => actor.WeaponTraits.Paired.Cadence == 1, 8, "the first evaded attack");
        var firstGrantAt = sim.Now;
        SimulationClock.RunUntil(sim, firstGrantAt + 9);
        actor.WeaponTraits.Paired.Cadence.Should().Be(1);
        SimulationClock.RunUntil(sim, () => actor.WeaponTraits.Paired.Cadence == 3, 40, "three separately gated evasion grants");
        actor.WeaponTraits.Paired.Cadence.Should().Be(3);
        sim.Now.Should().BeGreaterThanOrEqualTo(firstGrantAt + 19.8, "three grants require two ten-second cooldowns, allowing one clock step of tolerance");
    }

    [Test]
    public void FollowThrough_SpendsCapturedCadenceAndRefundsOnceDespiteTwoHits()
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
        actor.Stats.CadenceSpent.Should().Be(3);
        actor.Stats.StmRefunded.Should().Be(3);
        actor.WeaponTraits.Paired.Cadence.Should().Be(0);
    }

    [Test]
    public void ChangingAngles_ArmedByLeadShotsCadence_IsCapturedByTheNextAbility()
    {
        var scenario = TwinPistolScenarios.Create(true, false);
        var spec = scenario.Teams[0].Members[0];
        spec.Abilities = new List<AbilityUseSpec> { new() { Feat = "LeadShot4", MinIntervalSeconds = 600 } };
        var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, 47);
        var actor = sim.Combatants[0];
        actor.NextAttackAt = double.PositiveInfinity;
        actor.Spec.WeaponLoadout.Accuracy = 10000;
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilityImpacts > 0, 10, "Lead Shot");
        var next = actor.WeaponTraits.Paired.CaptureAbility(DateTime.UnixEpoch.AddSeconds(sim.Now), WeaponTraitState.Configuration(actor), true, true);
        next.CadenceSpent.Should().Be(1);
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

    [Test]
    public void ActualDeath_ResetsHasteGrantCooldownAndHitHistory_UnlikeStanceClear()
    {
        var (sim, actor) = SpawnOne("AkimboStance", "AkimboStance");
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 1, 10, "Akimbo");
        var config = WeaponTraitState.HasteConfiguration(actor);
        var now = DateTime.UnixEpoch.AddSeconds(sim.Now);
        actor.WeaponTraits.Haste.RecordHit(now, config, 2, 1).Should().BeTrue();
        actor.WeaponTraits.Haste.Stacks.Should().Be(1);
        actor.WeaponTraits.Haste.Clear();
        actor.WeaponTraits.Haste.RecordHit(now.AddSeconds(.1), config, 2, 2).Should().BeTrue();
        actor.WeaponTraits.Haste.Stacks.Should().Be(0, "an ordinary stance/equipment clear preserves the grant cooldown");

        sim.ApplyDamage(sim.Combatants[1], actor, actor.Hp);
        actor.IsDead.Should().BeTrue();
        actor.WeaponTraits.Haste.Stacks.Should().Be(0);
        // Isolate the actual death lifecycle from revive/stance cast times, using the real restored stance configuration.
        actor.IsDead = false;
        actor.Hp = 1;
        SimulationClock.Set(sim, sim.Now + .2);
        actor.WeaponTraits.Haste.RecordHit(now.AddSeconds(.2), config, 2, 1).Should().BeTrue();
        actor.WeaponTraits.Haste.Stacks.Should().Be(1, "death resets both the two-second grant gate and old roll identities");
    }

    private static void GrantThreeCadence(Combatant actor)
    {
        var config = WeaponTraitState.Configuration(actor);
        actor.WeaponTraits.Paired.RecordAccuracyEvasionMiss(DateTime.UnixEpoch.AddSeconds(-20), config);
        actor.WeaponTraits.Paired.RecordAccuracyEvasionMiss(DateTime.UnixEpoch.AddSeconds(-10), config);
        actor.WeaponTraits.Paired.RecordAccuracyEvasionMiss(DateTime.UnixEpoch, config);
        actor.WeaponTraits.Paired.Cadence.Should().Be(3);
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
}
