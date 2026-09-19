using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class ItemActivationEngineTests
    {
        [EngineTest("Activation repair traverses carried bags without changing stacks", Category = "ItemActivation")]
        public static Task RepairCarriedBag(EngineTestContext ctx) => VerifyBagRepair(ctx, false);

        [EngineTest("Acquiring a bag repairs legacy consumables inside it", Category = "ItemActivation")]
        public static Task RepairAcquiredBag(EngineTestContext ctx) => VerifyBagRepair(ctx, true);

        private static async Task VerifyBagRepair(EngineTestContext ctx, bool acquire)
        {
            var owner = ctx.SpawnCreature("civilian");
            var recipient = ctx.SpawnCreature("civilian", 2f);
            await ctx.WaitFrameAsync();
            var bag = await CreateItem(ctx, owner, "bag_ex", owner);
            var kit = await CreateItem(ctx, owner, "trap_kit_1", bag);
            var poison = await CreateItem(ctx, owner, "poison_vial_1", bag);
            var concentrate = await CreateItem(ctx, owner, "conc_poison_1", bag);
            var repaired = await CreateItem(ctx, owner, "trap_kit_2", bag);
            var ordinary = await CreateItem(ctx, owner, "ref_veldite", bag);

            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                foreach (var item in new[] { kit, poison, concentrate })
                {
                    var properties = new List<SWLOR.NWN.API.Engine.ItemProperty>();
                    for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
                        if (GetItemPropertyType(property) == ItemPropertyType.CastSpell)
                            properties.Add(property);
                    foreach (var property in properties)
                        RemoveItemProperty(item, property);
                    SetItemStackSize(item, 5);
                }
            });
            await ctx.WaitFrameAsync();

            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                foreach (var item in new[] { kit, poison, concentrate })
                    ctx.AssertEqual(0, CountActivations(item), "Fixture represents a legacy consumable without activation");

                if (acquire)
                    ctx.Assert(ItemPlugin.MoveTo(bag, recipient, true), "The recipient acquires the entire bag");
                else
                    Repair(bag);
            });
            await ctx.WaitUntilAsync(() => CountActivations(kit) == 1 && CountActivations(poison) == 1 &&
                CountActivations(concentrate) == 1, 3f, "all contained legacy consumables to receive activations");

            await ctx.ExecuteInCreatureContextAsync(acquire ? recipient : owner, () =>
            {
                Repair(bag);
                Repair(kit);
                Repair(OBJECT_INVALID);
                foreach (var item in new[] { kit, poison, concentrate, repaired })
                {
                    ctx.AssertEqual(1, CountActivations(item), "Repeated repair does not duplicate activation properties");
                    ctx.AssertEqual(bag, GetItemPossessor(item, true), "Repair leaves each item inside the bag");
                    var expected = item == kit || item == repaired ? CastSpell.UNIQUE_POWER_SELF_ONLY : CastSpell.UNIQUE_POWER;
                    for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
                    {
                        if (GetItemPropertyType(property) != ItemPropertyType.CastSpell) continue;
                        ctx.AssertEqual((int)expected, GetItemPropertySubType(property), "The repaired item uses its declared activation");
                        ctx.AssertEqual((int)CastSpellNumberUses.UNLIMITED_USE, GetItemPropertyCostTableValue(property),
                            "The scripted handler controls consumption");
                    }
                }
                foreach (var item in new[] { kit, poison, concentrate })
                    ctx.AssertEqual(5, GetItemStackSize(item), "Repair preserves the legacy stack");
                ctx.AssertEqual(0, CountActivations(bag), "Unregistered containers receive no activation");
                ctx.AssertEqual(0, CountActivations(ordinary), "Ordinary contained items receive no activation");
            });
        }

        private static async Task<uint> CreateItem(EngineTestContext ctx, uint owner, string resref, uint container)
        {
            var item = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(owner, () => item = CreateItemOnObject(resref, container));
            await ctx.WaitFrameAsync();
            ctx.Assert(GetIsObjectValid(item), $"Fixture item {resref} exists");
            ctx.AssertEqual(container, GetItemPossessor(item, true), $"Fixture item {resref} is inside its intended container");
            return item;
        }

        private static void Repair(uint item) => typeof(Item)
            .GetMethod("EnsureItemAndContentsActivations", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { item });

        private static int CountActivations(uint item)
        {
            var count = 0;
            for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
                if (GetItemPropertyType(property) == ItemPropertyType.CastSpell) count++;
            return count;
        }
    }
}
