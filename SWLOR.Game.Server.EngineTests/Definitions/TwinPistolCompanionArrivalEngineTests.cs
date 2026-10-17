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
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using ItemProperty = SWLOR.NWN.API.Engine.ItemProperty;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class TwinPistolCompanionArrivalEngineTests
{
    private static uint _actor = OBJECT_INVALID;
    private static uint _target = OBJECT_INVALID;
    private static int _damageEvents;
    private static readonly MethodInfo ResolveCompanion = typeof(PairedRangedAttackCycle)
        .GetMethod("ResolveCompanion", BindingFlags.NonPublic | BindingFlags.Static);

    [NWNEventHandler(ScriptName.OnCreatureDamagedAfter)]
    public static void ObserveDamage()
    {
        if (OBJECT_SELF == _target && GetLastDamager(OBJECT_SELF) == _actor && GetTotalDamageDealt() > 0)
            _damageEvents++;
    }

    [EngineTest("Companion ordinary rewards follow projectile arrival while attempted-shot charges remain at launch", Category = "TwinPistolCompanionArrival", TimeoutSeconds = 35f)]
    public static Task OrdinaryArrival(EngineTestContext ctx) => VerifyOrdinary(ctx, "arrival");

    [EngineTest("A companion projectile discarded by target death grants no landed-hit rewards", Category = "TwinPistolCompanionArrival", TimeoutSeconds = 35f)]
    public static Task OrdinaryTargetDeath(EngineTestContext ctx) => VerifyOrdinary(ctx, "death");

    [EngineTest("A companion projectile discarded by target area departure grants no landed-hit rewards", Category = "TwinPistolCompanionArrival", TimeoutSeconds = 35f)]
    public static Task OrdinaryTargetDeparture(EngineTestContext ctx) => VerifyOrdinary(ctx, "area");

    [EngineTest("The first landed companion consumes its queued cross-trained ability only at projectile arrival", Category = "TwinPistolCompanionArrival", TimeoutSeconds = 40f)]
    public static Task QueuedArrival(EngineTestContext ctx) => VerifyQueued(ctx, "arrival");

    [EngineTest("An old companion arrival cannot consume a replacement queue or its reservation", Category = "TwinPistolCompanionArrival", TimeoutSeconds = 40f)]
    public static Task QueuedReplacement(EngineTestContext ctx) => VerifyQueued(ctx, "replacement");

    [EngineTest("A cleared companion queue cannot impact when its old projectile arrives", Category = "TwinPistolCompanionArrival", TimeoutSeconds = 40f)]
    public static Task QueuedReset(EngineTestContext ctx) => VerifyQueued(ctx, "reset");

    [EngineTest("A weapon swap cancels only the old queued companion projectile", Category = "TwinPistolCompanionArrival", TimeoutSeconds = 40f)]
    public static Task QueuedWeaponSwap(EngineTestContext ctx) => VerifyQueued(ctx, "gear");

    [EngineTest("Target death releases a discarded companion reservation for the next queued hit", Category = "TwinPistolCompanionArrival", TimeoutSeconds = 40f)]
    public static Task QueuedTargetDeath(EngineTestContext ctx) => VerifyQueued(ctx, "death");

    [EngineTest("Target area departure releases a discarded companion reservation for the next queued hit", Category = "TwinPistolCompanionArrival", TimeoutSeconds = 40f)]
    public static Task QueuedTargetDeparture(EngineTestContext ctx) => VerifyQueued(ctx, "area");

    private static async Task VerifyOrdinary(EngineTestContext ctx, string outcome)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx, -.5f);
        var actor = player.Creature;
        // A real longer shot leaves room for the target's native death processing before
        // the half-flight sample; the production flight calculation remains unchanged.
        var (target, _, companion) = await Prepare(ctx, player, outcome == "death" ? 24f : 10f);
        var departure = outcome == "area" ? await DepartureLocation(ctx) : null;
        var health = GetCurrentHitPoints(target);
        var stamina = Stat.GetCurrentStamina(actor);
        var damage = 0;
        var launchedAt = 0L;
        var launchCompletedAt = 0L;
        var flight = 0f;
        var sampledAt = 0L;
        var sampledStamina = -1;
        var sampledPoints = -1;
        var sampledLeadership = -1;
        var sampledRare = -1;
        var sampledHaste = -1;
        var sampledConverging = true;
        try
        {
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                TemporaryStatModifier.Add(actor, StatType.PairedRangedOrdinaryHitRestoreCount, 1, 30f);
                TemporaryStatModifier.Add(actor, StatType.PairedRangedOrdinaryHitStaminaRestore, 1, 30f);
                TemporaryStatModifier.Add(actor, StatType.PairedRangedOrdinaryHitRestoreCooldownSeconds, 6, 30f);
                TemporaryStatModifier.Add(actor, StatType.PairedRangedRepeatedTargetRequiredHits, 1, 30f);
                TemporaryStatModifier.Add(actor, StatType.PairedRangedRepeatedTargetDamagePercent, 5, 30f);
                TemporaryStatModifier.Add(actor, StatType.PairedRangedRepeatedTargetDurationSeconds, 6, 30f);
                StatusEffect.ApplyPermanentStatusEffect<AkimboStanceStatusEffect>(actor, actor);
                GrantOrdinaryAttempts(ctx, actor);
                stamina = Stat.GetCurrentStamina(actor);
                launchedAt = Environment.TickCount64;
                flight = Launch(ctx, actor, target, companion);
                launchCompletedAt = Environment.TickCount64;
                ctx.Assert(outcome != "death" || flight >= .4f,
                    "The target's actual location provides the longer death-processing flight window");
                var shot = RangedWeaponAttackContext.GetRoll(actor, target, 0);
                ctx.Assert(shot is { Ordinary: true, Completed: true } && shot.Damage > 0,
                    "The actual resolver captures one completed ordinary companion shot");
                damage = shot.Damage;
                ctx.AssertEqual(5, StatusEffect.GetStatusEffect<TwinSunsStatusEffect>(actor)?.Attempts ?? 0,
                    "The attempted shot immediately consumes one Twin Suns charge");
                AssignCommand(GetModule(), () => DelayCommand(flight / 2f, () =>
                {
                    sampledStamina = Stat.GetCurrentStamina(actor);
                    sampledPoints = Points(actor, target, SkillType.TwinPistol);
                    sampledLeadership = Points(actor, target, SkillType.Leadership);
                    sampledRare = GetLocalInt(target, "RARE_BONUS_CHANCE");
                    sampledHaste = StatusEffect.GetStatusEffect<OrdinaryAttackHasteStatusEffect>(actor)?.Stacks ?? 0;
                    sampledConverging = StatusEffect.GetStatusEffect<ConvergingFireStatusEffect>(actor) != null;
                    sampledAt = Environment.TickCount64;
                }));
                DiscardTarget(ctx, actor, target, outcome, departure, launchedAt, flight);
            });
            await WaitFrames(ctx, () => sampledAt > 0, "the native half-flight reward sample");
            ctx.Log($"Ordinary {outcome} timing: resolverMs={launchCompletedAt - launchedAt}, " +
                    $"sampleMs={sampledAt - launchedAt}, flightMs={flight * 1000f:0.000}.");
            ctx.Assert(sampledAt < launchedAt + flight * 1000f,
                "Rewards are sampled inside the authoritative projectile flight window");
            ctx.AssertEqual(stamina, sampledStamina, "Alternating Fire restores no stamina before arrival");
            ctx.AssertEqual(0, sampledPoints, "The companion awards no Twin Pistol contribution before arrival");
            ctx.AssertEqual(0, sampledLeadership, "The companion awards no Leadership contribution before arrival");
            ctx.AssertEqual(0, sampledRare, "Loot bonuses are not marked before arrival");
            ctx.AssertEqual(0, sampledHaste, "Akimbo grants no ordinary-hit Haste before arrival");
            ctx.Assert(!sampledConverging, "Converging Fire grants no landed-hit progress before arrival");
            await ctx.DelaySecondsAsync(flight + .15f);
            var landed = outcome == "arrival";
            ctx.AssertEqual(stamina + (landed ? 1 : 0), Stat.GetCurrentStamina(actor), "Only an arriving hit restores stamina once");
            ctx.AssertEqual(landed ? 1 : 0, Points(actor, target, SkillType.TwinPistol), "Only an arriving hit grants its captured weapon contribution once");
            ctx.AssertEqual(landed ? 1 : 0, Points(actor, target, SkillType.Leadership), "Only an arriving hit grants Leadership once");
            ctx.AssertEqual(landed ? 42 : 0, GetLocalInt(target, "RARE_BONUS_CHANCE"), "Only an arriving hit marks loot bonuses");
            ctx.AssertEqual(landed ? 1 : 0, StatusEffect.GetStatusEffect<OrdinaryAttackHasteStatusEffect>(actor)?.Stacks ?? 0,
                "Only an arriving hit grants one Akimbo stack");
            ctx.AssertEqual(landed, StatusEffect.GetStatusEffect<ConvergingFireStatusEffect>(actor) != null,
                "Only an arriving hit advances Converging Fire");
            ctx.AssertEqual(landed ? 1 : 0, _damageEvents, "A discarded projectile produces no native damage callback");
            if (outcome != "death")
                ctx.AssertEqual(health - (landed ? damage : 0), GetCurrentHitPoints(target), "Only the captured arriving payload removes HP");
            ctx.AssertEqual(5, StatusEffect.GetStatusEffect<TwinSunsStatusEffect>(actor)?.Attempts ?? 0,
                "Arrival and discard never consume an extra attempted-shot charge");
        }
        finally { Cleanup(actor, target); }
    }

    private static async Task VerifyQueued(EngineTestContext ctx, string outcome)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx, -.5f);
        var actor = player.Creature;
        var (target, main, companion) = await Prepare(ctx, player);
        var departure = outcome == "area" ? await DepartureLocation(ctx) : null;
        var nextTarget = outcome is "replacement" or "death" or "area" ? ctx.SpawnCreature("nw_rat001", 10f, 1f) : OBJECT_INVALID;
        if (GetIsObjectValid(nextTarget))
        {
            // Do not let the newly spawned Defender attack the already-hostile original
            // target while its delayed spawn initialization settles.
            ctx.MakeHostile(nextTarget);
            SetCommandable(false, nextTarget);
            await ctx.DelaySecondsAsync(1f);
        }
        var health = GetCurrentHitPoints(target);
        var stamina = Stat.GetCurrentStamina(actor);
        var paidStamina = 0;
        var capturedMainDamage = 0;
        var launchedAt = 0L;
        var flight = 0f;
        var sampledAt = 0L;
        var sampledQueued = false;
        var sampledSummary = true;
        var sampledHealth = -1;
        var sampledLeadership = -1;
        var sampledRare = -1;
        CombatRecordWriter writer = null;
        try
        {
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                if (GetIsObjectValid(nextTarget)) PrepareTarget(ctx, nextTarget);
                GrantOrdinaryAttempts(ctx, actor);
                ctx.Assert(!CombatRecorder.IsRecording, "The queued arrival regression owns its recording");
                ctx.Assert(CombatRecorder.Start(GetArea(actor), "Queued companion arrival regression", out var message), message);
                writer = CombatRecorder.LastWriter;
                HostileAbilityDelay.EndForTesting(actor);
                ctx.Assert(UsePerkFeat.TryUseAbility(actor, target, FeatType.Headshot1, GetLocation(target)),
                    $"Headshot queues through its normal paid pipeline: {Ability.GetLastActivationDenialReason()}");
                paidStamina = Stat.GetCurrentStamina(actor);
                var cost = Ability.GetAbilityDetail(FeatType.Headshot1).Requirements.OfType<AbilityRequirementStamina>().Single().RequiredSTM;
                ctx.AssertEqual(stamina - cost, paidStamina, "The readied ability pays its authored cost exactly once at queue time");
                var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature();
                var defender = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(target).AsNWSObject();
                native.m_pcCombatRound.StartCombatRound(target);
                native.m_pcCombatRound.m_nCurrentAttack = 0;
                native.m_pcCombatRound.GetAttack(0).m_bRangedAttack = 1;
                Combat.SetAutoAttackHitResolutionOverride(false);
                native.ResolveAttackRoll(defender);
                ctx.AssertEqual(4, native.m_pcCombatRound.GetAttack(0).m_nAttackResult, "The real main launcher misses before the companion launches");
                ctx.Assert(UsePerkFeat.HasQueuedWeaponAbility(actor), "The main miss leaves Headshot readied");
                capturedMainDamage = WeaponDamage.GetEffectiveDMG(actor, main);
                Combat.SetAutoAttackHitResolutionOverride(true);
                launchedAt = Environment.TickCount64;
                flight = Launch(ctx, actor, target, companion);
                var shot = RangedWeaponAttackContext.GetRoll(actor, target, 0);
                ctx.Assert(shot is { Ordinary: false, Completed: true, Damage: 0 }, "The companion reserves one replacement without ordinary damage");
                ctx.AssertEqual(5, StatusEffect.GetStatusEffect<TwinSunsStatusEffect>(actor)?.Attempts ?? 0,
                    "Only the main miss consumes an ordinary attempt; the queued companion does not");
                AssignCommand(GetModule(), () => DelayCommand(flight / 2f, () =>
                {
                    sampledQueued = UsePerkFeat.HasQueuedWeaponAbility(actor);
                    sampledSummary = Ability.GetLastCompletedAbilityImpactSummary(actor) != null;
                    sampledHealth = GetCurrentHitPoints(target);
                    sampledLeadership = Points(actor, target, SkillType.Leadership);
                    sampledRare = GetLocalInt(target, "RARE_BONUS_CHANCE");
                    sampledAt = Environment.TickCount64;
                }));
                ReplaceProperty(main, ItemPropertyType.DMG, ItemPropertyCustom(ItemPropertyType.DMG, -1, 200));
                ReplaceProperty(main, ItemPropertyType.WeaponDamageType,
                    ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Fire));
                if (outcome == "replacement")
                {
                    Recast.ReduceRecastDelay(actor, RecastGroup.Headshot, 60f);
                    HostileAbilityDelay.EndForTesting(actor);
                    ctx.Assert(UsePerkFeat.TryUseAbility(actor, nextTarget, FeatType.Headshot1, GetLocation(nextTarget)),
                        "A new paid activation replaces the first queue while its projectile is in flight");
                    paidStamina = Stat.GetCurrentStamina(actor);
                    // Give the replacement its own real native reservation before the old arrival.
                    native.m_pcCombatRound.m_nCurrentAttack = 1;
                    native.m_pcCombatRound.GetAttack(1).m_bRangedAttack = 1;
                    native.ResolveAttackRoll(NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(nextTarget).AsNWSObject());
                }
                else if (outcome == "reset") UsePerkFeat.ClearQueuedAbility(actor);
                else if (outcome == "gear") CreaturePlugin.RunUnequip(actor, main);
                else DiscardTarget(ctx, actor, target, outcome, departure, launchedAt, flight);
            });
            await WaitFrames(ctx, () => sampledAt > 0, "the native half-flight queued sample");
            ctx.Assert(sampledAt < launchedAt + flight * 1000f, "The queued state is sampled before the captured projectile arrival deadline");
            ctx.AssertEqual(outcome is not "reset" and not "gear", sampledQueued, "Only an explicit queue reset or gear event clears readiness before arrival");
            ctx.Assert(!sampledSummary, "The launched replacement resolves no ability impact before arrival");
            if (outcome != "death") ctx.AssertEqual(health, sampledHealth, "The queued projectile removes no HP before arrival");
            ctx.AssertEqual(0, sampledLeadership, "The queued projectile grants no Leadership before arrival");
            ctx.AssertEqual(0, sampledRare, "The queued projectile marks no loot bonus before arrival");
            await ctx.DelaySecondsAsync(flight + .15f);
            ctx.AssertEqual(paidStamina, Stat.GetCurrentStamina(actor), "Arrival never pays the already readied ability again");
            if (outcome == "arrival")
            {
                var summary = Ability.GetLastCompletedAbilityImpactSummary(actor);
                ctx.Assert(summary != null && summary.AttributedDamage > 0 && summary.SkillType == SkillType.Rifle,
                    "One arriving companion resolves the original cross-trained ability");
                ctx.AssertEqual(capturedMainDamage, summary.TriggeringWeaponDamage ?? -1, "Arrival retains the launch-captured main gun damage");
                ctx.AssertEqual(CombatDamageType.Physical, summary.TriggeringWeaponDamageType ?? CombatDamageType.Force,
                    "Arrival retains the main gun type despite its in-flight Fire mutation");
                ctx.Assert(!UsePerkFeat.HasQueuedWeaponAbility(actor), "Arrival consumes the captured queue once");
                ctx.AssertEqual(3, Points(actor, target, SkillType.Rifle), "One queued impact awards its original skill contribution once");
                ctx.AssertEqual(1, Points(actor, target, SkillType.Leadership), "The arriving queued hit grants Leadership once");
                ctx.AssertEqual(42, GetLocalInt(target, "RARE_BONUS_CHANCE"), "The arriving queued hit marks its loot bonus");
                ctx.AssertEqual(0, Points(actor, target, SkillType.TwinPistol), "A replaced shot awards no ordinary Twin Pistol contribution");
                ctx.AssertEqual(1, _damageEvents, "One arriving replacement applies one native damage payload");
                await ctx.ExecuteInCreatureContextAsync(actor, () => UsePerkFeat.ResolveQueuedWeaponAbilityHit(actor, target, companion));
                ctx.AssertEqual(1, _damageEvents, "A repeated native callback cannot execute the consumed queue again");
            }
            else
            {
                ctx.Assert(Ability.GetLastCompletedAbilityImpactSummary(actor) == null, "The old projectile cannot resolve a canceled discarded or newer ability");
                ctx.AssertEqual(0, _damageEvents, "The old projectile deals no native ability damage");
                ctx.AssertEqual(0, Points(actor, target, SkillType.Leadership), "The old projectile grants no Leadership");
                ctx.AssertEqual(0, GetLocalInt(target, "RARE_BONUS_CHANCE"), "The old projectile marks no loot bonus");
                if (outcome == "replacement")
                {
                    ctx.Assert(UsePerkFeat.HasQueuedWeaponAbility(actor), "The replacement remains readied after the old arrival");
                    ctx.AssertEqual(0, Points(actor, nextTarget, SkillType.Rifle), "The old arrival cannot credit the newer reservation's target");
                }
                else if (outcome is "death" or "area")
                {
                    ctx.Assert(UsePerkFeat.HasQueuedWeaponAbility(actor), "A discarded projectile leaves the original paid ability readied");
                    await ctx.ExecuteInCreatureContextAsync(actor, () =>
                    {
                        UsePerkFeat.BeginWeaponAttackRoll(actor);
                        ctx.Assert(UsePerkFeat.HasQueuedWeaponAbility(actor, SkillType.TwinPistol, 0),
                            "Discard releases the captured reservation instead of blocking every later queued roll");
                        Launch(ctx, actor, nextTarget, companion);
                    });
                    await WaitFrames(ctx, () => Ability.GetLastCompletedAbilityImpactSummary(actor)?.AttributedDamage > 0,
                        "the next valid companion projectile to consume the retained queue");
                    ctx.Assert(!UsePerkFeat.HasQueuedWeaponAbility(actor), "The next valid hit consumes the retained queue");
                    ctx.AssertEqual(paidStamina, Stat.GetCurrentStamina(actor), "Retrying the retained queue charges no additional stamina");
                }
                else ctx.Assert(!UsePerkFeat.HasQueuedWeaponAbility(actor), "Explicit cancellation leaves no old queue readied");
            }
        }
        finally
        {
            if (writer != null && CombatRecorder.IsRecording) CombatRecorder.Stop("Queued companion arrival regression", out _);
            Cleanup(actor, target);
        }
        await ctx.WaitUntilAsync(() => writer.Completion.IsCompleted, 10f, "the queued arrival recording to finish");
        ctx.Assert(writer.Error == null, $"Queued arrival recording failed: {writer.Error}");
        var events = File.ReadAllLines(writer.EventsPath).Select(line =>
        {
            using var document = JsonDocument.Parse(line);
            return document.RootElement.Clone();
        }).ToArray();
        var recordedActor = events.First(e => e.GetProperty("type").GetString() == "ability_queued")
            .GetProperty("caster").GetInt32();
        var impactCount = outcome is "arrival" or "death" or "area" ? 1 : 0;
        ctx.AssertEqual(impactCount, events.Count(e => e.GetProperty("type").GetString() == "ability_impact" &&
                                                     e.GetProperty("caster").GetInt32() == recordedActor),
            "The recorder observes exactly the valid arriving queued impact");
        var damageEvents = events.Where(e => e.GetProperty("type").GetString() == "damage" &&
                                            e.GetProperty("source").ValueKind == JsonValueKind.Number &&
                                            e.GetProperty("source").GetInt32() == recordedActor).ToArray();
        ctx.AssertEqual(impactCount, damageEvents.Length, "The recorder contains no ordinary leak or canceled projectile payload");
        ctx.Assert(damageEvents.All(e => e.GetProperty("source_kind").GetString() == "ability" &&
                                       e.GetProperty("source_name").GetString() == "Headshot I"),
            "Delayed queued damage retains its original ability attribution");
    }

    private static async Task<(uint Target, uint Main, uint Companion)> Prepare(EngineTestContext ctx, PlayerAbilityFixture player,
        float targetOffset = 10f)
    {
        var actor = player.Creature;
        player.Update(record =>
        {
            record.Skills[SkillType.TwinPistol].Rank = 50;
            record.Skills[SkillType.Rifle].Rank = 50;
            record.Perks[PerkType.Headshot] = 1;
            record.Perks[PerkType.CreditFinder] = 1;
            record.Stamina = 100;
        });
        var main = await ctx.EquipItemAsync(actor, "b_twinpistol", InventorySlot.RightHand);
        var companion = await TwinPistolEquipmentEngineTests.WaitForPair(ctx, actor, main);
        var target = ctx.SpawnCreature("nw_rat001", targetOffset);
        await ctx.DelaySecondsAsync(1f);
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            ClearAllActions();
            SetEventScript(actor, EventScript.Creature_OnHeartbeat, "");
            SetEventScript(actor, EventScript.Creature_OnNotice, "");
            SetEventScript(actor, EventScript.Creature_OnMeleeAttacked, "");
            PrepareTarget(ctx, target);
            ReplaceProperty(main, ItemPropertyType.DMG, ItemPropertyCustom(ItemPropertyType.DMG, -1, 40));
            ReplaceProperty(companion, ItemPropertyType.DMG, ItemPropertyCustom(ItemPropertyType.DMG, -1, 80));
            ReplaceProperty(companion, ItemPropertyType.WeaponDamageType,
                ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Ice));
            CreaturePlugin.AddFeat(actor, FeatType.Headshot1);
            TemporaryStatModifier.Add(actor, StatType.RareItemFindChance, 42, 30f);
            Ability.ApplyAura(actor, typeof(DeflectingAuraStatusEffect), true, false, false);
            Ability.ClearLastCompletedAbilityImpactSummary(actor);
            ctx.Assert(TwinPistolEquipment.IsActivePair(actor), "The fixture uses a real generated companion pair");
        });
        _actor = actor;
        _target = target;
        _damageEvents = 0;
        Combat.SetAutoAttackHitResolutionOverride(true);
        Combat.SetAbilityHitResolutionOverride(true);
        return (target, main, companion);
    }

    private static void PrepareTarget(EngineTestContext ctx, uint target)
    {
        SetEventScript(target, EventScript.Creature_OnHeartbeat, "");
        SetEventScript(target, EventScript.Creature_OnNotice, "");
        SetEventScript(target, EventScript.Creature_OnMeleeAttacked, "");
        SetPlotFlag(target, false);
        ctx.SuppressNPCNaturalRegen(target);
        Stat.SetNPCMaxHitPoints(target, 20000, true);
        ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 30f);
        SetCommandable(false, target);
        TemporaryStatModifier.Add(target, StatType.RangedDeflection, -100, 30f);
        ctx.MakeHostile(target);
    }

    private static float Launch(EngineTestContext ctx, uint actor, uint target, uint companion)
    {
        ctx.Assert(ResolveCompanion != null, "The actual production companion resolver is available");
        var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature();
        var defender = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(target).AsNWSObject();
        var distance = GetDistanceBetween(actor, target);
        ctx.Assert(distance >= 9f && distance < native.MaxAttackRange(target),
            "The actual launch distance provides a native flight window inside weapon range");
        ctx.Log($"Companion launch: actor={GetPosition(actor)}, target={GetPosition(target)}, " +
                $"distance={distance:0.000}m, maximumRange={native.MaxAttackRange(target):0.000}m.");
        native.m_pcCombatRound.StartCombatRound(target);
        native.m_pcCombatRound.m_nCurrentAttack = 0;
        ctx.SeedRandom(1);
        // Use the existing native companion resolver without an unrelated main projectile.
        // The preflight sampler is registered in this same script before any asynchronous yield.
        ResolveCompanion.Invoke(null, new object[] { native, defender, WeaponShotProfile.Capture(actor, companion, SkillType.TwinPistol) });
        return PairedRangedPresentation.FlightSeconds(distance);
    }

    private static void GrantOrdinaryAttempts(EngineTestContext ctx, uint actor)
    {
        var bonus = PairedRangedTraits.BeginAbility(actor, false, false);
        PairedRangedTraits.CompleteAbility(actor, bonus, false, 0f, false, 0, 0, 6, 25, 12);
        ctx.AssertEqual(6, StatusEffect.GetStatusEffect<TwinSunsStatusEffect>(actor)?.Attempts ?? 0, "The fixture starts with six ordinary attempted-shot charges");
    }

    private static int Points(uint actor, uint target, SkillType skill)
    {
        var tracker = (Dictionary<uint, Dictionary<uint, Dictionary<SkillType, int>>>)typeof(CombatPoint)
            .GetField("_creatureCombatPointTracker", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        return tracker.TryGetValue(target, out var attackers) && attackers.TryGetValue(actor, out var skills)
            ? skills.GetValueOrDefault(skill) : 0;
    }

    private static async Task<SWLOR.NWN.API.Engine.Location> DepartureLocation(EngineTestContext ctx)
    {
        var quiet = await QuietArena.CreateAsync(ctx);
        var anchor = quiet.Spawn("nw_rat001", 0f, 0f);
        return GetLocation(anchor);
    }

    private static void DiscardTarget(EngineTestContext ctx, uint actor, uint target, string outcome,
        SWLOR.NWN.API.Engine.Location departure, long launchedAt, float flight)
    {
        if (outcome == "death")
            AssignCommand(GetModule(), () => ApplyEffectToObject(DurationType.Instant, EffectDeath(), target));
        else if (outcome == "area")
        {
            var launchArea = GetArea(actor);
            var destinationArea = GetAreaFromLocation(departure);
            ctx.Assert(GetIsObjectValid(destinationArea) && destinationArea != launchArea,
                "The private departure destination is a distinct actual area");
            // The target is paralyzed and uncommandable. Move it through the native object
            // API, as the weather fixture does, independently of its command state.
            ObjectPlugin.AddToArea(target, destinationArea, GetPositionFromLocation(departure));
            ctx.AssertEqual(destinationArea, GetArea(target), "The native target transfer reaches the destination area synchronously");
            ctx.Assert(GetArea(target) != launchArea, "The actual target area differs from the captured launch area");
            var transferredAt = Environment.TickCount64;
            ctx.Assert(transferredAt < launchedAt + flight * 1000f,
                "The target actually leaves before the captured projectile arrival deadline");
            ctx.Log($"Departure fixture: launchArea={launchArea:X}, targetArea={GetArea(target):X}, " +
                    $"transferMs={transferredAt - launchedAt}, flightMs={flight * 1000f}");
        }
    }

    private static void ReplaceProperty(uint weapon, ItemPropertyType type, ItemProperty replacement)
    {
        var properties = new List<ItemProperty>();
        for (var property = GetFirstItemProperty(weapon); GetIsItemPropertyValid(property); property = GetNextItemProperty(weapon))
            if (GetItemPropertyType(property) == type) properties.Add(property);
        foreach (var property in properties) RemoveItemProperty(weapon, property);
        AddItemProperty(DurationType.Permanent, replacement, weapon);
    }

    private static void Cleanup(uint actor, uint target)
    {
        _actor = _target = OBJECT_INVALID;
        Combat.SetAutoAttackHitResolutionOverride(null);
        Combat.SetAbilityHitResolutionOverride(null);
        if (GetIsObjectValid(actor))
        {
            AssignCommand(actor, () => ClearAllActions());
            UsePerkFeat.ClearQueuedAbility(actor);
            PairedRangedAttackCycle.Cancel(actor);
            PairedRangedTraits.ClearActor(actor);
            OrdinaryAttackTraits.ClearActor(actor);
            Ability.RemoveAura(actor, typeof(DeflectingAuraStatusEffect));
            StatusEffect.RemoveStatusEffect<AkimboStanceStatusEffect>(actor);
            TemporaryStatModifier.Clear(actor);
            HostileAbilityDelay.EndForTesting(actor);
        }
        ReviveRulesEngineTests.DestroyCorpsePlaceable(target);
        ReviveRulesEngineTests.ReleaseBody(target);
    }

    private static async Task WaitFrames(EngineTestContext ctx, Func<bool> condition, string description, float timeout = 4f)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeout);
        while (!condition() && DateTime.UtcNow < deadline) await ctx.WaitFrameAsync();
        ctx.Assert(condition(), $"Timed out waiting for {description}");
    }
}
