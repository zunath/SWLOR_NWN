using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CityStoreService
{
    /// <summary>
    /// Every city store: the store blueprint (<c>Module/utm</c>) that opens at each building or city level, and the
    /// limited items with weekly allowances. Each blueprint holds the whole stock of its level, so a higher level's
    /// blueprint lists everything the lower ones do. Blueprints use only items that existing NPC stores sell.
    /// </summary>
    public static class CityStoreCatalog
    {
        /// <summary>
        /// The exclusive items (house drinks, banners, crests) are a later content step: they join a store's
        /// limited items here, with a weekly allowance each.
        /// </summary>
        private static readonly CityStoreLimitedItem[] NoLimitedItems = System.Array.Empty<CityStoreLimitedItem>();

        private static readonly Dictionary<CityStoreKind, CityStoreDetail> _stores = new()
        {
            [CityStoreKind.CityHall] = new CityStoreDetail(
                CityStoreKind.CityHall,
                "City Hall Supplies",
                new[] { (1, "cs_hall_1"), (2, "cs_hall_2"), (3, "cs_hall_3") },
                NoLimitedItems),

            [CityStoreKind.Cantina] = new CityStoreDetail(
                CityStoreKind.Cantina,
                "Cantina Bar",
                new[] { (1, "cs_cantina_1"), (2, "cs_cantina_2"), (3, "cs_cantina_3") },
                NoLimitedItems),

            [CityStoreKind.MedicalCenter] = new CityStoreDetail(
                CityStoreKind.MedicalCenter,
                "Medical Supplies",
                new[] { (1, "cs_medical_1") },
                NoLimitedItems),

            [CityStoreKind.Starport] = new CityStoreDetail(
                CityStoreKind.Starport,
                "Starport Ship Supplies",
                new[] { (1, "cs_port_1"), (2, "cs_port_2"), (3, "cs_port_3"), (4, "cs_port_4"), (5, "cs_port_5") },
                NoLimitedItems),

            [CityStoreKind.StallGeneralGoods] = new CityStoreDetail(
                CityStoreKind.StallGeneralGoods,
                "General Goods Stall",
                new[] { (1, "cs_stall_goods") },
                NoLimitedItems),

            [CityStoreKind.StallCraftingSupplies] = new CityStoreDetail(
                CityStoreKind.StallCraftingSupplies,
                "Crafting Supplies Stall",
                new[] { (1, "cs_stall_craft") },
                NoLimitedItems),

            [CityStoreKind.StallDecor] = new CityStoreDetail(
                CityStoreKind.StallDecor,
                "Decor Stall",
                new[] { (1, "cs_stall_decor") },
                NoLimitedItems),
        };

        public static IReadOnlyCollection<CityStoreDetail> All => _stores.Values;

        /// <summary>The store's definition, or null for a kind with no store.</summary>
        public static CityStoreDetail Get(CityStoreKind kind)
        {
            return _stores.TryGetValue(kind, out var detail) ? detail : null;
        }
    }
}
