using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class SlamActivationEngineTests
{
    [EngineTest("Quest collector cleanup preserves newer busy activities", Category = "Slam", TimeoutSeconds = 60f)]
    public static async Task CollectorCleanupOwnership(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        const string questId = "engine_collector_cleanup";
        fixture.Update(record => record.Quests[questId] = new PlayerQuest { CurrentState = 1 });
        uint CreateCollector()
        {
            var collector = CreateObject(ObjectType.Placeable, "qst_item_collect", GetLocation(player));
            ctx.Track(collector);
            SetLocalObject(collector, "QUEST_PLAYER", player);
            SetLocalString(collector, "QUEST_ID", questId);
            return collector;
        }
        var active = CreateCollector();
        var stale = CreateCollector();
        var native = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp
            .GetGameObject(active).AsNWSPlaceable();
        native.OpenInventory(player);
        await ctx.WaitUntilAsync(() => Activity.IsBusy(player), 3f, "opening the active collector");
        await ctx.ExecuteInCreatureContextAsync(stale, Quest.CloseItemCollector);
        ctx.AssertEqual(ActivityStatusType.Quest, Activity.GetBusyType(player), "A stale collector cannot unlock the current collector.");
        native.CloseInventory(player);
        await ctx.WaitUntilAsync(() => !Activity.IsBusy(player), 3f, "closing the active collector to release busy state");

        active = CreateCollector();
        native = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(active).AsNWSPlaceable();
        native.OpenInventory(player);
        await ctx.WaitUntilAsync(() => Activity.IsBusy(player), 3f, "opening another collector");
        Activity.SetBusy(player, ActivityStatusType.UseItem);
        native.CloseInventory(player);
        await ctx.WaitFrameAsync();
        ctx.AssertEqual(ActivityStatusType.UseItem, Activity.GetBusyType(player), "Collector closure cannot unlock a newer item action.");
        Activity.ClearBusy(player);

        active = CreateCollector();
        native = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(active).AsNWSPlaceable();
        native.OpenInventory(player);
        await ctx.WaitUntilAsync(() => Activity.IsBusy(player), 3f, "opening the collector for timeout cleanup");
        await ctx.ExecuteInCreatureContextAsync(active, () => typeof(Quest)
            .GetMethod("DestroyItemCollector", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { active }));
        ctx.Assert(!Activity.IsBusy(player), "The timeout destruction path releases busy state without a client close event.");
    }

    [EngineTest("Slam is usable after a quest collector consumes its final item", Category = "Slam", TimeoutSeconds = 60f)]
    public static async Task QuestHandInReleasesPlayer(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        fixture.Update(record =>
        {
            record.Skills[SkillType.Staff].Rank = 50;
            record.Perks[PerkType.Slam] = 1;
        });
        await ctx.EquipItemAsync(player, "nw_wdbqs001", InventorySlot.RightHand);
        const string questId = "engine_slam_handin";
        var quest = new QuestBuilder().Create(questId, "Engine test hand-in")
            .AddState().SetStateJournalText("Turn in the medkit.").AddCollectItemObjective("nw_it_medkit001", 1)
            .AddState().SetStateJournalText("Finished.").Build()[questId];
        var cache = (Dictionary<string, QuestDetail>)typeof(Quest)
            .GetField("_quests", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        cache.Add(questId, quest);
        try
        {
            await ctx.ExecuteInCreatureContextAsync(player, () =>
                ctx.Assert(Quest.AcceptQuest(player, GetModule(), questId), "Accept the hand-in quest."));
            var collector = CreateObject(ObjectType.Placeable, "qst_item_collect", GetLocation(player));
            ctx.Track(collector);
            SetLocalObject(collector, "QUEST_OWNER", GetModule());
            SetLocalObject(collector, "QUEST_PLAYER", player);
            SetLocalString(collector, "QUEST_ID", questId);
            var native = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp
                .GetGameObject(collector).AsNWSPlaceable();
            native.OpenInventory(player);
            await ctx.WaitUntilAsync(() => Activity.IsBusy(player), 3f, "opening the quest collector to mark the player busy");
            var item = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(collector, () => item = CreateItemOnObject("nw_it_medkit001", collector));
            await ctx.WaitFrameAsync();
            ctx.Assert(GetIsObjectValid(item), "Create a valid hand-in item in the collector's script context.");
            await ctx.ExecuteInCreatureContextAsync(collector, () =>
            {
                native.m_oidLastDisturbed = player;
                native.m_oidInventoryDisturbItem = item;
                native.m_nInventoryDisturbType = (int)DisturbType.Added;
                ctx.AssertEqual(player, GetLastDisturbed(), "The real disturb event identifies the player.");
                ctx.AssertEqual(DisturbType.Added, GetInventoryDisturbType(), "The real disturb event identifies an item addition.");
                ctx.AssertEqual("nw_it_medkit001", GetResRef(GetInventoryDisturbItem()), "The hand-in item matches the objective.");
                Quest.DisturbItemCollector();
                var progress = DB.Get<SWLOR.Game.Server.Entity.Player>(fixture.Id).Quests[questId];
                ctx.AssertEqual(2, progress.CurrentState, "The final item advances the quest before the collector is destroyed.");
                ctx.Log($"After final item: collector={GetIsObjectValid(collector)}, busy={Activity.IsBusy(player)}, type={Activity.GetBusyType(player)}");
            });
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(collector), 3f, "the completed collector to disappear");
            ctx.Assert(!Activity.IsBusy(player), "Destroying a completed quest collector releases the player's busy state.");
            var target = ctx.SpawnCreature("nw_rat001", 2f);
            await ctx.WaitFrameAsync();
            SetAILevel(target, AILevel.VeryLow);
            ctx.MakeHostile(target);
            ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(5000), target, 120f);
            await ctx.ExecuteInCreatureContextAsync(player, () =>
                ctx.Assert(UsePerkFeat.TryUseAbility(player, target, FeatType.Slam1, GetLocation(target)),
                    $"Slam I works after hand-in: {Ability.GetLastActivationDenialReason()}"));
            await ctx.WaitUntilAsync(() => !Activity.IsBusy(player), 3f, "Slam I to finish before fixture cleanup");
        }
        finally
        {
            cache.Remove(questId);
        }
    }

    [EngineTest("Slam player activation releases busy state and spends stamina", Category = "Slam", TimeoutSeconds = 60f)]
    public static async Task PlayerActivation(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        fixture.Update(record =>
        {
            record.Skills[SkillType.Staff].Rank = 50;
            record.Perks[PerkType.Slam] = 1;
        });
        await ctx.EquipItemAsync(player, "nw_wdbqs001", InventorySlot.RightHand);
        var target = ctx.SpawnCreature("nw_rat001", 2f);
        await ctx.WaitFrameAsync();
        SetAILevel(target, AILevel.VeryLow);
        ctx.MakeHostile(target);
        ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(5000), target, 120f);
        var staminaBefore = Stat.GetCurrentStamina(player);
        var hpBefore = GetCurrentHitPoints(target);
        Ability.ClearLastCompletedAbilityImpactSummary(player);
        Combat.SetAbilityHitResolutionOverride(true);
        try
        {
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                ctx.Assert(!Activity.IsBusy(player), "A fresh player starts idle.");
                ctx.Assert(UsePerkFeat.TryUseAbility(player, target, FeatType.Slam1, GetLocation(target)),
                    $"Slam I starts: {Ability.GetLastActivationDenialReason()}");
            });
            await ctx.WaitUntilAsync(() => Ability.GetLastCompletedAbilityImpactSummary(player) != null,
                5f, "Slam I's player impact");
            ctx.Assert(!Activity.IsBusy(player), "Slam I releases busy state after impact.");
            ctx.Assert(Stat.GetCurrentStamina(player) < staminaBefore, "Slam I spends stamina.");
            ctx.Assert(GetCurrentHitPoints(target) < hpBefore, "Slam I damages its target.");
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                ctx.Assert(!UsePerkFeat.TryUseAbility(player, target, FeatType.Slam1, GetLocation(target)),
                    "A repeated Slam I is rejected during its cooldown.");
                ctx.Assert(!Ability.GetLastActivationDenialReason().Contains("busy"),
                    $"The cooldown rejection must not be busy: {Ability.GetLastActivationDenialReason()}");
            });
        }
        finally
        {
            Combat.SetAbilityHitResolutionOverride(null);
        }
    }
}
