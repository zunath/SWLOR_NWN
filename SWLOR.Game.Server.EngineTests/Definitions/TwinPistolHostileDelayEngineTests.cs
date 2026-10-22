using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
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

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class TwinPistolHostileDelayEngineTests
{
    [EngineTest("Twin hostile delay holds Final Salvo unpaid and spends Flow only at its normal impact", Category = "TwinPistolHostileDelay", TimeoutSeconds = 45f)]
    public static Task HeldFinisher(EngineTestContext ctx) =>
        VerifyReplay(ctx, FeatType.FinalSalvo);

    [EngineTest("A later Twin hostile press replaces a held queued weapon ability without paying it", Category = "TwinPistolHostileDelay", TimeoutSeconds = 45f)]
    public static Task LaterTwinReplacesQueuedPress(EngineTestContext ctx) =>
        VerifyReplay(ctx, FeatType.Headshot1, replacement: FeatType.FocusedBurst1);

    [EngineTest("A held queued ability captures its weapon only on replay and consumes once from a Twin pair", Category = "TwinPistolHostileDelay", TimeoutSeconds = 45f)]
    public static Task HeldQueueCapturesOnReplay(EngineTestContext ctx) =>
        VerifyReplay(ctx, FeatType.Headshot1);

    [EngineTest("A nonhostile Twin stance starts during the hostile delay and keeps its normal cast time", Category = "TwinPistolHostileDelay", TimeoutSeconds = 45f)]
    public static async Task NonhostileStanceStartsWithoutHostileWaiting(EngineTestContext ctx)
    {
        ctx.Assert(!CombatRecorder.IsRecording, "The stance regression owns its recording");
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        var actor = player.Creature;
        var first = Ability.GetAbilityDetail(FeatType.PairedVolley1);
        var ability = Ability.GetAbilityDetail(FeatType.AkimboStance);
        CombatRecordWriter writer = null;
        var firstPressedAt = 0L;
        var initialStamina = 0;
        var afterFirst = 0;
        var castDelay = 0f;
        Combat.SetAutoAttackHitResolutionOverride(false);
        Combat.SetAbilityHitResolutionOverride(true);
        try
        {
            var (target, _) = await Prepare(ctx, player);
            await InActorFrame(ctx, actor, () =>
            {
                HostileAbilityDelay.EndForTesting(actor);
                initialStamina = Stat.GetCurrentStamina(actor);
                ctx.Assert(CombatRecorder.Start(GetArea(actor), "Twin stance hostile delay regression", out var message), message);
                writer = CombatRecorder.LastWriter;
                firstPressedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                Press(ctx, actor, target, FeatType.PairedVolley1);
            });
            await WaitFrames(ctx, () => OnRecast(actor, first.RecastGroup) && !Activity.IsBusy(actor),
                "the first Twin ability to finish its zero-delay native callback");
            await InActorFrame(ctx, actor, () =>
            {
                AssertOriginalWindow(ctx, actor, firstPressedAt);
                afterFirst = Stat.GetCurrentStamina(actor);
                ctx.AssertEqual(initialStamina - Cost(FeatType.PairedVolley1), afterFirst,
                    "The first ability pays its actual stamina requirement once");
                Ability.ClearLastCompletedAbilityImpactSummary(actor);
                castDelay = ability.ActivationDelay(actor, actor, 1);
                ctx.AssertEqual(2f, castDelay, "The stance retains its existing two-second cast");
                Press(ctx, actor, actor, FeatType.AkimboStance);
                ctx.Assert(HostileAbilityDelay.GetRemainingSeconds(actor) > 0,
                    "The stance starts inside the original hostile delay");
                ctx.Assert(Activity.IsBusy(actor) && !string.IsNullOrWhiteSpace(UsePerkFeat.GetActiveAbilityActivationId(actor)),
                    "The nonhostile stance begins its real cast immediately");
                ctx.Assert(StatusEffect.GetStatusEffect<AkimboStanceStatusEffect>(actor) == null,
                    "Starting the stance cast does not apply its status early");
                ctx.AssertEqual(afterFirst, Stat.GetCurrentStamina(actor), "Starting the free stance spends no stamina");
            });
            await WaitFrames(ctx, () => !Activity.IsBusy(actor) && StatusEffect.GetStatusEffect<AkimboStanceStatusEffect>(actor) != null,
                "the stance to complete its normal cast", 5f);
            ctx.AssertEqual(afterFirst, Stat.GetCurrentStamina(actor), "The completed free stance spends no stamina");
            ctx.Assert(OnRecast(actor, ability.RecastGroup), "The completed stance starts its usual recast");
        }
        finally
        {
            if (writer != null && CombatRecorder.IsRecording) CombatRecorder.Stop("Twin stance hostile delay regression", out _);
            UsePerkFeat.InterruptAbilityActivation(actor);
            if (GetIsObjectValid(actor)) AssignCommand(actor, () => ClearAllActions());
            PairedRangedAttackCycle.Cancel(actor);
            HostileAbilityDelay.EndForTesting(actor);
            Combat.SetAutoAttackHitResolutionOverride(null);
            Combat.SetAbilityHitResolutionOverride(null);
        }
        await WaitFrames(ctx, () => writer.Completion.IsCompleted, "the stance recording to finish", 10f);
        ctx.Assert(writer.Error == null, $"The stance recording failed: {writer.Error}");
        var events = File.ReadAllLines(writer.EventsPath).Select(line =>
        {
            using var document = JsonDocument.Parse(line);
            return document.RootElement.Clone();
        }).ToArray();
        var cast = events.Single(e => IsEvent(e, "ability_cast", FeatType.AkimboStance));
        var impact = events.Single(e => IsEvent(e, "ability_impact", FeatType.AkimboStance));
        ctx.Assert(cast.GetProperty("ts_ms").GetInt64() - firstPressedAt < HostileAbilityDelay.Seconds * 1000f,
            "The nonhostile cast starts before the hostile delay ends");
        // The delayed impact can land a frame before the wall-clock cast duration; allow 100ms of frame jitter.
        ctx.Assert(impact.GetProperty("ts_ms").GetInt64() - cast.GetProperty("ts_ms").GetInt64() >= castDelay * 1000f - 100f,
            "The status impact retains the stance's ordinary cast duration");
        ctx.AssertEqual(0, events.Single(e => IsEvent(e, "ability_cost", FeatType.AkimboStance)).GetProperty("stm").GetInt32(),
            "The stance has one normal zero-stamina payment");
        File.Delete(writer.EventsPath);
        File.Delete(writer.SummaryPath);
    }

    private static async Task VerifyReplay(EngineTestContext ctx, FeatType held,
        FeatType? replacement = null)
    {
        ctx.Assert(!CombatRecorder.IsRecording, "The hostile-delay regression owns its recording");
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        var actor = player.Creature;
        var winner = replacement ?? held;
        var first = Ability.GetAbilityDetail(FeatType.PairedVolley1);
        var winningAbility = Ability.GetAbilityDetail(winner);
        CombatRecordWriter writer = null;
        var firstPressedAt = 0L;
        var initialStamina = 0;
        var afterFirst = 0;
        var replayWeaponDamage = 0;
        Combat.SetAutoAttackHitResolutionOverride(false);
        Combat.SetAbilityHitResolutionOverride(true);
        try
        {
            var (target, main) = await Prepare(ctx, player);
            // Existing native cancellation fixtures establish combat before seeding transient traits.
            await ctx.ExecuteInCreatureContextAsync(actor, () => ActionAttack(target));
            await WaitFrames(ctx, () => GetIsInCombat(actor) && GetAttackTarget(actor) == target,
                "the real ordinary attack to establish combat", 8f);
            await InActorFrame(ctx, actor, () =>
            {
                HostileAbilityDelay.EndForTesting(actor);
                initialStamina = Stat.GetCurrentStamina(actor);
                ctx.Assert(CombatRecorder.Start(GetArea(actor), "Twin hostile delay regression", out var message), message);
                writer = CombatRecorder.LastWriter;
                firstPressedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                Press(ctx, actor, target, FeatType.PairedVolley1);
            });
            await WaitFrames(ctx, () => OnRecast(actor, first.RecastGroup) && !Activity.IsBusy(actor),
                "the first Twin ability to finish its zero-delay native callback");
            await InActorFrame(ctx, actor, () =>
            {
                AssertOriginalWindow(ctx, actor, firstPressedAt);
                afterFirst = Stat.GetCurrentStamina(actor);
                ctx.AssertEqual(initialStamina - Cost(FeatType.PairedVolley1), afterFirst, "The first ability pays its actual stamina requirement once");

                // Seed the actual Flow state through its existing event adapter; never begin or
                // capture the held ability ourselves before the shared pipeline replays it.
                PairedRangedTraits.RecordAccuracyEvasionMiss(actor);
                ctx.AssertEqual(1, Flow(actor), "The existing evasion event grants one real Flow charge");
                Ability.ClearLastCompletedAbilityImpactSummary(actor);
                Press(ctx, actor, target, held);
                AssertHeld(ctx, actor, afterFirst);

                if (replacement.HasValue)
                {
                    Press(ctx, actor, target, replacement.Value);
                    AssertHeld(ctx, actor, afterFirst);
                }
                if (winner == FeatType.Headshot1)
                {
                    var properties = new System.Collections.Generic.List<SWLOR.NWN.API.Engine.ItemProperty>();
                    for (var property = GetFirstItemProperty(main); GetIsItemPropertyValid(property); property = GetNextItemProperty(main))
                        if (GetItemPropertyType(property) == ItemPropertyType.DMG) properties.Add(property);
                    foreach (var property in properties) RemoveItemProperty(main, property);
                    AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DMG, -1, 80), main);
                    replayWeaponDamage = WeaponDamage.GetEffectiveDMG(actor, main);
                    ctx.AssertEqual(afterFirst, Stat.GetCurrentStamina(actor), "Changing the held input's weapon spends no ability cost");
                }
            });

            if (winner == FeatType.FinalSalvo)
            {
                await WaitFrames(ctx, () => TwinPistolAbility.HasActiveActivation(actor), "Final Salvo to begin after the shared delay");
                ctx.Assert(Activity.IsBusy(actor), "Replay begins the real two-second finisher cast");
                ctx.AssertEqual(afterFirst, Stat.GetCurrentStamina(actor), "Replaying the cast still waits for its normal payment boundary");
                ctx.AssertEqual(1, Flow(actor), "The replayed cast captures Flow without spending it early");
                ctx.Assert(!OnRecast(actor, winningAbility.RecastGroup), "The unpaid cast starts no capstone recast");
            }
            else if (winner == FeatType.Headshot1)
            {
                await WaitFrames(ctx, () => UsePerkFeat.IsWeaponAbilityQueued(actor, winner), "the held Headshot to queue after the shared delay");
                ctx.AssertEqual(afterFirst - Cost(winner), Stat.GetCurrentStamina(actor), "Replay pays the queued requirement exactly once");
                // Any hostile weapon ability that costs STM spends all Flow when its cost is paid.
                ctx.AssertEqual(0, Flow(actor), "The paid queued cross-skill ability spends the captured Flow");
                ctx.Assert(Ability.GetLastCompletedAbilityImpactSummary(actor) == null, "Queueing is not an impact");
                Combat.SetAutoAttackHitResolutionOverride(true);
                AssignCommand(actor, () => ActionAttack(target));
            }

            await WaitFrames(ctx, () => Ability.GetLastCompletedAbilityImpactSummary(actor) is { AttributedDamage: > 0 } summary &&
                summary.Ability == winningAbility && !UsePerkFeat.HasQueuedWeaponAbility(actor),
                "the winning replay to complete one actual impact", 8f);
            AssignCommand(actor, () => ClearAllActions());
            PairedRangedAttackCycle.Cancel(actor);
            if (winner == FeatType.Headshot1)
                ctx.AssertEqual(replayWeaponDamage,
                    Ability.GetLastCompletedAbilityImpactSummary(actor).TriggeringWeaponDamage ?? -1,
                    "The replayed queue captures the gun changed while input was held");
            await ctx.DelaySecondsAsync(1f);
            ctx.AssertEqual(afterFirst - Cost(winner), Stat.GetCurrentStamina(actor), "Held input replay has only one payment");
            // Twin builders such as Focused Burst keep Flow; finishers and other skills' weapon abilities spend it.
            ctx.AssertEqual(winner == FeatType.FocusedBurst1 ? 1 : 0, Flow(actor), "Only a Flow-spending winner spends the captured Flow");
            ctx.Assert(OnRecast(actor, winningAbility.RecastGroup), "The winner applies its ordinary recast");
            if (replacement.HasValue)
                ctx.Assert(!OnRecast(actor, Ability.GetAbilityDetail(held).RecastGroup), "The replaced queued press starts no recast");
        }
        finally
        {
            if (writer != null && CombatRecorder.IsRecording) CombatRecorder.Stop("Twin hostile delay regression", out _);
            if (GetIsObjectValid(actor)) AssignCommand(actor, () => ClearAllActions());
            TwinPistolAbility.Cancel(actor);
            UsePerkFeat.InterruptAbilityActivation(actor);
            UsePerkFeat.ClearQueuedAbility(actor);
            PairedRangedAttackCycle.Cancel(actor);
            PairedRangedTraits.ClearActor(actor);
            HostileAbilityDelay.EndForTesting(actor);
            Combat.SetAutoAttackHitResolutionOverride(null);
            Combat.SetAbilityHitResolutionOverride(null);
        }

        await WaitFrames(ctx, () => writer.Completion.IsCompleted, "the owned recording to finish", 10f);
        ctx.Assert(writer.Error == null, $"The hostile-delay recording failed: {writer.Error}");
        var events = File.ReadAllLines(writer.EventsPath).Select(line =>
        {
            using var document = JsonDocument.Parse(line);
            return document.RootElement.Clone();
        }).ToArray();
        var replay = events.Single(e => IsEvent(e, winner == FeatType.Headshot1 ? "ability_queued" : "ability_cast", winner));
        ctx.Assert(replay.GetProperty("ts_ms").GetInt64() - firstPressedAt >= HostileAbilityDelay.Seconds * 1000f,
            "The winning input is replayed only after the actual shared hostile deadline");
        ctx.AssertEqual(1, events.Count(e => IsEvent(e, "ability_cost", winner)), "The recorder contains one winning ability payment");
        ctx.AssertEqual(Cost(winner), events.Single(e => IsEvent(e, "ability_cost", winner)).GetProperty("stm").GetInt32(),
            "The recorded winning payment matches its requirement");
        // A channeled winner records its later volleys as recurring impacts of the same activation.
        ctx.AssertEqual(1, events.Count(e => IsEvent(e, "ability_impact", winner) && !e.GetProperty("recurring").GetBoolean()),
            "The recorder contains one winning ability activation impact");
        if (replacement.HasValue)
            ctx.Assert(!events.Any(e => e.TryGetProperty("feat_id", out var feat) && feat.GetInt32() == (int)held),
                "The replaced press never queues, pays, casts or impacts");
        File.Delete(writer.EventsPath);
        File.Delete(writer.SummaryPath);
    }

    private static void AssertHeld(EngineTestContext ctx, uint actor, int stamina)
    {
        ctx.Assert(HostileAbilityDelay.GetRemainingSeconds(actor) > 0, "The second press is inside the shared hostile delay");
        ctx.Assert(!TwinPistolAbility.HasActiveActivation(actor) && !Activity.IsBusy(actor), "Held input has not captured a Twin activation");
        ctx.Assert(!UsePerkFeat.HasQueuedWeaponAbility(actor), "Held input has not captured a queued weapon activation");
        ctx.AssertEqual(stamina, Stat.GetCurrentStamina(actor), "Held input spends no early stamina");
        ctx.AssertEqual(1, Flow(actor), "Held input spends no early Flow");
        ctx.Assert(Ability.GetLastCompletedAbilityImpactSummary(actor) == null, "Held input resolves no early impact");
    }

    private static async Task<(uint Target, uint Main)> Prepare(EngineTestContext ctx, PlayerAbilityFixture player)
    {
        var actor = player.Creature;
        var perks = new[] { PerkType.PairedVolley, PerkType.FocusedBurst, PerkType.FinalSalvo,
            PerkType.AkimboStance, PerkType.Headshot, PerkType.TwinPistolMomentum, PerkType.TwinPistolSecondWind };
        player.Update(record =>
        {
            foreach (var skill in new[] { SkillType.TwinPistol, SkillType.Rifle }) record.Skills[skill].Rank = 50;
            foreach (var perk in perks) record.Perks[perk] = 1;
            foreach (var quest in Perk.GetPerkDetails(PerkType.FinalSalvo).PerkLevels.Values.SelectMany(level => level.Requirements).OfType<PerkRequirementQuest>())
                record.Quests[quest.QuestId] = new PlayerQuest { TimesCompleted = 1 };
        });
        var main = await ctx.EquipItemAsync(actor, "b_twinpistol", InventorySlot.RightHand);
        await TwinPistolEquipmentEngineTests.WaitForPair(ctx, actor, main);
        var target = ctx.SpawnCreature("nw_rat001", 2f);
        await ctx.DelaySecondsAsync(1f);
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            foreach (var feat in new[] { FeatType.PairedVolley1, FeatType.FocusedBurst1, FeatType.FinalSalvo, FeatType.AkimboStance, FeatType.Headshot1 })
                CreaturePlugin.AddFeat(actor, feat);
            foreach (var creature in new[] { actor, target })
            {
                SetEventScript(creature, EventScript.Creature_OnHeartbeat, "");
                SetEventScript(creature, EventScript.Creature_OnNotice, "");
                SetEventScript(creature, EventScript.Creature_OnMeleeAttacked, "");
            }
            ctx.SuppressNPCNaturalRegen(target);
            Stat.SetNPCMaxHitPoints(target, 20000, true);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 45f);
            SetCommandable(false, target);
            TemporaryStatModifier.Add(target, StatType.RangedDeflection, -100, 45f);
            ctx.MakeHostile(target);
            player.Update(record => record.Stamina = Stat.GetMaxStamina(actor));
            Ability.ClearLastCompletedAbilityImpactSummary(actor);
        });
        return (target, main);
    }

    private static void Press(EngineTestContext ctx, uint actor, uint target, FeatType feat) =>
        ctx.Assert(UsePerkFeat.TryUseAbility(actor, target, feat, GetLocation(target)),
            $"{feat} is accepted by the real ability pipeline: {Ability.GetLastActivationDenialReason()}");

    private static int Flow(uint actor) => StatusEffect.GetStatusEffect<MomentumStatusEffect>(actor)?.Charges ?? 0;
    private static int Cost(FeatType feat) => Ability.GetAbilityDetail(feat).Requirements.OfType<AbilityRequirementStamina>().Single().RequiredSTM;
    private static bool OnRecast(uint actor, RecastGroup group) => Recast.IsOnRecastDelay(actor, group).Item1;
    private static bool IsEvent(JsonElement e, string type, FeatType feat) =>
        e.GetProperty("type").GetString() == type && e.TryGetProperty("feat_id", out var id) && id.GetInt32() == (int)feat;

    private static void AssertOriginalWindow(EngineTestContext ctx, uint actor, long firstPressedAt)
    {
        var elapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - firstPressedAt;
        var remaining = HostileAbilityDelay.GetRemainingSeconds(actor);
        ctx.Assert(elapsed >= 0 && elapsed < HostileAbilityDelay.Seconds * 1000f && remaining > 0,
            $"The second input batch is inside the original hostile delay: elapsed={elapsed}ms, remaining={remaining:0.000}s");
        ctx.Log($"Second input batch: {elapsed}ms after first press, {remaining:0.000}s hostile delay remains.");
    }

    // The standard context helper polls every 250ms. Input batches use native frames so the
    // first DelayCommand(0) can finish without consuming the half-second window under test.
    private static async Task InActorFrame(EngineTestContext ctx, uint actor, Action action)
    {
        var executed = false;
        Exception failure = null;
        AssignCommand(actor, () =>
        {
            try { action(); }
            catch (Exception exception) { failure = exception; }
            finally { executed = true; }
        });
        await WaitFrames(ctx, () => executed, "the actor's input batch to execute");
        if (failure != null) throw failure;
    }

    private static async Task WaitFrames(EngineTestContext ctx, Func<bool> condition, string description, float timeout = 4f)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeout);
        while (!condition() && DateTime.UtcNow < deadline) await ctx.WaitFrameAsync();
        ctx.Assert(condition(), $"Timed out waiting for {description}");
    }
}
