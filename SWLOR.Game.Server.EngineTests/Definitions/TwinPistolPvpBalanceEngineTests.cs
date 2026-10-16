using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
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

    [EngineTest("The PvP HP healing cut preserves Final Salvo's Flow spend and full Follow Through stamina refund", Category = "TwinPistolPvpCombat", TimeoutSeconds = 60f)]
    public static async Task HealingRampDoesNotReduceFlowStaminaRefund(EngineTestContext ctx)
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
                for (var index = 0; index < 3; index++)
                {
                    var charge = PairedRangedTraits.BeginAbility(actor, true, false);
                    PairedRangedTraits.CompleteAbility(actor, charge, true, 3f, true, 0, 0);
                }
                ctx.AssertEqual(3, StatusEffect.GetStatusEffect<MomentumStatusEffect>(actor)?.Charges ?? 0,
                    "Three Flow charges were earned through the existing movement-completion adapter");
                Ability.ClearLastCompletedAbilityImpactSummary(actor);
                HostileAbilityDelay.EndForTesting(actor);
                ctx.SeedRandom(1);
                stamina = Stat.GetCurrentStamina(actor);
                health = GetCurrentHitPoints(target);
                ctx.Assert(UsePerkFeat.TryUseAbility(actor, target, FeatType.FinalSalvo, GetLocation(target)),
                    $"The real Flow finisher begins in the same batch as its earned charges: {Ability.GetLastActivationDenialReason()}");
            });
            await WaitFrames(ctx, () => Ability.GetLastCompletedAbilityImpactSummary(actor) is { AttributedDamage: > 0 } summary &&
                summary.Ability == ability && !Activity.IsBusy(actor), "Final Salvo's real paid impact", 5f);
            var impact = Ability.GetLastCompletedAbilityImpactSummary(actor);
            await WaitFrames(ctx, () => health - GetCurrentHitPoints(target) == impact.AttributedDamage,
                "all actual finisher damage to arrive", 2f);
            ctx.AssertEqual(stamina - Cost(FeatType.FinalSalvo) + 3, Stat.GetCurrentStamina(actor),
                "Three spent Flow charges restore the full three STM despite the 60% HP healing cut");
            ctx.AssertEqual(0, StatusEffect.GetStatusEffect<MomentumStatusEffect>(actor)?.Charges ?? 0,
                "The real paid finisher spends its captured Flow once");
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
        var capturedDamage = 0;
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
            capturedDamage = shot.Damage;
            ctx.Assert(health > capturedDamage, "The native player/NPC target survives the complete projectile");
        });
        await WaitFrames(ctx, () => health - GetCurrentHitPoints(target) == capturedDamage,
            "the captured ordinary projectile to remove exact native HP", 2f);
        ctx.AssertEqual(capturedDamage, health - GetCurrentHitPoints(target),
            "Physical application does not apply the already captured PvP ramp again");
        return capturedDamage;
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
                         PerkType.TwinPistolMomentum, PerkType.TwinPistolFollowThrough }) record.Perks[perk] = 1;
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
