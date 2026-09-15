using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.QuestContractService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class QuestContractEscrowEngineTests
    {
        [EngineTest("Contract partial submissions stay held across sessions and refund on abandonment", Category = "QuestContractEscrow")]
        public static async Task PartialSubmissionsAndAbandonment(EngineTestContext ctx)
        {
            var worker = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var contract = new QuestContract { AuthorPlayerId = Guid.NewGuid().ToString(), Status = QuestContractStatus.Published, CompletionsRemaining = 1 };
            await ctx.ExecuteInCreatureContextAsync(worker, () =>
            {
                DB.Set(contract);
                try
                {
                    var item = CreateItemOnObject("nw_it_medkit001", worker, 3);
                    ctx.Track(item);
                    var quest = QuestContractFactory.BuildQuest(contract);
                    quest.CollectedItemHandler(worker, item);
                    ctx.Assert(!QuestContractBoard.HasPendingDeliveries(contract.AuthorPlayerId), "Author cannot claim partial turn-ins");
                    ctx.Assert(!QuestContractBoard.HasPendingDeliveries(GetObjectUUID(worker)), "Worker cannot reclaim and also retain active progress");
                    var held = Deliveries(contract.Id).Single();
                    ctx.Assert(held.HeldForCompletion && held.Items.Count == 1, "The submission is persisted in its own held record");
                    quest.CollectedItemHandler(worker, item);
                    ctx.AssertEqual(2, Deliveries(contract.Id).Single().Items.Count, "Later turn-ins accumulate in the same attempt");
                    quest.OnAbandonActions.Single()(worker);
                    ctx.Assert(QuestContractBoard.HasPendingDeliveries(GetObjectUUID(worker)), "Abandonment refunds the worker");
                    ctx.Assert(!QuestContractBoard.HasPendingDeliveries(contract.AuthorPlayerId), "Abandonment never pays the author");
                    quest.CollectedItemHandler(worker, item);
                    var attempts = Deliveries(contract.Id);
                    ctx.AssertEqual(2, attempts.Count, "Reacceptance creates fresh escrow separate from the old refund");
                    ctx.AssertEqual(1, attempts.Count(delivery => delivery.HeldForCompletion), "Only the new attempt stays held");
                }
                finally { Cleanup(contract); }
            });
        }

        [EngineTest("Contract reward failure retains items and credits and settlement retry cannot pay twice", Category = "QuestContractEscrow")]
        public static async Task RewardFailureAndSettlementRecovery(EngineTestContext ctx)
        {
            var worker = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var contract = new QuestContract { AuthorPlayerId = Guid.NewGuid().ToString(), Status = QuestContractStatus.Published, CompletionsRemaining = 1, RewardCredits = 125 };
            await ctx.ExecuteInCreatureContextAsync(worker, () =>
            {
                var playerId = GetObjectUUID(worker);
                var item = CreateItemOnObject("nw_wswls001", worker);
                ctx.Track(item);
                contract.RewardItems.Add(new QuestContractItem { Data = ObjectPlugin.Serialize(item), Name = "Fixture reward" });
                DB.Set(contract);
                var gold = GetGold(worker);
                try
                {
                    var held = QuestContractBoard.GetOrCreateSubmission(playerId, contract);
                    held.Items.Add(new QuestContractItem { Data = ObjectPlugin.Serialize(item) });
                    DB.Set(held);
                    var other = QuestContractBoard.GetOrCreateSubmission(Guid.NewGuid().ToString(), contract);
                    other.Items.Add(new QuestContractItem { Data = ObjectPlugin.Serialize(item) });
                    DB.Set(other);
                    var repository = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(worker).AsNWSCreature().m_pcItemRepository;
                    var width = repository.m_nWidth;
                    var height = repository.m_nHeight;
                    var boundary = repository.m_nBoundary;
                    var scalable = repository.m_bScalable;
                    try
                    {
                        repository.m_nWidth = 0;
                        repository.m_nHeight = 0;
                        repository.m_nBoundary = 0;
                        repository.m_bScalable = 0;
                        QuestContractBoard.CompleteContract(contract, playerId);
                        ClaimWithSnapshot(ctx, worker);
                        var receipt = DB.Get<QuestContractDelivery>(contract.Id + "-reward");
                        ctx.AssertEqual(1, receipt.Items.Count, "Failed acquisition retains the serialized reward");
                        ctx.AssertEqual(125, receipt.Credits, "Credits stay pending with the failed item");
                        ctx.AssertEqual(gold, GetGold(worker), "A failed claim does not pay credits");
                        ClaimWithSnapshot(ctx, worker);
                        ctx.AssertEqual(1, DB.Get<QuestContractDelivery>(receipt.Id).Items.Count, "Repeated failure retains exactly one reward");
                    }
                    finally
                    {
                        repository.m_nWidth = width;
                        repository.m_nHeight = height;
                        repository.m_nBoundary = boundary;
                        repository.m_bScalable = scalable;
                    }

                    var settled = DB.Get<QuestContract>(contract.Id);
                    ctx.AssertEqual(QuestContractStatus.Fulfilled, settled.Status, "Winner is durably recorded");
                    ctx.AssertEqual(contract.AuthorPlayerId, DB.Get<QuestContractDelivery>(held.Id).PlayerId, "Author receives the winner's items");
                    ctx.AssertEqual(other.PlayerId, DB.Get<QuestContractDelivery>(other.Id).PlayerId, "Losing worker keeps their submissions");
                    ctx.Assert(!DB.Get<QuestContractDelivery>(other.Id).HeldForCompletion, "Losing worker's refund is claimable");
                    ClaimWithSnapshot(ctx, worker);
                    ctx.AssertEqual(gold + 125, GetGold(worker), "Retry pays the reward once space is available");
                    ctx.Assert(!QuestContractBoard.HasPendingDeliveries(playerId), "Empty receipt is not shown as a pending delivery");
                    QuestContractBoard.SettleCompletedContract(settled);
                    ClaimWithSnapshot(ctx, worker);
                    ctx.AssertEqual(gold + 125, GetGold(worker), "Restart settlement cannot recreate an already claimed payment");

                    // Simulate a stop after the winner was saved but before its payment was created.
                    var interrupted = new QuestContract
                    {
                        Status = QuestContractStatus.Fulfilled, CompletedByPlayerId = playerId, SettlementPending = 1,
                        AuthorPlayerId = contract.AuthorPlayerId, RewardCredits = 77,
                        RewardItems = new List<QuestContractItem>
                        {
                            new() { Data = ObjectPlugin.Serialize(item), Name = "Interrupted reward" }
                        }
                    };
                    DB.Set(interrupted);
                    try
                    {
                        typeof(QuestContractBoard).GetMethod("RecoverSettlements", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
                        var recovered = DB.Get<QuestContractDelivery>(interrupted.Id + "-reward");
                        ctx.Assert(recovered != null, "Boot recovery creates the missing payment");
                        ctx.AssertEqual(77, recovered.Credits, "Boot recovery preserves the missing credits");
                        ctx.AssertEqual(1, DB.Get<QuestContractDelivery>(interrupted.Id + "-reward").Items.Count, "Boot recovery preserves the missing reward item");
                    }
                    finally { Cleanup(interrupted); }
                }
                finally { Cleanup(contract); }
            });
        }

        [EngineTest("Completed player quests recover settlement while settled history is skipped", Category = "QuestContractEscrow")]
        public static async Task CompletedQuestRecovery(EngineTestContext ctx)
        {
            await ctx.WaitFrameAsync();
            var player = new Player(Guid.NewGuid().ToString());
            var contract = new QuestContract { AuthorPlayerId = Guid.NewGuid().ToString(), Status = QuestContractStatus.Published, CompletionsRemaining = 1, RewardCredits = 42 };
            var historical = new QuestContract { Status = QuestContractStatus.Fulfilled, CompletedByPlayerId = player.Id, RewardCredits = 99 };
            player.Quests[QuestContractFactory.BuildQuestId(contract.Id)] = new PlayerQuest { DateLastCompleted = DateTime.UtcNow, TimesCompleted = 1 };
            DB.Set(player);
            DB.Set(contract);
            DB.Set(historical);
            var submission = QuestContractBoard.GetOrCreateSubmission(player.Id, contract);
            submission.Items.Add(new QuestContractItem { Data = "unclaimed fixture" });
            DB.Set(submission);
            try
            {
                typeof(QuestContractBoard).GetMethod("RecoverSettlements", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
                ctx.AssertEqual(player.Id, DB.Get<QuestContract>(contract.Id).CompletedByPlayerId, "Persisted completion determines the winner");
                ctx.AssertEqual(42, DB.Get<QuestContractDelivery>(contract.Id + "-reward").Credits, "The completed quest receives its payment");
                ctx.AssertEqual(contract.AuthorPlayerId, DB.Get<QuestContractDelivery>(submission.Id).PlayerId, "Completed submission reaches the author");
                ctx.AssertEqual(0, DB.Get<QuestContract>(contract.Id).SettlementPending, "Settlement leaves the pending index");
                ctx.Assert(DB.Get<QuestContractDelivery>(historical.Id + "-reward") == null, "Recovery does not process already settled history");
            }
            finally
            {
                Cleanup(contract);
                Cleanup(historical);
                DB.Delete<Player>(player.Id);
            }
        }

        private static List<QuestContractDelivery> Deliveries(string contractId)
        {
            var query = new DBQuery<QuestContractDelivery>().AddFieldSearch(nameof(QuestContractDelivery.SourceContractId), contractId, false);
            return DB.Search(query.AddPaging(100, 0)).ToList();
        }

        private static void ClaimWithSnapshot(EngineTestContext ctx, uint player)
        {
            // A headless NPC has no authenticated servervault client. Verify the native snapshot
            // used by a synchronous character save carries the claim marker and its gold together.
            Action checkpoint = () =>
            {
                var restored = ObjectPlugin.Deserialize(ObjectPlugin.Serialize(player));
                ctx.Track(restored);
                ctx.AssertEqual(GetGold(player), GetGold(restored), "The native snapshot preserves awarded credits");
                foreach (var delivery in DB.Search(new DBQuery<QuestContractDelivery>()
                    .AddFieldSearch(nameof(QuestContractDelivery.PlayerId), GetObjectUUID(player), false).AddPaging(100, 0)))
                {
                    var name = "CONTRACT_CLAIM_" + delivery.Id;
                    ctx.AssertEqual(GetLocalString(player, name), GetLocalString(restored, name), "The native snapshot preserves the claim checkpoint with the awards");
                }
            };
            typeof(QuestContractBoard).GetMethod("ClaimDeliveries", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { player, checkpoint });
        }

        private static void Cleanup(QuestContract contract)
        {
            foreach (var delivery in Deliveries(contract.Id)) DB.Delete<QuestContractDelivery>(delivery.Id);
            DB.Delete<QuestContract>(contract.Id);
        }
    }
}
