using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        Impacts.Add((Math.Max(0, GetDamageDealtByType(DamageType.BaseWeapon)) +
                     Math.Max(0, GetDamageDealtByType(DamageType.Slashing)),
            Math.Max(0, GetDamageDealtByType(DamageType.Force)),
            Math.Max(0, GetDamageDealtByType(DamageType.Fire)), Environment.TickCount64));
    }

    [EngineTest("Companion partial Force conversion and its reactions arrive with the captured projectile", Category = "TwinPistolConversion", TimeoutSeconds = 45f)]
    public static Task PartialConversion(EngineTestContext ctx) => VerifyArrival(ctx, 50);

    [EngineTest("A fully converted companion projectile arrives even with no remaining physical damage", Category = "TwinPistolConversion", TimeoutSeconds = 45f)]
    public static Task FullConversion(EngineTestContext ctx) => VerifyArrival(ctx, 100);

    [EngineTest("A lethal converted companion hit applies its captured physical share before Force", Category = "TwinPistolConversion", TimeoutSeconds = 45f)]
    public static Task LethalConversion(EngineTestContext ctx) => VerifyArrival(ctx, 75, lethalConverted: true);

    private static async Task VerifyArrival(EngineTestContext ctx, int conversionPercent, bool lethalConverted = false)
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
        var preflightHealth = 0;
        var preflightFP = 0;
        var preflightNotifications = -1;
        var preflightImpacts = -1;
        var preflightAt = 0L;
        var preflightDirect = -1;
        var preflightPreDamage = -1;
        var actorHealth = 0;
        var preflightActorHealth = 0;
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
                ReplaceProperty(companion, ItemPropertyType.DMG, ItemPropertyCustom(ItemPropertyType.DMG, -1, 200));
                ReplaceProperty(companion, ItemPropertyType.WeaponDamageType,
                    ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Physical));
                TemporaryStatModifier.Add(target, StatType.IncomingPhysicalToForceConversionPercent, conversionPercent, 60f);
                TemporaryStatModifier.Add(target, StatType.LeadershipForceDamageTakenPercentAdjustment, -20, 60f);
                TemporaryStatModifier.Add(target, StatType.LeadershipPhysicalDamageTakenPercentAdjustment, -20, 60f);
                TemporaryStatModifier.Add(target, StatType.RangedDeflection, -100, 60f);
                TemporaryStatModifier.Add(actor, StatType.CriticalRatePercentAdjustment, 100, 60f);
                TemporaryStatModifier.Add(actor, StatType.CriticalTargetFPLossPercentOfDamage, 10, 60f);
                if (!lethalConverted)
                    ApplyEffectToObject(DurationType.Temporary, EffectDamageImmunityIncrease(DamageType.Force, 50), target, 60f);
                if (conversionPercent == 100)
                    ApplyEffectToObject(DurationType.Temporary, EffectDamageImmunityIncrease(DamageType.Slashing, 100), target, 60f);
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 60f);
                SetCommandable(false, target);
                ctx.SetNPCResources(target, 100, 100);
                ctx.MakeHostile(target);
                ctx.Assert(StatusEffect.ApplyStatusEffect(actor, target, probe, 60f), "The native reaction probe is active");
                ctx.Assert(StatusEffect.ApplyStatusEffect(actor, actor, new BloodWeaponStatusEffect(), 60f), "A real direct-hit Blood Weapon rider is active");
                ObjectPlugin.SetCurrentHitPoints(actor, 10000);
                ctx.Assert(TwinPistolEquipment.IsActivePair(actor), "The fixture equips a real companion pair");
            });
            await ctx.DelaySecondsAsync(.4f);
            Impacts.Clear();
            _observedActor = actor;
            _observedTarget = target;
            Combat.SetAutoAttackHitResolutionOverride(true);
            var resolveCompanion = typeof(PairedRangedAttackCycle).GetMethod("ResolveCompanion", BindingFlags.NonPublic | BindingFlags.Static);
            ctx.Assert(resolveCompanion != null, "The existing production companion resolver is available");
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                ClearAllActions();
                var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature();
                var defender = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(target).AsNWSObject();
                var distance = GetDistanceBetween(actor, target);
                flightMilliseconds = PairedRangedPresentation.FlightSeconds(distance) * 1000f;
                ctx.Assert(distance >= 9f && distance < native.MaxAttackRange(target),
                    "The actual launch distance provides a visible flight window within range");
                health = GetCurrentHitPoints(target);
                fp = Stat.GetCurrentFP(target);
                actorHealth = GetCurrentHitPoints(actor);
                ctx.SeedRandom(1);
                ctx.Assert(CombatRecorder.Start(GetArea(actor), "Paired projectile conversion regression", out var message), message);
                writer = CombatRecorder.LastWriter;
                native.m_pcCombatRound.StartCombatRound(target);
                native.m_pcCombatRound.m_nCurrentAttack = 0;
                detectedAt = Environment.TickCount64;
                // Invoke only the existing companion resolver: real native rolls and projectile dispatch,
                // with no main projectile or asynchronous launch-detection race in the timing fixture.
                resolveCompanion.Invoke(null, new object[]
                {
                    native, defender, WeaponShotProfile.Capture(actor, companion, SkillType.TwinPistol)
                });
                var shot = RangedWeaponAttackContext.GetRoll(actor, target, 0);
                ctx.Assert(shot is { Completed: true } && shot.Weapon == companion,
                    "The production native companion roll completes synchronously");
                launchDamage = shot.Damage;
                ctx.AssertEqual(CombatDamageType.Physical, shot.DamageType, "The launched result captures its physical profile");
                if (conversionPercent == 100)
                    ctx.AssertEqual(0, launchDamage, "Full conversion leaves no physical payload to drive scheduling");
                else ctx.Assert(GameMath.PercentOf(launchDamage, 2) > 0, "Partial conversion retains enough physical damage to exercise Blood Weapon healing");
                if (lethalConverted)
                {
                    ObjectPlugin.SetCurrentHitPoints(target, launchDamage + 1);
                    health = GetCurrentHitPoints(target);
                }
                // Creature-context helpers poll every 250ms, longer than this flight. Capture the
                // real pre-arrival state on the native scheduler before yielding back to the test.
                AssignCommand(GetModule(), () => DelayCommand(flightMilliseconds / 2000f, () =>
                {
                    preflightHealth = GetCurrentHitPoints(target);
                    preflightFP = Stat.GetCurrentFP(target);
                    preflightNotifications = probe.Notifications;
                    preflightImpacts = Impacts.Count;
                    preflightDirect = probe.DirectNotifications;
                    preflightPreDamage = probe.PreDamageNotifications;
                    preflightActorHealth = GetCurrentHitPoints(actor);
                    preflightAt = Environment.TickCount64;
                }));
                // Mutation after launch must not re-type or recompute either portion in flight.
                ReplaceProperty(companion, ItemPropertyType.DMG, ItemPropertyCustom(ItemPropertyType.DMG, -1, 10));
                ReplaceProperty(companion, ItemPropertyType.WeaponDamageType,
                    ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Fire));
            });
            await ctx.WaitUntilAsync(() => preflightAt > 0, 1f, "the native preflight sample to execute");
            ctx.Assert(preflightAt < detectedAt + flightMilliseconds,
                "The preflight assertion samples the actual launch window before arrival");
            ctx.AssertEqual(health, preflightHealth, "Neither damage portion lands before the bolt arrives");
            ctx.AssertEqual(fp, preflightFP, "The converted reaction spends no FP before arrival");
            ctx.AssertEqual(0, preflightNotifications, "The converted hit dispatches no status reaction before arrival");
            ctx.AssertEqual(0, preflightImpacts, "No native damage event precedes projectile arrival");
            ctx.AssertEqual(0, preflightDirect, "No ordinary direct-hit notification precedes arrival");
            ctx.AssertEqual(0, preflightPreDamage, "No physical pre-damage reaction precedes arrival");
            ctx.AssertEqual(actorHealth, preflightActorHealth, "Blood Weapon cannot heal from a bolt still in flight");
            await ctx.WaitUntilAsync(() => probe.Notifications > 0 && (lethalConverted
                    ? GetIsDead(target) : Impacts.Any(impact => impact.Force > 0)),
                3f, "the converted projectile and its complete triggered reaction to arrive");
            await ctx.DelaySecondsAsync(.3f);
            ctx.AssertEqual(1, probe.Notifications, "The converted portion dispatches its reaction exactly once");
            ctx.AssertEqual(fp - 1 - (launchDamage > 0 ? GameMath.PercentOf(launchDamage, 10) : 0), Stat.GetCurrentFP(target),
                "The captured critical rider and converted reaction spend FP only at arrival");
            ctx.AssertEqual(conversionPercent == 100 ? 0 : 1, probe.DirectNotifications,
                "Partial conversion notifies one direct hit; full conversion retains the native Triggered-only contract");
            ctx.AssertEqual(conversionPercent == 100 ? 0 : 1, probe.PreDamageNotifications,
                "Physical pre-damage reactions preserve their original count");
            ctx.AssertEqual(launchDamage, probe.DirectDamage, "The direct callback preserves the remaining physical amount");
            ctx.AssertEqual(actorHealth + (launchDamage > 0 ? GameMath.PercentOf(launchDamage, 2) : 0), GetCurrentHitPoints(actor),
                "Blood Weapon heals exactly once from the same physical share as the native path");
            ctx.AssertEqual(launchDamage, Impacts.Sum(impact => impact.Physical), "The physical portion uses its captured final mitigation once");
            ctx.AssertEqual(0, Impacts.Sum(impact => impact.Fire), "Changing the weapon in flight creates no new Fire payload");
            var force = Impacts.Sum(impact => impact.Force);
            if (!lethalConverted)
                ctx.Assert(Math.Abs(force - probe.Damage * .5f) <= 1f,
                    "The native 50 percent Force immunity applies once to the converted, Leadership-mitigated portion");
            if (conversionPercent == 50)
                ctx.Assert(Math.Abs(probe.Damage - launchDamage) <= 1,
                    "Both equal split portions receive their equal typed Leadership reductions exactly once");
            if (lethalConverted) ctx.Assert(GetIsDead(target), "The converted portion kills only after physical damage leaves one HP");
            else ctx.AssertEqual(launchDamage + force, health - GetCurrentHitPoints(target), "Exactly the captured physical and converted Force portions remove HP");
            ctx.Assert(Impacts.All(impact => impact.At >= detectedAt + flightMilliseconds - 50f) &&
                       probe.NotifiedAt >= detectedAt + flightMilliseconds - 50f,
                "Damage and triggered effects share projectile arrival timing within one native frame");
            if (launchDamage > 0)
                ctx.Assert(probe.PreDamageAt >= detectedAt + flightMilliseconds - 50f &&
                           probe.DirectNotifiedAt >= detectedAt + flightMilliseconds - 50f,
                    "Physical pre/post hit effects share the same arrival deadline");
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
            StatusEffect.RemoveStatusEffect(actor, typeof(BloodWeaponStatusEffect), actor, false);
            ReviveRulesEngineTests.DestroyCorpsePlaceable(target);
            ReviveRulesEngineTests.ReleaseBody(target);
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
        var attack = events.Single(e => e.GetProperty("type").GetString() == "attack");
        ctx.AssertEqual("critical", attack.GetProperty("result").GetString(), "The seeded real native roll exercises critical resource timing");
        ctx.AssertEqual(conversionPercent == 100 ? 1 : 2, hits.Length, "The recorder contains exactly the projectile's native damage portions");
        ctx.Assert(hits.All(e => e.GetProperty("source_kind").GetString() == "autoattack" &&
                                 e.GetProperty("source_name").GetString() == nameof(SkillType.TwinPistol)),
            "Both delayed portions retain their captured Twin Pistol ordinary attack attribution");
        if (lethalConverted)
        {
            ctx.Assert(hits[0].GetProperty("damage").TryGetProperty("physical", out var physical) && physical.GetInt32() == launchDamage &&
                       hits[1].GetProperty("damage").TryGetProperty("force", out _),
                "The physical recording precedes the lethal converted Force recording");
            ctx.AssertEqual(1, hits[0].GetProperty("hp_after").GetInt32(), "The physical share leaves the target alive for Force");
        }
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
