using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class QuestCollectorEngineTests
{
    [EngineTest("Story and guild collection stages can all open and cancel their collectors", Category = "QuestCollectors", TimeoutSeconds = 900f)]
    public static async Task RegisteredCollectionStages(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var quests = (Dictionary<string, QuestDetail>)typeof(Quest)
            .GetField("_quests", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        var stages = quests.Values.SelectMany(quest => quest.States
            .Where(state => state.Value.GetObjectives().OfType<CollectItemObjective>().Any())
            .Select(state => (Quest: quest, State: state.Key, Detail: state.Value))).ToArray();
        ctx.Assert(stages.Length > 0, "The sweep covers the server's registered quest definitions.");
        var checkedStages = 0;
        foreach (var stage in stages)
        {
            var questId = stage.Quest.QuestId;
            fixture.Update(record =>
            {
                record.Quests.Clear();
                var progress = new PlayerQuest { CurrentState = stage.State };
                stage.Detail.ReconcileProgress(progress);
                record.Quests.Add(questId, progress);
            });
            // DB.Get returns cached mutable entities, so preserve values rather than an alias.
            var before = DB.Get<Player>(fixture.Id).Quests[questId];
            var beforeState = before.CurrentState;
            var beforeItems = before.ItemProgresses.OrderBy(x => x.Key).ToArray();
            var beforeKills = before.KillProgresses.OrderBy(x => x.Key).ToArray();
            var beforeCompletions = before.TimesCompleted;
            var collector = await OpenCollector(ctx, fixture.Creature, GetModule(), questId);
            ctx.AssertEqual(ActivityStatusType.Quest, Activity.GetBusyType(fixture.Creature), $"{questId}/{stage.State} owns the busy state.");
            CloseCollector(collector, fixture.Creature);
            await ctx.WaitFrameAsync();
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(collector), 3f, $"{questId}/{stage.State} cancellation cleanup");
            ctx.Assert(!Activity.IsBusy(fixture.Creature), $"{questId}/{stage.State} cancellation releases busy state.");
            var after = DB.Get<Player>(fixture.Id).Quests[questId];
            ctx.AssertEqual(beforeState, after.CurrentState, $"{questId} cancellation preserves the stage.");
            ctx.Assert(beforeItems.SequenceEqual(after.ItemProgresses.OrderBy(x => x.Key)),
                $"{questId} cancellation preserves item progress.");
            ctx.Assert(beforeKills.SequenceEqual(after.KillProgresses.OrderBy(x => x.Key)),
                $"{questId} cancellation preserves kill progress.");
            ctx.AssertEqual(beforeCompletions, after.TimesCompleted, $"{questId} cancellation does not complete the quest.");
            if (++checkedStages % 50 == 0)
                ctx.Log($"Checked {checkedStages}/{stages.Length} registered collection stages.");
        }
        ctx.Log($"Verified {stages.Length} collection stages in {stages.Select(x => x.Quest.QuestId).Distinct().Count()} quests; {quests.Count} quests registered in total.");
    }

    [EngineTest("NPC hand-in resumes authored dialogue and grants its reward once", Category = "QuestCollectors", TimeoutSeconds = 60f)]
    public static async Task NpcDialogueAndReward(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        var npc = ctx.SpawnCreature("civilian", 1f);
        ObjectPlugin.SetDialogResref(npc, "crystal");
        var windows = InstallConversationWindow(fixture);
        var cdKey = GetPCPublicCDKey(player);
        var originalAccount = DB.Get<Account>(cdKey);
        DB.Set(new Account(cdKey));
        fixture.Update(record => record.Settings.DisplayAchievementNotification = false);
        const string questId = "daggers_crystal";
        try
        {
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                ctx.Assert(Quest.AcceptQuest(player, npc, questId), "Accept the real Weapons for Krystalle quest.");
                ctx.Assert(Conversation.TryStartAssigned(player, npc), "Open the NPC's authored hand-in dialogue.");
                SelectQuestAction(ctx, GetSession(player), "action-request-quest-items", questId);
                Conversation.End(player);
            });
            var collector = await FindAndOpenCollector(ctx, player, questId);
            ctx.AssertEqual(npc, GetLocalObject(collector, "QUEST_OWNER"), "The conversation snippet preserves the NPC owner.");
            await GiveItem(ctx, collector, player, "b_pistol", 1);
            ctx.AssertEqual(2, DB.Get<Player>(fixture.Id).Quests[questId].ItemProgresses["b_pistol"], "A partial hand-in records only the submitted pistol.");
            ctx.Assert(Activity.IsBusy(player), "A partial hand-in keeps the collector busy.");
            CloseCollector(collector, player);
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(collector), 3f, "partial hand-in cancellation");
            ctx.Assert(!Activity.IsBusy(player), "Cancelling a partial hand-in releases busy state.");
            collector = await OpenCollector(ctx, player, npc, questId);
            await GiveItem(ctx, collector, player, "b_pistol", 1);
            await GiveItem(ctx, collector, player, "b_pistol", 1);
            ctx.Assert(GetIsObjectValid(collector) && Activity.IsBusy(player), "The collector stays open while spears remain.");
            await GiveItem(ctx, collector, player, "b_spear", 1);
            await GiveItem(ctx, collector, player, "b_spear", 1);
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(collector), 3f, "completed NPC collector destruction");
            ctx.Assert(!Activity.IsBusy(player), "The final item releases busy state before resumed dialogue.");
            ctx.AssertEqual(2, DB.Get<Player>(fixture.Id).Quests[questId].CurrentState, "Both objectives advance the real quest.");
            var session = GetSession(player);
            ctx.Assert(!session.HasEnded, "The final item opens a new NPC conversation session.");
            ctx.Assert(Gui.IsWindowOpen(player, GuiWindowType.Conversation), "The engine created the resumed conversation window.");
            var rewardsBefore = CountItems(player, "p_crystal_red_qs");
            var xpBefore = DB.Get<Player>(fixture.Id).UnallocatedXP;
            await ctx.ExecuteInCreatureContextAsync(player, () =>
                SelectQuestAction(ctx, session, "action-advance-quest", questId));
            ctx.AssertEqual(1, DB.Get<Player>(fixture.Id).Quests[questId].TimesCompleted, "The reward choice completes the quest once.");
            ctx.AssertEqual(rewardsBefore + 1, CountItems(player, "p_crystal_red_qs"), "Krystalle's real item reward reaches the inventory.");
            ctx.AssertEqual(xpBefore + 4000, DB.Get<Player>(fixture.Id).UnallocatedXP, "Krystalle's real XP reward is paid.");
            ctx.AssertEqual(1UL, DB.Get<Account>(cdKey).AchievementProgress.QuestsCompleted, "The real completion event updates account achievement progress.");
            await ctx.ExecuteInCreatureContextAsync(player, () =>
                ctx.Assert(!Quest.AdvanceQuest(player, npc, questId), "A repeated completion cannot pay again."));
            ctx.AssertEqual(rewardsBefore + 1, CountItems(player, "p_crystal_red_qs"), "The item reward is not duplicated.");
            ctx.AssertEqual(1UL, DB.Get<Account>(cdKey).AchievementProgress.QuestsCompleted, "The completion achievement is not duplicated.");
        }
        finally
        {
            Conversation.End(player);
            windows.Remove(fixture.Id);
            if (originalAccount == null)
                DB.Delete<Account>(cdKey);
            else
                DB.Set(originalAccount);
        }
    }

    [EngineTest("Reward selection survives final-item collector cleanup", Category = "QuestCollectors", TimeoutSeconds = 60f)]
    public static async Task RewardSelection(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        var windows = InstallConversationWindow(fixture);
        const string questId = "engine_collector_rewards";
        var quest = new QuestBuilder().Create(questId, "Collector reward selection")
            .HasRewardSelection().AddGoldReward(25).AddItemReward("nw_it_medkit001", 1).AddGoldReward(7, false)
            .AddState().SetStateJournalText("Turn in one medkit.").AddCollectItemObjective("nw_it_medkit001", 1)
            .Build()[questId];
        quest.CountsTowardAchievements = false;
        Quest.RegisterRuntimeQuest(quest);
        try
        {
            await ctx.ExecuteInCreatureContextAsync(player, () =>
                ctx.Assert(Quest.AcceptQuest(player, GetModule(), questId), "Accept the reward-selection fixture."));
            var goldBefore = GetGold(player);
            var expectedReward = Quest.CalculateQuestGoldReward(player, false, 25) + Quest.CalculateQuestGoldReward(player, false, 7);
            var collector = await OpenCollector(ctx, player, GetModule(), questId);
            await GiveItem(ctx, collector, player, "nw_it_medkit001", 1);
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(collector), 3f, "reward-selection collector cleanup");
            ctx.Assert(!Activity.IsBusy(player), "Reward selection is usable after collector destruction.");
            ctx.AssertEqual(0, DB.Get<Player>(fixture.Id).Quests[questId].TimesCompleted, "No reward is paid before choosing.");
            var session = GetSession(player);
            ctx.AssertEqual(2, session.VisibleChoices.Count, "Both selectable rewards remain available.");
            var goldChoice = session.VisibleChoices.Select((choice, index) => (choice, index))
                .Single(x => x.choice.Text.Text == "25 Credits").index;
            await ctx.ExecuteInCreatureContextAsync(player, () => session.SelectChoice(goldChoice));
            ctx.AssertEqual(goldBefore + expectedReward, GetGold(player), "The selected gold and mandatory gold are paid once.");
            ctx.AssertEqual(0, CountItems(player, "nw_it_medkit001"), "The unselected item reward is not granted.");
            ctx.AssertEqual(1, DB.Get<Player>(fixture.Id).Quests[questId].TimesCompleted, "Reward selection completes the quest.");
            await ctx.ExecuteInCreatureContextAsync(player, () => Quest.AdvanceQuest(player, GetModule(), questId));
            ctx.AssertEqual(goldBefore + expectedReward, GetGold(player), "The completed quest cannot pay a second time.");
        }
        finally
        {
            Conversation.End(player);
            windows.Remove(fixture.Id);
            Quest.UnregisterRuntimeQuest(questId);
        }
    }

    [EngineTest("Rejected and excess items preserve hand-in progress and ownership", Category = "QuestCollectors", TimeoutSeconds = 60f)]
    public static async Task RejectedAndExcessItems(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        using var other = await PlayerAbilityFixture.CreateAsync(ctx, 2f);
        var player = fixture.Creature;
        const string questId = "engine_collector_items";
        var quest = new QuestBuilder().Create(questId, "Collector item handling")
            .AddState().SetStateJournalText("Turn in crafted medkits.")
            .AddCollectItemObjective("nw_it_medkit001", 3, CollectItemProducerRequirementType.PlayerProduced)
            .AddState().SetStateJournalText("Finished.").Build()[questId];
        Quest.RegisterRuntimeQuest(quest);
        try
        {
            await ctx.ExecuteInCreatureContextAsync(player, () => Quest.AcceptQuest(player, GetModule(), questId));
            other.Update(record => record.Quests[questId] = new PlayerQuest { CurrentState = 1 });
            var collector = await OpenCollector(ctx, player, GetModule(), questId);
            await GiveItem(ctx, collector, player, "nw_it_medkit001", 1);
            ctx.AssertEqual(3, DB.Get<Player>(fixture.Id).Quests[questId].ItemProgresses["nw_it_medkit001"], "Uncrafted items are rejected.");
            ctx.AssertEqual(1, CountItems(player, "nw_it_medkit001"), "The rejected item is returned.");
            await GiveItem(ctx, collector, player, "nw_wdbqs001", 1);
            ctx.AssertEqual(1, CountItems(player, "nw_wdbqs001"), "An unrelated item is returned.");
            await GiveItem(ctx, collector, other.Creature, "nw_it_medkit001", 1, true);
            ctx.AssertEqual(3, DB.Get<Player>(fixture.Id).Quests[questId].ItemProgresses["nw_it_medkit001"], "Another player's item cannot advance the owner's quest.");
            ctx.AssertEqual(1, CountItems(other.Creature, "nw_it_medkit001"), "The other player's item is returned.");
            ctx.Assert(Activity.IsBusy(player) && !Activity.IsBusy(other.Creature), "Rejection preserves only the owner's busy state.");
            await GiveItem(ctx, collector, player, "nw_it_medkit001", 5, true);
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(collector), 3f, "excess-stack collector cleanup");
            ctx.AssertEqual(3, CountItems(player, "nw_it_medkit001"), "Only the required three crafted items are consumed; two excess plus the rejected item remain.");
            ctx.AssertEqual(2, DB.Get<Player>(fixture.Id).Quests[questId].CurrentState, "The accepted stack advances the quest.");
            ctx.Assert(!Activity.IsBusy(player), "Excess-stack completion releases busy state.");

            fixture.Update(record => record.Quests[questId] = new PlayerQuest
            {
                CurrentState = 1,
                ItemProgresses = new Dictionary<string, int> { ["nw_it_medkit001"] = 3 }
            });
            collector = await OpenCollector(ctx, player, GetModule(), questId);
            Quest.UnregisterRuntimeQuest(questId);
            await GiveItem(ctx, collector, player, "nw_it_medkit001", 1, true);
            ctx.AssertEqual(4, CountItems(player, "nw_it_medkit001"), "An unregistered quest returns the submitted item.");
            ctx.AssertEqual(3, DB.Get<Player>(fixture.Id).Quests[questId].ItemProgresses["nw_it_medkit001"], "Unregistration cannot consume quest progress.");
            CloseCollector(collector, player);
            await ctx.WaitUntilAsync(() => !Activity.IsBusy(player), 3f, "cancelled unregistered quest cleanup");
        }
        finally
        {
            Quest.UnregisterRuntimeQuest(questId);
        }
    }

    private static async Task<uint> OpenCollector(EngineTestContext ctx, uint player, uint owner, string questId)
    {
        await ExecuteOnNextFrame(ctx, player, () =>
        {
            ctx.Assert(Quest.RequestItemsFromPlayer(player, owner, questId), $"Request the real collector for {questId}.");
            AssignCommand(player, () => ClearAllActions());
        });
        return await FindAndOpenCollector(ctx, player, questId);
    }

    private static async Task<uint> FindAndOpenCollector(EngineTestContext ctx, uint player, string questId)
    {
        await ctx.WaitFrameAsync();
        var collector = OBJECT_INVALID;
        for (var obj = GetFirstObjectInArea(ctx.Arena); GetIsObjectValid(obj); obj = GetNextObjectInArea(ctx.Arena))
        {
            if (GetLocalObject(obj, "QUEST_PLAYER") == player && GetLocalString(obj, "QUEST_ID") == questId)
            {
                collector = obj;
                break;
            }
        }
        ctx.Assert(GetIsObjectValid(collector), $"{questId} created a collector in the player's area.");
        ctx.Track(collector);
        if (!Activity.IsBusy(player))
            global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(collector).AsNWSPlaceable().OpenInventory(player);
        await ctx.WaitFrameAsync();
        await ctx.WaitUntilAsync(() => Activity.GetBusyType(player) == ActivityStatusType.Quest, 3f, $"{questId} collector opening");
        return collector;
    }

    private static void CloseCollector(uint collector, uint player) =>
        global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(collector).AsNWSPlaceable().CloseInventory(player);

    private static async Task ExecuteOnNextFrame(EngineTestContext ctx, uint target, Action action)
    {
        var completed = false;
        Exception failure = null;
        AssignCommand(target, () =>
        {
            if (ctx.CancellationToken.IsCancellationRequested)
                return;
            try { action(); }
            catch (Exception ex) { failure = ex; }
            finally { completed = true; }
        });
        // Yield before polling: the registry sweep runs this assigned context hundreds of times.
        await ctx.WaitFrameAsync();
        await ctx.WaitUntilAsync(() => completed, 3f, "the collector's assigned script context");
        if (failure != null)
            throw failure;
    }

    private static async Task GiveItem(EngineTestContext ctx, uint collector, uint player, string resref, int quantity, bool crafted = false)
    {
        await ctx.ExecuteInCreatureContextAsync(collector, () =>
        {
            var item = CreateItemOnObject(resref, collector);
            ctx.Assert(GetIsObjectValid(item), $"Create the hand-in item {resref}.");
            SetItemStackSize(item, quantity);
            ctx.AssertEqual(quantity, GetItemStackSize(item), $"Set the hand-in quantity for {resref}.");
            if (crafted)
                SetLocalBool(item, Item.PlayerProducedItemVariable, true);
            // Install the same event fields that the native inventory transfer supplies.
            var native = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(collector).AsNWSPlaceable();
            native.m_oidLastDisturbed = player;
            native.m_oidInventoryDisturbItem = item;
            native.m_nInventoryDisturbType = (int)DisturbType.Added;
            Quest.DisturbItemCollector();
        });
        await ctx.WaitFrameAsync();
    }

    private static int CountItems(uint player, string resref)
    {
        var count = 0;
        for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
            if (GetResRef(item) == resref)
                count += GetItemStackSize(item);
        return count;
    }

    private static Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>> InstallConversationWindow(PlayerAbilityFixture fixture)
    {
        var windows = (Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>>)typeof(Gui)
            .GetField("_playerWindows", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        var template = Gui.GetWindowTemplate(GuiWindowType.Conversation);
        var window = template.CreatePlayerWindowAction();
        window.ViewModel.Geometry = template.InitialGeometry;
        windows.Add(fixture.Id, new Dictionary<GuiWindowType, GuiPlayerWindow> { [GuiWindowType.Conversation] = window });
        return windows;
    }

    private static IConversationSession GetSession(uint player) =>
        (IConversationSession)typeof(ConversationViewModel).GetField("_session", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(Gui.GetPlayerWindow(player, GuiWindowType.Conversation).ViewModel);

    private static void SelectQuestAction(EngineTestContext ctx, IConversationSession session, string key, string questId)
    {
        var choice = session.VisibleChoices.Select((value, index) => (value, index))
            .Single(x => x.value.Actions.Any(action => action.Key == key && action.Arguments.Contains(questId)));
        ctx.Assert(session.SelectChoice(choice.index) != ConversationSelectionResult.InvalidChoice, $"Execute the authored {key} choice.");
    }
}
