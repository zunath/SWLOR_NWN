using System;

namespace SWLOR.Game.Server.Enumeration
{
    public enum FurnitureType
    {
        [Furniture("Invalid", "", "Invalid", false)]
        Invalid = 0,
        [Furniture("Obelisk, Large", "x0_obelisk", "", true)]
        ObeliskLarge = 4,
        [Furniture("Jukebox", "jukebox", "", true)]
        Jukebox = 5,
        [Furniture("Obelisk, Small", "nw_plc_seaobelsk", "", true)]
        ObeliskSmall = 6,
        [Furniture("Ladder, Light", "x0_weatheredladd", "", true)]
        LadderLight = 7,
        [Furniture("Ladder, Dark", "x0_ladder", "", true)]
        LadderDark = 8,
        [Furniture("Statue, Huge", "x2_plc_statue_h", "", true)]
        StatueHuge = 9,
        [Furniture("Mining Well", "x2_plc_minewell", "", true)]
        MiningWell = 10,
        [Furniture("Statue, Wizard", "x3_plc_gg001", "", true)]
        StatueWizard = 12,
        [Furniture("Gong", "plc_gong", "", true)]
        Gong = 13,
        [Furniture("Statue, Monster", "plc_statue3", "", true)]
        StatueMonster = 14,
        [Furniture("Brazier, Stone", "plc_brazier", "", true)]
        BrazierStone = 17,
        [Furniture("Statue, Guardian", "plc_statue1", "", true)]
        StatueGuardian = 18,
        [Furniture("Doorway, Metal", "x0_doormetal", "", true)]
        DoorwayMetal = 19,
        [Furniture("Flaming Statue", "x2_plc_statue_fl", "", true)]
        FlamingStatue = 20,
        [Furniture("Brazier, Round", "x3_plc_brazier", "", true)]
        BrazierRound = 21,
        [Furniture("Pedestal", "plc_pedestal", "", true)]
        Pedestal = 22,
        [Furniture("Rope Coil", "nw_plc_ropecoil1", "", true)]
        RopeCoil = 23,
        [Furniture("Statue, Wyvern ", "x3_plc_statuew", "", true)]
        StatueWyvern = 24,
        [Furniture("Pedestal, Evil", "x3_plc_pedestal", "", true)]
        PedestalEvil = 25,
        [Furniture("Birdbath", "plc_birdbath", "", true)]
        Birdbath = 26,
        [Furniture("Sphinx Statue", "x0_sphinxstatue", "", true)]
        SphinxStatue = 27,
        [Furniture("Mining Well Platform", "x2_plc_minewellb", "", true)]
        MiningWellPlatform = 28,
        [Furniture("Pedestal, Sword", "x3_plc_pedsword", "", true)]
        PedestalSword = 29,
        [Furniture("Doorway, Stone", "x0_stonewalldoor", "", true)]
        DoorwayStone = 30,
        [Furniture("Female Statue", "x2_plc_statue_f", "", true)]
        FemaleStatue = 31,
        [Furniture("Gnomish Contraption", "plc_gnomcntrptn", "", true)]
        GnomishContraption = 32,
        [Furniture("Pillar, Stone", "plc_pillar3", "", true)]
        PillarStone = 33,
        [Furniture("Statue of Lathander", "x2_plc_statue_la", "", true)]
        StatueofLathander = 35,
        [Furniture("Pillar, Flame", "plc_pillar1", "", true)]
        PillarFlame = 36,
        [Furniture("Cage", "plc_animalcage", "", true)]
        Cage = 37,
        [Furniture("Lamp Post", "plc_lamppost", "", true)]
        LampPost = 38,
        [Furniture("Torch Bracket", "plc_freetorch", "", true)]
        TorchBracket = 39,
        [Furniture("Pillar, Wood", "plc_pillar2", "", true)]
        PillarWood = 40,
        [Furniture("Statue, Cyric", "x3_plc_statuec", "", true)]
        StatueCyric = 41,
        [Furniture("Sea Idol", "nw_plc_seaidol", "", true)]
        SeaIdol = 42,
        [Furniture("Fountain", "plc_fountain", "", true)]
        Fountain = 43,
        [Furniture("Monster Statue", "x2_plc_statue_mo", "", true)]
        MonsterStatue = 44,
        [Furniture("Easel", "plc_pntingeasel", "", true)]
        Easel = 45,
        [Furniture("Keg", "plc_keg", "", true)]
        Keg = 47,
        [Furniture("Dran Statue ", "x2_plc_statu_dra", "", true)]
        DranStatue = 48,
        [Furniture("Net", "nw_plc_net", "", true)]
        Net = 51,
        [Furniture("Bed, Extra Large", "x0_largebed", "", true)]
        BedExtraLarge = 52,
        [Furniture("Carpet, Round, Blue", "x0_roundrugorien", "", true)]
        CarpetRoundBlue = 54,
        [Furniture("Altar, Evil", "plc_altrevil", "", true)]
        AltarEvil = 55,
        [Furniture("Table, Wood, Large", "x3_plc_table002", "", true)]
        TableWoodLarge = 56,
        [Furniture("Table, Wood, With Fish", "x3_plc_table001", "", true)]
        TableWoodWithFish = 57,
        [Furniture("Table, Stone, Small", "nw_plc_seatable", "", true)]
        TableStoneSmall = 58,
        [Furniture("Altar, Hand", "plc_altrod", "", true)]
        AltarHand = 59,
        [Furniture("Window", "x0_window", "", true)]
        Window = 60,
        [Furniture("Cushions", "x0_cushions", "", true)]
        Cushions = 61,
        [Furniture("Candle", "nw_plc_candle1", "", true)]
        Candle = 62,
        [Furniture("Bear Skin Rug", "x0_bearskinrug1", "", true)]
        BearSkinRug = 64,
        [Furniture("Chandelier", "x0_chandelier", "", true)]
        Chandelier = 65,
        [Furniture("Urn", "plc_urn", "", true)]
        Urn = 67,
        [Furniture("Altar, Stone", "plc_altrneutral", "", true)]
        AltarStone = 68,
        [Furniture("Cot", "plc_cot", "", true)]
        Cot = 69,
        [Furniture("Table, Wood", "plc_table", "", true)]
        TableWood = 70,
        [Furniture("Throw Rug", "plc_throwrug", "", true)]
        ThrowRug = 72,
        [Furniture("Table, Stone, Large", "nw_plc_dwarftabl", "", true)]
        TableStoneLarge = 73,
        [Furniture("Bed, Stone, Yellow", "nw_plc_dwarfbed", "", true)]
        BedStoneYellow = 74,
        [Furniture("Bed, Large", "x0_beddouble", "", true)]
        BedLarge = 75,
        [Furniture("Vase, Rounded", "x0_vaseflower", "", true)]
        VaseRounded = 76,
        [Furniture("Carpet", "x0_ruglarge", "", true)]
        Carpet = 77,
        [Furniture("Bed, Wood, Yellow", "plc_bed", "", true)]
        BedWoodYellow = 78,
        [Furniture("Overgrown Pillar", "x0_overgrownrui", "", true)]
        OvergrownPillar = 79,
        [Furniture("Tome", "x0_tome", "", true)]
        Tome = 80,
        [Furniture("Bird Cage", "plc_birdcage", "", true)]
        BirdCage = 81,
        [Furniture("Pillar, Wood, Dark", "x3_plc_pillar1", "", true)]
        PillarWoodDark = 82,
        [Furniture("Bunk Bed", "x2_plc_bunkbed", "", true)]
        BunkBed = 83,
        [Furniture("Vase, Tall", "x0_bigvase", "", true)]
        VaseTall = 84,
        [Furniture("Bed Roll", "plc_bedrolls", "", true)]
        BedRoll = 85,
        [Furniture("Ottoman", "x0_ottoman", "", true)]
        Ottoman = 86,
        [Furniture("Pillar, Rounded", "x0_ruinedpillar", "", true)]
        PillarRounded = 88,
        [Furniture("Painting 2", "x0_painting2", "", true)]
        Painting2 = 89,
        [Furniture("Candelabra", "plc_candelabra", "", true)]
        Candelabra = 90,
        [Furniture("Potted Plant", "plc_pottedplant", "", true)]
        PottedPlant = 91,
        [Furniture("Painting 1", "x0_painting", "", true)]
        Painting1 = 92,
        [Furniture("Carpet, Fancy", "x0_rugoriental", "", true)]
        CarpetFancy = 93,
        [Furniture("Illithid Table", "x2_plc_tablemind", "", true)]
        IllithidTable = 95,
        [Furniture("Carpet, Fancy, Smaller", "x0_rugoriental2", "", true)]
        CarpetFancySmaller = 96,
        [Furniture("Drow Altar", "x2_plc_drowaltar", "", true)]
        DrowAltar = 97,
        [Furniture("Dartboard", "x3_plc_dartbrd", "", true)]
        Dartboard = 98,
        [Furniture("Map", "x0_maps", "", true)]
        Map = 99,
        [Furniture("Floor-anchored shackles", "plc_flrshackles", "", true)]
        FloorAnchoredShackles = 100,
        [Furniture("Round Wooden Table", "x2_plc_tablernd", "", true)]
        RoundWoodenTable = 101,
        [Furniture("Drow Bar", "x2_plc_drowbar", "", true)]
        DrowBar = 102,
        [Furniture("Shrine of Umberlee", "nw_plc_shrnumbr1", "", true)]
        ShrineofUmberlee = 103,
        [Furniture("Rune Pillar", "x0_runepillar", "", true)]
        RunePillar = 104,
        [Furniture("Mirror", "x2_plc_mirror", "", true)]
        Mirror = 106,
        [Furniture("Footstool", "plc_footstool", "", true)]
        Footstool = 107,
        [Furniture("Drow Table", "x2_plc_tabledrow", "", true)]
        DrowTable = 108,
        [Furniture("Bench, Stone, Dwarven", "bench_stonedwarf", "", true)]
        BenchStoneDwarven = 111,
        [Furniture("Illithid Chair", "illithid_chair", "", true)]
        IllithidChair = 112,
        [Furniture("Bench, Wood", "bench_wood", "", true)]
        BenchWood = 113,
        [Furniture("Chair, Wood, Medium", "chair_med_wood", "", true)]
        ChairWoodMedium = 114,
        [Furniture("Bench, Large", "bench_large", "", true)]
        BenchLarge = 115,
        [Furniture("Drow Chair", "drow_chair", "", true)]
        DrowChair = 116,
        [Furniture("Bench, Wood, Large", "bench_largewood", "", true)]
        BenchWoodLarge = 117,
        [Furniture("Bench, Wood, Small", "bench_woodsmall", "", true)]
        BenchWoodSmall = 118,
        [Furniture("Chair, Wood", "chair_wood", "", true)]
        ChairWood = 119,
        [Furniture("Chair, Wood, Small", "chair_wood_small", "", true)]
        ChairWoodSmall = 120,
        [Furniture("Throne, Wood", "thronewood001", "", true)]
        ThroneWood = 121,
        [Furniture("Chair, Stone", "chair_stone", "", true)]
        ChairStone = 122,
        [Furniture("Bench, Wood, Small 2", "bench_wood2", "", true)]
        BenchWoodSmall2 = 123,
        [Furniture("Chair, Shell", "chair_shell", "", true)]
        ChairShell = 124,
        [Furniture("Couch, Wood, Yellow", "couch_wood", "", true)]
        CouchWoodYellow = 125,

    }

    public class FurnitureAttribute : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public string Resref { get; set; }

        public FurnitureAttribute(string name, string resref, string description, bool isActive)
        {
            Name = name;
            Resref = resref;
            Description = description;
            IsActive = isActive;
        }
    }
}
