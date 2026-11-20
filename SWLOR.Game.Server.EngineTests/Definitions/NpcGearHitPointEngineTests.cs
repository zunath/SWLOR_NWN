using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// NPCs whose maximum HP comes from their blueprint, not an NPCHP budget on their skin, were left with only the
    /// HP of the gear they were given: production logged a 500 HP Jedi Consular set to 50 and raised to its 181 HP
    /// Vitality floor.
    /// </summary>
    public static class NpcGearHitPointEngineTests
    {
        private const string Category = "NpcGearHitPoints";
        private const int GearHitPoints = 50;

        [EngineTest("Gear HP adds to an NPC's blueprint maximum when its skin has no HP budget", Category = Category, TimeoutSeconds = 60f)]
        public static async Task GearHitPointsAddToBlueprintMaximum(EngineTestContext ctx)
        {
            // The Jedi Consular's skin (vnpcssorc3) carries no NPCHP budget; its 500 HP come from the blueprint.
            var npc = ctx.SpawnCreature("vnpcjedicons3");
            await ctx.WaitFrameAsync();
            var blueprintMaxHP = GetMaxHitPoints(npc);
            ctx.AssertEqual(500, blueprintMaxHP, "the blueprint maximum");

            var belt = await CreateHitPointItemAsync(ctx, npc, "advent_belt", GearHitPoints);
            await EquipAsync(ctx, npc, belt, InventorySlot.Belt);
            ctx.AssertEqual(blueprintMaxHP + GearHitPoints, GetMaxHitPoints(npc), "the belt adds its HP to the blueprint maximum");

            await UnequipAsync(ctx, npc, belt);
            ctx.AssertEqual(blueprintMaxHP, GetMaxHitPoints(npc), "removing the belt returns the blueprint maximum");
        }

        private static async Task<uint> CreateHitPointItemAsync(EngineTestContext ctx, uint creature, string resref, int hitPoints)
        {
            var item = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(creature, () => item = CreateItemOnObject(resref, creature));
            await ctx.WaitFrameAsync();
            ctx.Assert(GetIsObjectValid(item), $"create a real {resref}");
            ctx.Track(item);
            for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetFirstItemProperty(item))
                RemoveItemProperty(item, property);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.HPBonus, -1, hitPoints), item);
            return item;
        }

        private static async Task EquipAsync(EngineTestContext ctx, uint creature, uint item, InventorySlot slot)
        {
            await ctx.ExecuteInCreatureContextAsync(creature, () => ActionEquipItem(item, slot));
            await ctx.WaitUntilAsync(() => GetItemInSlot(slot, creature) == item, 10f, $"equip {GetResRef(item)}");
            await ctx.WaitFrameAsync();
        }

        private static async Task UnequipAsync(EngineTestContext ctx, uint creature, uint item)
        {
            var slot = Item.GetItemSlot(creature, item);
            await ctx.ExecuteInCreatureContextAsync(creature, () => ActionUnequipItem(item));
            await ctx.WaitUntilAsync(() => GetItemInSlot(slot, creature) != item, 10f, $"unequip {GetResRef(item)}");
            await ctx.WaitFrameAsync();
        }
    }
}
