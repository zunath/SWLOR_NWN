using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Entity;
using Newtonsoft.Json;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class CraftingEngineTests
    {
        [EngineTest("Crafting reserves no inventory when a required material is missing", Category = "Crafting")]
        public static Task MissingMaterialLeavesInventoryUntouched(EngineTestContext ctx) => CheckReservation(ctx, false);

        [EngineTest("Crafting reserves exact serialized quantities and leaves the remaining stack intact", Category = "Crafting")]
        public static Task CompleteMaterialBudgetSerializesExactStacks(EngineTestContext ctx) => CheckReservation(ctx, true);

        [EngineTest("Crafting profession stats cannot leak into other skills or global combat stats", Category = "Crafting")]
        public static async Task ProfessionStatsAreScoped(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian"); await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                SetLocalInt(owner, $"PERK_LEVEL_{(int)PerkType.TemperedStrikes}", 2);
                ctx.AssertEqual(3, Stat.GetCraftingStatAdjustment(owner, SkillType.Smithery, StatType.CraftingWorkDurabilityRestore), "Smithery rank II replaces rank I");
                ctx.AssertEqual(0, Stat.GetCraftingStatAdjustment(owner, SkillType.Engineering, StatType.CraftingWorkDurabilityRestore), "Engineering receives no Smithery restoration");
                ctx.AssertEqual(0, Stat.GetStatAdjustment(owner, StatType.CraftingWorkDurabilityRestore), "The global stat path excludes scoped bonuses");
            });
        }

        [EngineTest("Crafting journal replays partial stack debits and persisted reward deliveries exactly once", Category = "Crafting")]
        public static async Task JournalReplaysWithoutDuplicatingMaterialsOrRewards(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian"); await ctx.WaitFrameAsync();
            string playerId = null; CraftingTransaction transaction = null; string readyReceipt = null;
            try
            {
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    playerId = GetObjectUUID(owner); DB.Set(new Player(playerId));
                    var item = CreateItemOnObject("kath_blood", owner); SetItemStackSize(item, 10);
                    var reserved = CraftingJournal.SerializeQuantity(item, 3);
                    var state = CraftSession.CreateLegacy(50, 50, new RecipeLevelDetail(186, 2641, 80, 0), 30, 29, 37);
                    transaction = CraftingJournal.Begin(owner, RecipeType.VenomCoating1, state,
                        new[] { new CraftComponentReservation(item, 3) }, new[] { reserved }, Array.Empty<string>(), OBJECT_INVALID);
                    ctx.AssertEqual(7, GetItemStackSize(item), "Only three materials are consumed");
                    CraftingJournal.CompletePreparation(owner, transaction);
                    ctx.AssertEqual(7, GetItemStackSize(item), "Replaying commitment consumes nothing more");
                    var restored = JsonConvert.DeserializeObject<CraftingTransaction>(JsonConvert.SerializeObject(transaction));
                    ctx.AssertEqual(state.Id, restored.Session.Id, "Receipt preserves the committed session ID");
                    restored.Session = restored.Session with { Status = CraftSessionStatus.Failed };
                    CraftingJournal.PrepareRewards(owner, restored, new[] { reserved }, 0, false);
                    readyReceipt = JsonConvert.SerializeObject(restored);
                    ctx.Assert(CraftingJournal.DeliverRewards(owner, restored), "Saved reward is delivered");
                });
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    var replay = JsonConvert.DeserializeObject<CraftingTransaction>(readyReceipt);
                    CraftingJournal.Save(owner, replay);
                    ctx.Assert(CraftingJournal.DeliverRewards(owner, replay), "Unacknowledged delivery is safely replayed");
                    ctx.AssertEqual(10, CraftingJournal.Inventory(owner).Where(item => GetResRef(item) == "kath_blood").Sum(GetItemStackSize),
                        "One refund restores ten total materials, never thirteen");
                    ctx.Assert(CraftingJournal.Get(owner) == null, "Completed receipt is cleared");
                });
            }
            finally
            {
                if (playerId != null) await ctx.ExecuteInCreatureContextAsync(owner, () => DB.Delete<Player>(playerId));
            }
        }

        [EngineTest("Crafting serialization leaves selected enhancement stacks untouched before commitment", Category = "Crafting")]
        public static async Task SetupSerializationDoesNotConsumeInventory(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian"); await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var item = CreateItemOnObject("kath_blood", owner); SetItemStackSize(item, 10);
                var serialized = CraftingJournal.SerializeQuantity(item, 1);
                ctx.AssertEqual(10, GetItemStackSize(item), "Setup keeps the entire source stack in inventory");
                var copy = ObjectPlugin.Deserialize(serialized); ctx.Track(copy);
                ctx.AssertEqual(1, GetItemStackSize(copy), "The selected enhancement represents exactly one item");
            });
        }

        [EngineTest("Crafting receipt replays blueprint fees and the final licensed run without a second charge", Category = "Crafting")]
        public static async Task BlueprintPaymentIsIdempotent(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian"); await ctx.WaitFrameAsync();
            string playerId = null; CraftingTransaction receipt = null; int goldAfter = 0;
            try
            {
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    playerId = GetObjectUUID(owner); DB.Set(new Player(playerId));
                    GiveGoldToCreature(owner, 10000);
                    var blueprint = CreateItemOnObject("blueprint", owner);
                    Craft.SetBlueprintDetails(blueprint, new BlueprintDetail { Recipe = RecipeType.VenomCoating1, Level = 1, LicensedRuns = 1 });
                    var state = CraftSession.CreateLegacy(50, 50, new RecipeLevelDetail(186, 2641, 80, 0), 30, 29, 37);
                    receipt = CraftingJournal.Begin(owner, RecipeType.VenomCoating1, state, Array.Empty<CraftComponentReservation>(),
                        Array.Empty<string>(), Array.Empty<string>(), blueprint);
                    goldAfter = GetGold(owner);
                    ctx.AssertEqual(receipt.GoldBefore - receipt.CreditCost, goldAfter, "Exactly one fee is paid");
                });
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    // Simulate the database acknowledgement being lost after the vault was saved.
                    receipt.Phase = CraftingTransactionPhase.Preparing;
                    CraftingJournal.Save(owner, receipt); CraftingJournal.CompletePreparation(owner, receipt);
                    ctx.AssertEqual(goldAfter, GetGold(owner), "Replaying the last license never charges again");
                    ctx.Assert(!CraftingJournal.Inventory(owner).Any(item => GetResRef(item) == "blueprint"), "The final licensed blueprint stays consumed");
                });
            }
            finally
            {
                if (playerId != null) await ctx.ExecuteInCreatureContextAsync(owner, () => DB.Delete<Player>(playerId));
            }
        }

        private static async Task CheckReservation(EngineTestContext ctx, bool complete)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var blood = OBJECT_INVALID;
            var herb = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                blood = CreateItemOnObject("kath_blood", owner);
                herb = CreateItemOnObject("herb_v", owner);
                ctx.Assert(GetIsObjectValid(blood) && GetIsObjectValid(herb), "Both material fixtures exist");
                SetItemStackSize(blood, 10);
                SetItemStackSize(herb, complete ? 2 : 1);
            });
            await ctx.WaitFrameAsync();

            var serialized = new List<string>();
            var reservations = new List<CraftComponentReservation>();
            string playerId = null;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var recipe = Craft.GetRecipe(RecipeType.VenomCoating1);
                reservations = CraftComponentBudget.Plan(recipe.Components, new[] {
                    new CraftComponentStack(blood, GetResRef(blood), GetItemStackSize(blood)),
                    new CraftComponentStack(herb, GetResRef(herb), GetItemStackSize(herb)) }).ToList();
                if (reservations.Count == 0) return;
                serialized.AddRange(reservations.Select(entry => CraftingJournal.SerializeQuantity(entry.Item, entry.Quantity)));
                playerId = GetObjectUUID(owner); DB.Set(new Player(playerId));
                try
                {
                    CraftingJournal.Begin(owner, RecipeType.VenomCoating1,
                        CraftSession.CreateLegacy(50, 50, new RecipeLevelDetail(186, 2641, 80, 0), 30, 29, 37),
                        reservations, serialized, Array.Empty<string>(), OBJECT_INVALID);
                }
                finally { DB.Delete<Player>(playerId); }
            });
            await ctx.WaitFrameAsync();
            if (!complete)
            {
                ctx.AssertEqual(0, reservations.Count, "Missing materials produce no reservation plan");
                ctx.AssertEqual(0, serialized.Count, "Nothing awaits a delayed partial refund");
                ctx.AssertEqual(10, GetItemStackSize(blood), "The available material stack is unchanged");
                ctx.AssertEqual(1, GetItemStackSize(herb), "The insufficient material stack is unchanged");
                return;
            }
            ctx.AssertEqual(2, reservations.Count, "Both component entries are reserved");
            ctx.AssertEqual(2, serialized.Count, "Both reserved quantities are serialized");
            ctx.AssertEqual(7, GetItemStackSize(blood), "Only the required blood quantity is removed");
            ctx.Assert(!GetIsObjectValid(herb), "The fully reserved herb stack is consumed");
            var restored = new List<uint>();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                foreach (var data in serialized)
                {
                    var item = ObjectPlugin.Deserialize(data);
                    ctx.Track(item);
                    restored.Add(item);
                }
            });
            ctx.AssertEqual(3, restored.Where(item => GetResRef(item) == "kath_blood").Sum(GetItemStackSize),
                "Serialized reservation contains exactly three blood");
            ctx.AssertEqual(2, restored.Where(item => GetResRef(item) == "herb_v").Sum(GetItemStackSize),
                "Serialized reservation contains exactly two herbs");
        }
    }
}
