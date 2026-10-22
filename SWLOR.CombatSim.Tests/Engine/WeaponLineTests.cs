using System.IO;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Tests.Engine;

/// <summary>Vibroblade Frenzy and Staff Crusher mechanics in the weapon-formula mode, checked against production rules.</summary>
[TestFixture]
public class WeaponLineTests
{
    private AbilityCatalog _catalog;
    [OneTimeSetUp] public void Load() => _catalog = AbilityCatalog.Load();

    private static CombatantSpec Fighter(string name, string stance = "None", params string[] perks) => new()
    {
        Name = name, MaxHp = 10000, MaxStm = 100, ReactionSeconds = 0.1f, Perks = perks.ToList(),
        WeaponCombat = new() { Accuracy = 400, Evasion = 0, EnableCriticals = false, Stance = stance }
    };

    private Combatant State(string name, uint id, string stance = "None", params string[] perks)
    {
        var spec = new CombatantSpec { Name = name, MaxHp = 1000, MaxStm = 100, Perks = perks.ToList(), WeaponCombat = new() { Stance = stance } };
        var combatant = new Combatant(name, (int)id - 1, spec) { SimId = id };
        combatant.Weapon = new(combatant, _catalog.Perks.GetSources(perks));
        return combatant;
    }

    private Simulation Duel(CombatantSpec attacker, CombatantSpec victim, bool pvp = true) =>
        TestScenarios.Spawn(TestScenarios.TwoTeams(pvp, new[] { attacker }, new[] { victim }), RuleSets.Current(), _catalog);

    private static double Diagnostic(Simulation sim, string name, string key) => sim.Get(name).Weapon.Diagnostics.GetValueOrDefault(key);

    [Test]
    public void Catalog_ReadsVibrobladeAndStaffSourceImpacts()
    {
        var riot = _catalog.Get(FeatType.RiotBlade4);
        riot.IsQueuedWeapon.Should().BeTrue();
        riot.SourceWeaponPower.Should().Be(25);

        var rending = _catalog.Get(FeatType.RendingStrike2);
        rending.SourceWeaponPower.Should().Be(32);
        rending.ImpactStatusDuration.Should().Be(30);
        rending.ImpactStatusTypes.Should().Equal(typeof(ExposedStatusEffect));
        ((ExposedStatusEffect)rending.WeaponStatusFactory()).StatGroup.Stats[StatType.DefensePercentAdjustment].Should().Be(-15);
        rending.SkillType.Should().Be(SkillType.Invalid, "the production definition declares no skill");
        rending.WeaponImpactSkillType.Should().Be(SkillType.Vibroblade);
        _catalog.Get(FeatType.RendingStrike1).SourceWeaponPower.Should().Be(18);

        var cleave = _catalog.Get(FeatType.SavageCleave3);
        cleave.SourceWeaponPower.Should().Be(20);
        cleave.WeaponAreaRadius.Should().Be(5);
        cleave.RestoresSecondaryTargetStamina.Should().BeTrue();
        _catalog.Get(FeatType.SavageCleave1).RestoresSecondaryTargetStamina.Should().BeFalse();

        _catalog.Get(FeatType.Slam4).SourceWeaponPower.Should().Be(38);
        _catalog.Get(FeatType.Slam4).ExtraDamageIfTargetControlled.Should().Be(26);
        _catalog.Get(FeatType.RibBreaker3).SourceWeaponPower.Should().Be(36);
        _catalog.Get(FeatType.RibBreaker3).ImpactStatusDuration.Should().Be(15);
        _catalog.Get(FeatType.RibBreaker3).ImpactStatusTypes.Should().Equal(typeof(DazedStatusEffect));

        var quake = _catalog.Get(FeatType.GroundQuake2);
        quake.SourceWeaponPower.Should().Be(30);
        quake.MaximumAreaTargets.Should().Be(5);
        quake.ImpactStatusTypes.Should().BeEmpty();
        quake.ConditionalTargetStatus.Status.Should().Be(typeof(KnockdownStatusEffect));
        quake.ConditionalTargetStatus.DurationSeconds.Should().Be(6);
        quake.ConditionalTargetStatus.RequiredStatus.Should().Be(typeof(DazedStatusEffect));
        quake.ConditionalTargetStatus.ConvertsRequiredStatus.Should().BeTrue();

        var worldbreaker = _catalog.Get(FeatType.Worldbreaker1);
        worldbreaker.SourceWeaponPower.Should().Be(35);
        worldbreaker.ExtraDamageIfTargetControlled.Should().Be(40);
        worldbreaker.ConditionalTargetStatus.Status.Should().Be(typeof(DazedStatusEffect));
        worldbreaker.ConditionalTargetStatus.DurationSeconds.Should().Be(30);
        worldbreaker.ConditionalTargetStatus.RequiredCategory.Should().Be(SWLOR.Game.Server.Service.StatusEffectService.StatusEffectCategory.Control);
        worldbreaker.CastSeconds.Should().Be(2);
    }

    [Test]
    public void UnmodeledProfileRiders_RefuseTheAbility()
    {
        _catalog.Get(FeatType.SweepingGuard1).UnmodeledRiders.Should().NotBeEmpty();
        var attacker = Fighter("Attacker");
        attacker.Abilities.Add(new() { Feat = "SweepingGuard1", Target = AbilityTarget.Self });
        var spawn = () => Duel(attacker, Fighter("Victim"));
        spawn.Should().Throw<InvalidDataException>().WithMessage("*does not model*");
    }

    [Test]
    public void CrusherStance_ReadsProductionStanceStats()
    {
        var crusher = State("c", 1, "Crusher");
        var none = State("n", 2);
        crusher.Weapon.HastePercent(0).Should().Be(-10);
        crusher.Weapon.Stat(StatType.DamageToControlTargetPercentAdjustment).Should().Be(15);
        crusher.Weapon.AttackSeconds(0).Should().BeGreaterThan(none.Weapon.AttackSeconds(0));
    }

    [TestCase(250, 110)]
    [TestCase(251, 100)]
    public void Executioner_AddsItsPercentBelowTheProductionThreshold(int targetHp, int expected)
    {
        var attacker = State("a", 1, "None", "Executioner:2");
        var target = State("t", 2);
        target.Hp = targetHp;
        attacker.Weapon.ApplyDamageDealt(100, target, true, 0).Should().Be(expected);
        attacker.Weapon.ApplyDamageDealt(100, target, false, 0).Should().Be(expected, "ability damage passes the same stage");
    }

    [Test]
    public void HeavyHandsAndCrusherStance_AddToDamageAgainstAnyControlEffect()
    {
        var attacker = State("a", 1, "Crusher", "HeavyHands");
        var target = State("t", 2);
        attacker.Weapon.ApplyDamageDealt(100, target, false, 0).Should().Be(100);
        target.Statuses["ExposedStatusEffect"] = new ActiveStatus { Stats = new Dictionary<StatType, int> { [StatType.DefensePercentAdjustment] = -15 }, ExpiresAt = 30 };
        attacker.Weapon.ApplyDamageDealt(100, target, false, 0).Should().Be(100, "Exposed is a debuff, not a control effect");
        target.Weapon.Control.Hamstring(0, 5);
        attacker.Weapon.ApplyDamageDealt(100, target, true, 1).Should().Be(125);
        target.Weapon.Control.TryDaze(6, 8).Should().BeTrue();
        attacker.Weapon.ApplyDamageDealt(100, target, false, 11).Should().Be(125);
        attacker.Weapon.ApplyDamageDealt(100, target, false, 14.1).Should().Be(100);
    }

    [Test]
    public void CrushingMastery_RaisesOnlyCriticalDamageAgainstControlledTargets()
    {
        var attacker = State("a", 1, "None", "CrushingMastery");
        var target = State("t", 2);
        attacker.Weapon.ApplyCriticalDamage(100, Combat.StandardCriticalRating, target, 0).Should().Be(100);
        target.Weapon.Control.TryDaze(0, 8).Should().BeTrue();
        attacker.Weapon.ApplyCriticalDamage(100, Combat.StandardCriticalRating, target, 1).Should().Be(115);
        attacker.Weapon.ApplyCriticalDamage(100, 0, target, 1).Should().Be(100);
    }

    [Test]
    public void Rundown_StacksOnLandedAutoAttacksPerTargetAndIgnoresAbilities()
    {
        var attacker = State("a", 1, "None", "Rundown:3");
        var first = State("f", 2);
        var second = State("s", 3);
        var hits = Enumerable.Range(0, 6).Select(_ => attacker.Weapon.ApplyDamageDealt(100, first, true, 0)).ToList();
        hits.Should().Equal(103, 106, 109, 112, 115, 115);
        attacker.Weapon.ApplyDamageDealt(100, first, false, 0).Should().Be(100);
        attacker.Weapon.ApplyDamageDealt(100, first, true, 0).Should().Be(115, "an ability hit neither builds nor breaks the chain");
        attacker.Weapon.ApplyDamageDealt(100, second, true, 0).Should().Be(103);
        attacker.Weapon.ApplyDamageDealt(100, first, true, 0).Should().Be(103);
    }

    [Test]
    public void FollowThrough_AddsDmgToEveryThirdLandedAutoAttack()
    {
        var attacker = State("a", 1, "None", "FollowThrough");
        Enumerable.Range(0, 6).Select(_ => attacker.Weapon.TakeLandedOrdinaryPower(0)).Should().Equal(0, 0, 10, 0, 0, 10);
    }

    [TestCase(15, 60)]
    [TestCase(16, 50)]
    public void SavageReflexes_RollsItsChanceForFlatDamageOnLandedAutoAttacks(int roll, int expected)
    {
        var attacker = State("a", 1, "None", "SavageReflexes");
        attacker.Weapon.ApplyAutoAttackDamage(50, () => roll).Should().Be(expected);
        attacker.Weapon.ApplyAutoAttackDamage(0, () => throw new AssertionException("no roll without damage")).Should().Be(0);
    }

    [Test]
    public void RendingStrike_ExposesTheTargetForThirtySecondsUnlessTraumaResistanceBlocksIt()
    {
        foreach (var resistance in new[] { 0, 100 })
        {
            var attacker = Fighter("Attacker");
            attacker.Abilities.Add(new() { Feat = "RendingStrike2" });
            var victim = Fighter("Victim");
            victim.WeaponCombat.TraumaResistance = resistance;
            var sim = Duel(attacker, victim);
            SimulationClock.RunUntil(sim, () => Diagnostic(sim, "Attacker", "AbilityImpactsLanded") == 1, 10, "a landed Rending Strike");
            var target = sim.Get("Victim");
            var hitAt = sim.Now;
            target.Weapon.DefensePercent(sim.Now).Should().Be(resistance == 0 ? -15 : 0);
            if (resistance != 0) continue;
            target.Statuses["ExposedStatusEffect"].ExpiresAt.Should().BeApproximately(hitAt + 30, 0.11);
            SimulationClock.RunUntil(sim, hitAt + 30.2);
            target.Weapon.DefensePercent(sim.Now).Should().Be(0);
        }
    }

    [Test]
    public void RiotBlade_RidesTheNextLandedAttackWithoutOrdinaryRiders()
    {
        var attacker = Fighter("Attacker", "None", "FollowThrough", "Rundown:3");
        attacker.AutoAttack = new() { AttacksPerSecond = 1, Damage = new() { Min = 1, Max = 1 } };
        attacker.Abilities.Add(new() { Feat = "RiotBlade4", WeaponPower = 25 });
        var sim = Duel(attacker, Fighter("Victim"));
        var source = sim.Get("Attacker");
        SimulationClock.RunUntil(sim, () => source.QueuedWeaponAbility != null, 2, "the queued Riot Blade");
        source.Stats.AbilitiesUsed.Should().Be(1);
        SimulationClock.RunUntil(sim, () => Diagnostic(sim, "Attacker", "AbilityImpactsLanded") == 1, 20, "the carried impact");
        source.QueuedWeaponAbility.Should().BeNull();
        SimulationClock.RunUntil(sim, 15);
        var diagnostics = source.Weapon.Diagnostics;
        (diagnostics.GetValueOrDefault("QueuedCarrierAttempts") + diagnostics.GetValueOrDefault("OrdinaryAttempts"))
            .Should().Be(diagnostics.GetValueOrDefault("SwingAnimations") + diagnostics.GetValueOrDefault("BatchedExtraAttacks"));
        diagnostics.GetValueOrDefault("AttackCycleProcs").Should().Be(Math.Floor(diagnostics["OrdinaryLanded"] / 3), "the carrier does not advance Follow-Through");
        var landed = (int)diagnostics["OrdinaryLanded"];
        diagnostics["RepeatedTargetDamage"].Should().Be(Enumerable.Range(1, landed).Sum(stack => Math.Min(15, stack * 3)), "the carrier does not build Rundown");
    }

    [Test]
    public void RiotBlade_MissedCarrierKeepsTheAbilityQueued()
    {
        var attacker = Fighter("Attacker");
        attacker.WeaponCombat.Accuracy = 0;
        attacker.AutoAttack = new() { AttacksPerSecond = 1, Damage = new() { Min = 1, Max = 1 } };
        attacker.Abilities.Add(new() { Feat = "RiotBlade4" });
        var victim = Fighter("Victim");
        victim.WeaponCombat.Evasion = 1000;
        var sim = Duel(attacker, victim);
        SimulationClock.RunUntil(sim, () => Diagnostic(sim, "Attacker", "QueuedCarrierAttempts") >= 1, 10, "a carrier roll");
        sim.Get("Attacker").QueuedWeaponAbility.Should().NotBeNull("this seed's first 20% carrier roll misses");
        sim.Get("Victim").Hp.Should().Be(victim.MaxHp);
    }

    [Test]
    public void RiotBlade_FormulaQueueIsRefusedOnAWeaponLoadout()
    {
        var attacker = Fighter("Attacker");
        attacker.WeaponLoadout = new WeaponLoadoutSpec();
        attacker.Abilities.Add(new() { Feat = "RiotBlade4", Target = AbilityTarget.Enemy });
        var spawn = () => Duel(attacker, Fighter("Victim"));
        spawn.Should().Throw<InvalidDataException>().WithMessage("*WeaponLoadout cannot carry*");
    }

    [Test]
    public void SavageCleave_HitsItsSelfCenteredSphereAndRefusesUnmodeledSecondaryStaminaRestore()
    {
        var attacker = Fighter("Attacker");
        attacker.Abilities.Add(new() { Feat = "SavageCleave3", Target = AbilityTarget.Self });
        var sim = Duel(attacker, Fighter("Victim"));
        SimulationClock.RunUntil(sim, 1);
        Diagnostic(sim, "Attacker", "AreaCasts").Should().Be(1);
        sim.Get("Victim").Hp.Should().BeLessThan(10000);

        var crowd = Fighter("Attacker");
        crowd.Abilities.Add(new() { Feat = "SavageCleave3", Target = AbilityTarget.Self });
        var multi = TestScenarios.Spawn(TestScenarios.TwoTeams(true, new[] { crowd }, new[] { Fighter("One"), Fighter("Two") }), RuleSets.Current(), _catalog);
        var run = () => SimulationClock.RunUntil(multi, 5);
        run.Should().Throw<InvalidDataException>().WithMessage("*secondary-target stamina*");
    }

    [TestCase(0, true)]
    [TestCase(100, false)]
    public void RibBreaker_DazeGrantsChargedBlowsSkullRattleAndBreakPosture(int mindResistance, bool applied)
    {
        var attacker = Fighter("Attacker", "None", "ChargedBlows:3", "SkullRattle", "BreakPosture");
        attacker.Abilities.Add(new() { Feat = "RibBreaker3" });
        var victim = Fighter("Victim");
        victim.WeaponCombat.MindResistance = mindResistance;
        var sim = Duel(attacker, victim);
        var source = sim.Get("Attacker");
        var target = sim.Get("Victim");
        SimulationClock.RunUntil(sim, () => Diagnostic(sim, "Attacker", "AbilityImpactsLanded") == 1, 10, "a landed Rib Breaker");
        var hitAt = sim.Now;
        target.Weapon.Control.IsDazed(sim.Now).Should().Be(applied);
        source.Weapon.NextAttackBonus(sim.Now).Should().Be(applied ? 26 : 0);
        target.Weapon.AccuracyPercent(sim.Now).Should().Be(applied ? -10 : 0);
        target.Weapon.DefensePercent(sim.Now).Should().Be(applied ? -10 : 0, "Break Posture needs the target controlled after the hit's own status");
        if (!applied) return;
        target.Weapon.Control.DazedUntil.Should().BeApproximately(hitAt + 8, 0.11, "PvP halves the 15-second daze");
        SimulationClock.RunUntil(sim, hitAt + 30.2);
        source.Weapon.NextAttackBonus(sim.Now).Should().Be(0);
        target.Weapon.AccuracyPercent(sim.Now).Should().Be(0);
        target.Weapon.DefensePercent(sim.Now).Should().Be(0);
    }

    [Test]
    public void ChargedBlows_TheNextLandedAttackOrAbilityHitSpendsTheBonusOnce()
    {
        var attacker = State("a", 1, "None", "ChargedBlows:3");
        attacker.Weapon.ApplyStatusRiders(SWLOR.Game.Server.Service.StatusEffectService.StatusEffectCategory.Control, 0);
        attacker.Weapon.NextAttackBonus(29.9).Should().Be(26);
        attacker.Weapon.TakeLandedOrdinaryPower(1).Should().Be(26);
        attacker.Weapon.TakeLandedOrdinaryPower(2).Should().Be(0);

        var staff = Fighter("Attacker", "None", "ChargedBlows:3");
        staff.Abilities.Add(new() { Feat = "Slam4" });
        var sim = Duel(staff, Fighter("Victim"));
        var source = sim.Get("Attacker");
        source.Weapon.ApplyStatusRiders(SWLOR.Game.Server.Service.StatusEffectService.StatusEffectCategory.Control, 0);
        SimulationClock.RunUntil(sim, () => Diagnostic(sim, "Attacker", "AbilityImpactsLanded") == 1, 10, "a landed Slam");
        Diagnostic(sim, "Attacker", "NextAttackBonusSpent").Should().Be(1);
        source.Weapon.NextAttackBonus(sim.Now).Should().Be(0);
    }

    [TestCase(false, 0)]
    [TestCase(true, 1)]
    public void Slam_AddsItsControlledTargetDmgOnlyAgainstControlledTargets(bool dazed, int boosted)
    {
        var attacker = Fighter("Attacker");
        attacker.Abilities.Add(new() { Feat = "Slam4" });
        var sim = Duel(attacker, Fighter("Victim"));
        if (dazed) sim.Get("Victim").Weapon.Control.TryDaze(0, 20).Should().BeTrue();
        SimulationClock.RunUntil(sim, () => Diagnostic(sim, "Attacker", "AbilityImpactsLanded") == 1, 10, "a landed Slam");
        Diagnostic(sim, "Attacker", "ControlledTargetPower").Should().Be(boosted);
    }

    [Test]
    public void ControlState_ConversionClampsToTheRemainingDazeAndGrantsImmunityAfterKnockdown()
    {
        var state = new WeaponControlState();
        state.TryConvertDazeToKnockdown(0, 6, 1).Should().BeFalse("only a dazed target converts");
        state.TryDaze(0, 8).Should().BeTrue();
        state.TryKnockdown(1, 6).Should().BeFalse("a new hard control does not stack on a daze");
        state.TryConvertDazeToKnockdown(5.5, 6, 1).Should().BeTrue();
        state.IsDazed(5.5).Should().BeFalse();
        state.KnockdownUntil.Should().Be(7.5, "two whole ticks of the daze remained");
        state.HardControlImmunityUntil.Should().Be(27.5);
        state.IsIncapacitated(7.4).Should().BeTrue();
        state.TryDaze(7.6, 8).Should().BeFalse();
        state.TryDaze(27.5, 8).Should().BeTrue();
        state.TryConvertDazeToKnockdown(34.8, 6, 1).Should().BeFalse("less than one tick of the daze remains");
        state.IsDazed(34.8).Should().BeTrue();
    }

    [TestCase(true, false, 3)]
    [TestCase(false, false, 6)]
    [TestCase(true, true, 0)]
    public void GroundQuake_ConvertsAnExistingDazeIntoAShorterPvpKnockdown(bool pvp, bool undazed, double knockdown)
    {
        var attacker = Fighter("Attacker");
        attacker.Abilities.Add(new() { Feat = "GroundQuake2", Target = AbilityTarget.Self });
        var victim = Fighter("Victim");
        victim.IsNpc = !pvp;
        var sim = Duel(attacker, victim, pvp);
        var target = sim.Get("Victim");
        if (!undazed) target.Weapon.Control.TryDaze(0, 15).Should().BeTrue();
        SimulationClock.RunUntil(sim, () => Diagnostic(sim, "Attacker", "AbilityImpactsLanded") == 1, 10, "a landed Ground Quake");
        target.Weapon.Control.IsKnockedDown(sim.Now).Should().Be(!undazed);
        target.Weapon.Control.IsDazed(sim.Now).Should().BeFalse();
        if (undazed) return;
        (target.Weapon.Control.KnockdownUntil - sim.Now).Should().BeApproximately(knockdown, 0.11);
        target.Weapon.Control.HardControlImmunityUntil.Should().Be(target.Weapon.Control.KnockdownUntil + 20);
        target.NextAttackAt.Should().BeLessThanOrEqualTo(target.Weapon.Control.KnockdownUntil);
    }

    [TestCase("None", 0, 0, 0)]
    [TestCase("Hamstring", 1, 1, 0)]
    [TestCase("Dazed", 1, 0, 1)]
    public void Worldbreaker_DazesControlledTargetsOnlyWhenNoHardControlIsActive(string control, int boosted, int dazes, int resisted)
    {
        var attacker = Fighter("Attacker");
        attacker.Abilities.Add(new() { Feat = "Worldbreaker1", Target = AbilityTarget.Self });
        var sim = Duel(attacker, Fighter("Victim"));
        var target = sim.Get("Victim");
        if (control == "Hamstring") target.Weapon.Control.Hamstring(0, 30);
        if (control == "Dazed") target.Weapon.Control.TryDaze(0, 30).Should().BeTrue();
        SimulationClock.RunUntil(sim, () => Diagnostic(sim, "Attacker", "AbilityImpactsLanded") == 1, 10, "a landed Worldbreaker");
        Diagnostic(sim, "Attacker", "ControlledTargetPower").Should().Be(boosted);
        Diagnostic(sim, "Attacker", "DazesApplied").Should().Be(dazes);
        Diagnostic(sim, "Attacker", "DazesResisted").Should().Be(resisted);
        if (dazes > 0) (target.Weapon.Control.DazedUntil - sim.Now).Should().BeApproximately(15, 0.11, "PvP halves the 30-second daze");
    }

    [Test]
    public void Knockdown_BlocksNewActionsAndSwingsUntilExpiry()
    {
        var victim = Fighter("Victim");
        victim.AutoAttack = new() { AttacksPerSecond = 1, Damage = new() { Min = 1, Max = 1 } };
        victim.Abilities.Add(new() { Feat = "Slam4" });
        var sim = Duel(victim, Fighter("Enemy"));
        var target = sim.Get("Victim");
        target.Weapon.Control.TryKnockdown(0, 6).Should().BeTrue();
        target.Weapon.Control.TryDaze(1, 8).Should().BeFalse();
        SimulationClock.RunUntil(sim, 5.9);
        target.Stats.AbilitiesUsed.Should().Be(0);
        target.Weapon.Diagnostics.GetValueOrDefault("OrdinaryAttempts").Should().Be(0);
        target.Weapon.Diagnostics.GetValueOrDefault("KnockdownSeconds").Should().BeApproximately(5.9, 0.11);
        SimulationClock.RunUntil(sim, 8);
        target.Stats.AbilitiesUsed.Should().BeGreaterThan(0);
        target.Weapon.Diagnostics.GetValueOrDefault("OrdinaryAttempts").Should().BeGreaterThan(0);
    }

    [TestCase(true, 8)]
    [TestCase(false, 0)]
    public void BloodFrenzy_HastesRestoresStaminaOnLandedAutoAttacksAndOnNpcKills(bool npcVictim, int killRestore)
    {
        var attacker = Fighter("Attacker", "None", "BloodFrenzy");
        attacker.AutoAttack = new() { AttacksPerSecond = 1, Damage = new() { Min = 1, Max = 1 } };
        attacker.Abilities.Add(new() { Feat = "BloodFrenzyBurst", Target = AbilityTarget.Self });
        var victim = Fighter("Victim");
        victim.IsNpc = npcVictim;
        var sim = Duel(attacker, victim);
        var source = sim.Get("Attacker");
        SimulationClock.RunUntil(sim, () => source.Statuses.ContainsKey(nameof(BloodFrenzyStatusEffect)), 3, "Blood Frenzy");
        var appliedAt = sim.Now;
        source.Weapon.HastePercent(sim.Now).Should().Be(15);
        source.Statuses[nameof(BloodFrenzyStatusEffect)].ExpiresAt.Should().BeApproximately(appliedAt + 45, 0.11);
        source.Stm = 10;
        var landedBefore = source.Weapon.Diagnostics.GetValueOrDefault("OrdinaryLanded");
        SimulationClock.RunUntil(sim, appliedAt + 10);
        var landed = source.Weapon.Diagnostics.GetValueOrDefault("OrdinaryLanded") - landedBefore;
        landed.Should().BeGreaterThan(0);
        source.Weapon.Diagnostics["AutoAttackStaminaRestored"].Should().Be(landed);

        sim.Get("Victim").Hp = 1;
        SimulationClock.RunUntil(sim, sim.Now + 5);
        sim.Get("Victim").IsDead.Should().BeTrue();
        source.Weapon.Diagnostics.GetValueOrDefault("DefeatedEnemyStaminaRestored").Should().Be(killRestore);
    }
}
