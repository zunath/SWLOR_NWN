using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using ItemProperty = SWLOR.NWN.API.Engine.ItemProperty;
using DamageType = SWLOR.NWN.API.NWScript.Enum.DamageType;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class PairedRangedConversionEngineTests
{
    private static uint _observedTarget = OBJECT_INVALID;
    private static uint _observedActor = OBJECT_INVALID;
    private static readonly List<(int Physical, int Force, int Fire, long At)> Impacts = new();

    [NWNEventHandler(ScriptName.OnCreatureDamagedAfter)]
    public static void ObserveNativeImpact()
    {
        if (OBJECT_SELF != _observedTarget || GetLastDamager(OBJECT_SELF) != _observedActor) return;
        Impacts.Add((Math.Max(0, GetDamageDealtByType(DamageType.Slashing)),
            Math.Max(0, GetDamageDealtByType(DamageType.Force)),
            Math.Max(0, GetDamageDealtByType(DamageType.Fire)), Environment.TickCount64));
    }

    [EngineTest("Companion partial Force conversion and its reactions arrive with the captured projectile", Category = "TwinPistolConversion", TimeoutSeconds = 45f)]
    public static Task PartialConversion(EngineTestContext ctx) => VerifyArrival(ctx, 50);

    [EngineTest("A fully converted companion projectile arrives even with no remaining physical damage", Category = "TwinPistolConversion", TimeoutSeconds = 45f)]
    public static Task FullConversion(EngineTestContext ctx) => VerifyArrival(ctx, 100);

    private static async Task VerifyArrival(EngineTestContext ctx, int conversionPercent)
    {
        ctx.Assert(!CombatRecorder.IsRecording, "The conversion regression owns its recording");
        var actor = ctx.SpawnCreature("nw_bandit001", -.5f);
        var target = ctx.SpawnCreature("nw_rat001", 10f);
        await ctx.DelaySecondsAsync(1f);
        var main = await ctx.EquipItemAsync(actor, "b_twinpistol", InventorySlot.RightHand);
        var companion = await TwinPistolEquipmentEngineTests.WaitForPair(ctx, actor, main);
        var probe = new ProjectileConversionProbeStatusEffect();
        CombatRecordWriter writer = null;
        var launchDamage = 0;
        var detectedAt = 0L;
        var flightMilliseconds = 0f;
        var health = 0;
        var fp = 0;
        try
        {
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                foreach (var creature in new[] { actor, target })
                {
                    SetEventScript(creature, EventScript.Creature_OnHeartbeat, "");
                    SetEventScript(creature, EventScript.Creature_OnNotice, "");
                    SetEventScript(creature, EventScript.Creature_OnMeleeAttacked, "");
                    ctx.SuppressNPCNaturalRegen(creature);
                    Stat.SetNPCMaxHitPoints(creature, 20000, true);
                }
                ReplaceProperty(companion, ItemPropertyType.DMG, ItemPropertyCustom(ItemPropertyType.DMG, -1, 40));
                ReplaceProperty(companion, ItemPropertyType.WeaponDamageType,
                    ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Physical));
                TemporaryStatModifier.Add(target, StatType.IncomingPhysicalToForceConversionPercent, conversionPercent, 60f);
                TemporaryStatModifier.Add(target, StatType.LeadershipForceDamageTakenPercentAdjustment, -20, 60f);
                TemporaryStatModifier.Add(target, StatType.LeadershipPhysicalDamageTakenPercentAdjustment, -20, 60f);
                TemporaryStatModifier.Add(target, StatType.RangedDeflection, -100, 60f);
                ApplyEffectToObject(DurationType.Temporary, EffectDamageImmunityIncrease(DamageType.Force, 50), target, 60f);
                if (conversionPercent == 100)
                    ApplyEffectToObject(DurationType.Temporary, EffectDamageImmunityIncrease(DamageType.Slashing, 100), target, 60f);
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 60f);
                SetCommandable(false, target);
                ctx.SetNPCResources(target, 100, 100);
                ctx.MakeHostile(target);
                ctx.Assert(StatusEffect.ApplyStatusEffect(actor, target, probe, 60f), "The native reaction probe is active");
                ctx.Assert(TwinPistolEquipment.IsActivePair(actor), "The fixture equips a real companion pair");
            });
            await ctx.DelaySecondsAsync(.4f);
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature();
            var distance = GetDistanceBetween(actor, target);
            flightMilliseconds = PairedRangedPresentation.FlightSeconds(distance) * 1000f;
            ctx.Assert(distance >= 9f && distance < native.MaxAttackRange(target), "The firing lane provides a visible flight window within range");
            health = GetCurrentHitPoints(target);
            fp = Stat.GetCurrentFP(target);
            Impacts.Clear();
            _observedActor = actor;
            _observedTarget = target;
            ctx.SeedRandom(20261015);
            ctx.Assert(CombatRecorder.Start(GetArea(actor), "Paired projectile conversion regression", out var message), message);
            writer = CombatRecorder.LastWriter;
            var initial = native.m_pcCombatRound.m_nCurrentAttack;
            Combat.SetAutoAttackHitResolutionOverride(false);
            await ctx.ExecuteInCreatureContextAsync(actor, () => ActionAttack(target));
            await ctx.WaitUntilAsync(() => native.m_pcCombatRound.m_nCurrentAttack != initial, 6f,
                "the native main hand to miss so only the companion can deal damage");
            Combat.SetAutoAttackHitResolutionOverride(true);
            var deadline = Environment.TickCount64 + 3000;
            RangedWeaponAttackContext.Roll shot;
            while ((shot = RangedWeaponAttackContext.GetRoll(actor, target, native.m_pcCombatRound.m_nCurrentAttack)) is not
                   { Completed: true } || shot.Weapon != companion)
            {
                ctx.Assert(Environment.TickCount64 < deadline, "The companion completes its native launch roll");
                await ctx.WaitFrameAsync();
            }
            detectedAt = Environment.TickCount64;
            launchDamage = shot.Damage;
            ctx.AssertEqual(CombatDamageType.Physical, shot.DamageType, "The launched result captures its physical profile");
            if (conversionPercent == 100)
                ctx.AssertEqual(0, launchDamage, "Full conversion leaves no physical payload to drive scheduling");
            else ctx.Assert(launchDamage > 0, "Partial conversion retains a physical payload");
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                ClearAllActions();
                // Mutation after launch must not re-type or recompute either portion in flight.
                ReplaceProperty(companion, ItemPropertyType.DMG, ItemPropertyCustom(ItemPropertyType.DMG, -1, 200));
                ReplaceProperty(companion, ItemPropertyType.WeaponDamageType,
                    ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Fire));
            });
            await ctx.DelaySecondsAsync(flightMilliseconds / 2000f);
            ctx.AssertEqual(health, GetCurrentHitPoints(target), "Neither damage portion lands before the bolt arrives");
            ctx.AssertEqual(fp, Stat.GetCurrentFP(target), "The converted reaction spends no FP before arrival");
            ctx.AssertEqual(0, probe.Notifications, "The converted hit dispatches no status reaction before arrival");
            ctx.AssertEqual(0, Impacts.Count, "No native damage event precedes projectile arrival");
            await ctx.WaitUntilAsync(() => Impacts.Any(impact => impact.Force > 0) && probe.Notifications > 0,
                3f, "the converted projectile and its complete triggered reaction to arrive");
            await ctx.DelaySecondsAsync(.3f);
            ctx.AssertEqual(1, probe.Notifications, "The converted portion dispatches its reaction exactly once");
            ctx.AssertEqual(fp - 1, Stat.GetCurrentFP(target), "The test-only reaction spends exactly one FP at arrival");
            ctx.AssertEqual(launchDamage, Impacts.Sum(impact => impact.Physical), "The physical portion uses its captured final mitigation once");
            ctx.AssertEqual(0, Impacts.Sum(impact => impact.Fire), "Changing the weapon in flight creates no new Fire payload");
            var force = Impacts.Sum(impact => impact.Force);
            ctx.Assert(Math.Abs(force - probe.Damage * .5f) <= 1f,
                "The native 50 percent Force immunity applies once to the converted, Leadership-mitigated portion");
            if (conversionPercent == 50)
                ctx.Assert(Math.Abs(probe.Damage - launchDamage) <= 1,
                    "Both equal split portions receive their equal typed Leadership reductions exactly once");
            ctx.AssertEqual(launchDamage + force, health - GetCurrentHitPoints(target), "Exactly the captured physical and converted Force portions remove HP");
            ctx.Assert(Impacts.All(impact => impact.At >= detectedAt + flightMilliseconds - 50f) &&
                       probe.NotifiedAt >= detectedAt + flightMilliseconds - 50f,
                "Damage and triggered effects share projectile arrival timing within one native frame");
            if (launchDamage > 0)
                ctx.Assert(Impacts.Max(impact => impact.At) - Impacts.Min(impact => impact.At) <= 100,
                    "Both native damage portions arrive together");
            ctx.Log($"Converted companion {conversionPercent}%: flightMs={flightMilliseconds}, physical={launchDamage}, " +
                    $"convertedBeforeNativeImmunity={probe.Damage}, forceApplied={force}, " +
                    $"firstImpactMs={Impacts.Min(impact => impact.At) - detectedAt}, reactionMs={probe.NotifiedAt - detectedAt}");
        }
        finally
        {
            if (writer != null && CombatRecorder.IsRecording) CombatRecorder.Stop("Paired projectile conversion regression", out _);
            if (GetIsObjectValid(actor)) AssignCommand(actor, () => ClearAllActions());
            PairedRangedAttackCycle.Cancel(actor);
            Combat.SetAutoAttackHitResolutionOverride(null);
            StatusEffect.RemoveStatusEffect(target, typeof(ProjectileConversionProbeStatusEffect), actor, false);
            _observedActor = OBJECT_INVALID;
            _observedTarget = OBJECT_INVALID;
        }
        await ctx.WaitUntilAsync(() => writer.Completion.IsCompleted, 10f, "the conversion recording to finish");
        ctx.Assert(writer.Error == null, $"The conversion recording failed: {writer.Error}");
        var events = File.ReadAllLines(writer.EventsPath).Select(line =>
        {
            using var document = JsonDocument.Parse(line);
            return document.RootElement.Clone();
        }).ToArray();
        var hits = events.Where(e => e.GetProperty("type").GetString() == "damage").ToArray();
        ctx.AssertEqual(conversionPercent == 100 ? 1 : 2, hits.Length, "The recorder contains exactly the projectile's native damage portions");
        ctx.Assert(hits.All(e => e.GetProperty("source_kind").GetString() == "autoattack" &&
                                 e.GetProperty("source_name").GetString() == nameof(SkillType.TwinPistol)),
            "Both delayed portions retain their captured Twin Pistol ordinary attack attribution");
        File.Delete(writer.EventsPath);
        File.Delete(writer.SummaryPath);
        Impacts.Clear();
    }

    private static void ReplaceProperty(uint weapon, ItemPropertyType type, ItemProperty replacement)
    {
        var old = new List<ItemProperty>();
        for (var property = GetFirstItemProperty(weapon); GetIsItemPropertyValid(property); property = GetNextItemProperty(weapon))
            if (GetItemPropertyType(property) == type) old.Add(property);
        foreach (var property in old) RemoveItemProperty(weapon, property);
        AddItemProperty(DurationType.Permanent, replacement, weapon);
    }
}
