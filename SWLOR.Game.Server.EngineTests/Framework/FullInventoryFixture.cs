using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Fills a creature's inventory so that neither a 2x5 item nor a 1x1 stack fits, and checks afterwards that every
    /// carried item is still there.
    /// </summary>
    public static class FullInventoryFixture
    {
        public const string LargeItemResref = "twin_elec_1"; // 2x5 Twin Electroblade
        public const string StackResref = "herb_m";
        public const int CarriedStackSize = 43;
        private const string FillerResref = "nw_maletatcivout";
        private const string SmallFillerResref = "survival_knife";

        /// <summary>
        /// Gives the creature a 2x1 medkit stack and a herb stack, then fills every other cell with 2x3 outfits and 1x1
        /// knives. Returns the fillers.
        /// </summary>
        public static List<uint> Fill(EngineTestContext ctx, uint creature)
        {
            var medkits = CreateItemOnObject("nw_it_medkit001", creature, 3);
            var herbs = CreateItemOnObject(StackResref, creature, CarriedStackSize);
            ctx.Assert(GetIsObjectValid(medkits) && GetIsObjectValid(herbs), "The creature carries a medkit stack and an herb stack");

            var fillers = new List<uint>();
            foreach (var resref in new[] { FillerResref, SmallFillerResref })
            {
                var probe = CreateStorageItem(ctx, resref);
                while (Item.FitsInInventory(creature, probe) && fillers.Count < 400)
                {
                    var filler = CreateItemOnObject(resref, creature);
                    ctx.Assert(GetItemPossessor(filler) == creature, $"A '{resref}' that fits is placed in the inventory");
                    fillers.Add(filler);
                }
                DestroyObject(probe);
            }

            foreach (var resref in new[] { LargeItemResref, StackResref })
            {
                var item = CreateStorageItem(ctx, resref);
                ctx.Assert(!Item.FitsInInventory(creature, item), $"The filled inventory has no room for a '{resref}'");
                DestroyObject(item);
            }
            return fillers;
        }

        public static uint CreateStorageItem(EngineTestContext ctx, string resref, int stackSize = 1)
        {
            var item = CreateItemOnObject(resref, GetObjectByTag("TEMP_ITEM_STORAGE"), stackSize);
            ctx.Assert(GetIsObjectValid(item), $"The '{resref}' item is created");
            return item;
        }

        /// <summary>Serializes a new item and destroys it, leaving only the data a record would hold.</summary>
        public static string SerializeNewItem(EngineTestContext ctx, string resref, int stackSize = 1)
        {
            var item = CreateStorageItem(ctx, resref, stackSize);
            var data = ObjectPlugin.Serialize(item);
            DestroyObject(item);
            return data;
        }

        public static Dictionary<uint, (string Resref, int Stack)> Snapshot(uint creature)
        {
            var items = new Dictionary<uint, (string, int)>();
            for (var item = GetFirstItemInInventory(creature); GetIsObjectValid(item); item = GetNextItemInInventory(creature))
                items[item] = (GetResRef(item), GetItemStackSize(item));
            return items;
        }

        public static void AssertCarriedIntact(EngineTestContext ctx, uint creature, Dictionary<uint, (string Resref, int Stack)> carried)
        {
            var now = Snapshot(creature);
            var changed = carried
                .Where(pair => !now.TryGetValue(pair.Key, out var current) || GetItemPossessor(pair.Key) != creature ||
                               current.Stack != pair.Value.Stack)
                .Select(pair => $"{pair.Value.Resref} (stack {pair.Value.Stack})")
                .ToList();
            ctx.Assert(changed.Count == 0, $"Every carried item stays in the inventory with its stack size; changed: {string.Join(", ", changed)}");
        }

        public static int CountCarried(uint creature, string resref)
        {
            return Snapshot(creature).Values.Count(item => item.Resref == resref);
        }

        public static int CarriedStackTotal(uint creature, string resref)
        {
            return Snapshot(creature).Values.Where(item => item.Resref == resref).Sum(item => item.Stack);
        }

        public static void Free(IEnumerable<uint> fillers)
        {
            foreach (var filler in fillers)
                DestroyObject(filler);
        }
    }
}
