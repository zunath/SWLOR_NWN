namespace SWLOR.Game.Server.Service.CityStoreService
{
    /// <summary>
    /// Which city store a shelf of stock belongs to: a building's store or a market stall's stock theme. Weekly
    /// allowances are saved under this value, so the numbers never change.
    /// </summary>
    public enum CityStoreKind
    {
        Invalid = 0,

        /// <summary>The City Hall clerk's general goods. Better stock opens with the city level.</summary>
        CityHall = 1,

        /// <summary>The Cantina bartender's food and drink. Better stock opens with the Cantina level.</summary>
        Cantina = 2,

        /// <summary>The Medical Center medic's supplies. Better stock opens with the Medical Center level.</summary>
        MedicalCenter = 3,

        /// <summary>The Starport dockmaster's ship supplies. Better stock opens with the Starport level.</summary>
        Starport = 4,

        /// <summary>A market stall whose mayor picked general goods.</summary>
        StallGeneralGoods = 5,

        /// <summary>A market stall whose mayor picked crafting supplies.</summary>
        StallCraftingSupplies = 6,

        /// <summary>A market stall whose mayor picked decor.</summary>
        StallDecor = 7
    }
}
