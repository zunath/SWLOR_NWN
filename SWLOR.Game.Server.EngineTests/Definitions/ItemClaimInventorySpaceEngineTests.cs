using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.QuestContractService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Acquiring an item that has no room in a creature's inventory can remove items the creature already carries.
    /// Claim paths check for room first and keep their record when the item does not fit.
    /// </summary>
    public static class ItemClaimInventorySpaceEngineTests
    {
        private const string LargeItemResref = "twin_elec_1"; // 2x5 Twin Electroblade
        private const string StackResref = "herb_m";
        private const string FillerResref = "nw_maletatcivout";
        private const string SmallFillerResref = "survival_knife";

        [EngineTest("A bank withdrawal without inventory room keeps the record and every carried item", Category = "ItemClaimSpace")]
        public static async Task BankWithdrawalWithoutRoom(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            InventoryItem large = null;
            InventoryItem stack = null;
            try
            {
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    var fillers = FillInventory(ctx, player);
                    var carried = Snapshot(player);
                    large = Bank.DepositItem(player, CreateStorageItem(ctx, LargeItemResref));
                    // A matching carried stack is not room: with no free cell the engine removes it instead of merging.
                    stack = Bank.DepositItem(player, CreateStorageItem(ctx, StackResref, 5));

                    foreach (var record in new[] { large, stack })
                    {
                        var failure = Bank.WithdrawItem(player, record.Id);
                        AssertCarriedIntact(ctx, player, carried);
                        ctx.Assert(DB.Get<InventoryItem>(record.Id) != null, $"The bank keeps the '{record.Resref}' that did not fit");
                        ctx.Assert(!string.IsNullOrEmpty(failure), $"Withdrawing the '{record.Resref}' without room fails");
                    }

                    foreach (var filler in fillers)
                        DestroyObject(filler);
                });
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    var carried = Snapshot(player);
                    ctx.AssertEqual(string.Empty, Bank.WithdrawItem(player, large.Id), "A withdrawal with room succeeds");
                    ctx.Assert(DB.Get<InventoryItem>(large.Id) == null, "The withdrawn item leaves the bank");
                    ctx.AssertEqual(1, CountCarried(player, LargeItemResref), "The withdrawn item reaches the inventory");
                    AssertCarriedIntact(ctx, player, carried);

                    ctx.AssertEqual(string.Empty, Bank.WithdrawItem(player, stack.Id), "A stack withdrawal with room succeeds");
                    ctx.Assert(DB.Get<InventoryItem>(stack.Id) == null, "The withdrawn stack leaves the bank");
                    ctx.AssertEqual(43 + stack.Quantity, CarriedStackTotal(player, StackResref), "The withdrawn stack joins the carried stack");
                });
            }
            finally
            {
                if (large != null)
                    DB.Delete<InventoryItem>(large.Id);
                if (stack != null)
                    DB.Delete<InventoryItem>(stack.Id);
            }
        }

        [EngineTest("Contract reward removal and delivery claims without inventory room keep escrow and every carried item", Category = "ItemClaimSpace")]
        public static async Task ContractClaimsWithoutRoom(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var draft = new QuestContract { AuthorPlayerId = fixture.Id, Status = QuestContractStatus.Draft };
            var delivery = new QuestContractDelivery { PlayerId = fixture.Id, SourceContractId = draft.Id, Credits = 50 };
            var gold = 0;
            try
            {
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    var source = CreateStorageItem(ctx, LargeItemResref);
                    var data = ObjectPlugin.Serialize(source);
                    DestroyObject(source);
                    draft.RewardItems.Add(new QuestContractItem { Data = data, Name = "Twin Electroblade" });
                    delivery.Items.Add(new QuestContractItem { Data = data, Name = "Twin Electroblade" });
                    DB.Set(draft);
                    DB.Set(delivery);

                    var fillers = FillInventory(ctx, player);
                    var carried = Snapshot(player);
                    gold = GetGold(player);

                    var failure = QuestContractBoard.RemoveRewardItem(player, 0);
                    AssertCarriedIntact(ctx, player, carried);
                    ctx.AssertEqual(1, DB.Get<QuestContract>(draft.Id).RewardItems.Count, "The draft keeps the escrowed reward");
                    ctx.Assert(!string.IsNullOrEmpty(failure), "Removing a reward without room fails");

                    ClaimDeliveries(player);
                    AssertCarriedIntact(ctx, player, carried);
                    var pending = DB.Get<QuestContractDelivery>(delivery.Id);
                    ctx.Assert(pending != null, "The delivery stays pending");
                    ctx.AssertEqual(1, pending.Items.Count, "The delivery keeps the item that did not fit");
                    ctx.AssertEqual(50, pending.Credits, "The delivery keeps its credits while an item is pending");
                    ctx.AssertEqual(gold, GetGold(player), "No credits are paid while an item is pending");

                    foreach (var filler in fillers)
                        DestroyObject(filler);
                });
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    var carried = Snapshot(player);
                    ctx.AssertEqual(string.Empty, QuestContractBoard.RemoveRewardItem(player, 0), "Removing a reward with room succeeds");
                    ctx.AssertEqual(0, DB.Get<QuestContract>(draft.Id).RewardItems.Count, "The reward leaves escrow");
                    ClaimDeliveries(player);
                    ctx.Assert(DB.Get<QuestContractDelivery>(delivery.Id) == null, "The claimed delivery is removed");
                    ctx.AssertEqual(gold + 50, GetGold(player), "The credits are paid once the item is claimed");
                    ctx.AssertEqual(2, CountCarried(player, LargeItemResref), "The reward and the delivery reach the inventory");
                    AssertCarriedIntact(ctx, player, carried);
                });
            }
            finally
            {
                DB.Delete<QuestContractDelivery>(delivery.Id);
                DB.Delete<QuestContract>(draft.Id);
            }
        }

        /// <summary>
        /// Gives the player a 2x1 medkit stack and a 43-herb stack, then fills every other cell with 2x3 outfits and
        /// 1x1 knives. Neither a 2x5 item nor a 1x1 stack then has room. Returns the fillers.
        /// </summary>
        private static List<uint> FillInventory(EngineTestContext ctx, uint player)
        {
            var medkits = CreateItemOnObject("nw_it_medkit001", player, 3);
            var herbs = CreateItemOnObject(StackResref, player, 43);
            ctx.Assert(GetIsObjectValid(medkits) && GetIsObjectValid(herbs), "The player carries a medkit stack and an herb stack");

            var fillers = new List<uint>();
            foreach (var resref in new[] { FillerResref, SmallFillerResref })
            {
                var probe = CreateStorageItem(ctx, resref);
                while (Item.FitsInInventory(player, probe) && fillers.Count < 400)
                {
                    var filler = CreateItemOnObject(resref, player);
                    ctx.Assert(GetItemPossessor(filler) == player, $"A '{resref}' that fits is placed in the inventory");
                    fillers.Add(filler);
                }
                DestroyObject(probe);
            }

            foreach (var resref in new[] { LargeItemResref, StackResref })
            {
                var item = CreateStorageItem(ctx, resref);
                ctx.Assert(!Item.FitsInInventory(player, item), $"The filled inventory has no room for a '{resref}'");
                DestroyObject(item);
            }
            return fillers;
        }

        private static uint CreateStorageItem(EngineTestContext ctx, string resref, int stackSize = 1)
        {
            var item = CreateItemOnObject(resref, GetObjectByTag("TEMP_ITEM_STORAGE"), stackSize);
            ctx.Assert(GetIsObjectValid(item), $"The '{resref}' item is created");
            return item;
        }

        private static Dictionary<uint, (string Resref, int Stack)> Snapshot(uint player)
        {
            var items = new Dictionary<uint, (string, int)>();
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
                items[item] = (GetResRef(item), GetItemStackSize(item));
            return items;
        }

        private static void AssertCarriedIntact(EngineTestContext ctx, uint player, Dictionary<uint, (string Resref, int Stack)> carried)
        {
            var now = Snapshot(player);
            var changed = carried
                .Where(pair => !now.TryGetValue(pair.Key, out var current) || GetItemPossessor(pair.Key) != player ||
                               current.Stack != pair.Value.Stack)
                .Select(pair => $"{pair.Value.Resref} (stack {pair.Value.Stack})")
                .ToList();
            ctx.Assert(changed.Count == 0, $"Every carried item stays in the inventory with its stack size; changed: {string.Join(", ", changed)}");
        }

        private static int CountCarried(uint player, string resref)
        {
            return Snapshot(player).Values.Count(item => item.Resref == resref);
        }

        private static int CarriedStackTotal(uint player, string resref)
        {
            return Snapshot(player).Values.Where(item => item.Resref == resref).Sum(item => item.Stack);
        }

        private static void ClaimDeliveries(uint player)
        {
            // The fixture has no servervault file to save, so the claim checkpoint is a no-op.
            typeof(QuestContractBoard).GetMethod("ClaimDeliveries", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { player, (System.Action)(() => { }) });
        }
    }
}
