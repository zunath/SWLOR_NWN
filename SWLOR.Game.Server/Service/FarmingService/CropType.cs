namespace SWLOR.Game.Server.Service.FarmingService
{
    /// <summary>
    /// The crops a garden plot grows (Player City Revamp, "Farming"): 26 in five tiers. Each is a plant item that harvesting
    /// nodes already give, or the fiberplast a scavenging bush gives at the same tier. The values are stored on garden plots,
    /// so a crop keeps its number.
    /// </summary>
    public enum CropType
    {
        Invalid = 0,

        // Tier 1
        ViscaranOrange = 1,
        ViscaranLemon = 2,
        ViscaranPebble = 3,
        ViscaranRosemary = 4,
        FiberShrubRuined = 5,

        // Tier 2
        ViscaranApple = 6,
        ViscaranPeas = 7,
        PassionFruit = 8,
        MandalorianThyme = 9,
        FiberShrubFlawed = 10,

        // Tier 3
        HutlarAcorn = 11,
        StrangePineapple = 12,
        VegetableClump = 13,
        MonCalanSeaweed = 14,
        FiberShrubGood = 15,

        // Tier 4
        Ginger = 16,
        Melon = 17,
        Mushroom = 18,
        Walnut = 19,
        Cornucopia = 20,
        TatooineDesertSage = 21,
        FiberShrubImperfect = 22,

        // Tier 5
        Turnip = 23,
        Tomato = 24,
        DathomirOregano = 25,
        FiberShrubHigh = 26,
    }
}
