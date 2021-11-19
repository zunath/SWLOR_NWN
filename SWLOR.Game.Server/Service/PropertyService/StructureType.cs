using System;

namespace SWLOR.Game.Server.Service.PropertyService
{
    public enum StructureType
    {
        [Structure("Invalid", 
            "", 
            "Invalid", 
            false)]
        Invalid = 0,
        [Structure("Obelisk, Large", 
            "x0_obelisk", 
            "", 
            true)]
        ObeliskLarge = 4,
        [Structure("Jukebox", 
            "jukebox", 
            "", 
            true)]
        Jukebox = 5,
        [Structure("Obelisk, Small", 
            "nw_plc_seaobelsk", 
            "", 
            true)]
        ObeliskSmall = 6,
        [Structure("Ladder, Light", 
            "x0_weatheredladd", 
            "", 
            true)]
        LadderLight = 7,
        [Structure("Ladder, Dark", 
            "x0_ladder", 
            "", 
            true)]
        LadderDark = 8,
        [Structure("Statue, Huge", 
            "x2_plc_statue_h", 
            "", 
            true)]
        StatueHuge = 9,
        [Structure("Mining Well", 
            "x2_plc_minewell", 
            "", 
            true)]
        MiningWell = 10,
        [Structure("Statue, Wizard", 
            "x3_plc_gg001", 
            "", 
            true)]
        StatueWizard = 12,
        [Structure("Gong", 
            "plc_gong", 
            "", 
            true)]
        Gong = 13,
        [Structure("Statue, Monster", 
            "plc_statue3", 
            "", 
            true)]
        StatueMonster = 14,
        [Structure("Brazier, Stone", 
            "plc_brazier", 
            "", 
            true)]
        BrazierStone = 17,
        [Structure("Statue, Guardian", 
            "plc_statue1", 
            "", 
            true)]
        StatueGuardian = 18,
        [Structure("Doorway, Metal", 
            "x0_doormetal", 
            "", 
            true)]
        DoorwayMetal = 19,
        [Structure("Flaming Statue", 
            "x2_plc_statue_fl", 
            "", 
            true)]
        FlamingStatue = 20,
        [Structure("Brazier, Round", 
            "x3_plc_brazier", 
            "", 
            true)]
        BrazierRound = 21,
        [Structure("Pedestal", 
            "plc_pedestal", 
            "", 
            true)]
        Pedestal = 22,
        [Structure("Rope Coil", 
            "nw_plc_ropecoil1", 
            "", 
            true)]
        RopeCoil = 23,
        [Structure("Statue, Wyvern ", 
            "x3_plc_statuew", 
            "", 
            true)]
        StatueWyvern = 24,
        [Structure("Pedestal, Evil", 
            "x3_plc_pedestal", 
            "",
            true)]
        PedestalEvil = 25,
        [Structure("Birdbath", 
            "plc_birdbath", 
            "", 
            true)]
        Birdbath = 26,
        [Structure("Sphinx Statue", 
            "x0_sphinxstatue", 
            "", 
            true)]
        SphinxStatue = 27,
        [Structure("Mining Well Platform", 
            "x2_plc_minewellb", 
            "", 
            true)]
        MiningWellPlatform = 28,
        [Structure("Pedestal, Sword", 
            "x3_plc_pedsword", 
            "", 
            true)]
        PedestalSword = 29,
        [Structure("Doorway, Stone", 
            "x0_stonewalldoor", 
            "", 
            true)]
        DoorwayStone = 30,
        [Structure("Female Statue", 
            "x2_plc_statue_f", 
            "", 
            true)]
        FemaleStatue = 31,
        [Structure("Gnomish Contraption", 
            "plc_gnomcntrptn", 
            "", 
            true)]
        GnomishContraption = 32,
        [Structure("Pillar, Stone", 
            "plc_pillar3", 
            "", 
            true)]
        PillarStone = 33,
        [Structure("Statue of Lathander", 
            "x2_plc_statue_la", 
            "", 
            true)]
        StatueOfLathander = 35,
        [Structure("Pillar, Flame", 
            "plc_pillar1", 
            "", 
            true)]
        PillarFlame = 36,
        [Structure("Cage", 
            "plc_animalcage", 
            "", 
            true)]
        Cage = 37,
        [Structure("Lamp Post", 
            "plc_lamppost", 
            "", 
            true)]
        LampPost = 38,
        [Structure("Torch Bracket", 
            "plc_freetorch", 
            "", 
            true)]
        TorchBracket = 39,
        [Structure("Pillar, Wood", 
            "plc_pillar2", 
            "", 
            true)]
        PillarWood = 40,
        [Structure("Statue, Cyric", 
            "x3_plc_statuec", 
            "", 
            true)]
        StatueCyric = 41,
        [Structure("Sea Idol", 
            "nw_plc_seaidol", 
            "", 
            true)]
        SeaIdol = 42,
        [Structure("Fountain", 
            "plc_fountain", 
            "", 
            true)]
        Fountain = 43,
        [Structure("Monster Statue", 
            "x2_plc_statue_mo", 
            "", 
            true)]
        MonsterStatue = 44,
        [Structure("Easel", 
            "plc_pntingeasel", 
            "", 
            true)]
        Easel = 45,
        [Structure("Keg", 
            "plc_keg", 
            "", 
            true)]
        Keg = 47,
        [Structure("Dran Statue ", 
            "x2_plc_statu_dra", 
            "", 
            true)]
        DranStatue = 48,
        [Structure("Net", 
            "nw_plc_net", 
            "", 
            true)]
        Net = 51,
        [Structure("Bed, Extra Large", 
            "x0_largebed", 
            "", 
            true)]
        BedExtraLarge = 52,
        [Structure("Carpet, Round, Blue", 
            "x0_roundrugorien", 
            "", 
            true)]
        CarpetRoundBlue = 54,
        [Structure("Altar, Evil", 
            "plc_altrevil", 
            "", 
            true)]
        AltarEvil = 55,
        [Structure("Table, Wood, Large", 
            "x3_plc_table002", 
            "", 
            true)]
        TableWoodLarge = 56,
        [Structure("Table, Wood, With Fish", 
            "x3_plc_table001", 
            "", 
            true)]
        TableWoodWithFish = 57,
        [Structure("Table, Stone, Small", 
            "nw_plc_seatable", 
            "", 
            true)]
        TableStoneSmall = 58,
        [Structure("Altar, Hand", 
            "plc_altrod", 
            "", 
            true)]
        AltarHand = 59,
        [Structure("Window", 
            "x0_window", 
            "", 
            true)]
        Window = 60,
        [Structure("Cushions", 
            "x0_cushions", 
            "", 
            true)]
        Cushions = 61,
        [Structure("Candle", 
            "nw_plc_candle1", 
            "", 
            true)]
        Candle = 62,
        [Structure("Bear Skin Rug", 
            "x0_bearskinrug1", 
            "", 
            true)]
        BearSkinRug = 64,
        [Structure("Chandelier", 
            "x0_chandelier", 
            "", 
            true)]
        Chandelier = 65,
        [Structure("Urn", 
            "plc_urn", 
            "", 
            true)]
        Urn = 67,
        [Structure("Altar, Stone", 
            "plc_altrneutral", 
            "", 
            true)]
        AltarStone = 68,
        [Structure("Cot", 
            "plc_cot", 
            "", 
            true)]
        Cot = 69,
        [Structure("Table, Wood", 
            "plc_table", 
            "", 
            true)]
        TableWood = 70,
        [Structure("Throw Rug", 
            "plc_throwrug", 
            "", 
            true)]
        ThrowRug = 72,
        [Structure("Table, Stone, Large", 
            "nw_plc_dwarftabl", 
            "", 
            true)]
        TableStoneLarge = 73,
        [Structure("Bed, Stone, Yellow", 
            "nw_plc_dwarfbed", 
            "", 
            true)]
        BedStoneYellow = 74,
        [Structure("Bed, Large", 
            "x0_beddouble", 
            "", 
            true)]
        BedLarge = 75,
        [Structure("Vase, Rounded", 
            "x0_vaseflower", 
            "", 
            true)]
        VaseRounded = 76,
        [Structure("Carpet", 
            "x0_ruglarge", 
            "", 
            true)]
        Carpet = 77,
        [Structure("Bed, Wood, Yellow", 
            "plc_bed", 
            "", 
            true)]
        BedWoodYellow = 78,
        [Structure("Overgrown Pillar", 
            "x0_overgrownrui", 
            "", 
            true)]
        OvergrownPillar = 79,
        [Structure("Tome", 
            "x0_tome", 
            "", 
            true)]
        Tome = 80,
        [Structure("Bird Cage", 
            "plc_birdcage", 
            "", 
            true)]
        BirdCage = 81,
        [Structure("Pillar, Wood, Dark", 
            "x3_plc_pillar1", 
            "", 
            true)]
        PillarWoodDark = 82,
        [Structure("Bunk Bed", 
            "x2_plc_bunkbed", 
            "", 
            true)]
        BunkBed = 83,
        [Structure("Vase, Tall", 
            "x0_bigvase", 
            "", 
            true)]
        VaseTall = 84,
        [Structure("Bed Roll", 
            "plc_bedrolls", 
            "", 
            true)]
        BedRoll = 85,
        [Structure("Ottoman", 
            "x0_ottoman", 
            "", 
            true)]
        Ottoman = 86,
        [Structure("Pillar, Rounded", 
            "x0_ruinedpillar", 
            "", 
            true)]
        PillarRounded = 88,
        [Structure("Painting 2", 
            "x0_painting2", 
            "", 
            true)]
        Painting2 = 89,
        [Structure("Candelabra", 
            "plc_candelabra", 
            "", 
            true)]
        Candelabra = 90,
        [Structure("Potted Plant", 
            "plc_pottedplant", 
            "", 
            true)]
        PottedPlant = 91,
        [Structure("Painting 1", 
            "x0_painting", 
            "", 
            true)]
        Painting1 = 92,
        [Structure("Carpet, Fancy", 
            "x0_rugoriental", 
            "", 
            true)]
        CarpetFancy = 93,
        [Structure("Illithid Table", 
            "x2_plc_tablemind", 
            "", 
            true)]
        IllithidTable = 95,
        [Structure("Carpet, Fancy, Smaller", 
            "x0_rugoriental2", 
            "", 
            true)]
        CarpetFancySmaller = 96,
        [Structure("Drow Altar", 
            "x2_plc_drowaltar", 
            "", 
            true)]
        DrowAltar = 97,
        [Structure("Dartboard", 
            "x3_plc_dartbrd", 
            "", 
            true)]
        Dartboard = 98,
        [Structure("Map", 
            "x0_maps", 
            "", 
            true)]
        Map = 99,
        [Structure("Floor-anchored shackles", 
            "plc_flrshackles", 
            "", 
            true)]
        FloorAnchoredShackles = 100,
        [Structure("Round Wooden Table", 
            "x2_plc_tablernd", 
            "", 
            true)]
        RoundWoodenTable = 101,
        [Structure("Drow Bar", 
            "x2_plc_drowbar", 
            "", 
            true)]
        DrowBar = 102,
        [Structure("Shrine of Umberlee", 
            "nw_plc_shrnumbr1", 
            "", 
            true)]
        ShrineOfUmberlee = 103,
        [Structure("Rune Pillar", 
            "x0_runepillar", 
            "", 
            true)]
        RunePillar = 104,
        [Structure("Mirror", 
            "x2_plc_mirror", 
            "", 
            true)]
        Mirror = 106,
        [Structure("Footstool", 
            "plc_footstool", 
            "", 
            true)]
        Footstool = 107,
        [Structure("Drow Table", 
            "x2_plc_tabledrow", 
            "", 
            true)]
        DrowTable = 108,
        [Structure("Bench, Stone, Dwarven", 
            "bench_stonedwarf", 
            "", true)]
        BenchStoneDwarven = 111,
        [Structure("Illithid Chair", 
            "illithid_chair", 
            "", 
            true)]
        IllithidChair = 112,
        [Structure("Bench, Wood", 
            "bench_wood", 
            "", 
            true)]
        BenchWood = 113,
        [Structure("Chair, Wood, Medium", 
            "chair_med_wood", 
            "", 
            true)]
        ChairWoodMedium = 114,
        [Structure("Bench, Large", 
            "bench_large", 
            "", 
            true)]
        BenchLarge = 115,
        [Structure("Drow Chair", 
            "drow_chair", 
            "", 
            true)]
        DrowChair = 116,
        [Structure("Bench, Wood, Large", 
            "bench_largewood", 
            "", 
            true)]
        BenchWoodLarge = 117,
        [Structure("Bench, Wood, Small", 
            "bench_woodsmall", 
            "", 
            true)]
        BenchWoodSmall = 118,
        [Structure("Chair, Wood", 
            "chair_wood", 
            "", 
            true)]
        ChairWood = 119,
        [Structure("Chair, Wood, Small", 
            "chair_wood_small", 
            "", 
            true)]
        ChairWoodSmall = 120,
        [Structure("Throne, Wood", 
            "thronewood001", 
            "", 
            true)]
        ThroneWood = 121,
        [Structure("Chair, Stone", 
            "chair_stone", 
            "", 
            true)]
        ChairStone = 122,
        [Structure("Bench, Wood, Small 2", 
            "bench_wood2", 
            "", 
            true)]
        BenchWoodSmall2 = 123,
        [Structure("Chair, Shell", 
            "chair_shell", 
            "", 
            true)]
        ChairShell = 124,
        [Structure("Couch, Wood, Yellow", 
            "couch_wood", 
            "", 
            true)]
        CouchWoodYellow = 125,
        [Structure("Weapon Rack, Wall Mounted", 
            "swlorweaponrack", 
            "", 
            true)]
        WeaponRackWallMounted = 126,
        [Structure("Statue, Twi'lek", 
            "statue_twilek", 
            "", 
            true)]
        StatueTwilek = 127,
        [Structure("Chair, Chancellor", 
            "chair_chancellor", 
            "", 
            true)]
        ChairChancellor = 128,
        [Structure("Statue, Robed Woman", 
            "statue_woman", 
            "", 
            true)]
        StatueRobedWoman = 129,
        [Structure("Armchair, High  Back (Orange)", 
            "_mdrn_pl_chair09", 
            "", 
            true)]
        ArmchairHighBackOrange = 130,
        [Structure("Armchair, High  Back (Blue)", 
            "_mdrn_pl_chair10", 
            "", 
            true)]
        ArmchairHighBackBlue = 131,
        [Structure("Bench", 
            "_mdrn_pl_df_chb", 
            "", 
            true)]
        Bench = 132,
        [Structure("Banner, Wall, Lizard", 
            "_mdrn_pl_bannr03", 
            "", 
            true)]
        BannerWallLizard = 133,
        [Structure("Bathtub ", 
            "_mdrn_pl_bathtb1 ", 
            "", 
            true)]
        Bathtub = 134,
        [Structure("Bed, High Back (Black/Grey) ", 
            "_mdrn_pl_beddbgr", 
            "", 
            true)]
        BedHighBackBlackGrey = 135,
        [Structure("Bed, High Back (Blue)", 
            "_mdrn_pl_bedsgbl", 
            "", 
            true)]
        BedHighBackBlue = 136,
        [Structure("Bed, Medical/Exam", 
            "_mdrn_pl_bmdcl", 
            "", 
            true)]
        BedMedicalExam = 137,
        [Structure("Bookshelf, Pedestal (White)", 
            "_mdrn_pl_bookcs5", 
            "", 
            true)]
        BookshelfPedestalWhite = 138,
        [Structure("Cabinet, Curved (Grey/White)", 
            "_mdrn_pl_cabint3", 
            "", 
            true)]
        CabinetCurvedGreyWhite = 139,
        [Structure("Bed, Side Table", 
            "_mdrn_pl_bedmode", 
            "", 
            true)]
        BedSideTable = 140,
        [Structure("Bed, Low", 
            "_mdrn_pl_beddbsc", 
            "", 
            true)]
        BedLow = 141,
        [Structure("Banner, Standing", 
            "_mdrn_pl_bannr04", 
            "", 
            true)]
        BannerStanding = 142,
        [Structure("Chair, Crew (Grey)", 
            "_mdrn_pl_chrgren", 
            "", 
            true)]
        ChairCrewGrey = 143,
        [Structure("Chair, Open Frame (Brown)", 
            "_mdrn_pl_chair32", 
            "", 
            true)]
        ChairOpenFrameBrown = 144,
        [Structure("Chair, Pedestal /w Arms", 
            "_mdrn_pl_chairbl", 
            "", 
            true)]
        ChairPedestalWithArms = 145,
        [Structure("Chair, Pedestal, Padded (Red)", 
            "_mdrn_pl_chairpr", 
            "", 
            true)]
        ChairPedestalPaddedRed = 146,
        [Structure("Chair, Pedestal, Panel", 
            "_mdrn_pl_chair31", 
            "", 
            true)]
        ChairPedestalPanel = 147,
        [Structure("Console, Floor Mounted (Blue Screens)", 
            "_mdrn_pl_cons001", 
            "", 
            true)]
        ConsoleFloorMountedBlueScreens = 148,
        [Structure("Console, Floor Mounted (Green Screens)", 
            "_mdrn_pl_conso04", 
            "", 
            true)]
        ConsoleFloorMountedGreenScreens = 149,
        [Structure("Cot /w Table", 
            "_mdrn_pl_cotsf", 
            "", 
            true)]
        CotWithTable = 150,
        [Structure("Desk, Control Board Inlay", 
            "_mdrn_pl_df_dscb", 
            "", 
            true)]
        DeskControlBoardInlay = 151,
        [Structure("Desk, Control Center", 
            "_mdrn_pl_conso03", 
            "", 
            true)]
        DeskControlCenter = 152,
        [Structure("Desk, Control Center, Large Screen", 
            "_mdrn_pl_conso27", 
            "", 
            true)]
        DeskControlCenterLargeScreen = 153,
        [Structure("Desk, Control Center, Wide", 
            "_mdrn_pl_conso19", 
            "", 
            true)]
        DeskControlCenterWide = 154,
        [Structure("Desk, Corner /w Terminal", 
            "_mdrn_pl_deskcn1", 
            "", 
            true)]
        DeskCornerwTerminal = 155,
        [Structure("Desk, Information/Control Center", 
            "_mdrn_pl_df_dicc", 
            "", 
            true)]
        DeskInformationControlCenter = 156,
        [Structure("Desk, Wall /w Terminal", 
            "_mdrn_pl_deskter", 
            "", 
            true)]
        DeskWallwTerminal = 157,
        [Structure("Desk, Wall /w Terminal, Wide", 
            "_mdrn_pl_desktew", 
            "", 
            true)]
        DeskWallwTerminalWide = 158,
        [Structure("Work Station, Droid Repair", 
            "_mdrn_pl_conso08", 
            "", 
            true)]
        WorkStationDroidRepair = 159,
        [Structure("Footlocker, Modern (Keyed Entry)", 
            "_mdrn_pl_df_fmke", 
            "", 
            true)]
        FootlockerModernKeyedEntry = 160,
        [Structure("Fountain, Oval", 
            "_mdrn_pl_fountn2", 
            "", 
            true)]
        FountainOval = 161,
        [Structure("Holo Display", 
            "_mdrn_pl_holod01", 
            "", 
            true)]
        HoloDisplay = 162,
        [Structure("Holo Display 2", 
            "_mdrn_pl_holod03", 
            "", 
            true)]
        HoloDisplay2 = 163,
        [Structure("Holo Display 4", 
            "_mdrn_pl_holod05", 
            "", 
            true)]
        HoloDisplay4 = 164,
        [Structure("Holo Display 5", 
            "_mdrn_pl_holod06", 
            "", 
            true)]
        HoloDisplay5 = 165,
        [Structure("Holo Projector 1", 
            "_mdrn_pl_holoco2", 
            "", 
            true)]
        HoloProjector1 = 166,
        [Structure("Holo Projector 2", 
            "_mdrn_pl_holobas", 
            "", 
            true)]
        HoloProjector2 = 167,
        [Structure("Instrument Panel, Large Monitor (Technical Data)", 
            "_mdrn_pl_instptd", 
            "", 
            true)]
        InstrumentPanelLargeMonitorTechnicalData = 168,
        [Structure("Kolto Tank (Empty)", 
            "_mdrn_pl_clntnke", 
            "", 
            true)]
        KoltoTankEmpty = 169,
        [Structure("Lamp, Eggs (Pink)", 
            "_mdrn_pl_lampd09", 
            "", 
            true)]
        LampEggsPink = 170,
        [Structure("Lamp, On Poles", 
            "_mdrn_pl_lampd04", 
            "", 
            true)]
        LampOnPoles = 171,
        [Structure("Lantern, Post, Marble", 
            "_mdrn_pl_lamp4", 
            "", 
            true)]
        LanternPostMarble = 172,
        [Structure("Locker, Metal Trapezoid", 
            "_mdrn_pl_rustedl", 
            "", 
            true)]
        LockerMetalTrapezoid = 173,
        [Structure("Mirror (Small)", 
            "_mdrn_pl_mirror1", 
            "", 
            true)]
        MirrorSmall = 174,
        [Structure("Monitor, Wall, Logo Display (Blue)", 
            "_mdrn_pl_screend", 
            "", 
            true)]
        MonitorWallLogoDisplayBlue = 175,
        [Structure("Ornament, Solar System", 
            "_mdrn_pl_ornamnt", 
            "", 
            true)]
        OrnamentSolarSystem = 176,
        [Structure("Ottoman, Decorated (Black)", 
            "_mdrn_pl_ottomn2", 
            "", 
            true)]
        OttomanDecoratedBlack = 177,
        [Structure("Pipes, Conduit (with Power Controls)", 
            "_mdrn_pl_df_pcpc", 
            "", 
            true)]
        PipesConduitwithPowerControls = 178,
        [Structure("Cylinder, Cross Top", 
            "_mdrn_pl_conta38", 
            "", 
            true)]
        CylinderCrossTop = 179,
        [Structure("Pot, Bush, Clipped", 
            "_mdrn_pl_flowrp2", 
            "", 
            true)]
        PotBushClipped = 180,
        [Structure("Pot, Bush, Flowers", 
            "_mdrn_pl_flowrby", 
            "", 
            true)]
        PotBushFlowers = 181,
        [Structure("Pot, Bush, Tall", 
            "_mdrn_pl_flowrpi", 
            "", 
            true)]
        PotBushTall = 182,
        [Structure("Pot, Clay Urn", 
            "_mdrn_pl_pottery", 
            "", 
            true)]
        PotClayUrn = 183,
        [Structure("Pot, Flower, Daisy", 
            "_mdrn_pl_flowpt1", 
            "", 
            true)]
        PotFlowerDaisy = 184,
        [Structure("Pot, Flower, Yellow", 
            "_mdrn_pl_flowpt2", 
            "", 
            true)]
        PotFlowerYellow = 185,
        [Structure("Pot, Long Leaf 1", 
            "_mdrn_pl_plant09", 
            "", 
            true)]
        PotLongLeaf1 = 186,
        [Structure("Pot, Plant, Aloa", 
            "_mdrn_pl_flowspk", 
            "", 
            true)]
        PotPlantAloa = 187,
        [Structure("Pot, Plant, Tropical", 
            "_mdrn_pl_potplnt", 
            "", 
            true)]
        PotPlantTropical = 188,
        [Structure("Pot, Urn, Grecian", 
            "_mdrn_pl_flowr02", 
            "", 
            true)]
        PotUrnGrecian = 189,
        [Structure("Skeleton, Medical Display", 
            "_mdrn_pl_skeleto", 
            "", 
            true)]
        SkeletonMedicalDisplay = 190,
        [Structure("Chest, Trapezoid (White)", 
            "_mdrn_pl_conta14", 
            "", 
            true)]
        ChestTrapezoidWhite = 191,
        [Structure("Space Suit (Tan)", 
            "_mdrn_pl_spcest1", 
            "", 
            true)]
        SpaceSuitTan = 192,
        [Structure("Specimen Tube (Alien)", 
            "_mdrn_pl_alntbea", 
            "", 
            true)]
        SpecimenTubeAlien = 193,
        [Structure("Specimen Tube, Empty", 
            "_mdrn_pl_tubeemp", 
            "", 
            true)]
        SpecimenTubeEmpty = 194,
        [Structure("Specimen Tube, Tall", 
            "_mdrn_pl_tube", 
            "", 
            true)]
        SpecimenTubeTall = 195,
        [Structure("Statue, Bust on Column", 
            "_mdrn_pl_bust", 
            "", 
            true)]
        StatueBustonColumn = 196,
        [Structure("Statue, Kneeling Man", 
            "_mdrn_pl_statue1", 
            "", 
            true)]
        StatueKneelingMan = 197,
        [Structure("Statue, Robed Figure /w Staff", 
            "_mdrn_pl_statu11", 
            "", 
            true)]
        StatueRobedFigurewStaff = 198,
        [Structure("Statue, Senator", 
            "_mdrn_pl_statue2", 
            "", 
            true)]
        StatueSenator = 199,
        [Structure("Storage Tank, Hemisphere /w Monitor", 
            "_mdrn_pl_machin1", 
            "", 
            true)]
        StorageTankHemispherewMonitor = 200,
        [Structure("Stuffed Toy, Bantha", 
            "_mdrn_pl_stuffed", 
            "", 
            true)]
        StuffedToyBantha = 201,
        [Structure("Table, Coffee, Elegant (White)", 
            "_mdrn_pl_table10", 
            "", 
            true)]
        TableCoffeeElegantWhite = 202,
        [Structure("Table, Conference, Centre Cloth", 
            "_mdrn_pl_cnfrtbl", 
            "", 
            true)]
        TableConferenceCentreCloth = 203,
        [Structure("Table, Oval, Centre Leg (Dark)", 
            "_mdrn_pl_table15", 
            "", 
            true)]
        TableOvalCentreLegDark = 204,
        [Structure("Table, Oval, Low (Blue)", 
            "_mdrn_pl_tabl001", 
            "", 
            true)]
        TableOvalLowBlue = 205,
        [Structure("Table, Round, Low (Blue)", 
            "_mdrn_pl_table19", 
            "", 
            true)]
        TableRoundLowBlue = 206,
        [Structure("Table, Stone (Blue)", 
            "_mdrn_pl_table24", 
            "", 
            true)]
        TableStoneBlue = 207,
        [Structure("Table, Stone (Brown)", 
            "_mdrn_pl_table23", 
            "", 
            true)]
        TableStoneBrown = 208,
        [Structure("Table, Wall, Oval", 
            "_mdrn_pl_table32", 
            "", 
            true)]
        TableWallOval = 209,
        [Structure("Toilet, White /w Cistern", 
            "_mdrn_pl_toilet", 
            "", 
            true)]
        ToiletWhitewCistern = 210,
        [Structure("Wall Light, Curved", 
            "_mdrn_pl_lights4", 
            "", 
            true)]
        WallLightCurved = 211,
        [Structure("Wall Light, Octagon", 
            "_mdrn_pl_lights6", 
            "", 
            true)]
        WallLightOctagon = 212,
        [Structure("Wardrobe, Curved (White)", 
            "_mdrn_pl_armoir3", 
            "", 
            true)]
        WardrobeCurvedWhite = 213,
        [Structure("Washbasin, Lever Faucet", 
            "_mdrn_pl_sinka", 
            "", 
            true)]
        WashbasinLeverFaucet = 214,
        [Structure("Weapon Rack", 
            "_mdrn_pl_weaprck", 
            "", 
            true)]
        WeaponRack = 215,
    }

    public class StructureAttribute : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public string Resref { get; set; }
        public PropertyType RestrictedPropertyTypes { get; set; }

        public StructureAttribute(
            string name, 
            string resref, 
            string description, 
            bool isActive,
            PropertyType restrictedPropertyTypes = 
                PropertyType.Apartment | 
                PropertyType.Building | 
                PropertyType.Starship | 
                PropertyType.City)
        {
            Name = name;
            Resref = resref;
            Description = description;
            IsActive = isActive;
            RestrictedPropertyTypes = restrictedPropertyTypes;
        }
    }
}
