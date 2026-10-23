using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Tests.Engine;
[TestFixture]
public class WeaponTechniqueTests
{
    private AbilityCatalog _catalog;
    [OneTimeSetUp] public void Load() => _catalog = AbilityCatalog.Load();
    private Combatant Fighter(string name, uint id, string stance = "None", params string[] perks)
    {
        var spec = new CombatantSpec { Name = name, MaxHp = 100000, MaxStm = 100, WeaponCombat = new() { Stance = stance }, Perks = perks.ToList() };
        var c = new Combatant(name, (int)id - 1, spec) { SimId = id };
        c.Weapon = new(c, _catalog.Perks.GetSources(perks));
        return c;
    }
    private TechniqueActivation Use(Combatant c, Combatant enemy, FeatType feat, double at, bool landed = true, int? paid = null, bool control = false)
    {
        var info = _catalog.Get(feat);
        var use = new AbilityUseSpec { Feat = feat.ToString(), IsControl = control };
        var activation = c.Weapon.Begin(info, use, enemy, paid ?? info.STM, at);
        c.Weapon.ApplyStatusRiders(control && landed ? StatusEffectCategory.Control : StatusEffectCategory.None, at);
        c.Weapon.Complete(info, use, activation, enemy, landed, at);
        return activation;
    }
    [Test] public void Catalog_ReadsActualTechniquePowerAndSharedRecasts()
    {
        var finish = _catalog.Get(FeatType.FinishingCombination2);
        finish.SourceWeaponPower.Should().Be(14);
        finish.Technique.ImpactCount.Should().Be(2);
        finish.Technique.WeaponDamagePercent.Should().Be(50);
        finish.RecastGroup.Should().Be(_catalog.Get(FeatType.LowKick2).RecastGroup);
        _catalog.Get(FeatType.Jab4).RecastGroup.Should().Be(_catalog.Get(FeatType.DirtyJab4).RecastGroup);
    }
    [Test] public void Opening_UnlocksWeaveAndAccuracyForOtherSkills()
    {
        var c = Fighter("c",1,"None","Jab:4","FlowingForm","MeasuredTechnique"); var enemy = Fighter("e",2);
        Use(c,enemy,FeatType.Jab4,0);
        c.Weapon.Sequence.WeavePower.Should().Be(16);
        var foreign = Use(c,enemy,FeatType.HookingStrike4,1);
        foreign.Accuracy.Should().Be(10);
        c.Weapon.Stage(enemy,1).Should().Be(MeleeTechniqueStage.Opening);
        c.Weapon.Sequence.TakeWeave(enemy.SimId,WeaponCombatState.At(1)).Should().Be(16);
        c.Weapon.Sequence.TakeWeave(enemy.SimId,WeaponCombatState.At(2)).Should().Be(0);
        c.Weapon.SetupAccuracy(enemy,1).Should().Be(10);
    }
    [Test] public void MissedSpender_ConsumesSetupWithoutRefundOrFlow()
    {
        var c = Fighter("c",1,"Flow","Jab:4","ControlledBreathing"); var enemy = Fighter("e",2);
        Use(c,enemy,FeatType.Jab4,0);
        var miss = Use(c,enemy,FeatType.FinishingCombination2,1,false);
        miss.Stage.Should().Be(MeleeTechniqueStage.Opening);
        c.Weapon.Stage(enemy,1).Should().Be(MeleeTechniqueStage.None);
        c.Stats.StmRefunded.Should().Be(0);
        c.Weapon.EvasionPercent(1).Should().Be(0);
        c.Weapon.Resources.CanActivate(WeaponCombatState.At(1.9)).Should().BeFalse();
        c.Weapon.Resources.CanActivate(WeaponCombatState.At(2)).Should().BeTrue();
    }
    [Test] public void MixedLines_DoNotAddOtherLinesConditionalPower()
    {
        var c = Fighter("c",1,"None","PracticedForm:3","HeavyBlows:3"); var enemy = Fighter("e",2);
        Use(c,enemy,FeatType.Jab4,0);
        var palm = Use(c,enemy,FeatType.PalmStrike3,1);
        palm.ConditionalPower.Should().Be(12);
        var finish = Use(c,enemy,FeatType.FinishingCombination2,2);
        finish.ConditionalPower.Should().Be(28);
    }
    [Test] public void ShortAndFullCombos_OpenDifferentWindowsWithSharedCooldown()
    {
        var c = Fighter("c",1,"Flow"); var enemy = Fighter("e",2);
        Use(c,enemy,FeatType.Jab4,0); Use(c,enemy,FeatType.FinishingCombination2,1);
        c.Weapon.EvasionPercent(2).Should().Be(20);
        c.Weapon.HastePercent(2).Should().Be(0);
        Use(c,enemy,FeatType.Jab4,2); Use(c,enemy,FeatType.PalmStrike3,3); Use(c,enemy,FeatType.FinishingCombination2,4);
        c.Weapon.HastePercent(4).Should().Be(0);
        c.Weapon.EvasionPercent(8.9).Should().Be(20);
        c.Weapon.EvasionPercent(9).Should().Be(0);
        Use(c,enemy,FeatType.Jab4,13); Use(c,enemy,FeatType.PalmStrike3,14); Use(c,enemy,FeatType.FinishingCombination2,15);
        c.Weapon.EvasionPercent(15).Should().Be(0);
        c.Weapon.HastePercent(15).Should().Be(15);
        c.Weapon.AttackSeconds(15).Should().Be(1.367);
        c.Weapon.HastePercent(22.9).Should().Be(15);
        c.Weapon.HastePercent(23).Should().Be(0);
    }
    [Test] public void PerfectForm_RequiresFinishAndKeepsTheOneItsCastStartedWith()
    {
        var c = Fighter("c",1,"None"); var enemy = Fighter("e",2);
        var perfect = _catalog.Get(FeatType.PerfectForm1);
        var use = new AbilityUseSpec { Feat = nameof(FeatType.PerfectForm1) };
        c.Weapon.CanUse(perfect, use, enemy, 0).Should().BeFalse("Perfect Form cannot start without a setup");
        Use(c,enemy,FeatType.Jab4,0);
        c.Weapon.CanUse(perfect, use, enemy, 1).Should().BeFalse("Opening does not satisfy the Finish requirement");
        Use(c,enemy,FeatType.PalmStrike3,1);
        c.Weapon.CanUse(perfect, use, enemy, 4.5).Should().BeTrue("the four-second Finish is still live");

        var interrupted = new PendingCast { Ability = perfect, Use = use, Target = enemy };
        c.Casting = interrupted;
        c.Weapon.HoldSetupForCast(interrupted, 4.5);
        c.Casting = null;
        c.Weapon.Stage(enemy, 5.5).Should().Be(MeleeTechniqueStage.None);
        c.Weapon.Sequence.Stage.Should().Be(MeleeTechniqueStage.None, "an interrupted cast holds nothing past the normal expiry");

        Use(c,enemy,FeatType.Jab4,6); Use(c,enemy,FeatType.PalmStrike3,7);
        var cast = new PendingCast { Ability = perfect, Use = use, Target = enemy };
        c.Casting = cast;
        c.Weapon.HoldSetupForCast(cast, 10.5);
        c.Weapon.Stage(enemy, 11.5).Should().Be(MeleeTechniqueStage.None, "the live Finish runs out during the cast");
        c.Casting = null;
        var activation = c.Weapon.Begin(perfect, use, enemy, perfect.STM, 12.5, cast);
        activation.Stage.Should().Be(MeleeTechniqueStage.Finish, "the completed cast spends the Finish it started with");
        activation.ConditionalPower.Should().Be(45);
        c.Weapon.Stage(enemy, 12.5).Should().Be(MeleeTechniqueStage.None);
    }
    [Test] public void PaybackPaidCost_DisablesHighGuardAtSevenButNotEight()
    {
        var c = Fighter("c",1,"None","HighGuard:3","Payback"); var enemy = Fighter("e",2);
        c.Weapon.DirectHpDamage(enemy,0);
        c.Weapon.Resources.TakeDiscount(9,WeaponCombatState.At(1)).Should().Be(2);
        Use(c,enemy,FeatType.Jab4,0);
        var finish = Use(c,enemy,FeatType.FinishingCombination2,1,paid:7);
        finish.FlatPower.Should().Be(0);
        c.Weapon.HighGuardUntil.Should().Be(30); // Jab4 itself costs 12.
        var palm = Use(c,enemy,FeatType.PalmStrike3,2,paid:8);
        palm.FlatPower.Should().Be(14);
        c.Weapon.HighGuardUntil.Should().Be(32);
    }
    [Test] public void PartialFinisher_RefundsOnceAndSharedGateAppliesAcrossLines()
    {
        var c = Fighter("c",1,"None","ControlledBreathing","CatchYourBreath"); var enemy = Fighter("e",2);
        c.Stm = 50;
        Use(c,enemy,FeatType.Jab4,0); Use(c,enemy,FeatType.FinishingCombination2,1);
        c.Stm.Should().Be(52);
        Use(c,enemy,FeatType.DirtyJab4,2); Use(c,enemy,FeatType.Uppercut3,3);
        c.Stm.Should().Be(52);
        Use(c,enemy,FeatType.DirtyJab4,14); Use(c,enemy,FeatType.Uppercut3,15,paid:1);
        c.Stm.Should().Be(53);
    }
    [Test] public void Bruiser_IndependentRetaliationAndHeavyPowerPreserveRattledAndRefundWithoutSetup()
    {
        var c = Fighter("c",1,"Bruiser","HeavyBlows:3","CatchYourBreath"); var enemy = Fighter("e",2);
        c.Stm = 50;
        c.Weapon.DirectHpDamage(enemy,0);
        c.Weapon.Reactions.GetRetaliation(enemy.SimId,WeaponCombatState.At(0)).Should().Be(12);
        var standalone = Use(c,enemy,FeatType.Uppercut3,1);
        standalone.Stage.Should().Be(MeleeTechniqueStage.None);
        standalone.ConditionalPower.Should().Be(24);
        c.Stm.Should().Be(52);
        c.Weapon.Reactions.GetRetaliation(enemy.SimId,WeaponCombatState.At(1)).Should().Be(0);
        Use(c,enemy,FeatType.DirtyJab4,2);
        var pressured = Use(c,enemy,FeatType.Haymaker1,3);
        pressured.Stage.Should().Be(MeleeTechniqueStage.None);
        pressured.ConditionalPower.Should().Be(12);
        c.Weapon.Stage(enemy,3).Should().Be(MeleeTechniqueStage.Distraction);
        enemy.Weapon.Rattled.Should().ContainKey(c.SimId);
        c.Stm.Should().Be(52);
    }
    [Test] public void IndependentRetaliation_ExpiresRetargetsAndConsumesOnMissWithoutRefund()
    {
        var c = Fighter("c",1,"Bruiser","CatchYourBreath"); var enemy = Fighter("e",2); var other = Fighter("o",3);
        c.Weapon.DirectHpDamage(enemy,0);
        c.Weapon.Reactions.GetRetaliation(enemy.SimId,WeaponCombatState.At(8)).Should().Be(0);
        c.Weapon.DirectHpDamage(enemy,18);
        c.Weapon.Target(other,19);
        c.Weapon.Reactions.GetRetaliation(enemy.SimId,WeaponCombatState.At(19)).Should().Be(0);
        c.Weapon.DirectHpDamage(enemy,36);
        Use(c,enemy,FeatType.Uppercut3,37,false).ConditionalPower.Should().Be(12);
        c.Stats.StmRefunded.Should().Be(0);
        c.Weapon.Reactions.GetRetaliation(enemy.SimId,WeaponCombatState.At(37)).Should().Be(0);
    }
    [Test] public void FreeHeavy_ConsumesRetaliationWithoutCreatingAStaminaRefund()
    {
        var c=Fighter("c",1,"Bruiser","CatchYourBreath"); var enemy=Fighter("e",2);
        c.Weapon.DirectHpDamage(enemy,0);
        Use(c,enemy,FeatType.Uppercut3,1,paid:0).ConditionalPower.Should().Be(12);
        c.Weapon.Reactions.GetRetaliation(enemy.SimId,WeaponCombatState.At(1)).Should().Be(0);
        c.Stats.StmRefunded.Should().Be(0);
    }
    [Test] public void HardKnocks_ShortensBothPressureDebuffsAndImmunityRejectsThem()
    {
        var c=Fighter("c",1,"None","DirtyFighting"); var enemy=Fighter("e",2,"None","HardKnocks");
        Use(c,enemy,FeatType.DirtyJab4,0);
        enemy.Weapon.Rattled[c.SimId].Until.Should().Be(7);
        Use(c,enemy,FeatType.LowKick2,1);
        enemy.Weapon.LowKick[c.SimId].Until.Should().Be(8);
        enemy.Spec.WeaponCombat.TraumaResistance=100;
        Use(c,enemy,FeatType.DirtyJab4,20);
        Use(c,enemy,FeatType.LowKick2,21);
        enemy.Weapon.Refresh(21);
        enemy.Weapon.Rattled.Should().BeEmpty();
        enemy.Weapon.LowKick.Should().BeEmpty();
        c.Weapon.Stage(enemy,21).Should().Be(MeleeTechniqueStage.None);
    }
    [Test] public void FriendlyAction_DoesNotClearSetup()
    {
        var c = Fighter("c",1); var enemy = Fighter("e",2);
        Use(c,enemy,FeatType.Jab4,0);
        Use(c,c,FeatType.EmergencyTriage1,1);
        c.Weapon.Stage(enemy,1).Should().Be(MeleeTechniqueStage.Opening);
    }
    [Test] public void SelfCenteredKatarAbility_DamagesEnemyNotCaster()
    {
        var c = Fighter("c",1); var enemy = Fighter("e",2);
        c.Spec.ReactionSeconds = 0.1f;
        c.Spec.Abilities.Add(new() { Feat="InterruptingSweep2", Target=AbilityTarget.Self, WeaponPower=30 });
        var sim = TestScenarios.Spawn(TestScenarios.TwoTeams(false,new[]{c.Spec},new[]{enemy.Spec},60),RuleSets.Current(),_catalog);
        SimulationClock.RunUntil(sim,5);
        sim.Get("c").Hp.Should().Be(c.Spec.MaxHp);
        sim.Get("e").Hp.Should().BeLessThan(enemy.Spec.MaxHp);
    }
    [Test] public void PureCombatMath_UsesRatingAccuracyAndNativeDelayRounding()
    {
        Combat.CalculateHitRate(120,100,0).Should().Be(85);
        Combat.CalculateHitRate(125,100,0).Should().Be(87);
        Combat.CalculateHitRate(120,WeaponCombatState.Adjust(100,20),0).Should().Be(75);
        Combat.CalculateEffectiveAttackDelay(Combat.CalculateAttackDelayMilliseconds(220,0,15,0)).Should().Be(1367);
        Combat.CalculateDamageRange(100,100,20,100,20,0).Should().Be((70,100));
        Combat.CalculateDamageRange(100,100,20,100,20,2).Should().Be((100,125));
    }
    [Test] public void NativeSwingFloor_CarriesFractionalAttacksBetweenAnimations()
    {
        var c = Fighter("c",1,"Berserker");
        c.Weapon.AttackSeconds(0).Should().Be(1.367);
        c.Weapon.SwingSeconds(0).Should().Be(1.750);
        var attacks = Enumerable.Range(0,100).Sum(i => c.Weapon.TakeSwingAttacks(i*1.750));
        attacks.Should().Be((int)(100*1750f/1367));
        c.Weapon.Diagnostics["BatchedExtraAttacks"].Should().Be(attacks-100);
    }
    [Test] public void ControlGuard_DurationDoesNotAddRedirectingGuardDuration()
    {
        var c = Fighter("c",1,"None","GuardTraining","RedirectingGuard","ScrapperControl:3"); var enemy=Fighter("e",2);
        var activation=Use(c,enemy,FeatType.HookingStrike4,0,control:true);
        activation.FlatPower.Should().Be(12);
        activation.HitPercent.Should().Be(7);
        c.Weapon.ControlDefenseUntil.Should().Be(30);
        c.Weapon.DefensePercent(29).Should().Be(10);
        c.Weapon.DefensePercent(30).Should().Be(0);
    }
    [Test] public void RejectedControl_DoesNotRefreshDefenseButRetainsAuthoredDamageAndAccuracyBonuses()
    {
        var c = Fighter("c", 1, "None", "GuardTraining", "RedirectingGuard", "ScrapperControl:3");
        var enemy = Fighter("e", 2);
        Use(c, enemy, FeatType.HookingStrike4, 0, control: true);
        var info = _catalog.Get(FeatType.JointLock3);
        var use = new AbilityUseSpec { Feat = "JointLock3", IsControl = true };

        var activation = c.Weapon.Begin(info, use, enemy, info.STM, 20);
        c.Weapon.ApplyStatusRiders(StatusEffectCategory.None, 20);
        c.Weapon.Complete(info, use, activation, enemy, true, 20);

        activation.FlatPower.Should().Be(12);
        activation.HitPercent.Should().Be(7);
        c.Weapon.ControlDefenseUntil.Should().Be(30);
        c.Weapon.DefensePercent(30).Should().Be(0);
    }
    [Test] public void IndependentBrawler_RequiresNoBraceWaitOrDirtyJab()
    {
        var c=Fighter("c",1,"Bruiser"); var enemy=Fighter("e",2);
        var info=_catalog.Get(FeatType.Uppercut3);
        var use=new AbilityUseSpec {Feat="Uppercut3"};
        c.Weapon.CanUse(info,use,enemy,0).Should().BeTrue();
    }
    [Test] public void Payback_ExcludesTemporaryHpAndNonDirectDamage()
    {
        var c=Fighter("c",1,"None","Payback"); var enemy=Fighter("e",2);
        var sim=TestScenarios.Spawn(TestScenarios.TwoTeams(false,new[]{c.Spec},new[]{enemy.Spec}),RuleSets.Current(),_catalog);
        var owner=sim.Get("c"); var source=sim.Get("e");
        sim.ApplyDamage(source,owner,1,directHostile:false);
        owner.Weapon.Resources.GetDiscount(WeaponCombatState.At(0)).Should().Be(0);
        owner.TemporaryHp.Add(new TemporaryHpPool {Amount=10, ExpiresAt=20});
        sim.ApplyDamage(source,owner,1,directHostile:true);
        owner.Weapon.Resources.GetDiscount(WeaponCombatState.At(0)).Should().Be(0);
        sim.ApplyDamage(source,owner,10,directHostile:true);
        owner.Weapon.Resources.GetDiscount(WeaponCombatState.At(0)).Should().Be(2);
    }

    [Test] public void RotationStaminaReserve_WaitsForThresholdWithoutChangingPayment()
    {
        var c=Fighter("c",1); var enemy=Fighter("e",2);
        var info=_catalog.Get(FeatType.Jab4);
        var use=new AbilityUseSpec {Feat="Jab4", MinimumStaminaBeforeUse=31};
        c.Stm=30;
        c.Weapon.CanUse(info,use,enemy,0).Should().BeFalse();
        c.Stm=31;
        c.Weapon.CanUse(info,use,enemy,0).Should().BeTrue();
        c.Stm.Should().Be(31);
        info.STM.Should().Be(12);
        use.MinimumStaminaBeforeUse=-1;
        c.Spec.Abilities.Add(use);
        var spawn=()=>TestScenarios.Spawn(TestScenarios.TwoTeams(false,new[]{c.Spec},new[]{enemy.Spec}),RuleSets.Current(),_catalog);
        spawn.Should().Throw<InvalidDataException>().WithMessage("*MinimumStaminaBeforeUse*");
    }

}
