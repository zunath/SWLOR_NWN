using FluentAssertions;
using NUnit.Framework;
using System.Text.Json;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.CombatSim.Tests.Engine;

[TestFixture]
public class CrossSkillWeaponTraitTests
{
    private AbilityCatalog _catalog;
    [OneTimeSetUp] public void Load() => _catalog = AbilityCatalog.Load();

    [Test]
    public void ALeadShotChainedIntoAForeignAbility_SpendsThenGrantsFreshCadenceAndAngles()
    {
        var scenario = Create("DoubleShot1", "DoubleShot:1", 2, 1, 1, true);
        var spec = scenario.Teams[0].Members[0];
        spec.Perks.Add("LeadShot:1");
        spec.Abilities.Insert(0, new() { Feat = "LeadShot1", MinIntervalSeconds = 600 });
        var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, 47);
        var actor = sim.Combatants[0];
        actor.NextAttackAt = double.PositiveInfinity; actor.NextActionAt = 0;
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 1 && actor.Casting == null, 8, "Lead Shot");
        actor.WeaponTraits.Paired.Cadence.Should().Be(1, "Lead Shot grants its own Cadence without a chain");
        actor.WeaponTraits.Paired.CaptureAbility(DateTime.UnixEpoch.AddSeconds(sim.Now), WeaponTraitState.Configuration(actor), true, false)
            .AccuracyAdjustment.Should().Be(5, "gaining Cadence arms Changing Angles");
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 2 && actor.Stats.AbilityImpacts > 2, 8, "foreign Double Shot");
        actor.Stats.CadenceChains.Should().Be(1, "a different paid weapon ability within six seconds continues the chain");
        actor.Stats.CadenceSpent.Should().Be(1);
        actor.Stats.CadenceDamagePowerApplied.Should().Be(2);
        actor.WeaponTraits.Paired.Cadence.Should().Be(1, "the chained hit grants one fresh charge after the spend");
    }

    [Test]
    public void WithoutTheCadenceTrait_LeadShotStillGrantsCadence_AndForeignAbilitiesLeaveIt()
    {
        var scenario = Create("DoubleShot1", "DoubleShot:1", 2, 1, 1, true);
        var spec = scenario.Teams[0].Members[0];
        spec.Perks.Remove("Cadence");
        spec.Perks.Add("LeadShot:1");
        spec.Abilities.Insert(0, new() { Feat = "LeadShot1", MinIntervalSeconds = 600 });
        var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, 47);
        var actor = sim.Combatants[0];
        actor.NextAttackAt = double.PositiveInfinity; actor.NextActionAt = 0;
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 1 && actor.Casting == null, 8, "Lead Shot");
        actor.WeaponTraits.Paired.Cadence.Should().Be(1, "Lead Shot carries its own Cadence pool");
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 2 && actor.Stats.AbilityImpacts > 2, 8, "foreign Double Shot");
        actor.Stats.CadenceSpent.Should().Be(0, "a foreign ability gains nothing per charge without the Cadence trait");
        actor.Stats.CadenceChains.Should().Be(0, "chaining belongs to the Cadence trait");
        actor.WeaponTraits.Paired.Cadence.Should().Be(1);
    }

    [Test]
    public void OrdinaryHits_NeverGrantCadence()
    {
        var (sim, actor) = Spawn(); actor.Spec.Abilities.Clear(); actor.NextActionAt = double.PositiveInfinity;
        actor.NextAttackAt = 0;
        SimulationClock.RunUntil(sim, () => actor.Stats.OrdinaryHits >= 6, 20, "landed ordinary attacks");
        actor.WeaponTraits.Paired.Cadence.Should().Be(0);
    }

    [Test]
    public void QueuedForeignAbility_CommitsCadenceOnceAtPayment_AndAnUnchainedImpactGrantsNone()
    {
        var (sim, actor) = Spawn("FortressStrike3", "FortressStrike:3");
        actor.Spec.WeaponLoadout.OffhandDamage = actor.Spec.WeaponLoadout.OffhandDelay = 0;
        GrantCadence(sim, actor);
        SimulationClock.RunUntil(sim, () => actor.QueuedWeaponAbility != null, 8, "queued weapon payment");
        actor.Stats.CadenceSpent.Should().Be(3);
        actor.WeaponTraits.Paired.Cadence.Should().Be(0);
        actor.NextActionAt = double.PositiveInfinity; actor.NextAttackAt = sim.Now;
        SimulationClock.RunUntil(sim, () => actor.QueuedWeaponAbility == null, 8, "queued weapon impact");
        actor.Stats.CadenceSpent.Should().Be(3);
        actor.Stats.CadenceDamagePowerApplied.Should().Be(6);
        actor.Stats.OrdinaryDamage.Should().Be(0, "the queued activation replaces its carrier");
        actor.Stats.OrdinaryHits.Should().Be(0);
        actor.WeaponTraits.Paired.Cadence.Should().Be(0, "the only ability used cannot continue a chain");
    }

    [Test]
    public void ForeignPassiveFamiliesRemainFailClosed()
    {
        var scenario = Create("DoubleShot1", "DoubleShot:1", 2, 1, 1, true);
        scenario.Teams[0].Members[0].Perks.Add("DefensiveHarmony");
        Action spawn = () => new Simulation(scenario, RuleSets.Current(), _catalog, 1).Spawn();
        spawn.Should().Throw<InvalidDataException>().WithMessage("*does not implement DefensiveHarmony*");
    }

    [TestCase(1, 2, 1)] [TestCase(1, 100, 3)] [TestCase(2, 100, 3)]
    public void ForeignMultiHitAndArea_ReceiveOneCadenceBudgetPerRealWeapon(int items, int hits, int targets)
    {
        var (sim, actor) = Spawn(hits: hits, targets: targets, items: items);
        GrantCadence(sim, actor);
        Finish(sim, actor);
        actor.Stats.CadenceSpent.Should().Be(3);
        actor.Stats.CadenceDamagePowerApplied.Should().Be(6 * items);
        actor.Stats.StmSpent.Should().Be(4);
        actor.Stats.StmRefunded.Should().Be(3);
        actor.WeaponTraits.Paired.Cadence.Should().Be(0);
        actor.Stats.AbilityImpacts.Should().Be(hits * targets);
    }

    [Test]
    public void NativeTwoWeaponProfile_CannotInventAnUnequippedSecondWeaponBudget()
    {
        var (sim, actor) = Spawn(items: 2);
        actor.Spec.WeaponLoadout.OffhandDamage = actor.Spec.WeaponLoadout.OffhandDelay = 0;
        GrantCadence(sim, actor); Finish(sim, actor);
        actor.Stats.CadenceDamagePowerApplied.Should().Be(6);
    }

    [TestCase(0, 0, 3)] [TestCase(.25f, 1, 0)]
    public void ActualStaminaControlsCadence_AndRefundCannotMakePaidAbilityFree(float multiplier, int paid, int remaining)
    {
        var rules = RuleSets.Current();
        rules.CostSkillThreshold = 0;
        rules.StmCostMultiplier = multiplier;
        var (sim, actor) = Spawn(rules: rules);
        GrantCadence(sim, actor);
        Finish(sim, actor);
        actor.Stats.StmSpent.Should().Be(paid);
        actor.Stats.StmRefunded.Should().Be(0);
        actor.WeaponTraits.Paired.Cadence.Should().Be(remaining);
        actor.Stats.CadenceDamagePowerApplied.Should().Be(paid > 0 ? 6 : 0);
    }

    [Test]
    public void CoolUnderFireMakesFirstLowCostCastFree_SecondCastPaysAndSpendsCadence()
    {
        var rules = RuleSets.Current(); rules.CostSkillThreshold = 0; rules.StmCostMultiplier = .5f;
        var (sim, actor) = Spawn(rules: rules);
        GrantCadence(sim, actor);
        sim.ApplyDamage(sim.Combatants[1], actor, 1);
        Finish(sim, actor);
        actor.Stats.StmSpent.Should().Be(0);
        actor.Stats.CadenceSpent.Should().Be(0);
        actor.WeaponTraits.Paired.Cadence.Should().Be(3);
        actor.RecastReadyAt.Clear(); actor.LastUsedAt.Clear();
        actor.HostileAbilityReadyAt = sim.Now; actor.NextActionAt = sim.Now;
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 2, 8, "second paid action");
        if (actor.QueuedWeaponAbility != null)
        {
            actor.NextAttackAt = sim.Now;
            SimulationClock.RunUntil(sim, () => actor.QueuedWeaponAbility == null, 8, "second queued impact");
        }
        actor.Stats.StmSpent.Should().Be(2);
        actor.Stats.CadenceSpent.Should().Be(3);
        actor.Stats.StmRefunded.Should().Be(1);
    }

    [Test]
    public void SupportMedKitConsumesCoolUnderFire_ButPreservesCadenceAndAngles()
    {
        var (sim, actor) = Spawn("MedKit1", "MedKit:1", damage: false);
        GrantCadence(sim, actor);
        sim.ApplyDamage(sim.Combatants[1], actor, 100);
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 1, 8, "First Aid support ability");
        actor.Stats.StmSpent.Should().Be(2);
        actor.WeaponTraits.Paired.Cadence.Should().Be(3);
        actor.WeaponTraits.Paired.CaptureAbility(DateTime.UnixEpoch.AddSeconds(sim.Now), WeaponTraitState.Configuration(actor), true, false)
            .AccuracyAdjustment.Should().Be(5);
        actor.Stats.HealingDone.Should().BeGreaterThan(0);
    }

    [Test]
    public void ForceAbilityPreservesCadence_AndBalancedGripSuppliesGlobalAccuracy()
    {
        var (sim, actor) = Spawn("ForcePush1", "ForcePush:1");
        GrantCadence(sim, actor);
        Finish(sim, actor, weapon: false);
        actor.GetStat(StatType.Accuracy).Should().Be(5);
        actor.WeaponTraits.Paired.Cadence.Should().Be(3);
        actor.Stats.CadenceSpent.Should().Be(0);
        actor.Stats.CadenceDamagePowerApplied.Should().Be(0);
    }

    [Test]
    public void BalancedGripChangesForceHitMath_RatherThanAddingFivePercentagePoints()
    {
        double withGrip = 0, withoutGrip = 0;
        foreach (var seed in Enumerable.Range(20261018, 64))
        foreach (var grip in new[] { false, true })
        {
            var scenario = Create("ForcePush1", "ForcePush:1", 1000, 1, 1, true);
            var spec = scenario.Teams[0].Members[0];
            spec.Abilities[0].Damage.Accuracy = 134;
            if (!grip) spec.Perks.Remove("BalancedGrip");
            var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, seed);
            var actor = sim.Combatants[0]; actor.NextActionAt = 0; actor.NextAttackAt = double.PositiveInfinity;
            Finish(sim, actor, weapon: false);
            if (grip) withGrip += actor.Stats.DamageDealt; else withoutGrip += actor.Stats.DamageDealt;
        }
        ((withGrip - withoutGrip) / 64000).Should().BeApproximately(.02, .005,
            "+5 ACC changes floor((ACC-EVA)/2) by two at equal even ratings");
    }

    [TestCase(SkillType.FirstAid, true)] [TestCase(SkillType.Force, true)]
    [TestCase(SkillType.Smithery, false)] [TestCase(SkillType.Invalid, false)]
    public void CostPolicy_AdmitsCombatSupportAndRejectsCrafting(SkillType skill, bool eligible)
    {
        WeaponAbilityTraitPolicy.IsEligibleCostReduction(skill, 1).Should().Be(eligible);
        WeaponAbilityTraitPolicy.IsEligibleCostReduction(skill, 0).Should().BeFalse();
    }

    [Test]
    public void UnknownFinalDamageWithCadence_FailsClosedInsteadOfAddingUnscaledDamage()
    {
        var scenario = Create("DoubleShot1", "DoubleShot:1", 2, 1, 1, true);
        var damage = scenario.Teams[0].Members[0].Abilities[0].Damage;
        damage.WeaponPower = null; damage.Min = damage.Max = 1;
        Action spawn = () => TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, 47);
        spawn.Should().Throw<InvalidDataException>().WithMessage("*cannot apply Cadence to calibrated final damage*");
        scenario.Teams[0].Members[0].Perks.Remove("Cadence");
        spawn.Should().NotThrow("calibrated damage remains usable without an unknown Cadence power conversion");
    }

    [TestCase(50, 94)] [TestCase(150, 94)] [TestCase(150, 250)]
    public void ForeignCadenceDamage_IsNativePowerBeforeAttackDefenseAndCriticalScaling(int attack, int defense)
    {
        var rows = new List<object>();
        foreach (var critical in new[] { -100, 100 })
        foreach (var seed in Enumerable.Range(20261019, 64))
        {
            double Run(bool cadence)
            {
                // One isolated Double Shot I impact: authored flat6 plus100% weapon rating.
                // +6 is the approved three-charge Cadence power, not six final damage points.
                var scenario = Create("DoubleShot1", "DoubleShot:1", 1, 1, 1, true);
                var spec = scenario.Teams[0].Members[0];
                spec.WeaponLoadout.Attack = attack;
                spec.WeaponLoadout.AbilityCriticalRateAdjustment = critical;
                scenario.Teams[1].Members[0].Defense = defense;
                if (!cadence) spec.Abilities[0].Damage.WeaponPower.FlatDamage += 6;
                var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, seed);
                var actor = sim.Combatants[0]; actor.NextActionAt = 0; actor.NextAttackAt = double.PositiveInfinity;
                if (cadence) GrantCadence(sim, actor);
                Finish(sim, actor);
                actor.Stats.CadenceDamagePowerApplied.Should().Be(cadence && actor.Stats.AbilityHits > 0 ? 6 : 0);
                return actor.Stats.DamageDealt;
            }
            var cadenceDamage = Run(true);
            var explicitPowerDamage = Run(false);
            cadenceDamage.Should().Be(explicitPowerDamage, "Cadence must be identical to the same native ability power, even across ATT/DEF and critical scaling");
            rows.Add(new { attack, defense, criticalAdjustment = critical, seed, cadenceDamage, explicitPowerDamage });
        }
        var directory = Environment.GetEnvironmentVariable("TWIN_SIM_OUTPUT") ?? Path.Combine(Path.GetTempPath(), "swlor-twin-cross-skill-sim-20261018");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"foreign-native-power-{attack}-{defense}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            method = "64 paired seeds x two critical-rate controls x two equivalent profiles. One isolated Double Shot I impact (source authored flat6, 100% native weapon rating) with three Cadence versus explicit flat18. " +
                "ATT/DEF pairs are unit sensitivity controls, not authored encounter balance. Native rank, attribute, ATT/DEF and active critical formulas; ordinary attacks disabled. " +
                "Source double-hit critical stamina refund and other foreign perk/status behavior are not modeled or claimed. Enlarged hit/target budget tests elsewhere are explicit stress fixtures.",
            simulations = rows.Count * 2, rows
        }, new JsonSerializerOptions { WriteIndented = true }));
        TestContext.AddTestAttachment(path);
    }

    [TestCase(true, 1)] [TestCase(false, 0)]
    public void OnlyGenuineAccuracyEvasionMisses_GrantSecondWind(bool nativeAccuracy, int expected)
    {
        var scenario = Create("ForcePush1", "ForcePush:1", 1000, 1, 1, true);
        var target = scenario.Teams[1].Members[0];
        target.Evasion = 10000;
        target.Perks = new() { "Cadence", "TwinPistolSecondWind" };
        var damage = scenario.Teams[0].Members[0].Abilities[0].Damage;
        if (nativeAccuracy) damage.Accuracy = 0; else damage.HitChance = 0;
        var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, 47);
        var actor = sim.Combatants[0]; actor.NextActionAt = 0; actor.NextAttackAt = double.PositiveInfinity;
        Finish(sim, actor, weapon: false);
        sim.Combatants[1].WeaponTraits.Paired.Cadence.Should().Be(expected,
            "a thousand misses in one cast can grant at most one charge through Second Wind's ten-second gate");
    }

    [TestCase(false)] [TestCase(true)]
    public void NativeWeaponPower_RequiresWeaponSkillAndLoadout(bool force)
    {
        var scenario = Create(force ? "ForcePush1" : "DoubleShot1", force ? "ForcePush:1" : "DoubleShot:1", 2, 1, 1, true);
        var spec = scenario.Teams[0].Members[0];
        if (force)
        {
            spec.Abilities[0].Damage.Min = spec.Abilities[0].Damage.Max = 0;
            spec.Abilities[0].Damage.WeaponPower = new() { FlatDamage = 6 };
        }
        else spec.WeaponLoadout = null;
        Action spawn = () => TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, 47);
        spawn.Should().Throw<InvalidDataException>().WithMessage("*WeaponPower needs a WeaponLoadout and a weapon skill*");
    }

    [TestCase("MatchedRhythm:3")]
    [TestCase("AlternatingFire")]
    public void HandlingAndAlternatingFire_ApplyOnlyWhileWieldingTwoWeapons(string perk)
    {
        var scenario = Create("DoubleShot1", "DoubleShot:1", 2, 1, 1, true);
        scenario.Teams[0].Members[0].Perks.Add(perk);
        var actor = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, 47).Combatants[0];
        actor.Spec.WeaponLoadout.HasOffhand.Should().BeTrue();
        var paired = (WeaponTraitState.Configuration(actor), WeaponTraitState.DualWieldStat(actor, StatType.DualWieldAttackDelayReductionPercent));
        actor.Spec.WeaponLoadout.OffhandDamage = actor.Spec.WeaponLoadout.OffhandDelay = 0;
        var single = (WeaponTraitState.Configuration(actor), WeaponTraitState.DualWieldStat(actor, StatType.DualWieldAttackDelayReductionPercent));
        (paired.Item1.OrdinaryDamagePercentAdjustment + paired.Item1.OrdinaryHitStaminaRestore + paired.Item2).Should().BePositive();
        single.Item1.OrdinaryDamagePercentAdjustment.Should().Be(0);
        single.Item1.OrdinaryHitStaminaRestore.Should().Be(0);
        single.Item1.OrdinaryHitRestoreCount.Should().Be(0);
        single.Item2.Should().Be(0);
    }

    private (Simulation Sim, Combatant Actor) Spawn(string feat = "DoubleShot1", string perk = "DoubleShot:1",
        int hits = 2, int targets = 1, int items = 1, RuleSet rules = null, bool damage = true)
    {
        var scenario = Create(feat, perk, hits, targets, items, damage);
        var sim = TestScenarios.Spawn(scenario, rules ?? RuleSets.Current(), _catalog, 47);
        var actor = sim.Combatants[0];
        actor.NextAttackAt = double.PositiveInfinity; actor.NextActionAt = 0;
        return (sim, actor);
    }

    private ScenarioSpec Create(string feat, string perk, int hits, int targets, int items, bool damage)
    {
        var scenario = TwinPistolScenarios.Create(false, true, duration: 8, targets: targets, catalog: _catalog);
        var spec = scenario.Teams[0].Members[0];
        spec.Perks = new() { "Cadence", "ChangingAngles", "TwinPistolFollowThrough", "CoolUnderFire", "BalancedGrip", perk };
        spec.StmRegenPer30Seconds = 0;
        spec.MaxFp = 100;
        spec.WeaponLoadout.Accuracy = 10000;
        // Native authored payloads: DoubleShotAbilityDefinition flat6/100% (rankI),
        // FortressStrikeAbilityDefinition flat30/100% (rankIII). Hits/targets can be
        // deliberately enlarged by bounded proc stress tests; that is not an authored DPS claim.
        var weaponPower = feat is "DoubleShot1" or "FortressStrike3";
        spec.Abilities = new() { new() { Feat = feat, Target = damage ? AbilityTarget.Enemy : AbilityTarget.Self,
            MinIntervalSeconds = 600, Damage = damage ? new() { Min = weaponPower ? 0 : 1, Max = weaponPower ? 0 : 1,
                WeaponPower = weaponPower ? new() { FlatDamage = feat == "FortressStrike3" ? 30 : 6 } : null,
                Hits = hits, Targets = targets, WeaponItems = items } : null } };
        return scenario;
    }

    private static void GrantCadence(Simulation sim, Combatant actor)
    {
        sim.GrantCadence(actor, 3);
        actor.WeaponTraits.Paired.Cadence.Should().Be(3);
    }

    private static void Finish(Simulation sim, Combatant actor, bool weapon = true)
    {
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 1, 8, "ability paid");
        if (actor.QueuedWeaponAbility != null) actor.NextAttackAt = sim.Now;
        SimulationClock.RunUntil(sim, () => !weapon || actor.Stats.AbilityImpacts > 0, 8, "ability impacts");
        actor.NextActionAt = double.PositiveInfinity; actor.NextAttackAt = double.PositiveInfinity;
    }
}
