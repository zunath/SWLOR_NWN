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
    public void MovementAndLandedWeaponHit_GrantFlowAndAngles_WithoutTwinActiveAbilities()
    {
        var (sim, actor) = Spawn();
        sim.RecordWeaponMovement(actor, 1);
        sim.RecordLandedWeaponHit(actor);
        actor.WeaponTraits.Paired.Momentum.Should().Be(0);
        sim.RecordWeaponMovement(actor, 2);
        sim.RecordLandedWeaponHit(actor);
        actor.WeaponTraits.Paired.Momentum.Should().Be(1);
        actor.WeaponTraits.Paired.CaptureAbility(DateTime.UnixEpoch, WeaponTraitState.Configuration(actor), true, false)
            .AccuracyAdjustment.Should().Be(5);
        sim.RecordLandedWeaponHit(actor);
        actor.WeaponTraits.Paired.Momentum.Should().Be(1, "another target or hand does not invent more movement");
    }

    [Test]
    public void MissedOrdinaryAttack_PreservesMovementOpportunityForNextLandedHit()
    {
        var (sim, actor) = Spawn(); actor.Spec.Abilities.Clear(); actor.NextActionAt = double.PositiveInfinity;
        var target = sim.Combatants[1]; target.Spec.WeaponAvoidancePercent = 100;
        sim.RecordWeaponMovement(actor, 2); actor.NextAttackAt = 0;
        SimulationClock.RunUntil(sim, () => actor.Stats.OrdinaryAttempts > 0, 4, "avoided attack");
        actor.WeaponTraits.Paired.Momentum.Should().Be(0);
        actor.WeaponTraits.Movement.DistanceTravelled.Should().Be(2);
        target.Spec.WeaponAvoidancePercent = 0;
        SimulationClock.RunUntil(sim, () => actor.Stats.OrdinaryHits > 0, 8, "landed ordinary attack");
        actor.WeaponTraits.Paired.Momentum.Should().Be(1);
    }

    [Test]
    public void QueuedForeignAbility_CommitsFlowOnceAtPayment_AndPreservesNewerMovementCharge()
    {
        var (sim, actor) = Spawn("FortressStrike3", "FortressStrike:3");
        actor.Spec.WeaponLoadout.OffhandDamage = actor.Spec.WeaponLoadout.OffhandDelay = 0;
        GrantFlow(sim, actor);
        SimulationClock.RunUntil(sim, () => actor.QueuedWeaponAbility != null, 8, "queued weapon payment");
        actor.Stats.FlowSpent.Should().Be(3);
        actor.WeaponTraits.Paired.Momentum.Should().Be(0);
        sim.RecordWeaponMovement(actor, 2);
        actor.NextActionAt = double.PositiveInfinity; actor.NextAttackAt = sim.Now;
        SimulationClock.RunUntil(sim, () => actor.QueuedWeaponAbility == null, 8, "queued weapon impact");
        actor.Stats.FlowSpent.Should().Be(3);
        actor.Stats.FlowDamagePowerApplied.Should().Be(12);
        actor.Stats.OrdinaryDamage.Should().Be(0, "the queued activation replaces its carrier");
        actor.Stats.OrdinaryHits.Should().Be(0);
        actor.WeaponTraits.Paired.Momentum.Should().Be(1);
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
    public void ForeignMultiHitAndArea_ReceiveOneFlowBudgetPerRealWeapon(int items, int hits, int targets)
    {
        var (sim, actor) = Spawn(hits: hits, targets: targets, items: items);
        GrantFlow(sim, actor);
        Finish(sim, actor);
        actor.Stats.FlowSpent.Should().Be(3);
        actor.Stats.FlowDamagePowerApplied.Should().Be(12 * items);
        actor.Stats.StmSpent.Should().Be(4);
        actor.Stats.StmRefunded.Should().Be(3);
        actor.WeaponTraits.Paired.Momentum.Should().Be(0);
        actor.Stats.AbilityImpacts.Should().Be(hits * targets);
    }

    [Test]
    public void NativeTwoWeaponProfile_CannotInventAnUnequippedSecondWeaponBudget()
    {
        var (sim, actor) = Spawn(items: 2);
        actor.Spec.WeaponLoadout.OffhandDamage = actor.Spec.WeaponLoadout.OffhandDelay = 0;
        GrantFlow(sim, actor); Finish(sim, actor);
        actor.Stats.FlowDamagePowerApplied.Should().Be(12);
    }

    [TestCase(0, 0, 3)] [TestCase(.25f, 1, 0)]
    public void ActualStaminaControlsFlow_AndRefundCannotMakePaidAbilityFree(float multiplier, int paid, int remaining)
    {
        var rules = RuleSets.Current();
        rules.CostSkillThreshold = 0;
        rules.StmCostMultiplier = multiplier;
        var (sim, actor) = Spawn(rules: rules);
        GrantFlow(sim, actor);
        Finish(sim, actor);
        actor.Stats.StmSpent.Should().Be(paid);
        actor.Stats.StmRefunded.Should().Be(0);
        actor.WeaponTraits.Paired.Momentum.Should().Be(remaining);
        actor.Stats.FlowDamagePowerApplied.Should().Be(paid > 0 ? 12 : 0);
    }

    [Test]
    public void CoolUnderFireMakesFirstLowCostCastFree_SecondCastPaysAndSpendsFlow()
    {
        var rules = RuleSets.Current(); rules.CostSkillThreshold = 0; rules.StmCostMultiplier = .5f;
        var (sim, actor) = Spawn(rules: rules);
        GrantFlow(sim, actor);
        sim.ApplyDamage(sim.Combatants[1], actor, 1);
        Finish(sim, actor);
        actor.Stats.StmSpent.Should().Be(0);
        actor.Stats.FlowSpent.Should().Be(0);
        actor.WeaponTraits.Paired.Momentum.Should().Be(3);
        actor.RecastReadyAt.Clear(); actor.LastUsedAt.Clear();
        actor.HostileAbilityReadyAt = sim.Now; actor.NextActionAt = sim.Now;
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 2, 8, "second paid action");
        if (actor.QueuedWeaponAbility != null)
        {
            actor.NextAttackAt = sim.Now;
            SimulationClock.RunUntil(sim, () => actor.QueuedWeaponAbility == null, 8, "second queued impact");
        }
        actor.Stats.StmSpent.Should().Be(2);
        actor.Stats.FlowSpent.Should().Be(3);
        actor.Stats.StmRefunded.Should().Be(1);
    }

    [Test]
    public void SupportMedKitConsumesCoolUnderFire_ButPreservesFlowAndAngles()
    {
        var (sim, actor) = Spawn("MedKit1", "MedKit:1", damage: false);
        GrantFlow(sim, actor);
        sim.ApplyDamage(sim.Combatants[1], actor, 100);
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 1, 8, "First Aid support ability");
        actor.Stats.StmSpent.Should().Be(2);
        actor.WeaponTraits.Paired.Momentum.Should().Be(3);
        actor.WeaponTraits.Paired.CaptureAbility(DateTime.UnixEpoch.AddSeconds(sim.Now), WeaponTraitState.Configuration(actor), true, false)
            .AccuracyAdjustment.Should().Be(5);
        actor.Stats.HealingDone.Should().BeGreaterThan(0);
    }

    [Test]
    public void ForceAbilityPreservesFlow_AndBalancedGripSuppliesGlobalAccuracy()
    {
        var (sim, actor) = Spawn("ForcePush1", "ForcePush:1");
        GrantFlow(sim, actor);
        Finish(sim, actor, weapon: false);
        actor.GetStat(StatType.Accuracy).Should().Be(5);
        actor.WeaponTraits.Paired.Momentum.Should().Be(3);
        actor.Stats.FlowSpent.Should().Be(0);
        actor.Stats.FlowDamagePowerApplied.Should().Be(0);
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
    public void UnknownFinalDamageWithFlow_FailsClosedInsteadOfAddingUnscaledDamage()
    {
        var scenario = Create("DoubleShot1", "DoubleShot:1", 2, 1, 1, true);
        var damage = scenario.Teams[0].Members[0].Abilities[0].Damage;
        damage.WeaponPower = null; damage.Min = damage.Max = 1;
        Action spawn = () => TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, 47);
        spawn.Should().Throw<InvalidDataException>().WithMessage("*cannot apply Flow to calibrated final damage*");
        scenario.Teams[0].Members[0].Perks.Remove("TwinPistolMomentum");
        spawn.Should().NotThrow("calibrated damage remains usable without an unknown Flow power conversion");
    }

    [TestCase(50, 94)] [TestCase(150, 94)] [TestCase(150, 250)]
    public void ForeignFlowDamage_IsNativePowerBeforeAttackDefenseAndCriticalScaling(int attack, int defense)
    {
        var rows = new List<object>();
        foreach (var critical in new[] { -100, 100 })
        foreach (var seed in Enumerable.Range(20261019, 64))
        {
            double Run(bool flow)
            {
                // One isolated Double Shot I impact: authored flat6 plus100% weapon rating.
                // +12 is the approved three-charge Flow power, not twelve final damage points.
                var scenario = Create("DoubleShot1", "DoubleShot:1", 1, 1, 1, true);
                var spec = scenario.Teams[0].Members[0];
                spec.WeaponLoadout.Attack = attack;
                spec.WeaponLoadout.AbilityCriticalRateAdjustment = critical;
                scenario.Teams[1].Members[0].Defense = defense;
                if (!flow) spec.Abilities[0].Damage.WeaponPower.FlatDamage += 12;
                var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, seed);
                var actor = sim.Combatants[0]; actor.NextActionAt = 0; actor.NextAttackAt = double.PositiveInfinity;
                if (flow) GrantFlow(sim, actor);
                Finish(sim, actor);
                actor.Stats.FlowDamagePowerApplied.Should().Be(flow && actor.Stats.AbilityHits > 0 ? 12 : 0);
                return actor.Stats.DamageDealt;
            }
            var flowDamage = Run(true);
            var explicitPowerDamage = Run(false);
            flowDamage.Should().Be(explicitPowerDamage, "Flow must be identical to the same native ability power, even across ATT/DEF and critical scaling");
            rows.Add(new { attack, defense, criticalAdjustment = critical, seed, flowDamage, explicitPowerDamage });
        }
        var directory = Environment.GetEnvironmentVariable("TWIN_SIM_OUTPUT") ?? Path.Combine(Path.GetTempPath(), "swlor-twin-cross-skill-sim-20261018");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"foreign-native-power-{attack}-{defense}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            method = "64 paired seeds x two critical-rate controls x two equivalent profiles. One isolated Double Shot I impact (source authored flat6, 100% native weapon rating) with three Flow versus explicit flat18. " +
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
        target.Perks = new() { "TwinPistolMomentum", "TwinPistolSecondWind" };
        var damage = scenario.Teams[0].Members[0].Abilities[0].Damage;
        if (nativeAccuracy) damage.Accuracy = 0; else damage.HitChance = 0;
        var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog, 47);
        var actor = sim.Combatants[0]; actor.NextActionAt = 0; actor.NextAttackAt = double.PositiveInfinity;
        Finish(sim, actor, weapon: false);
        sim.Combatants[1].WeaponTraits.Paired.Momentum.Should().Be(expected,
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
        var scenario = TwinPistolScenarios.Create(false, false, duration: 8, targets: targets, catalog: _catalog);
        var spec = scenario.Teams[0].Members[0];
        spec.Perks = new() { "TwinPistolMomentum", "ChangingAngles", "TwinPistolFollowThrough", "CoolUnderFire", "BalancedGrip", perk };
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

    private static void GrantFlow(Simulation sim, Combatant actor)
    {
        for (var i = 0; i < 3; i++) { sim.RecordWeaponMovement(actor, 2); sim.RecordLandedWeaponHit(actor); }
        actor.WeaponTraits.Paired.Momentum.Should().Be(3);
    }

    private static void Finish(Simulation sim, Combatant actor, bool weapon = true)
    {
        SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 1, 8, "ability paid");
        if (actor.QueuedWeaponAbility != null) actor.NextAttackAt = sim.Now;
        SimulationClock.RunUntil(sim, () => !weapon || actor.Stats.AbilityImpacts > 0, 8, "ability impacts");
        actor.NextActionAt = double.PositiveInfinity; actor.NextAttackAt = double.PositiveInfinity;
    }
}
