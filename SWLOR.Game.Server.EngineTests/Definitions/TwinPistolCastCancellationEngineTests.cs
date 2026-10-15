using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class TwinPistolCastCancellationEngineTests
{
    [EngineTest("Twin Suns weapon swap cancels its unpaid cast without clearing ordinary traits", Category = "TwinPistolCancellation", TimeoutSeconds = 45f)]
    public static Task TwinSunsSwap(EngineTestContext ctx) => WeaponSwap(ctx, FeatType.TwinSuns, PerkType.TwinSuns);

    [EngineTest("Final Salvo weapon swap preserves captured Momentum and cancels payment and impact", Category = "TwinPistolCancellation", TimeoutSeconds = 45f)]
    public static Task FinalSalvoSwap(EngineTestContext ctx) => WeaponSwap(ctx, FeatType.FinalSalvo, PerkType.FinalSalvo);

    [EngineTest("Twin Suns native death cancels its unpaid cast and records exactly one Died interruption", Category = "TwinPistolCancellation", TimeoutSeconds = 45f)]
    public static Task TwinSunsDeath(EngineTestContext ctx) => DeathDuringCast(ctx, FeatType.TwinSuns, PerkType.TwinSuns);

    [EngineTest("Final Salvo native death cancels its unpaid cast and records exactly one Died interruption", Category = "TwinPistolCancellation", TimeoutSeconds = 45f)]
    public static Task FinalSalvoDeath(EngineTestContext ctx) => DeathDuringCast(ctx, FeatType.FinalSalvo, PerkType.FinalSalvo);

    [EngineTest("A native fatal-damage save cancels the Twin cast without recording a death", Category = "TwinPistolCancellation", TimeoutSeconds = 45f)]
    public static Task FinalSalvoFatalSave(EngineTestContext ctx) => DeathDuringCast(ctx, FeatType.FinalSalvo, PerkType.FinalSalvo, fatalSave: true);

    private static async Task DeathDuringCast(EngineTestContext ctx, FeatType feat, PerkType perk, bool fatalSave = false)
    {
        ctx.Assert(!CombatRecorder.IsRecording, "The death regression owns its recording");
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        var actor = player.Creature;
        CombatRecordWriter writer = null;
        var ability = Ability.GetAbilityDetail(feat);
        Combat.SetAutoAttackHitResolutionOverride(false);
        Combat.SetAbilityHitResolutionOverride(true);
        try
        {
            var (target, _, _, _) = await Prepare(ctx, player, perk);
            var health = GetCurrentHitPoints(target);
            var stamina = Stat.GetCurrentStamina(actor);
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                SetPlotFlag(actor, false);
                SetImmortal(actor, false);
                if (fatalSave)
                {
                    TemporaryStatModifier.Add(actor, StatType.FatalDamageTemporaryHPPercent, 20, 30f);
                    TemporaryStatModifier.Add(actor, StatType.FatalDamageTemporaryHPDurationSeconds, 30, 30f);
                    ObjectPlugin.SetCurrentHitPoints(actor, 1);
                    ctx.AssertEqual(1, GetCurrentHitPoints(actor), "Native fatal-save setup starts at one HP");
                    ctx.AssertEqual(0, TemporaryHitPointEffects.GetRemaining(actor), "No shield exists before the native dying event");
                }
                Ability.ClearLastCompletedAbilityImpactSummary(actor);
                ctx.Assert(CombatRecorder.Start(GetArea(actor), "Twin capstone death regression", out var message), message);
                writer = CombatRecorder.LastWriter;
                ctx.Assert(UsePerkFeat.TryUseAbility(actor, target, feat, GetLocation(target)),
                    $"The real two-second cast starts: {Ability.GetLastActivationDenialReason()}");
                ctx.Assert(TwinPistolAbility.HasActiveActivation(actor) && Activity.IsBusy(actor),
                    "The captured activation is unpaid and active before death");
                ctx.AssertEqual(stamina, Stat.GetCurrentStamina(actor), "The cast has not paid its stamina cost");
            });
            await ctx.DelaySecondsAsync(.25f);
            // Native effects run the death/dying events rather than calling the cancellation helper.
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
                ApplyEffectToObject(DurationType.Instant, fatalSave ? EffectDamage(2) : EffectDeath(), actor));
            await ctx.WaitUntilAsync(() => !Activity.IsBusy(actor) && (fatalSave
                    ? !GetIsDead(actor) && GetCurrentHitPoints(actor) > 0 && TemporaryHitPointEffects.GetRemaining(actor) > 0
                    : GetIsDead(actor)), 1f,
                "the native death/dying event to release the cast before its two-second deadline");
            ctx.Assert(!TwinPistolAbility.HasActiveActivation(actor), "The native event releases the captured Twin activation");
            ctx.AssertEqual(string.Empty, UsePerkFeat.GetActiveAbilityActivationId(actor),
                "The native event removes the exact shared activation");
            await ctx.DelaySecondsAsync(2.2f);
            ctx.AssertEqual(stamina, Stat.GetCurrentStamina(actor), "The canceled cast never pays stamina");
            ctx.Assert(!Recast.IsOnRecastDelay(actor, ability.RecastGroup).Item1, "The canceled cast starts no capstone recast");
            ctx.Assert(Ability.GetLastCompletedAbilityImpactSummary(actor) == null, "The canceled cast never resolves an impact");
            ctx.AssertEqual(health, GetCurrentHitPoints(target), "The canceled cast never damages its selected target");
        }
        finally
        {
            if (writer != null && CombatRecorder.IsRecording)
                CombatRecorder.Stop("Twin capstone death regression", out _);
            Cleanup(actor);
            ReviveRulesEngineTests.DestroyCorpsePlaceable(actor);
            ReviveRulesEngineTests.ReleaseBody(actor);
        }

        await ctx.WaitUntilAsync(() => writer.Completion.IsCompleted, 10f, "the owned death recording to finish");
        ctx.Assert(writer.Error == null, $"The death recording failed: {writer.Error}");
        var events = File.ReadAllLines(writer.EventsPath).Select(line =>
        {
            using var document = JsonDocument.Parse(line);
            return document.RootElement.Clone();
        }).ToArray();
        var casts = events.Where(e => e.GetProperty("type").GetString() == "ability_cast" &&
                                     e.GetProperty("feat_id").GetInt32() == (int)feat).ToArray();
        ctx.AssertEqual(1, casts.Length, "The recorder contains one real capstone activation");
        var casterId = casts[0].GetProperty("caster").GetInt32();
        var interruptions = events.Where(e => e.GetProperty("type").GetString() == "ability_interrupted" &&
                                             e.GetProperty("caster").GetInt32() == casterId).ToArray();
        ctx.AssertEqual(1, interruptions.Length, "Death and the canceled deadline produce exactly one interruption");
        var expectedCause = fatalSave ? "other" : "died";
        ctx.AssertEqual(expectedCause, interruptions[0].GetProperty("cause").GetString(), "The interruption preserves the settled death/dying outcome");
        ctx.Assert(!interruptions.Any(e => e.GetProperty("cause").GetString() == (fatalSave ? "died" : "other")),
            "Cleanup never relabels the settled death/dying outcome");
        ctx.Assert(!events.Any(e => (e.GetProperty("type").GetString() is "ability_cost" or "ability_impact") &&
                                  e.GetProperty("caster").GetInt32() == casterId),
            "The recording contains no capstone payment or impact after death");
        ctx.AssertEqual(1, writer.Completion.Result.GetParticipant(casterId).AbilityInterruptions[ability.Name][expectedCause],
            "The recorder summary counts the interruption once");
        File.Delete(writer.EventsPath);
        File.Delete(writer.SummaryPath);
    }

    private static async Task WeaponSwap(EngineTestContext ctx, FeatType feat, PerkType perk)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        var actor = player.Creature;
        Combat.SetAutoAttackHitResolutionOverride(false);
        Combat.SetAbilityHitResolutionOverride(true);
        try
        {
            var (target, main, companion, replacement) = await Prepare(ctx, player, perk);
            await ctx.ExecuteInCreatureContextAsync(actor, () => ActionAttack(target));
            await ctx.WaitUntilAsync(() => GetIsInCombat(actor) && GetAttackTarget(actor) == target,
                8f, "the real ordinary attack to establish combat before the cast");
            var stamina = 0;
            var health = GetCurrentHitPoints(target);
            var haste = 0;
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                SeedExistingTraits(actor, target);
                stamina = Stat.GetCurrentStamina(actor);
                haste = Stat.GetStatAdjustment(actor, StatType.AttackDelayReductionPercent);
                Ability.ClearLastCompletedAbilityImpactSummary(actor);
                ctx.Assert(UsePerkFeat.TryUseAbility(actor, target, feat, GetLocation(target)),
                    $"The real two-second cast starts: {Ability.GetLastActivationDenialReason()}");
                ctx.Assert(TwinPistolAbility.HasActiveActivation(actor) && Activity.IsBusy(actor), "The unpaid capture belongs to the active cast");
                ctx.AssertEqual(stamina, Stat.GetCurrentStamina(actor), "Starting the cast does not pay stamina");
                ctx.AssertEqual(3, StatusEffect.GetStatusEffect<MomentumStatusEffect>(actor)?.Charges ?? 0, "Starting the cast only captures Momentum");
            });
            await ctx.DelaySecondsAsync(.25f);
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                SwapToRifle(ctx, actor, main, companion, replacement);
                ctx.Assert(!TwinPistolAbility.HasActiveActivation(actor) && !Activity.IsBusy(actor), "The actual equipment event interrupts its captured cast");
                ctx.AssertEqual(string.Empty, UsePerkFeat.GetActiveAbilityActivationId(actor), "The canceled shared activation is removed");
                ctx.AssertEqual(3, StatusEffect.GetStatusEffect<MomentumStatusEffect>(actor)?.Charges ?? 0, "Swapping preserves the unspent captured Momentum");
                ctx.AssertEqual(5, PairedRangedTraits.GetOrdinaryDamagePercentAdjustment(actor, target), "Swapping preserves the repeated-target counter");
                ctx.Assert(StatusEffect.GetStatusEffect<AkimboStanceStatusEffect>(actor) != null, "Swapping preserves the cross-trained stance");
                ctx.AssertEqual(1, StatusEffect.GetStatusEffect<OrdinaryAttackHasteStatusEffect>(actor)?.Stacks ?? 0, "Swapping preserves the earned ordinary Haste stack");
                ctx.AssertEqual(haste, Stat.GetStatAdjustment(actor, StatType.AttackDelayReductionPercent), "The rifle retains the same global Haste");
                OrdinaryAttackTraits.RecordHit(actor, target, long.MaxValue - 10);
                ctx.AssertEqual(1, StatusEffect.GetStatusEffect<OrdinaryAttackHasteStatusEffect>(actor)?.Stacks ?? 0, "Swapping does not reset the ramp grant cooldown");
                ActionAttack(target);
            });
            await ctx.DelaySecondsAsync(2.2f);
            ctx.AssertEqual(stamina, Stat.GetCurrentStamina(actor), "The canceled deadline does not spend stamina");
            ctx.Assert(!Recast.IsOnRecastDelay(actor, Ability.GetAbilityDetail(feat).RecastGroup).Item1, "The canceled deadline starts no capstone recast");
            ctx.AssertEqual(3, StatusEffect.GetStatusEffect<MomentumStatusEffect>(actor)?.Charges ?? 0, "The canceled deadline spends no Momentum");
            ctx.Assert(Ability.GetLastCompletedAbilityImpactSummary(actor) == null, "The canceled deadline resolves no ability impact");
            ctx.AssertEqual(health, GetCurrentHitPoints(target), "With ordinary misses forced, the canceled cast deals no damage");
            ctx.Assert(StatusEffect.GetStatusEffect<TwinSunsStatusEffect>(actor) == null, "The canceled cast grants no ordinary-shot capstone bonus");
        }
        finally { Cleanup(actor); }
    }

    [EngineTest("A stale Twin capture cannot interrupt a later unrelated cast on a weapon swap", Category = "TwinPistolCancellation", TimeoutSeconds = 45f)]
    public static async Task StaleCapturePreservesUnrelatedCast(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        var actor = player.Creature;
        try
        {
            var (target, main, companion, replacement) = await Prepare(ctx, player, PerkType.FinalSalvo);
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                ctx.Assert(UsePerkFeat.TryUseAbility(actor, target, FeatType.FinalSalvo, GetLocation(target)),
                    $"The first cast starts: {Ability.GetLastActivationDenialReason()}");
                var oldId = UsePerkFeat.GetActiveAbilityActivationId(actor);
                ctx.Assert(!string.IsNullOrWhiteSpace(oldId), "The Twin capture has a real shared identity");
                ctx.Assert(UsePerkFeat.InterruptAbilityActivation(actor), "A generic interruption cancels the first shared cast");
                ctx.Assert(!TwinPistolAbility.HasActiveActivation(actor), "A stale capture is not an active cast");
                ctx.Assert(UsePerkFeat.TryUseAbility(actor, actor, FeatType.WatchfulPresence1, GetLocation(actor)),
                    $"The unrelated two-second cast starts: {Ability.GetLastActivationDenialReason()}");
                var newId = UsePerkFeat.GetActiveAbilityActivationId(actor);
                ctx.Assert(!string.IsNullOrWhiteSpace(newId) && newId != oldId, "The later cast owns a distinct identity");
                ctx.Assert(!UsePerkFeat.InterruptAbilityActivation(actor, oldId, CombatRecordInterruptCause.Died),
                    "A stale death cancellation cannot interrupt the newer shared activation");
                TwinPistolAbility.Cancel(actor, CombatRecordInterruptCause.Died);
                ctx.AssertEqual(newId, UsePerkFeat.GetActiveAbilityActivationId(actor),
                    "Death cleanup of the stale Twin capture preserves the unrelated activation");
                SwapToRifle(ctx, actor, main, companion, replacement);
                ctx.AssertEqual(newId, UsePerkFeat.GetActiveAbilityActivationId(actor), "Stale equipment cleanup does not interrupt the later cast");
                ctx.Assert(Activity.IsBusy(actor), "The unrelated cast remains busy");
            });
            await ctx.WaitUntilAsync(() => !Activity.IsBusy(actor) &&
                StatusEffect.GetStatusEffect<WatchfulPresence1StatusEffect>(actor) != null,
                5f, "the unrelated cast to complete its actual aura impact");
            ctx.Assert(Recast.IsOnRecastDelay(actor, Ability.GetAbilityDetail(FeatType.WatchfulPresence1).RecastGroup).Item1,
                "The unrelated cast completes its normal recast");
            ctx.Assert(!Recast.IsOnRecastDelay(actor, Ability.GetAbilityDetail(FeatType.FinalSalvo).RecastGroup).Item1,
                "The interrupted Twin cast remains unpaid and without recast");
        }
        finally { Cleanup(actor); }
    }

    private static async Task<(uint Target, uint Main, uint Companion, uint Replacement)> Prepare(
        EngineTestContext ctx, PlayerAbilityFixture player, PerkType perk)
    {
        var actor = player.Creature;
        player.Update(record =>
        {
            foreach (var skill in new[] { SkillType.TwinPistol, SkillType.Pistol, SkillType.Rifle, SkillType.Leadership })
                record.Skills[skill].Rank = 50;
            record.Perks[perk] = 1;
            record.Perks[PerkType.TwinPistolMomentum] = 1;
            record.Perks[PerkType.ConvergingFire] = 1;
            record.Perks[PerkType.WatchfulPresence] = 1;
            foreach (var requirement in Perk.GetPerkDetails(perk).PerkLevels.Values.SelectMany(level => level.Requirements).OfType<PerkRequirementQuest>())
                record.Quests[requirement.QuestId] = new PlayerQuest { TimesCompleted = 1 };
        });
        var main = await ctx.EquipItemAsync(actor, "b_twinpistol", InventorySlot.RightHand);
        var companion = await TwinPistolEquipmentEngineTests.WaitForPair(ctx, actor, main);
        var target = ctx.SpawnCreature("nw_rat001", 2f);
        await ctx.DelaySecondsAsync(1f);
        var replacement = OBJECT_INVALID;
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            replacement = CreateItemOnObject("b_rifle", actor);
            ctx.Track(replacement);
            ctx.Assert(GetIsObjectValid(replacement), "The replacement rifle is already owned before casting");
            CreaturePlugin.AddFeat(actor, perk == PerkType.TwinSuns ? FeatType.TwinSuns : FeatType.FinalSalvo);
            CreaturePlugin.AddFeat(actor, FeatType.WatchfulPresence1);
            SetEventScript(actor, EventScript.Creature_OnHeartbeat, "");
            SetEventScript(actor, EventScript.Creature_OnNotice, "");
            SetEventScript(target, EventScript.Creature_OnHeartbeat, "");
            SetEventScript(target, EventScript.Creature_OnNotice, "");
            SetEventScript(target, EventScript.Creature_OnMeleeAttacked, "");
            SetPlotFlag(target, false);
            ctx.SuppressNPCNaturalRegen(target);
            Stat.SetNPCMaxHitPoints(target, 10000, true);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 45f);
            SetCommandable(false, target);
            ctx.MakeHostile(target);
            ctx.AssertEqual(10000, GetCurrentHitPoints(target), "The target is alive with a measured native damage budget");
            ctx.Assert(TwinPistolEquipment.IsActivePair(actor), "The initial owned pair is valid");
            player.Update(record => record.Stamina = Stat.GetMaxStamina(actor));
        });
        return (target, main, companion, replacement);
    }

    private static void SeedExistingTraits(uint actor, uint target)
    {
        StatusEffect.ApplyPermanentStatusEffect<AkimboStanceStatusEffect>(actor, actor);
        // Controlled event setup isolates cancellation; movement generation has its own engine coverage.
        for (var index = 0; index < 3; index++)
        {
            var snapshot = PairedRangedTraits.BeginAbility(actor, true, false);
            PairedRangedTraits.CompleteAbility(actor, snapshot, true, 3f, true, 0, 0);
            PairedRangedTraits.RecordOrdinaryAttempt(actor, target, long.MaxValue - index);
            PairedRangedTraits.RecordOrdinaryHit(actor, target, long.MaxValue - index);
            OrdinaryAttackTraits.RecordHit(actor, target, long.MaxValue - index);
        }
    }

    private static void SwapToRifle(EngineTestContext ctx, uint actor, uint main, uint companion, uint replacement)
    {
        CreaturePlugin.RunUnequip(actor, main);
        CreaturePlugin.RunUnequip(actor, companion);
        ctx.Assert(CreaturePlugin.RunEquip(actor, replacement, InventorySlot.RightHand), "The actual weapon swap succeeds during casting");
        ctx.AssertEqual(replacement, GetItemInSlot(InventorySlot.RightHand, actor), "The newly equipped rifle is the current attack source");
        ctx.Assert(Combat.CanItemTriggerWeaponAbility(replacement, SkillType.TwinPistol), "The replacement stays eligible for cross-trained Twin abilities");
    }

    private static void Cleanup(uint actor)
    {
        Combat.SetAutoAttackHitResolutionOverride(null);
        Combat.SetAbilityHitResolutionOverride(null);
        if (!GetIsObjectValid(actor)) return;
        AssignCommand(actor, () => ClearAllActions());
        TwinPistolAbility.Cancel(actor);
        UsePerkFeat.InterruptAbilityActivation(actor);
        UsePerkFeat.ClearQueuedAbility(actor);
        PairedRangedAttackCycle.Cancel(actor);
        PairedRangedTraits.ClearActor(actor);
        OrdinaryAttackTraits.ClearActor(actor);
    }
}
