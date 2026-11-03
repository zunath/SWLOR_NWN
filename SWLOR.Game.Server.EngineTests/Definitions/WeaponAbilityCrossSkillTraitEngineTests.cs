using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class WeaponAbilityCrossSkillTraitEngineTests
{
    [EngineTest("A rifle Lead Shot grants Cadence and Changing Angles that foreign Headshot spends and chains", Category = "WeaponCrossSkill", TimeoutSeconds = 60f)]
    public static Task RifleChainedHeadshot(EngineTestContext ctx) => ChainedHeadshot(ctx, shield: false);

    [EngineTest("A sword-and-shield Lead Shot fuels foreign Headshot without a Twin Pistol loadout", Category = "WeaponCrossSkill", TimeoutSeconds = 60f)]
    public static Task ShieldChainedHeadshot(EngineTestContext ctx) => ChainedHeadshot(ctx, shield: true);

    private static async Task ChainedHeadshot(EngineTestContext ctx, bool shield)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        var actor = player.Creature;
        try
        {
            player.Update(record =>
            {
                record.Perks.Clear();
                record.Perks[PerkType.Headshot] = 1;
                record.Perks[PerkType.LeadShot] = 1;
                record.Perks[PerkType.Cadence] = 1;
                record.Perks[PerkType.ChangingAngles] = 1;
                record.Perks[PerkType.TwinPistolFollowThrough] = 1;
                record.Skills[SkillType.Rifle].Rank = 50;
                record.Skills[SkillType.TwinPistol].Rank = 50;
                record.Skills[SkillType.Armor].Rank = 50;
            });
            var weapon = await ctx.EquipItemAsync(actor, shield ? "b_longsword" : "b_rifle", InventorySlot.RightHand);
            if (shield) await ctx.EquipItemAsync(actor, "ec_shield", InventorySlot.LeftHand);
            var target = await Target(ctx, actor, 5f);
            await InFrame(ctx, actor, () =>
            {
                CreaturePlugin.AddFeat(actor, FeatType.LeadShot1);
                CreaturePlugin.AddFeat(actor, FeatType.Headshot1);
                Combat.SetAbilityHitResolutionOverride(true);
                Combat.SetAutoAttackHitResolutionOverride(true);
            });
            await InFrame(ctx, actor, () => ctx.Assert(UsePerkFeat.TryUseAbility(actor, target, FeatType.LeadShot1, GetLocation(target)),
                $"Lead Shot enters the pipeline: {Ability.GetLastActivationDenialReason()}"));
            await Until(ctx, () => State(actor)?.Cadence == 1, "Lead Shot's landed hit to grant one Cadence");
            await Until(ctx, () => !Activity.IsBusy(actor) && !TwinPistolAbility.HasActiveActivation(actor), "Lead Shot resolution");
            var state = State(actor);
            ctx.AssertEqual(1, state.Cadence, "Lead Shot grants one Cadence on its own, without a chain");
            ctx.Assert(state.CadenceAccuracyExpires > DateTime.UtcNow, "Gaining Cadence arms Changing Angles");
            // A hostile press inside the shared hostile ability delay is held, not queued; wait it out
            // (half a second, well inside the six-second chain window).
            await Until(ctx, () => HostileAbilityDelay.GetRemainingSeconds(actor) <= 0f, "the shared hostile ability delay to end");
            WeaponAbilityTraitActivation traits = null;
            var before = Stat.GetCurrentStamina(actor);
            var hp = GetCurrentHitPoints(target);
            await InFrame(ctx, actor, () =>
            {
                ClearAllActions();
                ctx.Assert(UsePerkFeat.TryUseAbility(actor, target, FeatType.Headshot1, GetLocation(target)),
                    $"Foreign Rifle Headshot queues: {Ability.GetLastActivationDenialReason()}");
                var dictionary = (Dictionary<uint, WeaponAbilityTraitActivation>)typeof(UsePerkFeat)
                    .GetField("_queuedWeaponTraits", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                traits = dictionary[actor];
                ctx.AssertEqual(5, UsePerkFeat.GetQueuedWeaponTraitAccuracy(actor), "Captured Changing Angles reaches the carrier attack roll");
                ctx.AssertEqual(1, traits.Snapshot.CadenceSpent, "A foreign paid weapon ability captures Cadence");
                ctx.AssertEqual(0, State(actor).Cadence, "The captured Cadence is spent once when the queued cost is paid");
                ctx.Assert(traits.ContinuesChain, "Headshot within six seconds of a different ability continues the chain");
                ActionAttack(target);
            });
            await Until(ctx, () => traits.RecoveryCompleted, "the native carrier hit to resolve foreign Headshot and Follow Through");
            await InFrame(ctx, actor, () => ClearAllActions());
            ctx.Assert(GetCurrentHitPoints(target) < hp, "The outside-skill ability delivers actual native damage");
            ctx.AssertEqual(0, traits.TryTakeDamageBonus(weapon), "The real foreign weapon damage path claimed its Cadence budget");
            ctx.AssertEqual(before - 6 + 1, Stat.GetCurrentStamina(actor), "Headshot pays its authored cost and refunds one Cadence charge exactly once");
            ctx.AssertEqual(1, State(actor).Cadence, "Headshot's landed chain hit grants one fresh Cadence after spending the old one");
            ctx.AssertEqual(0, UsePerkFeat.GetQueuedWeaponTraitAccuracy(actor), "The queued accuracy context clears after resolution");
            ctx.SetResultDetail($"{(shield ? "Sword/shield" : "Rifle")} Lead Shot granted Cadence1/Angles5; foreign Headshot consumed both, delivered {hp - GetCurrentHitPoints(target)} DMG, paid6/refunded1, and its chained hit granted Cadence1.");
        }
        finally { Cleanup(actor); }
    }

    [EngineTest("Captured weapon abilities add Steady Cadence to their per-charge damage and carry their own Cadence grant", Category = "WeaponCrossSkill", TimeoutSeconds = 30f)]
    public static async Task SteadyCadenceCapture(EngineTestContext ctx)
    {
        var actor = ctx.SpawnCreature("civilian");
        try
        {
            await ctx.EquipItemAsync(actor, "b_twinpistol", InventorySlot.RightHand);
            await InFrame(ctx, actor, () =>
            {
                TemporaryStatModifier.Add(actor, StatType.CadenceMaximum, 3, 30f);
                TemporaryStatModifier.Add(actor, StatType.CadenceDamagePerCharge, 2, 30f);
                TemporaryStatModifier.Add(actor, StatType.CadenceBonusDamagePerCharge, 3, 30f);
                var salvo = PairedRangedTraits.CaptureWeaponAbility(actor, Ability.GetAbilityDetail(FeatType.FinalSalvo));
                var closing = PairedRangedTraits.CaptureWeaponAbility(actor, Ability.GetAbilityDetail(FeatType.ClosingShot1));
                var lead = PairedRangedTraits.CaptureWeaponAbility(actor, Ability.GetAbilityDetail(FeatType.LeadShot1));
                var tempo = PairedRangedTraits.CaptureWeaponAbility(actor, Ability.GetAbilityDetail(FeatType.TempoShot1));
                ctx.AssertEqual(7, salvo.DamagePerCharge, "Final Salvo's own 4 per charge plus Steady Cadence III's 3");
                ctx.AssertEqual(5, closing.DamagePerCharge, "Closing Shot's generic 2 per charge plus Steady Cadence III's 3");
                ctx.Assert(salvo.Snapshot.SpendsCadence && closing.Snapshot.SpendsCadence, "Finishers spend Cadence");
                ctx.Assert(!lead.Snapshot.SpendsCadence && !tempo.Snapshot.SpendsCadence, "Builders keep Cadence");
                ctx.AssertEqual(1, lead.CadenceEarnedOnHit, "Lead Shot grants one Cadence without a chain");
                ctx.AssertEqual(2, tempo.CadenceEarnedOnHit, "Tempo Shot grants two Cadence without a chain");
            });
            ctx.SetResultDetail("Final Salvo 4+3 and Closing Shot 2+3 per charge; Lead Shot 1 and Tempo Shot 2 Cadence on hit.");
        }
        finally { Cleanup(actor); }
    }

    [EngineTest("Cool Under Fire discounts a real First Aid support cast to zero once", Category = "WeaponCrossSkill", TimeoutSeconds = 60f)]
    public static Task SupportDiscountToZero(EngineTestContext ctx) => SupportDiscount(ctx, otherMakesFree: false);

    [EngineTest("An independently free First Aid cast preserves Cool Under Fire for the next paid support cast", Category = "WeaponCrossSkill", TimeoutSeconds = 60f)]
    public static Task FreeSupportPreservesCharge(EngineTestContext ctx) => SupportDiscount(ctx, otherMakesFree: true);

    private static async Task SupportDiscount(EngineTestContext ctx, bool otherMakesFree)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        var actor = player.Creature;
        try
        {
            player.Update(record =>
            {
                record.Perks.Clear(); record.Perks[PerkType.CoolUnderFire] = 1; record.Perks[PerkType.Shielding] = 1;
                record.Skills[SkillType.TwinPistol].Rank = 50;
            });
            await ctx.EquipItemAsync(actor, "b_longsword", InventorySlot.RightHand);
            var enemy = await Target(ctx, actor, 3f);
            await InFrame(ctx, actor, () =>
            {
                CreaturePlugin.AddFeat(actor, FeatType.Shielding1);
                CreateItemOnObject("stim_pack", actor, 5);
                SetPlotFlag(actor, false); SetImmortal(actor, false);
                // Shielding's authored3 becomes1 before Cool Under Fire.
                TemporaryStatModifier.Add(actor, StatType.SkillAbilityStaminaCostFlatAdjustmentSkillType, (int)SkillType.FirstAid, 30f);
                TemporaryStatModifier.Add(actor, StatType.SkillAbilityStaminaCostFlatAdjustment, -2, 30f);
                if (otherMakesFree)
                    TemporaryStatModifier.Add(actor, StatType.NextAbilityNoStaminaCostSkillType, (int)SkillType.FirstAid, 30f,
                        StatType.NextAbilityNoStaminaCostSkillType);
            });
            var hp = GetCurrentHitPoints(actor);
            await InFrame(ctx, enemy, () => Ability.ApplyCombatImpact(enemy, actor, GetLocation(actor), SkillType.Rifle, 10, 0,
                null, false, damageType: CombatDamageType.Physical, resolvesHit: false, canCritical: false));
            await Until(ctx, () => PairedRangedTraits.GetAbilityStaminaCostAdjustment(actor) == -2, "actual direct hostile native damage to arm Cool Under Fire");
            ctx.Assert(GetCurrentHitPoints(actor) < hp, "The discount source deals actual hostile damage");
            var stamina = Stat.GetCurrentStamina(actor);
            await InFrame(ctx, actor, () => ctx.Assert(UsePerkFeat.TryUseAbility(actor, actor, FeatType.Shielding1, GetLocation(actor)),
                $"Real First Aid support cast enters the pipeline: {Ability.GetLastActivationDenialReason()}"));
            await Until(ctx, () => !Activity.IsBusy(actor), "First Aid support cast completion");
            ctx.AssertEqual(stamina, Stat.GetCurrentStamina(actor), "The first support cast pays zero STM");
            ctx.AssertEqual(otherMakesFree ? -2 : 0, PairedRangedTraits.GetAbilityStaminaCostAdjustment(actor),
                "Only a positive cost before Cool consumes its charge, including one reduced to zero by Cool itself");
            // A direct requirement invocation isolates the next payment without bypassing
            // or resetting Shielding's real cooldown. No damage event can re-arm the charge.
            await InFrame(ctx, actor, () => new AbilityRequirementStamina(3).AfterActivationAction(actor,
                Ability.GetAbilityDetail(FeatType.Shielding1)));
            ctx.AssertEqual(stamina - (otherMakesFree ? 0 : 1), Stat.GetCurrentStamina(actor),
                "The charge cannot make an unlimited series of one-STM support actions free");
            ctx.AssertEqual(0, PairedRangedTraits.GetAbilityStaminaCostAdjustment(actor), "The charge is consumed at most once");
            ctx.SetResultDetail($"Real hostile damage armed Cool; First Aid Shielding paid0; next isolated support payment {(otherMakesFree ? "used preserved discount" : "paid1 without a second discount")}; no cooldown reset.");
        }
        finally { Cleanup(actor); }
    }

    [EngineTest("Twin intrinsic recovery and Twin Suns work without Cadence or Changing Angles ownership", Category = "WeaponCrossSkill", TimeoutSeconds = 60f)]
    public static async Task IntrinsicRewardsWithoutCadenceTraits(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        var actor = player.Creature;
        try
        {
            player.Update(record =>
            {
                record.Perks.Clear(); record.Perks[PerkType.ControlledBurst] = 1; record.Perks[PerkType.TwinSuns] = 1;
                record.Skills[SkillType.TwinPistol].Rank = 50;
            });
            await ctx.EquipItemAsync(actor, "b_rifle", InventorySlot.RightHand);
            var target = await Target(ctx, actor, 2f);
            await InFrame(ctx, actor, () =>
            {
                CreaturePlugin.AddFeat(actor, FeatType.ControlledBurst1); CreaturePlugin.AddFeat(actor, FeatType.TwinSuns);
                Combat.SetAbilityHitResolutionOverride(true); Combat.SetAutoAttackHitResolutionOverride(false);
                player.Update(record => record.Stamina = Stat.GetMaxStamina(actor) - 100);
            });
            var stamina = Stat.GetCurrentStamina(actor);
            await InFrame(ctx, actor, () => ctx.Assert(UsePerkFeat.TryUseAbility(actor, target, FeatType.ControlledBurst1, GetLocation(target)),
                $"Controlled Burst enters the pipeline: {Ability.GetLastActivationDenialReason()}"));
            await Until(ctx, () => !Activity.IsBusy(actor) && !TwinPistolAbility.HasActiveActivation(actor), "Controlled Burst resolution");
            ctx.AssertEqual(stamina - 6 + 2, Stat.GetCurrentStamina(actor), "Neutral Cadence context preserves Controlled Burst's intrinsic recovery");
            await InFrame(ctx, actor, () =>
            {
                ClearAllActions();
                ctx.Assert(UsePerkFeat.TryUseAbility(actor, target, FeatType.TwinSuns, GetLocation(target)),
                    $"Twin Suns enters the pipeline: {Ability.GetLastActivationDenialReason()}");
            });
            await Until(ctx, () => State(actor)?.BonusAttempts == 6, "Twin Suns intrinsic six-attempt buff");
            ctx.AssertEqual(25, State(actor).BonusDamagePercent, "Neutral context preserves Twin Suns damage buff");
            ctx.AssertEqual(0, State(actor).Cadence, "Neither intrinsic action requires owning Cadence");
            ctx.SetResultDetail("No Cadence/Changing Angles purchased: cross-trained Controlled Burst paid6/restored2; Twin Suns granted six ordinary attempts at+25%.");
        }
        finally { Cleanup(actor); }
    }

    private static async Task<uint> Target(EngineTestContext ctx, uint actor, float offset, uint area = OBJECT_INVALID)
    {
        var target = area == OBJECT_INVALID ? ctx.SpawnCreature("nw_rat001", offset) :
            CreateObject(ObjectType.Creature, "nw_rat001", Location(area, GetPosition(actor) + new Vector3(offset, 0, 0), 0));
        if (area != OBJECT_INVALID) ctx.Track(target);
        await ctx.DelaySecondsAsync(.3f);
        await InFrame(ctx, actor, () =>
        {
            foreach (var creature in new[] { actor, target })
            {
                SetEventScript(creature, EventScript.Creature_OnHeartbeat, "");
                SetEventScript(creature, EventScript.Creature_OnNotice, "");
                SetEventScript(creature, EventScript.Creature_OnMeleeAttacked, "");
            }
            SetPlotFlag(target, false); ctx.SuppressNPCNaturalRegen(target); Stat.SetNPCMaxHitPoints(target, 20000, true);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 55f); SetCommandable(false, target);
            TemporaryStatModifier.Add(target, StatType.RangedDeflection, -100, 55f); ctx.MakeHostile(target);
        });
        return target;
    }

    private static PairedRangedTraitState State(uint actor) =>
        ((Dictionary<uint, PairedRangedTraitState>)typeof(PairedRangedTraits).GetField("States", BindingFlags.NonPublic | BindingFlags.Static)
            .GetValue(null)).GetValueOrDefault(actor);

    private static async Task InFrame(EngineTestContext ctx, uint actor, Action action)
    {
        var completed = false; Exception error = null;
        AssignCommand(actor, () => { try { action(); } catch (Exception exception) { error = exception; } finally { completed = true; } });
        await Until(ctx, () => completed, "the actor's native command frame");
        if (error != null) throw error;
    }

    private static async Task Until(EngineTestContext ctx, Func<bool> condition, string description)
    {
        var deadline = DateTime.UtcNow.AddSeconds(12);
        while (!condition() && DateTime.UtcNow < deadline) await ctx.WaitFrameAsync();
        ctx.Assert(condition(), $"Timed out waiting for {description}");
    }

    private static void Cleanup(uint actor)
    {
        Combat.SetAutoAttackHitResolutionOverride(null); Combat.SetAbilityHitResolutionOverride(null);
        if (!GetIsObjectValid(actor)) return;
        AssignCommand(actor, () => ClearAllActions());
        TwinPistolAbility.Cancel(actor); UsePerkFeat.InterruptAbilityActivation(actor); UsePerkFeat.ClearQueuedAbility(actor);
        PairedRangedAttackCycle.Cancel(actor); PairedRangedTraits.ClearActor(actor); TemporaryStatModifier.Clear(actor);
    }
}
