using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.QuestContractService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class QuestContractQuantityEngineTests
{
    [EngineTest("Contract exact twenty emerald stack conserves units", Category = "QuestContractQuantity")]
    public static Task Exact(EngineTestContext ctx) => Complete(ctx, new[] { 20 });

    [EngineTest("Contract excess forty emerald stack conserves units", Category = "QuestContractQuantity")]
    public static Task Excess(EngineTestContext ctx) => Complete(ctx, new[] { 40 });

    [EngineTest("Contract excess twenty-one emerald stack conserves units", Category = "QuestContractQuantity")]
    public static Task OneExcess(EngineTestContext ctx) => Complete(ctx, new[] { 21 });

    [EngineTest("Contract maximum emerald stack conserves units", Category = "QuestContractQuantity")]
    public static Task Maximum(EngineTestContext ctx) => Complete(ctx, new[] { 99 });

    [EngineTest("Contract multiple exact emerald stacks conserve units", Category = "QuestContractQuantity")]
    public static Task Multiple(EngineTestContext ctx) => Complete(ctx, new[] { 7, 13 });

    [EngineTest("Contract multiple excess emerald stacks conserve units", Category = "QuestContractQuantity")]
    public static Task MultipleExcess(EngineTestContext ctx) => Complete(ctx, new[] { 7, 25 });

    [EngineTest("Contract completed quest recovers twenty units and payment after interrupted settlement", Category = "QuestContractQuantity")]
    public static Task InterruptedCompletion(EngineTestContext ctx) => Complete(ctx, new[] { 20 }, true);

    private static async Task Complete(EngineTestContext ctx, int[] stacks, bool interruptSettlement = false)
    {
        using var worker = await PlayerAbilityFixture.CreateAsync(ctx);
        using var author = await PlayerAbilityFixture.CreateAsync(ctx, 2f);
        var contract = Contract(author.Id);
        var questId = QuestContractFactory.BuildQuestId(contract.Id);
        DB.Set(contract);
        var runtime = QuestContractFactory.BuildQuest(contract);
        if (interruptSettlement) runtime.Rewards.Clear();
        Quest.RegisterRuntimeQuest(runtime);
        try
        {
            await ctx.ExecuteInCreatureContextAsync(worker.Creature, () =>
                ctx.Assert(Quest.AcceptQuest(worker.Creature, GetModule(), questId), "Accept the contract."));
            var gold = GetGold(worker.Creature);
            var collector = await Open(ctx, worker.Creature, questId);
            var total = 0;
            foreach (var quantity in stacks)
            {
                var before = Remaining(worker.Id, questId);
                await Transfer(ctx, worker.Creature, collector, quantity);
                total += quantity;
                await ctx.WaitUntilAsync(() => Remaining(worker.Id, questId) != before,
                    5f, "native emerald transfer");
                await ctx.WaitFrameAsync();
                var progress = Remaining(worker.Id, questId);
                ctx.AssertEqual(Math.Max(0, 20 - total), progress, "Progress counts submitted units.");
                ctx.Log($"Submitted {total}; worker inventory {Count(worker.Creature)}; remaining {progress}; serialized units {SerializedUnits(ctx, contract.Id)}.");
                ctx.AssertEqual(Math.Max(0, total - 20), Count(worker.Creature), "Only excess emerald units remain with the worker.");
                ctx.AssertEqual(Math.Min(total, 20), SerializedUnits(ctx, contract.Id), "Escrow contains exactly the consumed units.");
                if (progress > 0)
                {
                    global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(collector).AsNWSPlaceable().CloseInventory(worker.Creature);
                    await ctx.WaitUntilAsync(() => !GetIsObjectValid(collector), 3f, "partial collector cleanup");
                    Recover();
                    ctx.AssertEqual(progress, Remaining(worker.Id, questId), "Recovery preserves partial progress.");
                    collector = await Open(ctx, worker.Creature, questId);
                }
            }
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(collector), 3f, "completed collector cleanup");
            if (interruptSettlement)
            {
                ctx.AssertEqual(QuestContractStatus.Published, DB.Get<QuestContract>(contract.Id).Status, "The stop occurs before the reward handler settles.");
                ctx.Assert(DB.Get<Player>(worker.Id).Quests[questId].DateLastCompleted != null, "Quest completion was persisted first.");
                Recover();
            }
            ctx.AssertEqual(QuestContractStatus.Fulfilled, DB.Get<QuestContract>(contract.Id).Status, "Completion settles the contract.");
            await ctx.ExecuteInCreatureContextAsync(worker.Creature, () => Claim(ctx, worker.Creature));
            ctx.AssertEqual(gold + 20000, GetGold(worker.Creature), "The unchanged reward pays exactly once.");
            await ctx.ExecuteInCreatureContextAsync(author.Creature, () => Claim(ctx, author.Creature));
            ctx.AssertEqual(20, Count(author.Creature), "Author actually acquires twenty units.");
            Recover();
            await ctx.ExecuteInCreatureContextAsync(worker.Creature, () => Claim(ctx, worker.Creature));
            await ctx.ExecuteInCreatureContextAsync(author.Creature, () => Claim(ctx, author.Creature));
            ctx.AssertEqual(gold + 20000, GetGold(worker.Creature), "Recovery cannot pay twice.");
            ctx.AssertEqual(20, Count(author.Creature), "Recovery cannot duplicate delivery units.");
            ctx.AssertEqual(stacks.Sum(), Count(worker.Creature) + Count(author.Creature), "All source units are conserved.");
        }
        finally { Cleanup(contract); }
    }

    [EngineTest("Partial contract emeralds survive cancellation and boot recovery without payment", Category = "QuestContractQuantity")]
    public static Task PartialCancellationAndRecovery(EngineTestContext ctx) => PartialRefund(ctx, false);

    [EngineTest("Partial contract emeralds refund once on abandonment and boot recovery", Category = "QuestContractQuantity")]
    public static Task PartialAbandonmentAndRecovery(EngineTestContext ctx) => PartialRefund(ctx, true);

    private static async Task PartialRefund(EngineTestContext ctx, bool abandon)
    {
        using var worker = await PlayerAbilityFixture.CreateAsync(ctx);
        using var author = await PlayerAbilityFixture.CreateAsync(ctx, 2f);
        var contract = Contract(author.Id);
        var questId = QuestContractFactory.BuildQuestId(contract.Id);
        DB.Set(contract);
        var runtime = QuestContractFactory.BuildQuest(contract);
        Quest.RegisterRuntimeQuest(runtime);
        try
        {
            await ctx.ExecuteInCreatureContextAsync(worker.Creature, () => Quest.AcceptQuest(worker.Creature, GetModule(), questId));
            var gold = GetGold(worker.Creature);
            var collector = await Open(ctx, worker.Creature, questId);
            await Transfer(ctx, worker.Creature, collector, 7);
            await ctx.WaitUntilAsync(() => Remaining(worker.Id, questId) == 13, 5f, "partial submission");
            global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(collector).AsNWSPlaceable().CloseInventory(worker.Creature);
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(collector), 3f, "cancel collector");
            Recover();
            ctx.AssertEqual(7, SerializedUnits(ctx, contract.Id), "Active partial escrow survives boot recovery.");
            ctx.Assert(Deliveries(contract.Id).Single().HeldForCompletion, "Closing the collector does not refund active progress.");
            ctx.AssertEqual(0, Count(worker.Creature), "The partial units were removed.");
            ctx.AssertEqual(gold, GetGold(worker.Creature), "Partial progress pays no credits.");
            if (abandon)
                await ctx.ExecuteInCreatureContextAsync(worker.Creature, () => Quest.AbandonQuest(worker.Creature, questId));
            else
            {
                // Simulate a stop after cancellation is saved but before held submissions are released.
                QuestContractBoardInternals.Invoke("RefundEscrowToAuthor", contract);
                contract.Status = QuestContractStatus.Cancelled;
                DB.Set(contract);
            }
            Recover();
            ctx.Assert(!Deliveries(contract.Id).Single(delivery => delivery.PlayerId == worker.Id).HeldForCompletion, "Refund remains claimable after boot recovery.");
            await ctx.ExecuteInCreatureContextAsync(worker.Creature, () => Claim(ctx, worker.Creature));
            Recover();
            await ctx.ExecuteInCreatureContextAsync(worker.Creature, () => Claim(ctx, worker.Creature));
            ctx.AssertEqual(7, Count(worker.Creature), "Cancellation refunds exactly seven units once.");
            ctx.AssertEqual(gold, GetGold(worker.Creature), "Cancellation pays no completion reward.");
            if (!abandon)
            {
                var authorGold = GetGold(author.Creature);
                await ctx.ExecuteInCreatureContextAsync(author.Creature, () => Claim(ctx, author.Creature));
                Recover();
                await ctx.ExecuteInCreatureContextAsync(author.Creature, () => Claim(ctx, author.Creature));
                ctx.AssertEqual(authorGold + 20000, GetGold(author.Creature), "Cancelled escrow credits refund the author once.");
                ctx.AssertEqual(0, Count(author.Creature), "The author receives no partial objective units.");
            }
        }
        finally { Cleanup(contract); }
    }

    private static QuestContract Contract(string author) => new()
    {
        Title = "Emerald quantity fixture", AuthorPlayerId = author, Status = QuestContractStatus.Published,
        CompletionsRemaining = 1, RewardCredits = 20000, DateExpires = DateTime.UtcNow.AddDays(30),
        Objectives = new() { new() { ItemResref = "emerald", ItemName = "Emerald", Quantity = 20 } }
    };

    private static async Task<uint> Open(EngineTestContext ctx, uint player, string questId)
    {
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            ctx.Assert(Quest.RequestItemsFromPlayer(player, GetModule(), questId), "Open the real contract collector.");
            AssignCommand(player, () => ClearAllActions());
        });
        await ctx.WaitFrameAsync();
        var collector = OBJECT_INVALID;
        for (var obj = GetFirstObjectInArea(ctx.Arena); GetIsObjectValid(obj); obj = GetNextObjectInArea(ctx.Arena))
            if (GetLocalObject(obj, "QUEST_PLAYER") == player && GetLocalString(obj, "QUEST_ID") == questId)
                collector = obj;
        ctx.Assert(GetIsObjectValid(collector), "Collector exists.");
        ctx.Track(collector);
        global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(collector).AsNWSPlaceable().OpenInventory(player);
        await ctx.WaitUntilAsync(() => Activity.GetBusyType(player) == ActivityStatusType.Quest, 3f, "collector opening");
        return collector;
    }

    private static async Task Transfer(EngineTestContext ctx, uint player, uint collector, int quantity)
    {
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            var item = CreateItemOnObject("emerald", player);
            ctx.Assert(GetIsObjectValid(item), "Create the real emerald blueprint.");
            SetItemStackSize(item, quantity);
            ctx.AssertEqual(quantity, GetItemStackSize(item), "Source stack has the requested units.");
            var server = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp;
            ctx.Assert(server.GetGameObject(player).AsNWSCreature().AddRepositoryMoveActions(
                server.GetGameObject(item).AsNWSItem(), collector, 0xff, 0xff) != 0, "Queue a native inventory transfer.");
        });
    }

    private static int Remaining(string playerId, string questId) => DB.Get<Player>(playerId).Quests[questId].ItemProgresses.GetValueOrDefault("emerald");

    private static int Count(uint owner)
    {
        var total = 0;
        for (var item = GetFirstItemInInventory(owner); GetIsObjectValid(item); item = GetNextItemInInventory(owner))
            if (GetResRef(item) == "emerald") total += GetItemStackSize(item);
        return total;
    }

    private static int SerializedUnits(EngineTestContext ctx, string contractId)
    {
        var total = 0;
        foreach (var data in Deliveries(contractId).SelectMany(delivery => delivery.Items))
        {
            var item = ObjectPlugin.Deserialize(data.Data);
            ctx.Track(item);
            ctx.Assert(GetIsObjectValid(item), "Escrow deserializes into a native item.");
            ctx.AssertEqual(data.StackSize, GetItemStackSize(item), "Metadata matches serialized units.");
            total += GetItemStackSize(item);
            DestroyObject(item);
        }
        return total;
    }

    private static List<QuestContractDelivery> Deliveries(string id) => DB.Search(new DBQuery<QuestContractDelivery>()
        .AddFieldSearch(nameof(QuestContractDelivery.SourceContractId), id, false).AddPaging(100, 0)).ToList();

    private static void Claim(EngineTestContext ctx, uint player) => QuestContractBoardInternals.Invoke("ClaimDeliveries", player,
        (Action)(() =>
        {
            var restored = ObjectPlugin.Deserialize(ObjectPlugin.Serialize(player));
            ctx.Track(restored);
            ctx.AssertEqual(Count(player), Count(restored), "The character snapshot preserves awarded units.");
            ctx.AssertEqual(GetGold(player), GetGold(restored), "The character snapshot preserves credits.");
        }));

    private static void Recover() => QuestContractBoardInternals.Invoke("RecoverSettlements");

    private static void Cleanup(QuestContract contract)
    {
        Quest.UnregisterRuntimeQuest(QuestContractFactory.BuildQuestId(contract.Id));
        foreach (var delivery in Deliveries(contract.Id)) DB.Delete<QuestContractDelivery>(delivery.Id);
        DB.Delete<QuestContract>(contract.Id);
    }
}
