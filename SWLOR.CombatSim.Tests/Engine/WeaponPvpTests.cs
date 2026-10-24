using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Tests.Engine;

[TestFixture]
public class WeaponPvpTests
{
    private AbilityCatalog _catalog;

    [OneTimeSetUp]
    public void Load() => _catalog = AbilityCatalog.Load();

    private static CombatantSpec Fighter(string name, string stance = "None", params string[] perks) => new()
    {
        Name = name, MaxHp = 1000, MaxStm = 100, MaxFp = 100, ReactionSeconds = 0.1f,
        Perks = perks.ToList(),
        WeaponCombat = new() { Stance = stance, Attack = 100, Accuracy = 200, Defense = 100, Evasion = 100, EnableCriticals = false }
    };

    private Simulation Spawn(CombatantSpec first, CombatantSpec second, bool pvp = true) =>
        TestScenarios.Spawn(TestScenarios.TwoTeams(pvp, new[]{first}, new[]{second}), RuleSets.Current(), _catalog);

    private TechniqueActivation Strike(Combatant source, Combatant target, FeatType feat, double at)
    {
        var info = _catalog.Get(feat);
        var use = new AbilityUseSpec { Feat = feat.ToString() };
        var activation = source.Weapon.Begin(info, use, target, info.STM, at);
        source.Weapon.Complete(info, use, activation, target, true, at);
        return activation;
    }

    [Test]
    public void NativeWeaponSwings_StartPvpAndDoNotTriggerPaybackUntilRealHpIsLost()
    {
        var attacker = Fighter("Attacker");
        attacker.AutoAttack = new() { AttacksPerSecond = 1, Damage = new() { Min = 1, Max = 1, HitChance = 1 } };
        var simulation = Spawn(attacker, Fighter("Target", "None", "Payback"));
        var target = simulation.Get("Target");
        target.TemporaryHp.Add(new() { Key = "shield", Amount = 100, ExpiresAt = 60 });
        simulation.Get("Attacker").NextAttackAt = 0;
        SimulationClock.RunUntil(simulation, () => target.Stats.DamageAbsorbedByTemporaryHp > 0, 10, "a weapon hit on the shield");
        target.Hp.Should().Be(1000);
        target.Weapon.Resources.GetDiscount(WeaponCombatState.At(simulation.Now)).Should().Be(0);
        target.PvpCombatStartedAt.Should().BeGreaterThanOrEqualTo(0);
        SimulationClock.RunUntil(simulation, () => target.Hp < 1000, 30, "a weapon hit on real HP");
        target.Weapon.Resources.GetDiscount(WeaponCombatState.At(simulation.Now)).Should().Be(2);
    }

    [TestCase(false, 1)]
    [TestCase(true, 2)]
    public void PairedHands_RollEachHandPerPairedCycle(bool paired, int rollsPerCycle)
    {
        var attacker = Fighter("Attacker");
        attacker.AutoAttack = new() { AttacksPerSecond = 1, Damage = new() { Min = 1, Max = 1, HitChance = 1 } };
        attacker.WeaponCombat.PairedHands = paired;
        attacker.WeaponCombat.OffhandDelayReductionPercent = paired ? 30 : 0;
        var simulation = Spawn(attacker, Fighter("Target"), false);
        var weapon = simulation.Get("Attacker").Weapon;
        var delay = SWLOR.Game.Server.Service.Combat.CalculateAttackDelayMilliseconds(220, paired ? 220 : 0, 0, paired ? 30 : 0);
        weapon.AttackSeconds(0).Should().Be(SWLOR.Game.Server.Service.Combat.CalculateEffectiveAttackDelay(delay) / 1000d,
            "the sim uses the server's paired delay for Power Gloves");
        simulation.Get("Attacker").NextAttackAt = 0;
        SimulationClock.RunUntil(simulation, () => weapon.Diagnostics.GetValueOrDefault("SwingAnimations") >= 3, 30, "three attack cycles");
        var cycles = weapon.Diagnostics["SwingAnimations"] + weapon.Diagnostics.GetValueOrDefault("BatchedExtraAttacks");
        weapon.Diagnostics["OrdinaryAttempts"].Should().Be(cycles * rollsPerCycle, "each cycle rolls every hand once");
        attacker.WeaponCombat.AbilityWeaponDamage.Should().Be(attacker.WeaponCombat.WeaponDamage * rollsPerCycle,
            "Unarmed abilities count both Power Gloves' DMG");
    }

    [Test]
    public void PairedHands_UnarmedAbilitiesAddBothGlovesDamage()
    {
        double JabDamage(bool paired, int weaponDamage)
        {
            var attacker = Fighter("Attacker");
            attacker.WeaponCombat.PairedHands = paired;
            attacker.WeaponCombat.WeaponDamage = weaponDamage;
            attacker.Abilities = new() { new() { Feat = "Jab1", Target = AbilityTarget.Enemy } };
            var simulation = Spawn(attacker, Fighter("Target"), false);
            var target = simulation.Get("Target");
            SimulationClock.RunUntil(simulation, () => target.Hp < 1000, 30, "a Jab hit");
            return 1000 - target.Hp;
        }
        var paired = JabDamage(true, 19);
        paired.Should().Be(JabDamage(false, 38), "a paired ability impact adds both gloves' DMG");
        paired.Should().BeGreaterThan(JabDamage(false, 19));
    }

    [Test]
    public void TwinPistolLoadoutHits_ArmTheDefendersPaybackLikeOtherDirectDamage()
    {
        var scenario = TwinPistolScenarios.Create(false, false, 60, pvp: true, catalog: _catalog);
        var shooter = scenario.Teams[0].Members[0];
        shooter.Abilities = new();
        shooter.WeaponCombat = new() { Attack = 100, Accuracy = 200, Defense = 100, Evasion = 100, EnableCriticals = false };
        scenario.Teams[1].Members[0] = Fighter("Target", "None", "Payback");
        var simulation = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog);
        var target = simulation.Get("Target");
        SimulationClock.RunUntil(simulation, () => target.Hp < target.Spec.MaxHp, 30, "a Twin Pistol hit on real HP");
        target.Weapon.Resources.GetDiscount(WeaponCombatState.At(simulation.Now)).Should().Be(2,
            "native loadout hits are direct hostile damage, so they arm Payback");
    }

    [Test]
    public void FormulaWeaponDamage_UsesCurrentVictimPvpRamp()
    {
        double Hit(bool pvp)
        {
            var attacker = Fighter("Attacker");
            attacker.AutoAttack = new() { AttacksPerSecond = 1, Damage = new() { Min = 1, Max = 1, HitChance = 1 } };
            var simulation = Spawn(attacker, Fighter("Target"), pvp);
            var target = simulation.Get("Target");
            target.PvpCombatStartedAt = 0;
            target.LastPvpActivityAt = 300;
            simulation.Get("Attacker").NextAttackAt = 300;
            SimulationClock.Set(simulation, 300);
            SimulationClock.RunUntil(simulation, () => target.Hp < 1000, 310, "a formula weapon hit");
            return 1000-target.Hp;
        }
        Hit(true).Should().Be(Hit(false)*2);
    }

    [TestCase(179.99, 1, 1)]
    [TestCase(180, 1, 2)]
    [TestCase(180, 6, 8)]
    [TestCase(210, 6, 9)]
    [TestCase(240, 6, 10)]
    [TestCase(270, 6, 11)]
    [TestCase(300, 6, 12)]
    public void PvpDamageRamp_RoundsTheIncreaseUpAtEachBoundary(double time, int amount, int expected)
    {
        var simulation = Spawn(Fighter("Attacker"), Fighter("Target"));
        var target = simulation.Get("Target");
        target.PvpCombatStartedAt = 0;
        target.LastPvpActivityAt = time;
        SimulationClock.Set(simulation, time);
        simulation.ApplyDamage(simulation.Get("Attacker"), target, amount).Should().Be(expected);
    }

    [TestCase(1, 2)]
    [TestCase(6, 7)]
    [TestCase(6.2, 7)]
    public void FractionalCandidateRamp_PreservesProfilesAndRoundsIntegerDamageIncreaseUp(double damage, double expected)
    {
        var rules = RuleSets.Current();
        rules.PvpDamageBackstop = new() { new RampStep(0,10.5f) };
        var simulation = TestScenarios.Spawn(TestScenarios.TwoTeams(true,new[]{Fighter("Attacker")},new[]{Fighter("Target")}),rules,_catalog);
        simulation.ApplyDamage(simulation.Get("Attacker"),simulation.Get("Target"),damage).Should().Be(expected);
    }

    [Test]
    public void CurrentPvpHealingReduction_RemainsActiveWithWeaponState()
    {
        var simulation = Spawn(Fighter("Healer"), Fighter("Target"));
        var target = simulation.Get("Target");
        target.Hp = 100;
        target.PvpCombatStartedAt = 0;
        target.LastPvpActivityAt = 300;
        SimulationClock.Set(simulation,300);
        simulation.ApplyHealing(target, target, 100, HealKind.InstantAbility);
        target.Hp.Should().BeApproximately(140, 0.00001);
    }

    [Test]
    public void ForceHealingPackage_UsesFinalCalibratedAffinityWithoutCountingPerksTwice()
    {
        var perks = new[]{"Benevolence:3","Renewal:3","ForceSanctuary"};
        _catalog.Perks.GetStats(perks,new[]{StatType.ForceAffinity})[StatType.ForceAffinity].Should().Be(3);
        double Heal(bool listPerks)
        {
            var spec = Fighter("Healer","None",listPerks ? perks : Array.Empty<string>());
            spec.ForceAffinity = 3;
            spec.Abilities.Add(new() { Feat="Benevolence3", Target=AbilityTarget.Self, BelowHpPercent=90 });
            var simulation = Spawn(spec,Fighter("Target"));
            var healer = simulation.Get("Healer");
            healer.Hp = 100;
            SimulationClock.RunUntil(simulation,()=>healer.Stats.HealingDone>0,10,"a calibrated Force heal");
            return healer.Stats.HealingDone;
        }
        Heal(true).Should().Be(Heal(false));
        // (63 HP + 3.5% of 1,000) = 98, x0.625 at 10 Willpower -> 62, x1.15 at Force affinity 3 -> 71.
        Heal(true).Should().Be(71);
    }

    [Test]
    public void Death_ClearsSetupDiscountAndEnemyBoundFlowWindow()
    {
        var simulation = Spawn(Fighter("Owner", "Flow", "Payback"), Fighter("Target"));
        var owner=simulation.Get("Owner"); var target=simulation.Get("Target");
        Strike(owner,target,FeatType.Jab4,0);
        Strike(owner,target,FeatType.FinishingCombination2,1);
        owner.Weapon.EvasionPercent(1).Should().Be(20);
        simulation.ApplyDamage(owner,target,10000,directHostile:true);
        owner.Weapon.Refresh(1);
        owner.Weapon.EvasionPercent(1).Should().Be(0);
        target.IsDead = false;
        target.Hp = 100;
        owner.Weapon.DirectHpDamage(target,1);
        simulation.ApplyDamage(target,owner,10000,directHostile:true);
        owner.Weapon.Sequence.Stage.Should().Be(MeleeTechniqueStage.None);
        owner.Weapon.Resources.GetDiscount(WeaponCombatState.At(1)).Should().Be(0);
    }

    [Test]
    public void Retargeting_DiscardsOnlyTheOwnersSetupAndCountsWaste()
    {
        var simulation=TestScenarios.Spawn(TestScenarios.TwoTeams(true,new[]{Fighter("Owner")},new[]{Fighter("First"),Fighter("Second")}),RuleSets.Current(),_catalog);
        var owner=simulation.Get("Owner"); var first=simulation.Get("First");
        Strike(owner,first,FeatType.Jab4,0);
        owner.Weapon.Target(simulation.Get("Second"),1);
        owner.Weapon.Stage(first,1).Should().Be(MeleeTechniqueStage.None);
        owner.Weapon.Diagnostics["SetupLostToRetarget"].Should().Be(1);
        owner.Weapon.Diagnostics["SetupWasted"].Should().Be(1);
    }

    [Test]
    public void StanceReplacement_EndsFlowWindowAndKeepsConduitCostPayments()
    {
        var source=Fighter("Owner","Flow","ConduitTraining");
        source.Abilities = new()
        {
            new() { Feat="ConduitStance1", Target=AbilityTarget.Self, KeepUp=true },
            new() { Feat="ForceLightning3", Damage=new(){Min=1,Max=1,HitChance=1} }
        };
        var simulation=Spawn(source,Fighter("Target"));
        var owner=simulation.Get("Owner"); var target=simulation.Get("Target");
        Strike(owner,target,FeatType.Jab4,0);
        Strike(owner,target,FeatType.FinishingCombination2,1);
        owner.Weapon.EvasionPercent(1).Should().Be(20);
        owner.Stm=20;
        SimulationClock.Set(simulation,1);
        SimulationClock.RunUntil(simulation,()=>owner.Stats.AbilitiesUsed >= 2,20,"stance then Force cast");
        owner.Statuses.Values.Count(s=>s.IsStance).Should().Be(1);
        owner.Weapon.Refresh(simulation.Now);
        owner.Weapon.EvasionPercent(simulation.Now).Should().Be(0);
        owner.Stats.StmRefunded.Should().Be(6);
    }
}
