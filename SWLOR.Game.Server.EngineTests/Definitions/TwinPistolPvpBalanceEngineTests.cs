using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class TwinPistolPvpBalanceEngineTests
{
    private static readonly MethodInfo ResolveCompanion = typeof(PairedRangedAttackCycle)
        .GetMethod("ResolveCompanion", BindingFlags.NonPublic | BindingFlags.Static);
    private static readonly MethodInfo EnsureNativeLevels = typeof(PlayerInitialization)
        .GetMethod("EnsureNativeLevels", BindingFlags.NonPublic | BindingFlags.Static);

    [EngineTest("Twin companion damage follows only the player target's PvP ramp exactly once", Category = "TwinPistolPvpCombat", TimeoutSeconds = 80f)]
    public static async Task OrdinaryCompanionUsesTargetRamp(EngineTestContext ctx)
    {
        using var attacker = await PlayerAbilityFixture.CreateAsync(ctx, -.5f);
        using var defender = await PlayerAbilityFixture.CreateAsync(ctx, 10f);
        using var fresh = await PlayerAbilityFixture.CreateAsync(ctx, 10f);
        var actor = attacker.Creature;
        var area = GetArea(actor);
        var originalPvp = AreaPlugin.GetPVPSetting(area);
        var npc = ctx.SpawnCreature("nw_rat001", 10f);
        await ctx.DelaySecondsAsync(1f);
        try
        {
            AreaPlugin.SetPVPSetting(area, PvPSetting.FullPvP);
            var (_, companion) = await PrepareAttacker(ctx, attacker);
            await PreparePlayer(ctx, defender.Creature, hostile: true);
            await PreparePlayer(ctx, fresh.Creature, hostile: true);
            await ctx.ExecuteInCreatureContextAsync(actor, () => PrepareNpc(ctx, npc));
            ctx.AssertEqual(Stat.GetDefense(defender.Creature, CombatDamageType.Physical, AbilityType.Vitality),
                Stat.GetDefense(fresh.Creature, CombatDamageType.Physical, AbilityType.Vitality),
                "The original and fresh player targets have identical measured defenses");

            PvpCombat.SetCombatSecondsForTesting(actor, 0);
            var baseline = await OrdinaryDamage(ctx, actor, defender.Creature, companion, playerSeconds: 0);
            PvpCombat.SetCombatSecondsForTesting(actor, 300);
            var mature = await OrdinaryDamage(ctx, actor, defender.Creature, companion, playerSeconds: 300);
            ctx.AssertEqual(baseline * 2, mature, "A mature player target doubles the same native companion roll exactly once");
            var freshDamage = await OrdinaryDamage(ctx, actor, fresh.Creature, companion, playerSeconds: 0);
            ctx.AssertEqual(baseline, freshDamage, "The mature attacker's clock cannot increase damage to a fresh matched player");
            PvpCombat.SetCombatSecondsForTesting(actor, 0);
            var npcBaseline = await OrdinaryDamage(ctx, actor, npc, companion);
            PvpCombat.SetCombatSecondsForTesting(actor, 300);
            var npcAfterLongFight = await OrdinaryDamage(ctx, actor, npc, companion);
            ctx.AssertEqual(npcBaseline, npcAfterLongFight, "The mature attacker's clock cannot increase companion damage to the same NPC");
            ctx.Log($"Companion delivered HP: fresh={baseline}, mature={mature}, fresh entrant={freshDamage}, NPC={npcBaseline}/{npcAfterLongFight}.");
        }
        finally
        {
            Cleanup(actor, defender.Creature, fresh.Creature);
            TemporaryStatModifier.Clear(npc);
            AreaPlugin.SetPVPSetting(area, originalPvp);
        }
    }

    [EngineTest("Twin paired casts and companion-triggered queued cross-skills share the target PvP ramp and real costs", Category = "TwinPistolPvpCombat", TimeoutSeconds = 80f)]
    public static async Task ActivatedAndQueuedDamageUseTargetRamp(EngineTestContext ctx)
    {
        using var attacker = await PlayerAbilityFixture.CreateAsync(ctx, -.5f);
        using var defender = await PlayerAbilityFixture.CreateAsync(ctx, 10f);
        var actor = attacker.Creature;
        var area = GetArea(actor);
        var originalPvp = AreaPlugin.GetPVPSetting(area);
        try
        {
            AreaPlugin.SetPVPSetting(area, PvPSetting.FullPvP);
            var (_, companion) = await PrepareAttacker(ctx, attacker);
            await PreparePlayer(ctx, defender.Creature, hostile: true);
            PvpCombat.SetCombatSecondsForTesting(actor, 300);
            foreach (var feat in new[] { FeatType.ControlledBurst1, FeatType.Headshot1 })
            {
                var baseline = await ActivatedDamage(ctx, actor, defender.Creature, companion, feat, 0);
                var mature = await ActivatedDamage(ctx, actor, defender.Creature, companion, feat, 300);
                ctx.AssertEqual(baseline * 2, mature, $"{feat} doubles delivered HP exactly once against the mature target");
                ctx.Log($"{feat} delivered HP: fresh={baseline}, mature={mature}; real net STM verified for both impacts.");
            }
        }
        finally
        {
            Cleanup(actor, defender.Creature);
            AreaPlugin.SetPVPSetting(area, originalPvp);
        }
    }

    [EngineTest("The PvP HP healing cut preserves Final Salvo's Cadence spend and full Follow Through stamina refund", Category = "TwinPistolPvpCombat", TimeoutSeconds = 60f)]
    public static async Task HealingRampDoesNotReduceCadenceStaminaRefund(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx, -.5f);
        var actor = player.Creature;
        var stamina = 0;
        var health = 0;
        var ability = Ability.GetAbilityDetail(FeatType.FinalSalvo);
        var target = ctx.SpawnCreature("nw_rat001", 2f);
        await ctx.DelaySecondsAsync(1f);
        try
        {
            await PrepareAttacker(ctx, player);
            await PreparePlayer(ctx, actor, hostile: false);
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                PrepareNpc(ctx, target);
                PvpCombat.SetCombatSecondsForTesting(actor, 300);
                ctx.AssertEqual(60, PvpCombat.GetHealingReductionPercent(actor), "The real PvP HP healing cut is active");
                ObjectPlugin.SetCurrentHitPoints(actor, GetMaxHitPoints(actor) - 50);
                var before = GetCurrentHitPoints(actor);
                ApplyEffectToObject(DurationType.Instant, EffectHeal(10), actor);
                ctx.AssertEqual(before + 4, GetCurrentHitPoints(actor), "A native ten-HP heal restores four under the PvP cut");
                CadenceFixture.Grant(actor, 3);
                ctx.AssertEqual(3, StatusEffect.GetStatusEffect<CadenceStatusEffect>(actor)?.Charges ?? 0,
                    "Three Cadence charges were earned through the production landed-hit adapter");
                Ability.ClearLastCompletedAbilityImpactSummary(actor);
                HostileAbilityDelay.EndForTesting(actor);
                ctx.SeedRandom(1);
                stamina = Stat.GetCurrentStamina(actor);
                health = GetCurrentHitPoints(target);
                ctx.Assert(UsePerkFeat.TryUseAbility(actor, target, FeatType.FinalSalvo, GetLocation(target)),
                    $"The real Cadence finisher begins in the same batch as its earned charges: {Ability.GetLastActivationDenialReason()}");
            });
            await WaitFrames(ctx, () => Ability.GetLastCompletedAbilityImpactSummary(actor) is { AttributedDamage: > 0 } summary &&
                summary.Ability == ability && !Activity.IsBusy(actor), "Final Salvo's real paid impact", 5f);
            var impact = Ability.GetLastCompletedAbilityImpactSummary(actor);
            await WaitFrames(ctx, () => health - GetCurrentHitPoints(target) == impact.AttributedDamage,
                "all actual finisher damage to arrive", 2f);
            ctx.AssertEqual(stamina - Cost(FeatType.FinalSalvo) + 3, Stat.GetCurrentStamina(actor),
                "Three spent Cadence charges restore the full three STM despite the 60% HP healing cut");
            ctx.AssertEqual(0, StatusEffect.GetStatusEffect<CadenceStatusEffect>(actor)?.Charges ?? 0,
                "The real paid finisher spends its captured Cadence once");
            ctx.AssertEqual(60, PvpCombat.GetHealingReductionPercent(actor), "The HP healing cut remains active through the resource refund");
        }
        finally
        {
            Cleanup(actor);
            TemporaryStatModifier.Clear(target);
        }
    }

    private static async Task<int> OrdinaryDamage(EngineTestContext ctx, uint actor, uint target, uint companion,
        double? playerSeconds = null)
    {
        var health = 0;
        RangedWeaponAttackContext.Roll captured = null;
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            ResetShot(ctx, actor, target);
            if (playerSeconds.HasValue) PvpCombat.SetCombatSecondsForTesting(target, playerSeconds.Value);
            health = GetCurrentHitPoints(target);
            ctx.SeedRandom(1);
            LaunchCompanion(ctx, actor, target, companion);
            var shot = RangedWeaponAttackContext.GetRoll(actor, target, 0);
            ctx.Assert(shot is { Ordinary: true, Completed: true } && shot.Damage > 0,
                "The production resolver captured a positive ordinary companion payload");
            captured = shot;
            ctx.Assert(health > shot.Damage * 2, "The native target survives even a mature final PvP payload");
        });
        await WaitFrames(ctx, () => GetCurrentHitPoints(target) < health,
            "the ordinary projectile's target-owned incoming stage and native HP delivery", 2f);
        ctx.AssertEqual(captured.Damage, health - GetCurrentHitPoints(target),
            "Physical application does not apply the already captured PvP ramp again");
        return health - GetCurrentHitPoints(target);
    }

    [EngineTest("Discarded companion arrivals preserve PvP clocks fatal saves low-HP shields redirects and finite DR", Category = "TwinPistolPvpCombat", TimeoutSeconds = 100f)]
    public static async Task DiscardedArrivalPreservesIncomingState(EngineTestContext ctx)
    {
        using var attacker = await PlayerAbilityFixture.CreateAsync(ctx, -.5f);
        using var defender = await PlayerAbilityFixture.CreateAsync(ctx, 10f);
        var actor = attacker.Creature;
        var area = GetArea(actor);
        var originalPvp = AreaPlugin.GetPVPSetting(area);
        var protector = ctx.SpawnCreature("nw_rat001", 12f);
        var launchedAt = 0L;
        var diedAt = 0L;
        var flightMilliseconds = 0f;
        await ctx.DelaySecondsAsync(1f);
        try
        {
            AreaPlugin.SetPVPSetting(area, PvPSetting.FullPvP);
            var (main, companion) = await PrepareAttacker(ctx, attacker);
            await PreparePlayer(ctx, defender.Creature, hostile: true);
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                PrepareNpc(ctx, protector);
                foreach (var weapon in new[] { main, companion })
                    ReplaceProperty(weapon, ItemPropertyType.DMG, ItemPropertyCustom(ItemPropertyType.DMG, -1, 80));
                ResetShot(ctx, actor, defender.Creature);
                PvpCombat.EndForTesting(actor);
                PvpCombat.EndForTesting(defender.Creature);
                ctx.SeedRandom(1);
                launchedAt = Environment.TickCount64;
                flightMilliseconds = PairedRangedPresentation.FlightSeconds(GetDistanceBetween(actor, defender.Creature)) * 1000f;
                LaunchCompanion(ctx, actor, defender.Creature, companion);
                ctx.Assert(!PvpCombat.IsInPvpCombat(actor) && !PvpCombat.IsInPvpCombat(defender.Creature),
                    "A completed launch roll creates no PvP activity before projectile arrival");
                AssignCommand(GetModule(), () =>
                {
                    ApplyEffectToObject(DurationType.Instant, EffectDeath(), defender.Creature);
                    if (GetIsDead(defender.Creature)) diedAt = Environment.TickCount64;
                });
            });
            await WaitFrames(ctx, () => GetIsDead(defender.Creature), "the module-owned target death before the pending arrival", 1f);
            ctx.Assert(diedAt > 0 && diedAt < launchedAt + flightMilliseconds,
                "The player target actually dies before the captured projectile deadline");
            await ctx.DelaySecondsAsync(.6f);
            ctx.Assert(!PvpCombat.IsInPvpCombat(actor), "A dead-target discarded projectile never starts its living attacker's PvP clock");

            var quiet = await QuietArena.CreateAsync(ctx);
            var anchor = quiet.Spawn("nw_rat001", 0f, 0f);
            var departure = GetLocation(anchor);
            foreach (var channel in new[] { "fatal", "lowHP", "redirect", "finiteDR" })
                await VerifyDiscardedProtection(ctx, actor, companion, protector, departure, channel);
        }
        finally
        {
            Cleanup(actor, defender.Creature);
            TemporaryStatModifier.Clear(protector);
            ReviveRulesEngineTests.DestroyCorpsePlaceable(defender.Creature);
            ReviveRulesEngineTests.ReleaseBody(defender.Creature);
            AreaPlugin.SetPVPSetting(area, originalPvp);
        }
    }

    [EngineTest("A fresh rotation NPC has no reactive incoming stages and its companion raw payload equals delivered HP", Category = "TwinPistolPvpCombat", TimeoutSeconds = 45f)]
    public static async Task NeutralRotationTargetKeepsCapturedDamage(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx, -.5f);
        var actor = player.Creature;
        var target = ctx.SpawnCreature("nw_rat001", 10f);
        await ctx.DelaySecondsAsync(1f);
        try
        {
            var (main, companion) = await PrepareAttacker(ctx, player);
            var raw = 0;
            var health = 0;
            RangedWeaponAttackContext.Roll shot = null;
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                SuppressAi(target);
                ctx.SuppressNPCNaturalRegen(target);
                Stat.SetNPCMaxHitPoints(target, 30000, true);
                TemporaryStatModifier.Add(target, StatType.PhysicalDefense,
                    94 - Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality), 30f);
                TemporaryStatModifier.Add(target, StatType.Evasion, 134 - Stat.GetEvasion(target, SkillType.Invalid), 30f);
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneImmobilize(), target, 30f);
                SetCommandable(false, target);
                ctx.MakeHostile(target);
                foreach (var weapon in new[] { main, companion })
                    ReplaceProperty(weapon, ItemPropertyType.DMG, ItemPropertyCustom(ItemPropertyType.DMG, -1, 22));
                foreach (var stat in new[] { StatType.Guard, StatType.DamageTakenRedirectToStatusSourcePercent,
                             StatType.DamageTakenShareToStatusSourcePercent, StatType.FatalDamageTemporaryHPPercent,
                             StatType.LowHPTemporaryHPPercent, StatType.DamageTakenFlatAdjustment,
                             StatType.DamageTakenPercentAdjustment, StatType.IncomingPhysicalToForceConversionPercent })
                    ctx.AssertEqual(0, Stat.GetStatAdjustment(target, stat), $"The fresh rotation target has no {stat}");
                for (var effect = GetFirstEffect(target); GetIsEffectValid(effect); effect = GetNextEffect(target))
                    ctx.Assert(GetEffectType(effect) is not EffectTypeScript.DamageReduction and not EffectTypeScript.DamageResistance,
                        "The fresh rotation target has no finite native physical absorption effects");
                ctx.AssertEqual(94, Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality), "Actual rotation defense");
                ctx.AssertEqual(134, Stat.GetEvasion(target, SkillType.Invalid), "Actual rotation evasion");
                ctx.AssertEqual(30000, GetMaxHitPoints(target), "Actual native rotation HP budget");
                ctx.AssertEqual(0, TemporaryHitPointEffects.GetRemaining(target), "The fresh rotation target has no reactive shield");
                ResetShot(ctx, actor, target);
                PvpCombat.EndForTesting(actor);
                health = GetCurrentHitPoints(target);
                ctx.SeedRandom(1);
                LaunchCompanion(ctx, actor, target, companion);
                shot = RangedWeaponAttackContext.GetRoll(actor, target, 0);
                raw = shot.Damage;
                ctx.Assert(raw > 0, "The native companion produces a positive pre-incoming payload");
                ctx.AssertEqual(health, GetCurrentHitPoints(target), "The pending neutral payload removes no HP at launch");
            });
            await WaitFrames(ctx, () => GetCurrentHitPoints(target) < health, "neutral companion arrival", 2f);
            ctx.AssertEqual(raw, shot.Damage, "The neutral incoming finalizer preserves its captured raw damage");
            ctx.AssertEqual(raw, health - GetCurrentHitPoints(target), "Native HP delivery equals raw and finalized damage exactly once");
            ctx.Assert(!PvpCombat.IsInPvpCombat(actor), "An unowned NPC target creates no PvP clock");
            ctx.Log($"Neutral rotation NPC: HP30000, DEF94, EVA134, Guard0, redirect/share/fatal/lowHP/flat0, no native DR/resistance; raw={raw}, final={shot.Damage}, delivered={health - GetCurrentHitPoints(target)}. Qualified neutral-target equivalence only.");
        }
        finally
        {
            Cleanup(actor);
            TemporaryStatModifier.Clear(target);
        }
    }

    private static async Task VerifyDiscardedProtection(EngineTestContext ctx, uint actor, uint companion,
        uint protector, Location departure, string channel)
    {
        var target = ctx.SpawnCreature("nw_rat001", 10f);
        await ctx.DelaySecondsAsync(1f);
        await ctx.ExecuteInCreatureContextAsync(actor, () => PrepareNpc(ctx, target));
        var baseline = await OrdinaryDamage(ctx, actor, target, companion);
        ctx.Assert(baseline > 5, "The measured native payload is large enough to exercise finite absorption and low-HP protection");
        var origin = GetLocation(target);
        var initialHP = 0;
        var protectorHP = GetCurrentHitPoints(protector);
        try
        {
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                ResetShot(ctx, actor, target);
                if (channel == "fatal")
                {
                    TemporaryStatModifier.Add(target, StatType.FatalDamageTemporaryHPPercent, 20, 30f);
                    TemporaryStatModifier.Add(target, StatType.FatalDamageTemporaryHPDurationSeconds, 30, 30f);
                    TemporaryStatModifier.Add(target, StatType.FatalDamageTemporaryHPCooldownSeconds, 30, 30f);
                    ObjectPlugin.SetCurrentHitPoints(target, 1);
                }
                else if (channel == "lowHP")
                {
                    // Calibrated actual damage makes this hit fatal precisely at the configured
                    // threshold, with enough real shield capacity to survive the arriving hit.
                    Stat.SetNPCMaxHitPoints(target, baseline * 5, true);
                    ObjectPlugin.SetCurrentHitPoints(target, baseline);
                    TemporaryStatModifier.Add(target, StatType.LowHPTemporaryHPThresholdPercent, 20, 30f);
                    TemporaryStatModifier.Add(target, StatType.LowHPTemporaryHPPercent, 50, 30f);
                    TemporaryStatModifier.Add(target, StatType.LowHPTemporaryHPDurationSeconds, 30, 30f);
                    TemporaryStatModifier.Add(target, StatType.LowHPTemporaryHPCooldownSeconds, 30, 30f);
                }
                else if (channel == "redirect")
                    ctx.Assert(StatusEffect.ApplyStatusEffect(protector, target, new ForceIntercept1StatusEffect(), 30f),
                        "A real one-use source-owned redirect is active");
                else
                    ApplyEffectToObject(DurationType.Temporary, EffectDamageReduction(9999, DamagePower.PlusOne, 5), target, 30f);
                initialHP = GetCurrentHitPoints(target);
                ctx.SeedRandom(1);
                var launchedAt = Environment.TickCount64;
                var flight = PairedRangedPresentation.FlightSeconds(GetDistanceBetween(actor, target));
                LaunchCompanion(ctx, actor, target, companion);
                ctx.AssertEqual(initialHP, GetCurrentHitPoints(target), "Pending damage does not change target HP");
                ctx.AssertEqual(0, TemporaryHitPointEffects.GetRemaining(target), "Pending damage grants no fatal/low-HP shield or spends its cooldown");
                ctx.AssertEqual(protectorHP, GetCurrentHitPoints(protector), "Pending damage does not damage the redirect protector");
                if (channel == "redirect") ctx.Assert(StatusEffect.GetStatusEffect<ForceIntercept1StatusEffect>(target) != null,
                    "The pending projectile does not consume its one-use redirect");
                ObjectPlugin.AddToArea(target, GetAreaFromLocation(departure), GetPositionFromLocation(departure));
                ctx.Assert(GetArea(target) == GetAreaFromLocation(departure) && GetArea(target) != GetArea(actor),
                    "The target actually leaves the captured launch area synchronously");
                ctx.Assert(Environment.TickCount64 < launchedAt + flight * 1000f, "Actual departure precedes the projectile deadline");
            });
            await ctx.DelaySecondsAsync(.6f);
            ctx.AssertEqual(initialHP, GetCurrentHitPoints(target), "Discarded damage leaves target HP untouched");
            ctx.AssertEqual(0, TemporaryHitPointEffects.GetRemaining(target), "Discarded damage consumes no protection trigger");
            ctx.AssertEqual(protectorHP, GetCurrentHitPoints(protector), "Discarded damage leaves the protector untouched");
            if (channel == "redirect") ctx.Assert(StatusEffect.GetStatusEffect<ForceIntercept1StatusEffect>(target) != null,
                "The discarded hit leaves the real one-use redirect available");
            RangedWeaponAttackContext.Roll arriving = null;
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                ObjectPlugin.AddToArea(target, GetAreaFromLocation(origin), GetPositionFromLocation(origin));
                ctx.AssertEqual(GetArea(actor), GetArea(target), "The same protected target returns for an actual valid hit");
                ClearAllActions();
                RangedWeaponAttackContext.Clear(actor);
                ctx.SeedRandom(1);
                LaunchCompanion(ctx, actor, target, companion);
                arriving = RangedWeaponAttackContext.GetRoll(actor, target, 0);
                ctx.AssertEqual(0, TemporaryHitPointEffects.GetRemaining(target), "Even the valid retry leaves protection untouched until arrival");
            });
            await ctx.DelaySecondsAsync(.6f);
            ctx.Assert(!GetIsDead(target), "The same target survives the valid arrival");
            if (channel is "fatal" or "lowHP")
            {
                ctx.Assert(TemporaryHitPointEffects.GetRemaining(target) > 0,
                    "The valid arrival can still trigger the protection cooldown that the discarded shot did not consume");
                if (channel == "fatal") ctx.AssertEqual(0, arriving.Damage, "A successful fatal save finalizes this incoming damage to zero");
            }
            else if (channel == "redirect")
            {
                ctx.Assert(StatusEffect.GetStatusEffect<ForceIntercept1StatusEffect>(target) == null, "Only the actual arriving hit consumes the one-use redirect");
                ctx.AssertEqual(baseline, initialHP - GetCurrentHitPoints(target) + protectorHP - GetCurrentHitPoints(protector),
                    "The valid redirect splits one captured payload across target and protector exactly once");
            }
            else
            {
                ctx.AssertEqual(baseline - 5, initialHP - GetCurrentHitPoints(target),
                    "The discarded projectile left the full finite five-point DR budget for the first valid arrival");
                var before = GetCurrentHitPoints(target);
                await ctx.ExecuteInCreatureContextAsync(actor, () =>
                {
                    RangedWeaponAttackContext.Clear(actor);
                    ctx.SeedRandom(1);
                    LaunchCompanion(ctx, actor, target, companion);
                });
                await WaitFrames(ctx, () => GetCurrentHitPoints(target) < before, "the post-budget native physical hit", 2f);
                ctx.AssertEqual(baseline, before - GetCurrentHitPoints(target), "One valid hit consumes the finite DR budget, so the next hit lands in full");
            }
            ctx.Log($"Discard/valid incoming-state control {channel}: measured raw={baseline}, finalized retry={arriving.Damage}.");
        }
        finally
        {
            StatusEffect.RemoveStatusEffect<ForceIntercept1StatusEffect>(target);
            TemporaryStatModifier.Clear(target);
        }
    }

    private static async Task<int> ActivatedDamage(EngineTestContext ctx, uint actor, uint target, uint companion,
        FeatType feat, double playerSeconds)
    {
        var ability = Ability.GetAbilityDetail(feat);
        var health = 0;
        var stamina = 0;
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            ResetShot(ctx, actor, target);
            PvpCombat.SetCombatSecondsForTesting(target, playerSeconds);
            Recast.ReduceRecastDelay(actor, ability.RecastGroup, 120f);
            HostileAbilityDelay.EndForTesting(actor);
            Ability.ClearLastCompletedAbilityImpactSummary(actor);
            health = GetCurrentHitPoints(target);
            stamina = Stat.GetCurrentStamina(actor);
            ctx.SeedRandom(1);
            ctx.Assert(UsePerkFeat.TryUseAbility(actor, target, feat, GetLocation(target)),
                $"The actual ability is accepted: {Ability.GetLastActivationDenialReason()}");
            if (feat != FeatType.Headshot1) return;
            ctx.AssertEqual(stamina - Cost(feat), Stat.GetCurrentStamina(actor), "Headshot pays exactly once at real queue time");
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature();
            native.m_pcCombatRound.StartCombatRound(target);
            native.m_pcCombatRound.m_nCurrentAttack = 0;
            native.m_pcCombatRound.GetAttack(0).m_bRangedAttack = 1;
            Combat.SetAutoAttackHitResolutionOverride(false);
            native.ResolveAttackRoll(NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(target).AsNWSObject());
            ctx.AssertEqual(4, native.m_pcCombatRound.GetAttack(0).m_nAttackResult, "The actual native main hand misses");
            Combat.SetAutoAttackHitResolutionOverride(true);
            LaunchCompanion(ctx, actor, target, companion);
            var shot = RangedWeaponAttackContext.GetRoll(actor, target, 0);
            ctx.Assert(shot is { Ordinary: false, Completed: true, Damage: 0 },
                "The landed companion reserves Headshot and suppresses ordinary damage");
        });
        await WaitFrames(ctx, () => Ability.GetLastCompletedAbilityImpactSummary(actor) is { AttributedDamage: > 0 } summary &&
            summary.Ability == ability && !Activity.IsBusy(actor) && !UsePerkFeat.HasQueuedWeaponAbility(actor),
            "the actual activated or queued impact to complete", 4f);
        var impact = Ability.GetLastCompletedAbilityImpactSummary(actor);
        ctx.Assert(health > impact.AttributedDamage, "The native player target survives its complete activated payload");
        await WaitFrames(ctx, () => health - GetCurrentHitPoints(target) == impact.AttributedDamage,
            "the completed impact to reconcile exactly to target HP", 2f);
        var refund = feat == FeatType.ControlledBurst1 ? 2 : 0;
        ctx.AssertEqual(stamina - Cost(feat) + refund, Stat.GetCurrentStamina(actor),
            "Actual STM includes the authored on-hit refund and no duplicate queue payment");
        ctx.AssertEqual(feat == FeatType.Headshot1 ? SkillType.Rifle : SkillType.TwinPistol, impact.SkillType,
            "The queued cross-skill keeps Rifle attribution while the paired cast keeps Twin Pistol attribution");
        return health - GetCurrentHitPoints(target);
    }

    private static async Task<(uint Main, uint Companion)> PrepareAttacker(EngineTestContext ctx, PlayerAbilityFixture player)
    {
        var actor = player.Creature;
        player.Update(record =>
        {
            record.Skills[SkillType.TwinPistol].Rank = record.Skills[SkillType.Rifle].Rank = 50;
            foreach (var perk in new[] { PerkType.ControlledBurst, PerkType.Headshot, PerkType.FinalSalvo,
                         PerkType.Cadence, PerkType.TwinPistolFollowThrough }) record.Perks[perk] = 1;
            foreach (var quest in Perk.GetPerkDetails(PerkType.FinalSalvo).PerkLevels.Values
                         .SelectMany(level => level.Requirements).OfType<PerkRequirementQuest>())
                record.Quests[quest.QuestId] = new PlayerQuest { TimesCompleted = 1 };
            record.MaxStamina = 100;
            record.Stamina = 50;
        });
        var main = await ctx.EquipItemAsync(actor, "b_twinpistol", InventorySlot.RightHand);
        var companion = await TwinPistolEquipmentEngineTests.WaitForPair(ctx, actor, main);
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            SuppressAi(actor);
            foreach (var feat in new[] { FeatType.ControlledBurst1, FeatType.Headshot1, FeatType.FinalSalvo })
                CreaturePlugin.AddFeat(actor, feat);
            foreach (var weapon in new[] { main, companion })
                ReplaceProperty(weapon, ItemPropertyType.DMG, ItemPropertyCustom(ItemPropertyType.DMG, -1, 20));
            TemporaryStatModifier.Add(actor, StatType.CriticalRatePercentAdjustment, -100, 90f);
            Combat.SetAutoAttackHitResolutionOverride(true);
            Combat.SetAbilityHitResolutionOverride(true);
        });
        return (main, companion);
    }

    private static async Task PreparePlayer(EngineTestContext ctx, uint player, bool hostile)
    {
        ctx.Assert(EnsureNativeLevels != null, "The existing production native-level initialization is available");
        SetPlotFlag(player, false);
        SetImmortal(player, false);
        CreaturePlugin.SetClassByPosition(player, 0, ClassType.Standard);
        await ctx.ExecuteInCreatureContextAsync(player, () => EnsureNativeLevels.Invoke(null, new object[] { player }));
        CreaturePlugin.SetRawAbilityScore(player, AbilityType.Vitality, 10);
        await ctx.WaitFrameAsync();
        ctx.Assert(GetMaxHitPoints(player) >= 400, "Actual native player levels provide a survivable HP budget");
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            SuppressAi(player);
            ObjectPlugin.SetCurrentHitPoints(player, GetMaxHitPoints(player));
            if (hostile) PrepareTarget(ctx, player);
        });
    }

    private static void PrepareNpc(EngineTestContext ctx, uint npc)
    {
        SuppressAi(npc);
        SetPlotFlag(npc, false);
        ctx.SuppressNPCNaturalRegen(npc);
        Stat.SetNPCMaxHitPoints(npc, 20000, true);
        PrepareTarget(ctx, npc);
    }

    private static void PrepareTarget(EngineTestContext ctx, uint target)
    {
        ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 90f);
        SetCommandable(false, target);
        TemporaryStatModifier.Add(target, StatType.RangedDeflection, -100, 90f);
        TemporaryStatModifier.Add(target, StatType.PhysicalDefense,
            94 - Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality), 90f);
        ctx.MakeHostile(target);
    }

    private static void SuppressAi(uint creature)
    {
        SetEventScript(creature, EventScript.Creature_OnHeartbeat, "");
        SetEventScript(creature, EventScript.Creature_OnNotice, "");
        SetEventScript(creature, EventScript.Creature_OnMeleeAttacked, "");
        AssignCommand(creature, () => ClearAllActions());
    }

    private static void ResetShot(EngineTestContext ctx, uint actor, uint target)
    {
        ClearAllActions();
        PairedRangedAttackCycle.Cancel(actor);
        RangedWeaponAttackContext.Clear(actor);
        PairedRangedTraits.ClearActor(actor);
        Combat.ClearAttackSwingDebt(actor);
        ObjectPlugin.SetCurrentHitPoints(target, GetMaxHitPoints(target));
        SetIsTemporaryEnemy(target, actor);
        if (GetIsPC(target))
        {
            SetPCDislike(actor, target);
            SetPCDislike(target, actor);
        }
        ctx.Assert(GetIsReactionTypeHostile(target, actor), "Native faction/PvP/player hostility permits the actual hostile impact");
    }

    private static void LaunchCompanion(EngineTestContext ctx, uint actor, uint target, uint companion)
    {
        ctx.Assert(ResolveCompanion != null, "The existing production companion resolver is available");
        var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature();
        var defender = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(target).AsNWSObject();
        var distance = GetDistanceBetween(actor, target);
        ctx.Assert(distance >= 9f && distance < native.MaxAttackRange(target), "The actual companion launch is within native ranged reach");
        native.m_pcCombatRound.StartCombatRound(target);
        native.m_pcCombatRound.m_nCurrentAttack = 0;
        ResolveCompanion.Invoke(null, new object[] { native, defender, WeaponShotProfile.Capture(actor, companion, SkillType.TwinPistol) });
    }

    private static int Cost(FeatType feat) => Ability.GetAbilityDetail(feat).Requirements
        .OfType<AbilityRequirementStamina>().Single().RequiredSTM;

    private static void ReplaceProperty(uint item, ItemPropertyType type, ItemProperty replacement)
    {
        var properties = new List<ItemProperty>();
        for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
            if (GetItemPropertyType(property) == type) properties.Add(property);
        foreach (var property in properties) RemoveItemProperty(item, property);
        AddItemProperty(DurationType.Permanent, replacement, item);
    }

    private static void Cleanup(uint actor, params uint[] otherPlayers)
    {
        Combat.SetAutoAttackHitResolutionOverride(null);
        Combat.SetAbilityHitResolutionOverride(null);
        if (GetIsObjectValid(actor))
        {
            AssignCommand(actor, () => ClearAllActions());
            TwinPistolAbility.Cancel(actor);
            UsePerkFeat.InterruptAbilityActivation(actor);
            UsePerkFeat.ClearQueuedAbility(actor);
            PairedRangedAttackCycle.Cancel(actor);
            RangedWeaponAttackContext.Clear(actor);
            PairedRangedTraits.ClearActor(actor);
            TemporaryStatModifier.Clear(actor);
            HostileAbilityDelay.EndForTesting(actor);
        }
        foreach (var player in otherPlayers.Append(actor))
        {
            PvpCombat.EndForTesting(player);
            TemporaryStatModifier.Clear(player);
        }
    }

    private static async Task WaitFrames(EngineTestContext ctx, Func<bool> condition, string description, float timeout)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeout);
        while (!condition() && DateTime.UtcNow < deadline) await ctx.WaitFrameAsync();
        ctx.Assert(condition(), $"Timed out waiting for {description}");
    }
}
