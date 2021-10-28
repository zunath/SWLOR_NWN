using System;

namespace SWLOR.Game.Server.Service.HousingService
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
        StatueOfLathander = 35,
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
        ShrineOfUmberlee = 103,
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
        [Furniture("Weapon Rack, Wall Mounted", "swlorweaponrack", "", true)]
        WeaponRackWallMounted = 126,
        [Furniture("Statue, Twi'lek", "statue_twilek", "", true)]
        StatueTwilek = 127,
        [Furniture("Chair, Chancellor", "chair_chancellor", "", true)]
        ChairChancellor = 128,
        [Furniture("Statue, Robed Woman", "statue_woman", "", true)]
        StatueRobedWoman = 129,
        [Furniture("Armchair, High  Back (Orange)", "_mdrn_pl_chair09", "", true)]
        ArmchairHighBackOrange = 130,
        [Furniture("Armchair, High  Back (Blue)", "_mdrn_pl_chair10", "", true)]
        ArmchairHighBackBlue = 131,
        [Furniture("Bench", "_mdrn_pl_df_chb", "", true)]
        Bench = 132,
        [Furniture("Banner, Wall, Lizard", "_mdrn_pl_bannr03", "", true)]
        BannerWallLizard = 133,
        [Furniture("Bathtub ", "_mdrn_pl_bathtb1 ", "", true)]
        Bathtub = 134,
        [Furniture("Bed, High Back (Black/Grey) ", "_mdrn_pl_beddbgr", "", true)]
        BedHighBackBlackGrey = 135,
        [Furniture("Bed, High Back (Blue)", "_mdrn_pl_bedsgbl", "", true)]
        BedHighBackBlue = 136,
        [Furniture("Bed, Medical/Exam", "_mdrn_pl_bmdcl", "", true)]
        BedMedicalExam = 137,
        [Furniture("Bookshelf, Pedestal (White)", "_mdrn_pl_bookcs5", "", true)]
        BookshelfPedestalWhite = 138,
        [Furniture("Cabinet, Curved (Grey/White)", "_mdrn_pl_cabint3", "", true)]
        CabinetCurvedGreyWhite = 139,
        [Furniture("Bed, Side Table", "_mdrn_pl_bedmode", "", true)]
        BedSideTable = 140,
        [Furniture("Bed, Low", "_mdrn_pl_beddbsc", "", true)]
        BedLow = 141,
        [Furniture("Banner, Standing", "_mdrn_pl_bannr04", "", true)]
        BannerStanding = 142,
        [Furniture("Chair, Crew (Grey)", "_mdrn_pl_chrgren", "", true)]
        ChairCrewGrey = 143,
        [Furniture("Chair, Open Frame (Brown)", "_mdrn_pl_chair32", "", true)]
        ChairOpenFrameBrown = 144,
        [Furniture("Chair, Pedestal /w Arms", "_mdrn_pl_chairbl", "", true)]
        ChairPedestalWithArms = 145,
        [Furniture("Chair, Pedestal, Padded (Red)", "_mdrn_pl_chairpr", "", true)]
        ChairPedestalPaddedRed = 146,
        [Furniture("Chair, Pedestal, Panel", "_mdrn_pl_chair31", "", true)]
        ChairPedestalPanel = 147,
        [Furniture("Console, Floor Mounted (Blue Screens)", "_mdrn_pl_cons001", "", true)]
        ConsoleFloorMountedBlueScreens = 148,
        [Furniture("Console, Floor Mounted (Green Screens)", "_mdrn_pl_conso04", "", true)]
        ConsoleFloorMountedGreenScreens = 149,
        [Furniture("Cot /w Table", "_mdrn_pl_cotsf", "", true)]
        CotWithTable = 150,
        [Furniture("Desk, Control Board Inlay", "_mdrn_pl_df_dscb", "", true)]
        DeskControlBoardInlay = 151,
        [Furniture("Desk, Control Center", "_mdrn_pl_conso03", "", true)]
        DeskControlCenter = 152,
        [Furniture("Desk, Control Center, Large Screen", "_mdrn_pl_conso27", "", true)]
        DeskControlCenterLargeScreen = 153,
        [Furniture("Desk, Control Center, Wide", "_mdrn_pl_conso19", "", true)]
        DeskControlCenterWide = 154,
        [Furniture("Desk, Corner /w Terminal", "_mdrn_pl_deskcn1", "", true)]
        DeskCornerwTerminal = 155,
        [Furniture("Desk, Information/Control Center", "_mdrn_pl_df_dicc", "", true)]
        DeskInformationControlCenter = 156,
        [Furniture("Desk, Wall /w Terminal", "_mdrn_pl_deskter", "", true)]
        DeskWallwTerminal = 157,
        [Furniture("Desk, Wall /w Terminal, Wide", "_mdrn_pl_desktew", "", true)]
        DeskWallwTerminalWide = 158,
        [Furniture("Work Station, Droid Repair", "_mdrn_pl_conso08", "", true)]
        WorkStationDroidRepair = 159,
        [Furniture("Footlocker, Modern (Keyed Entry)", "_mdrn_pl_df_fmke", "", true)]
        FootlockerModernKeyedEntry = 160,
        [Furniture("Fountain, Oval", "_mdrn_pl_fountn2", "", true)]
        FountainOval = 161,
        [Furniture("Holo Display", "_mdrn_pl_holod01", "", true)]
        HoloDisplay = 162,
        [Furniture("Holo Display 2", "_mdrn_pl_holod03", "", true)]
        HoloDisplay2 = 163,
        [Furniture("Holo Display 4", "_mdrn_pl_holod05", "", true)]
        HoloDisplay4 = 164,
        [Furniture("Holo Display 5", "_mdrn_pl_holod06", "", true)]
        HoloDisplay5 = 165,
        [Furniture("Holo Projector 1", "_mdrn_pl_holoco2", "", true)]
        HoloProjector1 = 166,
        [Furniture("Holo Projector 2", "_mdrn_pl_holobas", "", true)]
        HoloProjector2 = 167,
        [Furniture("Instrument Panel, Large Monitor (Technical Data)", "_mdrn_pl_instptd", "", true)]
        InstrumentPanelLargeMonitorTechnicalData = 168,
        [Furniture("Kolto Tank (Empty)", "_mdrn_pl_clntnke", "", true)]
        KoltoTankEmpty = 169,
        [Furniture("Lamp, Eggs (Pink)", "_mdrn_pl_lampd09", "", true)]
        LampEggsPink = 170,
        [Furniture("Lamp, On Poles", "_mdrn_pl_lampd04", "", true)]
        LampOnPoles = 171,
        [Furniture("Lantern, Post, Marble", "_mdrn_pl_lamp4", "", true)]
        LanternPostMarble = 172,
        [Furniture("Locker, Metal Trapezoid", "_mdrn_pl_rustedl", "", true)]
        LockerMetalTrapezoid = 173,
        [Furniture("Mirror (Small)", "_mdrn_pl_mirror1", "", true)]
        MirrorSmall = 174,
        [Furniture("Monitor, Wall, Logo Display (Blue)", "_mdrn_pl_screend", "", true)]
        MonitorWallLogoDisplayBlue = 175,
        [Furniture("Ornament, Solar System", "_mdrn_pl_ornamnt", "", true)]
        OrnamentSolarSystem = 176,
        [Furniture("Ottoman, Decorated (Black)", "_mdrn_pl_ottomn2", "", true)]
        OttomanDecoratedBlack = 177,
        [Furniture("Pipes, Conduit (with Power Controls)", "_mdrn_pl_df_pcpc", "", true)]
        PipesConduitwithPowerControls = 178,
        [Furniture("Cylinder, Cross Top", "_mdrn_pl_conta38", "", true)]
        CylinderCrossTop = 179,
        [Furniture("Pot, Bush, Clipped", "_mdrn_pl_flowrp2", "", true)]
        PotBushClipped = 180,
        [Furniture("Pot, Bush, Flowers", "_mdrn_pl_flowrby", "", true)]
        PotBushFlowers = 181,
        [Furniture("Pot, Bush, Tall", "_mdrn_pl_flowrpi", "", true)]
        PotBushTall = 182,
        [Furniture("Pot, Clay Urn", "_mdrn_pl_pottery", "", true)]
        PotClayUrn = 183,
        [Furniture("Pot, Flower, Daisy", "_mdrn_pl_flowpt1", "", true)]
        PotFlowerDaisy = 184,
        [Furniture("Pot, Flower, Yellow", "_mdrn_pl_flowpt2", "", true)]
        PotFlowerYellow = 185,
        [Furniture("Pot, Long Leaf 1", "_mdrn_pl_plant09", "", true)]
        PotLongLeaf1 = 186,
        [Furniture("Pot, Plant, Aloa", "_mdrn_pl_flowspk", "", true)]
        PotPlantAloa = 187,
        [Furniture("Pot, Plant, Tropical", "_mdrn_pl_potplnt", "", true)]
        PotPlantTropical = 188,
        [Furniture("Pot, Urn, Grecian", "_mdrn_pl_flowr02", "", true)]
        PotUrnGrecian = 189,
        [Furniture("Skeleton, Medical Display", "_mdrn_pl_skeleto", "", true)]
        SkeletonMedicalDisplay = 190,
        [Furniture("Chest, Trapezoid (White)", "_mdrn_pl_conta14", "", true)]
        ChestTrapezoidWhite = 191,
        [Furniture("Space Suit (Tan)", "_mdrn_pl_spcest1", "", true)]
        SpaceSuitTan = 192,
        [Furniture("Specimen Tube (Alien)", "_mdrn_pl_alntbea", "", true)]
        SpecimenTubeAlien = 193,
        [Furniture("Specimen Tube, Empty", "_mdrn_pl_tubeemp", "", true)]
        SpecimenTubeEmpty = 194,
        [Furniture("Specimen Tube, Tall", "_mdrn_pl_tube", "", true)]
        SpecimenTubeTall = 195,
        [Furniture("Statue, Bust on Column", "_mdrn_pl_bust", "", true)]
        StatueBustonColumn = 196,
        [Furniture("Statue, Kneeling Man", "_mdrn_pl_statue1", "", true)]
        StatueKneelingMan = 197,
        [Furniture("Statue, Robed Figure /w Staff", "_mdrn_pl_statu11", "", true)]
        StatueRobedFigurewStaff = 198,
        [Furniture("Statue, Senator", "_mdrn_pl_statue2", "", true)]
        StatueSenator = 199,
        [Furniture("Storage Tank, Hemisphere /w Monitor", "_mdrn_pl_machin1", "", true)]
        StorageTankHemispherewMonitor = 200,
        [Furniture("Stuffed Toy, Bantha", "_mdrn_pl_stuffed", "", true)]
        StuffedToyBantha = 201,
        [Furniture("Table, Coffee, Elegant (White)", "_mdrn_pl_table10", "", true)]
        TableCoffeeElegantWhite = 202,
        [Furniture("Table, Conference, Centre Cloth", "_mdrn_pl_cnfrtbl", "", true)]
        TableConferenceCentreCloth = 203,
        [Furniture("Table, Oval, Centre Leg (Dark)", "_mdrn_pl_table15", "", true)]
        TableOvalCentreLegDark = 204,
        [Furniture("Table, Oval, Low (Blue)", "_mdrn_pl_tabl001", "", true)]
        TableOvalLowBlue = 205,
        [Furniture("Table, Round, Low (Blue)", "_mdrn_pl_table19", "", true)]
        TableRoundLowBlue = 206,
        [Furniture("Table, Stone (Blue)", "_mdrn_pl_table24", "", true)]
        TableStoneBlue = 207,
        [Furniture("Table, Stone (Brown)", "_mdrn_pl_table23", "", true)]
        TableStoneBrown = 208,
        [Furniture("Table, Wall, Oval", "_mdrn_pl_table32", "", true)]
        TableWallOval = 209,
        [Furniture("Toilet, White /w Cistern", "_mdrn_pl_toilet", "", true)]
        ToiletWhitewCistern = 210,
        [Furniture("Wall Light, Curved", "_mdrn_pl_lights4", "", true)]
        WallLightCurved = 211,
        [Furniture("Wall Light, Octagon", "_mdrn_pl_lights6", "", true)]
        WallLightOctagon = 212,
        [Furniture("Wardrobe, Curved (White)", "_mdrn_pl_armoir3", "", true)]
        WardrobeCurvedWhite = 213,
        [Furniture("Washbasin, Lever Faucet", "_mdrn_pl_sinka", "", true)]
        WashbasinLeverFaucet = 214,
        [Furniture("Weapon Rack", "_mdrn_pl_weaprck", "", true)]
        WeaponRack = 215,
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
