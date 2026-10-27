using System.Collections.Generic;
using System.Linq;
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
                        QuestContractBoardInternals.Invoke("RecoverSettlements");
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
                QuestContractBoardInternals.Invoke("RecoverSettlements");
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

        /// <summary>Verifies repost creation, independent persisted edits, and draft protection against NWNX and Redis.</summary>
        [EngineTest("Post Again opens an independent prefilled contract draft for editing", Category = "QuestContractEscrow")]
        public static async Task PostAgainCreatesEditableDraft(EngineTestContext ctx)
        {
            var author = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(author, () =>
            {
                var source = new QuestContract
                {
                    AuthorPlayerId = GetObjectUUID(author), Status = QuestContractStatus.Expired,
                    Title = "Medical supplies", Description = "Deliver three medkits.", RewardCredits = 125,
                    DateClosed = DateTime.UtcNow,
                    Objectives = new() { new() { ItemResref = "nw_it_medkit001", ItemName = "Medkit", Quantity = 3 } }
                };
                QuestContract draft = null;
                DB.Set(source);
                try
                {
                    ctx.AssertEqual(string.Empty, QuestContractBoard.PostAgain(author, source.Id), "Reposting creates a draft");
                    draft = QuestContractBoard.GetDraft(author);
                    ctx.Assert(draft.Id != source.Id && draft.Status == QuestContractStatus.Draft, "Reposting does not publish or reuse the old identity");
                    ctx.AssertEqual(source.Title, draft.Title, "Title is prefilled");
                    ctx.AssertEqual(source.Description, draft.Description, "Description is prefilled");
                    ctx.AssertEqual(125, draft.RewardCredits, "Credit reward is prefilled");
                    ctx.AssertEqual(3, draft.Objectives.Single().Quantity, "Objective is prefilled");
                    ctx.Assert(draft.DateClosed == null, "The new draft has no closing date");
                    draft.Title = "Edited supplies";
                    draft.RewardCredits = 250;
                    draft.Objectives[0].Quantity = 5;
                    DB.Set(draft);
                    ctx.AssertEqual("Edited supplies", QuestContractBoard.GetDraft(author).Title, "Edits persist before posting");
                    ctx.AssertEqual(3, DB.Get<QuestContract>(source.Id).Objectives.Single().Quantity, "Editing does not change the original");
                    ctx.Assert(QuestContractBoard.PostAgain(author, source.Id) != string.Empty, "An existing draft is never overwritten");
                }
                finally
                {
                    if (draft != null) Cleanup(draft);
                    Cleanup(source);
                }
            });
        }

        /// <summary>
        /// Sweeps more than one default page of settled history, preserves unsettled records, and
        /// claims a serialized item and credits after their source history has been removed.
        /// </summary>
        [EngineTest("Contract history cleanup exceeds fifty rows and preserves claimable deliveries", Category = "QuestContractEscrow")]
        public static async Task HistoryCleanupPreservesDeliveries(EngineTestContext ctx)
        {
            var worker = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(worker, () =>
            {
                var now = DateTime.UtcNow;
                var contracts = Enumerable.Range(0, 55).Select(_ => new QuestContract
                {
                    Status = QuestContractStatus.Fulfilled, DateClosed = now.AddDays(-31)
                }).ToList();
                var legacyExpired = new QuestContract { Status = QuestContractStatus.Expired, DateExpires = now.AddDays(-31) };
                contracts.Add(legacyExpired);
                var preserved = new[]
                {
                    new QuestContract { Status = QuestContractStatus.Draft, DateCreated = now.AddYears(-1) },
                    new QuestContract { Status = QuestContractStatus.Published, DatePublished = now.AddYears(-1) },
                    new QuestContract { Status = QuestContractStatus.Expired, DateClosed = now.AddDays(-29) },
                    new QuestContract { Status = QuestContractStatus.Fulfilled, DateClosed = now.AddDays(-31), SettlementPending = 1 },
                    new QuestContract { Status = QuestContractStatus.Fulfilled, DateClosed = now.AddDays(-31) },
                    new QuestContract { Status = QuestContractStatus.Cancelled }
                };
                try
                {
                    foreach (var contract in contracts.Concat(preserved)) DB.Set(contract);
                    var item = CreateItemOnObject("nw_it_medkit001", worker);
                    ctx.Track(item);
                    var pending = new QuestContractDelivery
                    {
                        PlayerId = GetObjectUUID(worker), SourceContractId = contracts[0].Id, SourceContractTitle = "Old reward",
                        IsRewardPayment = true, Credits = 75, Items = new() { new() { Data = ObjectPlugin.Serialize(item) } }
                    };
                    DB.Set(pending);
                    var emptyReceipt = new QuestContractDelivery { IsRewardPayment = true, SourceContractId = contracts[0].Id };
                    DB.Set(emptyReceipt);
                    DB.Set(new QuestContractDelivery { HeldForCompletion = true, SourceContractId = preserved[4].Id });
                    QuestContractBoardInternals.Invoke("PruneHistory", now);
                    foreach (var contract in contracts)
                        ctx.Assert(DB.Get<QuestContract>(contract.Id) == null, "All old settled history rows are deleted, including rows beyond fifty");
                    foreach (var contract in preserved)
                        ctx.Assert(DB.Get<QuestContract>(contract.Id) != null, "Open, recent, unsettled, held, and undated history are preserved");
                    ctx.Assert(DB.Get<QuestContract>(preserved[5].Id).DateClosed != null, "Undated legacy history starts its retention window");
                    ctx.Assert(DB.Get<QuestContractDelivery>(emptyReceipt.Id) == null, "Empty receipts are deleted");
                    ctx.AssertEqual("Old reward", DB.Get<QuestContractDelivery>(pending.Id).SourceContractTitle, "Pending deliveries retain their source title");
                    var gold = GetGold(worker);
                    ClaimWithSnapshot(ctx, worker);
                    ctx.AssertEqual(gold + 75, GetGold(worker), "Pending items and credits can still be claimed after history deletion");
                    ctx.Assert(DB.Get<QuestContractDelivery>(pending.Id) == null, "Claimed reward receipts no longer accumulate after their source is deleted");
                }
                finally
                {
                    foreach (var contract in contracts.Concat(preserved)) Cleanup(contract);
                }
            });
        }

        /// <summary>
        /// Removes more than fifty empty orphaned receipts with no closed history present while
        /// retaining existing-source receipts, held submissions, items, and credits.
        /// </summary>
        [EngineTest("Contract orphaned empty deliveries are swept without closed history and retain unclaimed value", Category = "QuestContractEscrow")]
        public static async Task OrphanedDeliveryCleanup(EngineTestContext ctx)
        {
            await ctx.WaitFrameAsync();
            var source = new QuestContract { Status = QuestContractStatus.Published };
            var missingSourceId = Guid.NewGuid().ToString();
            var empty = Enumerable.Range(0, 55).Select(index => new QuestContractDelivery
            {
                SourceContractId = missingSourceId, IsRewardPayment = index % 2 == 0
            }).ToList();
            var protectedDeliveries = new[]
            {
                new QuestContractDelivery { SourceContractId = source.Id, IsRewardPayment = true },
                new QuestContractDelivery { SourceContractId = missingSourceId, Credits = 75, IsRewardPayment = true },
                new QuestContractDelivery { SourceContractId = missingSourceId, Items = new() { new() { Data = "unclaimed item" } } },
                new QuestContractDelivery { SourceContractId = missingSourceId, HeldForCompletion = true }
            };
            try
            {
                DB.Set(source);
                foreach (var delivery in empty.Concat(protectedDeliveries)) DB.Set(delivery);
                ctx.AssertEqual(0L, DB.SearchCount(new DBQuery<QuestContract>().AddFieldSearch(nameof(QuestContract.Status),
                    new[] { (int)QuestContractStatus.Fulfilled, (int)QuestContractStatus.Expired, (int)QuestContractStatus.Cancelled, (int)QuestContractStatus.TakenDown })),
                    "The sweep must work when no closed history exists");
                QuestContractBoardInternals.Invoke("PruneHistory", DateTime.UtcNow);
                foreach (var delivery in empty)
                    ctx.Assert(DB.Get<QuestContractDelivery>(delivery.Id) == null, "Empty orphaned deliveries are removed beyond fifty rows");
                foreach (var delivery in protectedDeliveries)
                    ctx.Assert(DB.Get<QuestContractDelivery>(delivery.Id) != null, "Existing-source receipts, unclaimed value, and held submissions are retained");
                ctx.AssertEqual(75, DB.Get<QuestContractDelivery>(protectedDeliveries[1].Id).Credits, "Unclaimed credits remain intact");
                ctx.AssertEqual("unclaimed item", DB.Get<QuestContractDelivery>(protectedDeliveries[2].Id).Items.Single().Data, "Unclaimed items remain intact");
            }
            finally
            {
                foreach (var delivery in empty.Concat(protectedDeliveries)) DB.Delete<QuestContractDelivery>(delivery.Id);
                DB.Delete<QuestContract>(source.Id);
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
            QuestContractBoardInternals.Invoke("ClaimDeliveries", player, checkpoint);
        }

        private static void Cleanup(QuestContract contract)
        {
            foreach (var delivery in Deliveries(contract.Id)) DB.Delete<QuestContractDelivery>(delivery.Id);
            DB.Delete<QuestContract>(contract.Id);
        }
    }
}
