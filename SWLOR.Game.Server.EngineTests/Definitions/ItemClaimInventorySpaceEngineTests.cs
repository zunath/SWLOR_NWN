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
        // A 2x5 Twin Electroblade. On 2026-10-18 acquiring one into a nearly full inventory removed a carried
        // 2x1 item and a carried stack.
        private const string LargeItemResref = "twin_elec_1";
        private const string FillerResref = "nw_maletatcivout";

        [EngineTest("A bank withdrawal without inventory room keeps the record and every carried item", Category = "ItemClaimSpace")]
        public static async Task BankWithdrawalWithoutRoom(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            InventoryItem record = null;
            try
            {
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    var fillers = FillInventory(ctx, player);
                    var carried = Snapshot(player);
                    record = Bank.DepositItem(player, CreateStorageItem(ctx, LargeItemResref));

                    ctx.Assert(!string.IsNullOrEmpty(Bank.WithdrawItem(player, record.Id)), "A withdrawal without room fails");
                    ctx.Assert(DB.Get<InventoryItem>(record.Id) != null, "The bank keeps the item that did not fit");
                    AssertCarriedIntact(ctx, player, carried);

                    foreach (var filler in fillers)
                        DestroyObject(filler);
                });
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    var carried = Snapshot(player);
                    ctx.AssertEqual(string.Empty, Bank.WithdrawItem(player, record.Id), "A withdrawal with room succeeds");
                    ctx.Assert(DB.Get<InventoryItem>(record.Id) == null, "The withdrawn item leaves the bank");
                    ctx.AssertEqual(1, CountCarried(player, LargeItemResref), "The withdrawn item reaches the inventory");
                    AssertCarriedIntact(ctx, player, carried);
                });
            }
            finally
            {
                if (record != null)
                    DB.Delete<InventoryItem>(record.Id);
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
                    var data = ObjectPlugin.Serialize(CreateStorageItem(ctx, LargeItemResref));
                    draft.RewardItems.Add(new QuestContractItem { Data = data, Name = "Twin Electroblade" });
                    delivery.Items.Add(new QuestContractItem { Data = data, Name = "Twin Electroblade" });
                    DB.Set(draft);
                    DB.Set(delivery);

                    var fillers = FillInventory(ctx, player);
                    var carried = Snapshot(player);
                    gold = GetGold(player);

                    ctx.Assert(!string.IsNullOrEmpty(QuestContractBoard.RemoveRewardItem(player, 0)), "Removing a reward without room fails");
                    ctx.AssertEqual(1, DB.Get<QuestContract>(draft.Id).RewardItems.Count, "The draft keeps the escrowed reward");
                    AssertCarriedIntact(ctx, player, carried);

                    ClaimDeliveries(player);
                    var pending = DB.Get<QuestContractDelivery>(delivery.Id);
                    ctx.Assert(pending != null, "The delivery stays pending");
                    ctx.AssertEqual(1, pending.Items.Count, "The delivery keeps the item that did not fit");
                    ctx.AssertEqual(50, pending.Credits, "The delivery keeps its credits while an item is pending");
                    ctx.AssertEqual(gold, GetGold(player), "No credits are paid while an item is pending");
                    AssertCarriedIntact(ctx, player, carried);

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
        /// Gives the player a 2x1 medkit stack and a 43-herb stack, then fills the rest of the inventory with 2x3 outfits
        /// until none fits. A 2x5 item then has no room. Returns the outfits.
        /// </summary>
        private static List<uint> FillInventory(EngineTestContext ctx, uint player)
        {
            var medkits = CreateItemOnObject("nw_it_medkit001", player, 3);
            var herbs = CreateItemOnObject("herb_m", player, 43);
            ctx.Assert(GetIsObjectValid(medkits) && GetIsObjectValid(herbs), "The player carries a medkit stack and an herb stack");

            var probe = CreateStorageItem(ctx, FillerResref);
            var fillers = new List<uint>();
            while (Item.FitsInInventory(player, probe) && fillers.Count < 200)
            {
                var filler = CreateItemOnObject(FillerResref, player);
                ctx.Assert(GetItemPossessor(filler) == player, "An outfit that fits is placed in the inventory");
                fillers.Add(filler);
            }
            DestroyObject(probe);

            var large = CreateStorageItem(ctx, LargeItemResref);
            ctx.Assert(!Item.FitsInInventory(player, large), "The filled inventory has no room for a 2x5 item");
            DestroyObject(large);
            return fillers;
        }

        private static uint CreateStorageItem(EngineTestContext ctx, string resref)
        {
            var item = CreateItemOnObject(resref, GetObjectByTag("TEMP_ITEM_STORAGE"));
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
            foreach (var (item, (resref, stack)) in carried)
            {
                ctx.Assert(GetIsObjectValid(item) && GetItemPossessor(item) == player, $"The carried '{resref}' stays in the inventory");
                ctx.AssertEqual(stack, GetItemStackSize(item), $"The carried '{resref}' keeps its stack size");
            }
            var now = Snapshot(player);
            ctx.Assert(carried.Keys.All(now.ContainsKey), "Every carried item is still listed in the inventory");
        }

        private static int CountCarried(uint player, string resref)
        {
            return Snapshot(player).Values.Count(item => item.Resref == resref);
        }

        private static void ClaimDeliveries(uint player)
        {
            // The fixture has no servervault file to save, so the claim checkpoint is a no-op.
            typeof(QuestContractBoard).GetMethod("ClaimDeliveries", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { player, (System.Action)(() => { }) });
        }
    }
}
