namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>
    /// A trade good carried as units in a ship's cargo bay (plan section 13). Cargo is ship data, never an item.
    /// Persisted by name in PlayerShip.Cargo and TradeExchangeState.Markets, so a rename needs a data migration.
    /// </summary>
    public enum CommodityType
    {
        Invalid = 0,
        VelesHoney = 1,
        ViscaranCrystals = 2,
        KinrathSilk = 3,
        PurifiedWater = 4,
        MedicalSupplies = 5,
        ReactorCores = 6,
        CzerkaMachineParts = 7,
        Grain = 8,
        AraciaTimber = 9,
        Seafood = 10,
        HullSections = 11,
        NeonSalt = 12,
        ByyskHides = 13,
        BanthaHides = 14,
        BeastHides = 15,
        JawaSalvage = 16,
        Moonspice = 17,
        EncryptedDataChips = 18,
        SithRelics = 19,
        WeaponsCrates = 20,
        AbdamarylliaExtract = 21,
        WitchHerbs = 22,
        BanthaMilk = 23,
        StationFuel = 24,

        // Holding goods (Planetary Holdings' Processing Plants).
        RationPacks = 25,
        FocusingLenses = 26,
        PrefabShelters = 27,
        Glitterstim = 28,
        SithHolocrons = 29,
        AgedBanthaCheese = 30,

        // Eshan's export (the Eshan merge, master #2344).
        EshanRice = 31,
    }
}
