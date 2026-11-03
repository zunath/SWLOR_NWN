using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CityStoreService
{
    /// <summary>
    /// Every city store: the store blueprint (<c>Module/utm</c>) that opens at each building or city level, and the
    /// limited items with weekly allowances. Each blueprint holds the whole stock of its level, so a higher level's
    /// blueprint lists everything the lower ones do. Blueprints use only items that existing NPC stores sell (and the first
    /// crop seeds); the exclusive items are the limited ones.
    /// </summary>
    public static class CityStoreCatalog
    {
        // PROPOSAL - NEEDS THE OWNER'S APPROVAL: the exclusive items below, with their unlock levels, weekly limits and prices.
        // The unlock level is the store's level (the city level for City Hall, the building's for the others, and 1 for a stall).
        // The prices are the items' own value and are written by tools/CityExclusives/generate.py from tools/CityExclusives/catalog.py,
        // which also holds the names and the stats each drink copies. Comparable prices: a Cantina shot 249, the Decor stall's trinkets
        // 151, crafted furniture 100, a Mk III mining laser 400. Food and drink cost 250 a tier; decor 250, or 400 for the larger pieces.
        // The weekly limits are in line with the travelers' (1 to 6 for rare stock, 2 to 4 for seeds). Each item is sold only by the
        // store it is listed under, and its allowance belongs to the city and that store (CityStockAllowance).

        // Cantina house drinks, each a copy of the "au lait" of its tier: Orange (250), Pineapple (750) and Tomato (1,250).
        private static readonly CityStoreLimitedItem[] CantinaLimitedItems =
        {
            new CityStoreLimitedItem("house_citrus", 1, 12),
            new CityStoreLimitedItem("house_sunrise", 3, 8),
            new CityStoreLimitedItem("house_reserve", 5, 5)
        };

        // City banners (250) and crests (400), decor structures 5190 to 5193.
        private static readonly CityStoreLimitedItem[] CityHallLimitedItems =
        {
            new CityStoreLimitedItem("structure_5190", 1, 6),
            new CityStoreLimitedItem("structure_5191", 1, 6),
            new CityStoreLimitedItem("structure_5192", 2, 4),
            new CityStoreLimitedItem("structure_5193", 2, 4)
        };

        // Clinic Display Panel (250) and Clinic Bacta Tank (400), decor structures 5194 and 5195.
        private static readonly CityStoreLimitedItem[] MedicalCenterLimitedItems =
        {
            new CityStoreLimitedItem("structure_5194", 1, 4),
            new CityStoreLimitedItem("structure_5195", 3, 3)
        };

        // Spaceport Signpost and Wall Sign (250 each), decor structures 5196 and 5197.
        private static readonly CityStoreLimitedItem[] StarportLimitedItems =
        {
            new CityStoreLimitedItem("structure_5196", 1, 4),
            new CityStoreLimitedItem("structure_5197", 3, 4)
        };

        // Stallkeeper's Pot Pie (500), a copy of the tier 2 Cairnmog Pot Pie.
        private static readonly CityStoreLimitedItem[] GeneralGoodsStallLimitedItems =
        {
            new CityStoreLimitedItem("stall_pie", 1, 10)
        };

        // Artisans' Guild Banner (250), decor structure 5199.
        private static readonly CityStoreLimitedItem[] CraftingSuppliesStallLimitedItems =
        {
            new CityStoreLimitedItem("structure_5199", 1, 4)
        };

        // Pavilion Banner (250), decor structure 5198.
        private static readonly CityStoreLimitedItem[] DecorStallLimitedItems =
        {
            new CityStoreLimitedItem("structure_5198", 1, 4)
        };

        private static readonly Dictionary<CityStoreKind, CityStoreDetail> _stores = new()
        {
            [CityStoreKind.CityHall] = new CityStoreDetail(
                CityStoreKind.CityHall,
                "City Hall Supplies",
                new[] { (1, "cs_hall_1"), (2, "cs_hall_2"), (3, "cs_hall_3") },
                CityHallLimitedItems),

            [CityStoreKind.Cantina] = new CityStoreDetail(
                CityStoreKind.Cantina,
                "Cantina Bar",
                new[] { (1, "cs_cantina_1"), (2, "cs_cantina_2"), (3, "cs_cantina_3") },
                CantinaLimitedItems),

            [CityStoreKind.MedicalCenter] = new CityStoreDetail(
                CityStoreKind.MedicalCenter,
                "Medical Supplies",
                new[] { (1, "cs_medical_1") },
                MedicalCenterLimitedItems),

            // A level 1 Starport sells the dockhand store's full stock, mining lasers included, so no city loses anything it
            // sells today. A higher level only adds to it (the limited items below), and never takes anything away.
            [CityStoreKind.Starport] = new CityStoreDetail(
                CityStoreKind.Starport,
                "Starport Ship Supplies",
                new[] { (1, "cs_port_1") },
                StarportLimitedItems),

            [CityStoreKind.StallGeneralGoods] = new CityStoreDetail(
                CityStoreKind.StallGeneralGoods,
                "General Goods Stall",
                new[] { (1, "cs_stall_goods") },
                GeneralGoodsStallLimitedItems),

            [CityStoreKind.StallCraftingSupplies] = new CityStoreDetail(
                CityStoreKind.StallCraftingSupplies,
                "Crafting Supplies Stall",
                new[] { (1, "cs_stall_craft") },
                CraftingSuppliesStallLimitedItems),

            [CityStoreKind.StallDecor] = new CityStoreDetail(
                CityStoreKind.StallDecor,
                "Decor Stall",
                new[] { (1, "cs_stall_decor") },
                DecorStallLimitedItems),
        };

        public static IReadOnlyCollection<CityStoreDetail> All => _stores.Values;

        /// <summary>The store's definition, or null for a kind with no store.</summary>
        public static CityStoreDetail Get(CityStoreKind kind)
        {
            return _stores.TryGetValue(kind, out var detail) ? detail : null;
        }
    }
}
