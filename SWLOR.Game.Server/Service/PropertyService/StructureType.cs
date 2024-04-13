﻿using System;

namespace SWLOR.Game.Server.Service.PropertyService
{
    public enum StructureType
    {
        [Structure("Invalid",
            "",
            "Invalid",
            false,
            0)]
        Invalid = 0,
        [Structure("Obelisk, Large",
            "x0_obelisk",
            "",
            true,
            1)]
        ObeliskLarge = 4,
        [Structure("Jukebox",
            "jukebox",
            "",
            true,
            1)]
        Jukebox = 5,
        [Structure("Obelisk, Small",
            "nw_plc_seaobelsk",
            "",
            true,
            1)]
        ObeliskSmall = 6,
        [Structure("Ladder, Light",
            "x0_weatheredladd",
            "",
            true,
            1)]
        LadderLight = 7,
        [Structure("Ladder, Dark",
            "x0_ladder",
            "",
            true,
            1)]
        LadderDark = 8,
        [Structure("Statue, Huge",
            "x2_plc_statue_h",
            "",
            true,
            1)]
        StatueHuge = 9,
        [Structure("Mining Well",
            "x2_plc_minewell",
            "",
            true,
            1)]
        MiningWell = 10,

        // 11 is free

        [Structure("Statue, Wizard",
            "x3_plc_gg001",
            "",
            true,
            1)]
        StatueWizard = 12,
        [Structure("Gong",
            "plc_gong",
            "",
            true,
            1)]
        Gong = 13,
        [Structure("Statue, Monster",
            "plc_statue3",
            "",
            true,
            1)]
        StatueMonster = 14,

        // 15-16 free

        [Structure("Brazier, Stone",
            "plc_brazier",
            "",
            true,
            1)]
        BrazierStone = 17,
        [Structure("Statue, Guardian",
            "plc_statue1",
            "",
            true,
            1)]
        StatueGuardian = 18,
        [Structure("Doorway, Metal",
            "x0_doormetal",
            "",
            true,
            1)]
        DoorwayMetal = 19,
        [Structure("Flaming Statue",
            "x2_plc_statue_fl",
            "",
            true,
            1)]
        FlamingStatue = 20,
        [Structure("Brazier, Round",
            "x3_plc_brazier",
            "",
            true,
            1)]
        BrazierRound = 21,
        [Structure("Pedestal",
            "plc_pedestal",
            "",
            true,
            1)]
        Pedestal = 22,
        [Structure("Rope Coil",
            "nw_plc_ropecoil1",
            "",
            true,
            1)]
        RopeCoil = 23,
        [Structure("Statue, Wyvern ",
            "x3_plc_statuew",
            "",
            true,
            1)]
        StatueWyvern = 24,
        [Structure("Pedestal, Evil",
            "x3_plc_pedestal",
            "",
            true,
            1)]
        PedestalEvil = 25,
        [Structure("Birdbath",
            "plc_birdbath",
            "",
            true,
            1)]
        Birdbath = 26,
        [Structure("Sphinx Statue",
            "x0_sphinxstatue",
            "",
            true,
            1)]
        SphinxStatue = 27,
        [Structure("Mining Well Platform",
            "x2_plc_minewellb",
            "",
            true,
            1)]
        MiningWellPlatform = 28,
        [Structure("Pedestal, Sword",
            "x3_plc_pedsword",
            "",
            true,
            1)]
        PedestalSword = 29,
        [Structure("Doorway, Stone",
            "x0_stonewalldoor",
            "",
            true,
            1)]
        DoorwayStone = 30,
        [Structure("Female Statue",
            "x2_plc_statue_f",
            "",
            true,
            1)]
        FemaleStatue = 31,
        [Structure("Gnomish Contraption",
            "plc_gnomcntrptn",
            "",
            true,
            1)]
        GnomishContraption = 32,
        [Structure("Pillar, Stone",
            "plc_pillar3",
            "",
            true,
            1)]
        PillarStone = 33,

        // 34 is free

        [Structure("Statue of Lathander",
            "x2_plc_statue_la",
            "",
            true,
            1)]
        StatueOfLathander = 35,
        [Structure("Pillar, Flame",
            "plc_pillar1",
            "",
            true,
            1)]
        PillarFlame = 36,
        [Structure("Cage",
            "plc_animalcage",
            "",
            true,
            1)]
        Cage = 37,
        [Structure("Lamp Post",
            "plc_lamppost",
            "",
            true,
            1)]
        LampPost = 38,
        [Structure("Torch Bracket",
            "plc_freetorch",
            "",
            true,
            1)]
        TorchBracket = 39,
        [Structure("Pillar, Wood",
            "plc_pillar2",
            "",
            true,
            1)]
        PillarWood = 40,
        [Structure("Statue, Cyric",
            "x3_plc_statuec",
            "",
            true,
            1)]
        StatueCyric = 41,
        [Structure("Sea Idol",
            "nw_plc_seaidol",
            "",
            true,
            1)]
        SeaIdol = 42,
        [Structure("Fountain",
            "plc_fountain",
            "",
            true,
            1)]
        Fountain = 43,
        [Structure("Monster Statue",
            "x2_plc_statue_mo",
            "",
            true,
            1)]
        MonsterStatue = 44,
        [Structure("Easel",
            "plc_pntingeasel",
            "",
            true,
            1)]
        Easel = 45,

        // 46 is free

        [Structure("Keg",
            "plc_keg",
            "",
            true,
            1)]
        Keg = 47,
        [Structure("Dran Statue ",
            "x2_plc_statu_dra",
            "",
            true,
            1)]
        DranStatue = 48,

        // 49-50 free

        [Structure("Net",
            "nw_plc_net",
            "",
            true,
            1)]
        Net = 51,
        [Structure("Bed, Extra Large",
            "x0_largebed",
            "",
            true,
            1)]
        BedExtraLarge = 52,

        // 53 is free

        [Structure("Carpet, Round, Blue",
            "x0_roundrugorien",
            "",
            true,
            1)]
        CarpetRoundBlue = 54,
        [Structure("Altar, Evil",
            "plc_altrevil",
            "",
            true, 1)]
        AltarEvil = 55,
        [Structure("Table, Wood, Large",
            "x3_plc_table002",
            "",
            true,
            1)]
        TableWoodLarge = 56,
        [Structure("Table, Wood, With Fish",
            "x3_plc_table001",
            "",
            true,
            1)]
        TableWoodWithFish = 57,
        [Structure("Table, Stone, Small",
            "nw_plc_seatable",
            "",
            true,
            1)]
        TableStoneSmall = 58,
        [Structure("Hand Chair",
            "zep_chair004",
            "",
            true,
            1)]
        HandChair = 59,
        [Structure("Window",
            "x0_window",
            "",
            true,
            1)]
        Window = 60,
        [Structure("Cushions",
            "x0_cushions",
            "",
            true,
            1)]
        Cushions = 61,
        [Structure("Candle",
            "nw_plc_candle1",
            "",
            true,
            1)]
        Candle = 62,

        // 63 is free

        [Structure("Bear Skin Rug",
            "x0_bearskinrug1",
            "",
            true,
            1)]
        BearSkinRug = 64,
        [Structure("Chandelier",
            "x0_chandelier",
            "",
            true,
            1)]
        Chandelier = 65,

        // 66 is free

        [Structure("Urn",
            "plc_urn",
            "",
            true,
            1)]
        Urn = 67,
        [Structure("Altar, Stone",
            "plc_altrneutral",
            "",
            true,
            1)]
        AltarStone = 68,
        [Structure("Cot",
            "plc_cot",
            "",
            true,
            1)]
        Cot = 69,
        [Structure("Table, Wood",
            "plc_table",
            "",
            true,
            1)]
        TableWood = 70,

        // 71 is free

        [Structure("Throw Rug",
            "plc_throwrug",
            "",
            true,
            1)]
        ThrowRug = 72,
        [Structure("Table, Stone, Large",
            "nw_plc_dwarftabl",
            "",
            true,
            1)]
        TableStoneLarge = 73,
        [Structure("Bed, Stone, Yellow",
            "nw_plc_dwarfbed",
            "",
            true,
            1)]
        BedStoneYellow = 74,
        [Structure("Bed, Large",
            "x0_beddouble",
            "",
            true,
            1)]
        BedLarge = 75,
        [Structure("Vase, Rounded",
            "x0_vaseflower",
            "",
            true,
            1)]
        VaseRounded = 76,
        [Structure("Carpet",
            "x0_ruglarge",
            "",
            true,
            1)]
        Carpet = 77,
        [Structure("Bed, Wood, Yellow",
            "plc_bed",
            "",
            true,
            1)]
        BedWoodYellow = 78,
        [Structure("Overgrown Pillar",
            "x0_overgrownrui",
            "",
            true,
            1)]
        OvergrownPillar = 79,
        [Structure("Tome",
            "x0_tome",
            "",
            true,
            1)]
        Tome = 80,
        [Structure("Bird Cage",
            "plc_birdcage",
            "",
            true,
            1)]
        BirdCage = 81,
        [Structure("Pillar, Wood, Dark",
            "x3_plc_pillar1",
            "",
            true,
            1)]
        PillarWoodDark = 82,
        [Structure("Bunk Bed",
            "x2_plc_bunkbed",
            "",
            true,
            1)]
        BunkBed = 83,
        [Structure("Vase, Tall",
            "x0_bigvase",
            "",
            true,
            1)]
        VaseTall = 84,
        [Structure("Bed Roll",
            "plc_bedrolls",
            "",
            true,
            1)]
        BedRoll = 85,
        [Structure("Ottoman",
            "x0_ottoman",
            "",
            true,
            1)]
        Ottoman = 86,

        // 87 is free

        [Structure("Pillar, Rounded",
            "x0_ruinedpillar",
            "",
            true,
            1)]
        PillarRounded = 88,
        [Structure("Painting 2",
            "x0_painting2",
            "",
            true,
            1)]
        Painting2 = 89,
        [Structure("Candelabra",
            "plc_candelabra",
            "",
            true,
            1)]
        Candelabra = 90,
        [Structure("Potted Plant",
            "plc_pottedplant",
            "",
            true,
            1)]
        PottedPlant = 91,
        [Structure("Painting 1",
            "x0_painting",
            "",
            true,
            1)]
        Painting1 = 92,
        [Structure("Carpet, Fancy",
            "x0_rugoriental",
            "",
            true,
            1)]
        CarpetFancy = 93,

        // 94 is free

        [Structure("Illithid Table",
            "x2_plc_tablemind",
            "",
            true,
            1)]
        IllithidTable = 95,
        [Structure("Carpet, Fancy, Smaller",
            "x0_rugoriental2",
            "",
            true,
            1)]
        CarpetFancySmaller = 96,
        [Structure("Drow Altar",
            "x2_plc_drowaltar",
            "",
            true,
            1)]
        DrowAltar = 97,
        [Structure("Dartboard",
            "x3_plc_dartbrd",
            "",
            true,
            1)]
        Dartboard = 98,
        [Structure("Map",
            "x0_maps",
            "",
            true,
            1)]
        Map = 99,
        [Structure("Floor-anchored shackles",
            "plc_flrshackles",
            "",
            true,
            1)]
        FloorAnchoredShackles = 100,
        [Structure("Round Wooden Table",
            "x2_plc_tablernd",
            "",
            true,
            1)]
        RoundWoodenTable = 101,
        [Structure("Drow Bar",
            "x2_plc_drowbar",
            "",
            true,
            1)]
        DrowBar = 102,
        [Structure("Shrine of Umberlee",
            "nw_plc_shrnumbr1",
            "",
            true,
            1)]
        ShrineOfUmberlee = 103,
        [Structure("Rune Pillar",
            "x0_runepillar",
            "",
            true,
            1)]
        RunePillar = 104,

        // 105 is free

        [Structure("Mirror",
            "x2_plc_mirror",
            "",
            true,
            1)]
        Mirror = 106,
        [Structure("Footstool",
            "plc_footstool",
            "",
            true,
            1)]
        Footstool = 107,
        [Structure("Drow Table",
            "x2_plc_tabledrow",
            "",
            true,
            1)]
        DrowTable = 108,

        // 109-110 free

        [Structure("Bench, Stone, Dwarven",
            "bench_stonedwarf",
            "",
            true,
            1)]
        BenchStoneDwarven = 111,
        [Structure("Illithid Chair",
            "illithid_chair",
            "",
            true,
            1)]
        IllithidChair = 112,
        [Structure("Bench, Wood",
            "bench_wood",
            "",
            true,
            1)]
        BenchWood = 113,
        [Structure("Chair, Wood, Medium",
            "chair_med_wood",
            "",
            true,
            1)]
        ChairWoodMedium = 114,
        [Structure("Bench, Large",
            "bench_large",
            "",
            true,
            1)]
        BenchLarge = 115,
        [Structure("Drow Chair",
            "drow_chair",
            "",
            true,
            1)]
        DrowChair = 116,
        [Structure("Bench, Wood, Large",
            "bench_largewood",
            "",
            true,
            1)]
        BenchWoodLarge = 117,
        [Structure("Bench, Wood, Small",
            "bench_woodsmall",
            "",
            true,
            1)]
        BenchWoodSmall = 118,
        [Structure("Chair, Wood",
            "chair_wood",
            "",
            true,
            1)]
        ChairWood = 119,
        [Structure("Chair, Wood, Small",
            "chair_wood_small",
            "",
            true,
            1)]
        ChairWoodSmall = 120,
        [Structure("Throne, Wood",
            "thronewood001",
            "",
            true,
            1)]
        ThroneWood = 121,
        [Structure("Chair, Stone",
            "chair_stone",
            "",
            true,
            1)]
        ChairStone = 122,
        [Structure("Bench, Wood, Small 2",
            "bench_wood2",
            "",
            true,
            1)]
        BenchWoodSmall2 = 123,
        [Structure("Chair, Shell",
            "chair_shell",
            "",
            true,
            1)]
        ChairShell = 124,
        [Structure("Couch, Wood, Yellow",
            "couch_wood",
            "",
            true,
            1)]
        CouchWoodYellow = 125,
        [Structure("Weapon Rack, Wall Mounted",
            "swlorweaponrack",
            "",
            true,
            1)]
        WeaponRackWallMounted = 126,
        [Structure("Statue, Twi'lek",
            "statue_twilek",
            "",
            true,
            1)]
        StatueTwilek = 127,
        [Structure("Chair, Chancellor",
            "statue_twilek001",
            "",
            true,
            1)]
        ChairChancellor = 128,
        [Structure("Statue, Robed Woman",
            "statue_woman",
            "",
            true,
            1)]
        StatueRobedWoman = 129,
        [Structure("Armchair, High Back (Orange)",
            "_mdrn_pl_chair09",
            "",
            true,
            1)]
        ArmchairHighBackOrange = 130,
        [Structure("Armchair, High Back (Blue)",
            "_mdrn_pl_chair10",
            "",
            true,
            1)]
        ArmchairHighBackBlue = 131,
        [Structure("Bench",
            "_mdrn_pl_df_chb",
            "",
            true,
            1)]
        Bench = 132,
        [Structure("Banner, Wall, Lizard",
            "_mdrn_pl_bannr03",
            "",
            true,
            1)]
        BannerWallLizard = 133,
        [Structure("Bathtub ",
            "_mdrn_pl_bathtb1 ",
            "",
            true,
            1)]
        Bathtub = 134,
        [Structure("Bed, High Back (Black/Grey)",
            "_mdrn_pl_beddbgr",
            "",
            true,
            1)]
        BedHighBackBlackGrey = 135,
        [Structure("Bed, High Back (Blue)",
            "_mdrn_pl_bedsgbl",
            "",
            true,
            1)]
        BedHighBackBlue = 136,
        [Structure("Bed, Medical/Exam",
            "_mdrn_pl_bmdcl",
            "",
            true,
            1)]
        BedMedicalExam = 137,
        [Structure("Bookshelf, Pedestal (White)",
            "_mdrn_pl_bookcs5",
            "",
            true,
            1)]
        BookshelfPedestalWhite = 138,
        [Structure("Cabinet, Curved (Grey/White)",
            "_mdrn_pl_cabint3",
            "",
            true,
            1)]
        CabinetCurvedGreyWhite = 139,
        [Structure("Bed, Side Table",
            "_mdrn_pl_bedmode",
            "",
            true,
            1)]
        BedSideTable = 140,
        [Structure("Bed, Low",
            "_mdrn_pl_beddbsc",
            "",
            true,
            1)]
        BedLow = 141,
        [Structure("Banner, Standing",
            "_mdrn_pl_bannr04",
            "",
            true,
            1)]
        BannerStanding = 142,
        [Structure("Chair, Crew (Grey)",
            "_mdrn_pl_chrgren",
            "",
            true,
            1)]
        ChairCrewGrey = 143,
        [Structure("Chair, Open Frame (Brown)",
            "_mdrn_pl_chair32",
            "",
            true,
            1)]
        ChairOpenFrameBrown = 144,
        [Structure("Chair, Pedestal /w Arms",
            "_mdrn_pl_chairbl",
            "",
            true,
            1)]
        ChairPedestalWithArms = 145,
        [Structure("Chair, Pedestal, Padded (Red)",
            "_mdrn_pl_chairpr",
            "",
            true,
            1)]
        ChairPedestalPaddedRed = 146,
        [Structure("Chair, Pedestal, Panel",
            "_mdrn_pl_chair31",
            "",
            true,
            1)]
        ChairPedestalPanel = 147,
        [Structure("Console, Floor Mounted (Blue Screens)",
            "_mdrn_pl_cons001",
            "",
            true,
            1)]
        ConsoleFloorMountedBlueScreens = 148,
        [Structure("Console, Floor Mounted (Green Screens)",
            "_mdrn_pl_conso04",
            "",
            true,
            1)]
        ConsoleFloorMountedGreenScreens = 149,
        [Structure("Cot /w Table",
            "_mdrn_pl_cotsf",
            "",
            true,
            1)]
        CotWithTable = 150,
        [Structure("Desk, Control Board Inlay",
            "_mdrn_pl_df_dscb",
            "",
            true,
            1)]
        DeskControlBoardInlay = 151,
        [Structure("Desk, Control Center",
            "_mdrn_pl_conso03",
            "",
            true,
            1)]
        DeskControlCenter = 152,
        [Structure("Desk, Control Center, Large Screen",
            "_mdrn_pl_conso27",
            "",
            true,
            1)]
        DeskControlCenterLargeScreen = 153,
        [Structure("Desk, Control Center, Wide",
            "_mdrn_pl_conso19",
            "",
            true,
            1)]
        DeskControlCenterWide = 154,
        [Structure("Desk, Corner /w Terminal",
            "_mdrn_pl_deskcn1",
            "",
            true,
            1)]
        DeskCornerWithTerminal = 155,
        [Structure("Desk, Information/Control Center",
            "_mdrn_pl_df_dicc",
            "",
            true,
            1)]
        DeskInformationControlCenter = 156,
        [Structure("Desk, Wall /w Terminal",
            "_mdrn_pl_deskter",
            "",
            true,
            1)]
        DeskWallwTerminal = 157,
        [Structure("Desk, Wall /w Terminal, Wide",
            "_mdrn_pl_desktew",
            "",
            true,
            1)]
        DeskWallwTerminalWide = 158,
        [Structure("Work Station, Droid Repair",
            "_mdrn_pl_conso08",
            "",
            true,
            1)]
        WorkStationDroidRepair = 159,
        [Structure("Footlocker, Modern (Keyed Entry)",
            "_mdrn_pl_df_fmke",
            "",
            true,
            1)]
        FootlockerModernKeyedEntry = 160,
        [Structure("Fountain, Oval",
            "_mdrn_pl_fountn2",
            "",
            true,
            1)]
        FountainOval = 161,
        [Structure("Holo Display",
            "_mdrn_pl_holod01",
            "",
            true,
            1)]
        HoloDisplay = 162,
        [Structure("Holo Display 2",
            "_mdrn_pl_holod03",
            "",
            true,
            1)]
        HoloDisplay2 = 163,
        [Structure("Holo Display 4",
            "_mdrn_pl_holod05",
            "",
            true,
            1)]
        HoloDisplay4 = 164,
        [Structure("Holo Display 5",
            "_mdrn_pl_holod06",
            "",
            true,
            1)]
        HoloDisplay5 = 165,
        [Structure("Holo Projector 1",
            "_mdrn_pl_holoco2",
            "",
            true,
            1)]
        HoloProjector1 = 166,
        [Structure("Holo Projector 2",
            "_mdrn_pl_holobas",
            "",
            true,
            1)]
        HoloProjector2 = 167,
        [Structure("Instrument Panel, Large Monitor (Technical Data)",
            "_mdrn_pl_instptd",
            "",
            true,
            1)]
        InstrumentPanelLargeMonitorTechnicalData = 168,
        [Structure("Kolto Tank (Empty)",
            "_mdrn_pl_clntnke",
            "",
            true,
            1)]
        KoltoTankEmpty = 169,
        [Structure("Lamp, Eggs (Pink)",
            "_mdrn_pl_lampd09",
            "",
            true,
            1)]
        LampEggsPink = 170,
        [Structure("Lamp, On Poles",
            "_mdrn_pl_lampd04",
            "",
            true,
            1)]
        LampOnPoles = 171,
        [Structure("Lantern, Post, Marble",
            "_mdrn_pl_lamp4",
            "",
            true,
            1)]
        LanternPostMarble = 172,
        [Structure("Locker, Metal Trapezoid",
            "_mdrn_pl_rustedl",
            "",
            true,
            1)]
        LockerMetalTrapezoid = 173,
        [Structure("Mirror (Small)",
            "_mdrn_pl_mirror1",
            "",
            true,
            1)]
        MirrorSmall = 174,
        [Structure("Monitor, Wall, Logo Display (Blue)",
            "_mdrn_pl_screend",
            "",
            true,
            1)]
        MonitorWallLogoDisplayBlue = 175,
        [Structure("Ornament, Solar System",
            "_mdrn_pl_ornamnt",
            "",
            true,
            1)]
        OrnamentSolarSystem = 176,
        [Structure("Ottoman, Decorated (Black)",
            "_mdrn_pl_ottomn2",
            "",
            true,
            1)]
        OttomanDecoratedBlack = 177,
        [Structure("Pipes, Conduit (with Power Controls)",
            "_mdrn_pl_df_pcpc",
            "",
            true,
            1)]
        PipesConduitWithPowerControls = 178,
        [Structure("Cylinder, Cross Top",
            "_mdrn_pl_conta38",
            "",
            true,
            1)]
        CylinderCrossTop = 179,
        [Structure("Pot, Bush, Clipped",
            "_mdrn_pl_flowrp2",
            "",
            true,
            1)]
        PotBushClipped = 180,
        [Structure("Pot, Bush, Flowers",
            "_mdrn_pl_flowrby",
            "",
            true,
            1)]
        PotBushFlowers = 181,
        [Structure("Pot, Bush, Tall",
            "_mdrn_pl_flowrpi",
            "",
            true,
            1)]
        PotBushTall = 182,
        [Structure("Pot, Clay Urn",
            "_mdrn_pl_pottery",
            "",
            true,
            1)]
        PotClayUrn = 183,
        [Structure("Pot, Flower, Daisy",
            "_mdrn_pl_flowpt1",
            "",
            true,
            1)]
        PotFlowerDaisy = 184,
        [Structure("Pot, Flower, Yellow",
            "_mdrn_pl_flowpt2",
            "",
            true,
            1)]
        PotFlowerYellow = 185,
        [Structure("Pot, Long Leaf 1",
            "_mdrn_pl_plant09",
            "",
            true,
            1)]
        PotLongLeaf1 = 186,
        [Structure("Pot, Plant, Aloa",
            "_mdrn_pl_flowspk",
            "",
            true,
            1)]
        PotPlantAloa = 187,
        [Structure("Pot, Plant, Tropical",
            "_mdrn_pl_potplnt",
            "",
            true,
            1)]
        PotPlantTropical = 188,
        [Structure("Pot, Urn, Grecian",
            "_mdrn_pl_flowr02",
            "",
            true,
            1)]
        PotUrnGrecian = 189,
        [Structure("Skeleton, Medical Display",
            "_mdrn_pl_skeleto",
            "",
            true,
            1)]
        SkeletonMedicalDisplay = 190,
        [Structure("Chest, Trapezoid (White)",
            "_mdrn_pl_conta14",
            "",
            true,
            1)]
        ChestTrapezoidWhite = 191,
        [Structure("Space Suit (Tan)",
            "_mdrn_pl_spcest1",
            "",
            true,
            1)]
        SpaceSuitTan = 192,
        [Structure("Specimen Tube (Alien)",
            "_mdrn_pl_alntbea",
            "",
            true,
            1)]
        SpecimenTubeAlien = 193,
        [Structure("Specimen Tube, Empty",
            "_mdrn_pl_tubeemp",
            "",
            true,
            1)]
        SpecimenTubeEmpty = 194,
        [Structure("Specimen Tube, Tall",
            "_mdrn_pl_tube",
            "",
            true,
            1)]
        SpecimenTubeTall = 195,
        [Structure("Statue, Bust on Column",
            "_mdrn_pl_bust",
            "",
            true,
            1)]
        StatueBustonColumn = 196,
        [Structure("Statue, Kneeling Man",
            "_mdrn_pl_statue1",
            "",
            true,
            1)]
        StatueKneelingMan = 197,
        [Structure("Statue, Robed Figure /w Staff",
            "_mdrn_pl_statu11",
            "",
            true,
            1)]
        StatueRobedFigurewStaff = 198,
        [Structure("Statue, Senator",
            "_mdrn_pl_statue2",
            "",
            true,
            1)]
        StatueSenator = 199,
        [Structure("Storage Tank, Hemisphere /w Monitor",
            "_mdrn_pl_machin1",
            "",
            true,
            1)]
        StorageTankHemispherewMonitor = 200,
        [Structure("Stuffed Toy, Bantha",
            "_mdrn_pl_stuffed",
            "",
            true,
            1)]
        StuffedToyBantha = 201,
        [Structure("Table, Coffee, Elegant (White)",
            "_mdrn_pl_table10",
            "",
            true,
            1)]
        TableCoffeeElegantWhite = 202,
        [Structure("Table, Conference, Centre Cloth",
            "_mdrn_pl_cnfrtbl",
            "",
            true,
            1)]
        TableConferenceCentreCloth = 203,
        [Structure("Table, Oval, Centre Leg (Dark)",
            "_mdrn_pl_table15",
            "",
            true,
            1)]
        TableOvalCentreLegDark = 204,
        [Structure("Table, Oval, Low (Blue)",
            "_mdrn_pl_tabl001",
            "",
            true,
            1)]
        TableOvalLowBlue = 205,
        [Structure("Table, Round, Low (Blue)",
            "_mdrn_pl_table19",
            "",
            true,
            1)]
        TableRoundLowBlue = 206,
        [Structure("Table, Stone (Blue)",
            "_mdrn_pl_table24",
            "",
            true,
            1)]
        TableStoneBlue = 207,
        [Structure("Table, Stone (Brown)",
            "_mdrn_pl_table23",
            "",
            true,
            1)]
        TableStoneBrown = 208,
        [Structure("Table, Wall, Oval",
            "_mdrn_pl_table32",
            "",
            true,
            1)]
        TableWallOval = 209,
        [Structure("Toilet, White /w Cistern",
            "_mdrn_pl_toilet",
            "",
            true,
            1)]
        ToiletWhitewCistern = 210,
        [Structure("Wall Light, Curved",
            "_mdrn_pl_lights4",
            "",
            true,
            1)]
        WallLightCurved = 211,
        [Structure("Wall Light, Octagon",
            "_mdrn_pl_lights6",
            "",
            true,
            1)]
        WallLightOctagon = 212,
        [Structure("Wardrobe, Curved (White)",
            "_mdrn_pl_armoir3",
            "",
            true,
            1)]
        WardrobeCurvedWhite = 213,
        [Structure("Washbasin, Lever Faucet",
            "_mdrn_pl_sinka",
            "",
            true,
            1)]
        WashbasinLeverFaucet = 214,
        [Structure("Weapon Rack",
            "_mdrn_pl_weaprck",
            "",
            true,
            1)]
        WeaponRack = 215,
        [Structure("Fridge, Worn",
            "_mdrn_pl_fridge3",
            "",
            true,
            1)]
        FridgeWorn = 216,
        [Structure("Fridge, Dark",
            "_mdrn_pl_fridge2",
            "",
            true,
            1)]
        FridgeDark = 217,
        [Structure("Fridge, Stainless",
            "_mdrn_pl_fridge1",
            "",
            true,
            1)]
        FridgeStainless = 218,
        [Structure("Cookpot",
            "cookpot",
            "",
            true,
            0)]
        Cookpot = 219,
        [Structure("Engineering Terminal",
            "engineering_term",
            "",
            true,
            0)]
        EngineeringTerminal = 220,
        [Structure("Fabrication Terminal",
            "fabrication_term",
            "",
            true,
            0)]
        FabricationTerminal = 221,
        [Structure("Refinery",
            "cft_forge",
            "",
            true,
            0)]
        Refinery = 222,
        [Structure("Smithery Bench",
            "smithery_bench",
            "",
            true,
            0)]
        SmitheryBench = 223,
        [Structure("Rug, (Classic Light Brown)",
            "_mdrn_pl_carpt01",
            "",
            true,
            1)]
        RugClassicLightBrown = 224,
        [Structure("Pile of Cushions (White)",
            "zep_cushion002",
            "",
            true,
            1)]
        PileofCushionsWhite = 225,
        [Structure("Oven",
            "_mdrn_pl_oven",
            "",
            true,
            1)]
        Oven = 226,
        [Structure("Coffee Maker",
            "_mdrn_pl_coffful",
            "",
            true,
            1)]
        CoffeeMaker = 227,
        [Structure("Microwave (Black)",
            "_mdrn_pl_microwb",
            "",
            true,
            1)]
        MicrowaveBlack = 228,
        [Structure("Bar - White",
            "_mdrn_pl_bar",
            "",
            true,
            1)]
        BarWooden = 229,
        [Structure("Couch Leather - Blue",
            "frn_couch_swlr05",
            "",
            true,
            1)]
        CouchLeatherBlue = 230,

        // 231-232 are open

        [Structure("Table, Plastic (Large)",
            "_mdrn_pl_tabplal",
            "",
            true,
            1)]
        TablePlasticLarge = 233,
        [Structure("Table, Round, Oak",
            "_mdrn_pl_table1o",
            "",
            true,
            1)]
        TableRoundOak = 234,
        [Structure("Metal Wall, Wide, Light",
            "_mdrn_pl_hwall24",
            "",
            true,
            1)]
        MetalWallWideLight = 235,
        [Structure("Store Counter (Stained)",
            "_mdrn_pl_store02",
            "",
            true,
            1)]
        StoreCounterStained = 236,
        [Structure("Chair, Large (Grey/Red)",
            "_mdrn_pl_chair30",
            "",
            true,
            1)]
        ChairLargeGreyRed = 237,
        [Structure("Bunkbed (Metal), Grey",
            "_mdrn_pl_shipbnk",
            "",
            true,
            1)]
        BunkbedMetalGrey = 238,
        [Structure("Shower, White",
            "_mdrn_pl_shower",
            "",
            true,
            1)]
        ShowerWhite = 239,
        [Structure("Shower, Floor Basin",
            "_mdrn_pl_showerf",
            "",
            true,
            1)]
        ShowerFloorBasin = 240,
        [Structure("Couch, Leather - Grey",
            "frn_couch_swlr06",
            "",
            true,
            1)]
        CouchLeatherPanelsGrey = 241,
        [Structure("Shelves, Warehouse, Full",
            "_mdrn_pl_shelf14",
            "",
            true,
            1)]
        ShelvesWarehouseFull = 242,
        [Structure("Bookshelf, Jedi",
            "swlor_0486",
            "",
            true,
            1)]
        BookshelfJedi = 243,
        [Structure("Wooden Wall, Planks (Small)",
            "_mdrn_pl_wwall6t",
            "",
            true,
            1)]
        MetalWallSinglePipes = 244,
        [Structure("Television, Big Screen",
            "_mdrn_pl_tvbgscr",
            "",
            true,
            1)]
        TelevisionBigScreen = 245,

        // 246 is open

        [Structure("Foyer Chandelier",
            "zep_ci_lgt_003",
            "",
            true,
            1)]
        FoyerChandelier = 247,
        [Structure("Metal Wall, Door, Light",
            "_mdrn_pl_dwall09",
            "",
            true,
            1)]
        MetalWallDoorLight = 248,
        [Structure("Metal Wall, Wide, Panels",
            "_mdrn_pl_hwall19",
            "",
            true,
            1)]
        MetalWallWidePanels = 249,
        [Structure("Metal Wall, Door, Panels",
            "_mdrn_pl_dwall02",
            "",
            true,
            1)]
        MetalWallDoorPanels = 250,
        [Structure("Metal Wall, Single, Light",
            "_mdrn_pl_qwall09",
            "",
            true,
            1)]
        MetalWallSingleLight = 251,
        [Structure("Metal Wall, Single, Ribbed",
            "_mdrn_pl_qwall08",
            "",
            true,
            1)]
        MetalWallSingleRibbed = 252,
        [Structure("Metal Wall, Wide, Ribbed",
            "_mdrn_pl_hwall23",
            "",
            true,
            1)]
        MetalWallWideRibbed = 253,
        [Structure("Metal Wall, Door, Ribbed",
            "_mdrn_pl_dwall08",
            "",
            true,
            1)]
        MetalWallDoorRibbed = 254,
        [Structure("Metal Wall, Wide, Pipes",
            "_mdrn_pl_hwall31",
            "",
            true,
            1)]
        MetalWallWidePipes = 255,
        [Structure("Metal Wall, Door, Pipes",
            "_mdrn_pl_dwall16",
            "",
            true,
            1)]
        MetalWallDoorPipes = 256,
        [Structure("Couch, Cushion - Red",
            "frn_couch_swlr04",
            "",
            true,
            1)]
        CouchCushionGreyRed = 257,
        [Structure("Bench, Elegant, Grey",
            "frn_bench_swlr02",
            "",
            true,
            1)]
        BenchElegantGrey = 258,
        [Structure("Chaise Lounge - Orange",
            "frn_couch_swlr01",
            "",
            true,
            1)]
        ChaiseLoungeOrange = 259,
        [Structure("Couch, Blanket Cover - Red",
            "frn_couch_swlr03",
            "",
            true,
            1)]
        CouchBlanketCoverRed = 260,
        [Structure("Chair, Plinth",
            "_mdrn_pl_chairel",
            "",
            true,
            1)]
        ChairPlinth = 261,
        [Structure("Chair, Dining - Grey",
            "frn_chair_swlr02",
            "",
            true,
            1)]
        ChairDiningGrey = 262,
        [Structure("Chaise Lounge - Red",
            "frn_couch_swlr02",
            "",
            true,
            1)]
        ChaiseLoungeRed = 263,
        [Structure("Chair, Dining - Orange",
            "_mdrn_pl_chairdi",
            "",
            true,
            1)]
        ChairDiningOrange = 264,
        [Structure("Table, Polygon Design",
            "_mdrn_pl_table26",
            "",
            true,
            1)]
        TablePolygonDesign = 265,
        [Structure("Table, Round, Glass",
            "_mdrn_pl_table16",
            "",
            true,
            1)]
        TableRoundGlass = 266,
        [Structure("Table, Dark, Glass",
            "zep_tableglass",
            "",
            true,
            1)]
        TableDarkGlass = 267,
        [Structure("Television Old Model",
            "_mdrn_pl_tvold",
            "",
            true,
            1)]
        TelevisionOldModel = 268,

        [Structure("Droid Assembly Terminal",
            "droid_ass_term",
            "",
            true,
            0)]
        DroidAssemblyTerminal = 269,

        [Structure("Beast Stable Terminal",
            "bst_stables_term",
            "",
            true,
            0)]
        BeastStableTerminal = 270,

        [Structure("Incubator",
            "incubator",
            "",
            true,
            0, 
            PropertyType.Lab,
            PropertyLayoutType.Invalid,
            true,
            StructureCategoryType.ResearchDevice)]
        Incubator = 271,

        // Buildings start here (5000+)
        [Structure("City Hall - Style 1",
            "city_hall",
            "",
            true,
            0,
            PropertyType.City,
            PropertyLayoutType.CityHallStyle1,
            false)]
        CityHall = 5000,

        [Structure("Bank - Style 1",
            "bank",
            "",
            true,
            0,
            PropertyType.City,
            PropertyLayoutType.BankStyle1)]
        Bank = 5001,

        [Structure("Medical Center - Style 1",
            "medical_center",
            "",
            true,
            0,
            PropertyType.City,
            PropertyLayoutType.MedicalCenterStyle1)]
        MedicalCenter = 5002,

        [Structure("Starport - Style 1",
            "starport",
            "",
            true,
            0,
            PropertyType.City,
            PropertyLayoutType.StarportStyle1)]
        Starport = 5003,

        [Structure("Cantina - Style 1",
            "cantina",
            "",
            true,
            0,
            PropertyType.City,
            PropertyLayoutType.CantinaStyle1)]
        Cantina = 5004,

        [Structure("Small House - Style 1",
            "house1",
            "",
            true,
            0,
            PropertyType.City,
            PropertyLayoutType.SmallHouseStyle1)]
        SmallHouseStyle1 = 5005,

        [Structure("Small House - Style 2",
            "house1",
            "",
            true,
            0,
            PropertyType.City,
            PropertyLayoutType.SmallHouseStyle2)]
        SmallHouseStyle2 = 5006,

        [Structure("Small House - Style 3",
            "house1",
            "",
            true,
            0,
            PropertyType.City,
            PropertyLayoutType.SmallHouseStyle3)]
        SmallHouseStyle3 = 5007,

        [Structure("Small House - Style 4",
            "house1",
            "",
            true,
            0,
            PropertyType.City,
            PropertyLayoutType.SmallHouseStyle4)]
        SmallHouseStyle4 = 5008,

        [Structure("Medium House - Style 1",
            "house1",
            "",
            true,
            0,
            PropertyType.City,
            PropertyLayoutType.MediumHouseStyle1)]
        MediumHouseStyle1 = 5009,

        [Structure("Medium House - Style 2",
            "house1",
            "",
            true,
            0,
            PropertyType.City,
            PropertyLayoutType.MediumHouseStyle2)]
        MediumHouseStyle2 = 5010,

        [Structure("Medium House - Style 3",
            "house1",
            "",
            true,
            0,
            PropertyType.City,
            PropertyLayoutType.MediumHouseStyle3)]
        MediumHouseStyle3 = 5011,

        [Structure("Medium House - Style 4",
            "house1",
            "",
            true,
            0,
            PropertyType.City,
            PropertyLayoutType.MediumHouseStyle4)]
        MediumHouseStyle4 = 5012,

        [Structure("Large House - Style 1",
            "house1",
            "",
            true,
            0,
            PropertyType.City,
            PropertyLayoutType.LargeHouseStyle1)]
        LargeHouseStyle1 = 5013,

        [Structure("Large House - Style 2",
            "house1",
            "",
            true,
            0,
            PropertyType.City,
            PropertyLayoutType.LargeHouseStyle2)]
        LargeHouseStyle2 = 5014,

        [Structure("Large House - Style 3",
            "house1",
            "",
            true,
            0,
            PropertyType.City,
            PropertyLayoutType.LargeHouseStyle3)]
        LargeHouseStyle3 = 5015,

        [Structure("Large House - Style 4",
            "house1",
            "",
            true,
            0,
            PropertyType.City,
            PropertyLayoutType.LargeHouseStyle4)]
        LargeHouseStyle4 = 5016,

        [Structure("Lab - Style 1",
            "lab1",
            "",
            true,
            0,
            PropertyType.City,
            PropertyLayoutType.LabStyle1)]
        LabStyle1 = 5017,
    }

    public class StructureAttribute : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public string Resref { get; set; }
        public int ItemStorage { get; set; }
        public PropertyType RestrictedPropertyTypes { get; set; }
        public PropertyLayoutType LayoutType { get; set; }
        public bool CanBeRetrieved { get; set; }
        public StructureCategoryType Category { get; set; }

        public StructureAttribute(
            string name,
            string resref,
            string description,
            bool isActive,
            int itemStorage,
            PropertyType restrictedPropertyTypes =
                PropertyType.Apartment |
                PropertyType.CityHall |
                PropertyType.Starship |
                PropertyType.City |
                PropertyType.Bank |
                PropertyType.MedicalCenter |
                PropertyType.Starport |
                PropertyType.Cantina |
                PropertyType.House |
                PropertyType.Lab,
            PropertyLayoutType layoutType = PropertyLayoutType.Invalid,
            bool canBeRetrieved = true,
            StructureCategoryType category = StructureCategoryType.Structure)
        {
            Name = name;
            Resref = resref;
            Description = description;
            IsActive = isActive;
            ItemStorage = itemStorage;
            RestrictedPropertyTypes = restrictedPropertyTypes;
            LayoutType = layoutType;
            CanBeRetrieved = canBeRetrieved;
            Category = category;
        }
    }
}
