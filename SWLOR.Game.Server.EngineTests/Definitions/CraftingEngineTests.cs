using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class CraftingEngineTests
    {
        [EngineTest("Crafting reserves no inventory when a required material is missing", Category = "Crafting")]
        public static Task MissingMaterialLeavesInventoryUntouched(EngineTestContext ctx) => CheckReservation(ctx, false);

        [EngineTest("Crafting reserves exact serialized quantities and leaves the remaining stack intact", Category = "Crafting")]
        public static Task CompleteMaterialBudgetSerializesExactStacks(EngineTestContext ctx) => CheckReservation(ctx, true);

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

            var view = new CraftViewModel();
            typeof(CraftViewModel).GetField("_recipe", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(view, RecipeType.VenomCoating1);
            var reserved = new List<uint>();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                reserved = (List<uint>)typeof(CraftViewModel)
                    .GetMethod("AggregateComponents", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(view, new object[] { new List<uint> { blood, herb } });
            });
            await ctx.WaitFrameAsync();
            var serialized = (List<string>)typeof(CraftViewModel)
                .GetField("_components", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
            if (!complete)
            {
                ctx.AssertEqual(0, reserved.Count, "Missing materials produce no reservation plan");
                ctx.AssertEqual(0, serialized.Count, "Nothing awaits a delayed partial refund");
                ctx.AssertEqual(10, GetItemStackSize(blood), "The available material stack is unchanged");
                ctx.AssertEqual(1, GetItemStackSize(herb), "The insufficient material stack is unchanged");
                return;
            }
            ctx.AssertEqual(2, reserved.Count, "Both component entries are reserved");
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
