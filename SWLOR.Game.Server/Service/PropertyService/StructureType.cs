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
        [Structure("Campfire",
            "plc_campfr",
            "",
            true,
            1)]
        Campfire = 11,
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
        [Structure("Table, Darkwood",
            "zep_table001",
            "",
            true,
            1)]
        TableDarkwood = 15,
        [Structure("Table, Coffee, Shelf",
            "_mdrn_pl_table20",
            "",
            true,
            1)]
        TTableCoffeeShelf = 16,
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
        [Structure("Lamp, Bars",
            "_mdrn_pl_lghtpl4",
            "",
            true,
            1)]
        LampBars = 34,
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
        [Structure("Mat, Small Tatami",
            "zep_tatami001",
            "",
            true,
            1)]
        MatSmallTatami = 46,
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
        [Structure("Umbrella, Blue",
            "_mdrn_pl_umbllab",
            "",
            true,
            1)]
        UmbrellaBlue = 49,
        [Structure("Umbrella, Red",
            "_mdrn_pl_umbllar",
            "",
            true,
            1)]
        UmbrellaRed = 50,
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
        [Structure("Chair, Wooden Padded",
            "zep_chking_004",
            "",
            true,
            1)]
        ChairWoodenPadded = 53,
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
        [Structure("Chair, Curved Form",
            "_mdrn_pl_chairdo",
            "",
            true,
            1)]
        ChairCurvedForm = 63,
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
        [Structure("Chair, Wooden Striped",
            "_mdrn_pl_chairws",
            "",
            true,
            1)]
        ChairWoodenStriped = 66,
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
        [Structure("Footlocker, Black",
            "_mdrn_pl_canten1",
            "",
            true,
            1)]
        FootlockerBlack = 71,
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
        [Structure("Table, Steel, Stained",
            "swd_table04",
            "",
            true,
            1)]
        TableSteelStained = 87,
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
        [Structure("Monitor, Overhead",
            "_mdrn_pl_monitr5",
            "",
            true,
            1)]
        MonitorOverhead = 94,
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
        [Structure("Chair, Dining, Gothic",
            "zep_ci_chr_001",
            "",
            true,
            1)]
        ChairDiningGothic = 105,
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
        [Structure("Bed, Low (Blue)",
            "zep_lowbed002",
            "",
            true,
            1)]
        BedLowBlue = 109,
        [Structure("Bed, Low (Red)",
            "zep_lowbed004",
            "",
            true,
            1)]
        BedLowRed = 110,
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
        [Structure("Washbasin",
            "_mdrn_pl_washbdl",
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
        [Structure("Pillar, Power Control",
            "_mdrn_pl_powerps",
            "",
            true,
            1)]
        PillarPowerControl = 231,
        [Structure("Carpet, Medallion",
            "zep_throwrug013",
            "",
            true,
            1)]
        CarpetMedallion = 232,
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
        [Structure("Fountain, Stone, 4 Spouts",
            "zep_fountain004",
            "",
            true,
            1)]
        FountainStone4Spouts = 246,
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

        [Structure("Research Terminal",
            "research_term",
            "",
            true,
            0,
            PropertyType.Lab,
            PropertyLayoutType.Invalid,
            true,
            StructureCategoryType.ResearchDevice)]
        ResearchTerminal = 272,

        [Structure("Locker, Open Shelves",
            "_mdrn_pl_lockerm",
            "",
            true,
            1)]
        LockerOpenShelves = 273,
        [Structure("Cabinet, Liquor (Mixed)",
            "nwed_mixbarshlf",
            "",
            true,
            1)]
        CabinetLiquorMixed = 274,
        [Structure("Counter, Lab (Straight)",
            "_mdrn_pl_cntlabs",
            "",
            true,
            1)]
        CounterLabStraight = 275,
        [Structure("Counter, Lab (Sink)",
            "_mdrn_pl_cntlbws",
            "",
            true,
            1)]
        CounterLabSink = 276,
        [Structure("Chair, Brown Wingback",
            "zep_ci_chr_003",
            "",
            true,
            1)]
        ChairBrownWingback = 277,
        [Structure("Pillar, Dish Tower",
            "_mdrn_pl_comtwr1",
            "",
            true,
            1)]
        PillarDishTower = 278,
        [Structure("Console, Tall w/ Screen (Blue Spiral)",
            "_mdrn_pl_conso16",
            "",
            true,
            1)]
        ConsoleTwScreenSpiral = 279,
        [Structure("Storage Tank, Cylinder",
            "_mdrn_pl_tanker",
            "",
            true,
            1)]
        StorageTankCylinder = 280,
        [Structure("Work Station, Monitors",
            "_mdrn_pl_df_wseg",
            "",
            true,
            1)]
        WorkStationMonitors = 281,
        [Structure("Chest, Lengthwise",
            "_mdrn_pl_conta27",
            "",
            true,
            1)]
        ChestLengthwise = 282,
        [Structure("Console, Central Control",
            "swlor_0054",
            "",
            true,
            1)]
        ConsoleCentralControl = 283,
        [Structure("Console, Circular Command",
            "swlor_0049",
            "",
            true,
            1)]
        ConsoleCircularCommand = 284,
        [Structure("Storage, Cargo Container",
            "_mdrn_pl_conta39",
            "",
            true,
            1)]
        StorageCargoContainer = 285,
        [Structure("Console, Corner, Large",
            "swlor_0106",
            "",
            true,
            1)]
        ConsoleCornerLarge = 286,
        [Structure("Pile Of Cushions, Square",
            "nwplc_cushpilwh",
            "",
            true,
            1)]
        PileOfCushionsSquare = 287,
        [Structure("Trash Can",
            "_mdrn_pl_trshcn2",
            "",
            true,
            1)]
        TrashCan = 288,
        [Structure("Screen, Closed",
            "zep_screen016",
            "",
            true,
            1)]
        ScreenClosed = 289,
        [Structure("Screen, Open",
            "zep_screen015",
            "",
            true,
            1)]
        ScreenOpen = 290,
        [Structure("Chair, Desk (Black/Dark Grey)",
            "_mdrn_pl_chrmdsk",
            "",
            true,
            1)]
        ChairDeskBlackDarkGrey = 291,
        [Structure("Lamp, Bars (Dark)",
            "_mdrn_pl_lghtpl5",
            "",
            true,
            1)]
        LampBarsDark = 292,
        [Structure("Mat, Medium Tatami",
            "zep_tatami002",
            "",
            true,
            1)]
        MatMediumTatami = 293,
        [Structure("Lantern, Floor Small",
            "nwed_flrlntrn002",
            "",
            true,
            1)]
        LanternFloorSmall = 294,
        [Structure("Table, Coffee, Wood /w Glass",
            "_mdrn_pl_table04",
            "",
            true,
            1)]
        TableCoffeeWoodwGlass = 295,
        [Structure("Bed, Art Deco",
            "_mdrn_pl_beddbsf",
            "",
            true,
            1)]
        BedArtDeco = 296,
        [Structure("Wardrobe, Grey, Low",
            "_mdrn_pl_armoir2",
            "",
            true,
            1)]
        WardrobeGreyLow = 297,
        [Structure("Fountain, Dark Grey",
            "_mdrn_pl_fountol",
            "",
            true,
            1)]
        FountainDarkGrey = 298,
        [Structure("Canopy, Leather",
            "swd_canopy04",
            "",
            true,
            1)]
        CanopyLeather = 299,
        [Structure("Console, Floor Mounted, Dark",
            "_mdrn_pl_conso34",
            "",
            true,
            1)]
        ConsoleFloorMountedDark = 300,
        [Structure("Chair, Pilot",
            "_mdrn_pl_chrplt",
            "",
            true,
            1)]
        ChairPilot = 301,
        [Structure("Carpet, Twisted Pattern",
            "swp_rug003",
            "",
            true,
            1)]
        CarpetTwistedPattern = 302,
        [Structure("Console, Tall w/ Screen (Orange)",
            "_mdrn_pl_conso14",
            "",
            true,
            1)]
        ConsoleTallwScreenOrange = 303,
        [Structure("Holo Projector, Standing",
            "swd_rholo01",
            "",
            true,
            1)]
        HoloProjectorStanding = 304,
        [Structure("Metal Wall, Single, Indent",
            "_mdrn_pl_qwall10",
            "",
            true,
            1)]
        MetalWallSingleIndent = 305,
        [Structure("Metal Wall, Wide, Indent",
            "_mdrn_pl_hwall25",
            "",
            true,
            1)]
        MetalWallWideIndent = 306,
        [Structure("Metal Wall, Door, Indent",
            "_mdrn_pl_dwall10",
            "",
            true,
            1)]
        MetalWallDoorIndent = 307,
        [Structure("Holo Display 1",
            "_mdrn_pl_holod02",
            "",
            true,
            1)]
        HoloDisplay1 = 308,
        [Structure("Metal Wall, Single, Grating",
            "_mdrn_pl_qwall07",
            "",
            true,
            1)]
        MetalWallSingleGrating = 309,
        [Structure("Metal Wall, Wide, Grating",
            "_mdrn_pl_hwall22",
            "",
            true,
            1)]
        MetalWallWideGrating = 310,
        [Structure("Metal Wall, Door, Grating",
            "_mdrn_pl_dwall07",
            "",
            true,
            1)]
        MetalWallDoorGrating = 311,
        [Structure("Holo Projector, Small Tree",
            "swd_holot03",
            "",
            true,
            1)]
        HoloProjectorSmallTree = 312,
        [Structure("Medical, Monitoring Unit",
            "_mdrn_pl_measure",
            "",
            true,
            1)]
        MedicalMonitoringUnit = 313,
        [Structure("Dance Floor",
            "swd_dncflo01",
            "",
            true,
            1)]
        DanceFloor = 314,
        [Structure("Holo Projector, Hover",
            "swd3_hovrh01",
            "",
            true,
            1)]
        HoloProjectorHover = 315,
        [Structure("Storage, Warehouse, Industry",
            "swd2_scaf003",
            "",
            true,
            1)]
        StorageWarehouseIndustry = 316,
        [Structure("Banner, Jedi Order",
            "swlor_0119",
            "",
            true,
            1)]
        BannerJediOrder = 317,
        [Structure("Banner, Empire",
            "swlor_0111",
            "",
            true,
            1)]
        BannerEmpire = 318,
        [Structure("Banner, Mandalorian",
            "swlor_0115",
            "",
            true,
            1)]
        BannerMandalorian = 319,
        [Structure("Banner, Republic",
            "swlor_0112",
            "",
            true,
            1)]
        BannerRepublic = 320,
        [Structure("Banner, Cartel",
            "swlor_0113",
            "",
            true,
            1)]
        BannerCartel = 321,
        [Structure("Metal Wall, Single, Hazard",
            "_mdrn_pl_qwall01",
            "",
            true,
            1)]
        MetalWallSingleHazard = 322,
        [Structure("Metal Wall, Wide, Hazard",
            "_mdrn_pl_hwall16",
            "",
            true,
            1)]
        MetalWallWideHazard = 323,
        [Structure("Metal Wall, Door, Hazard",
            "_mdrn_pl_dwall01",
            "",
            true,
            1)]
        MetalWallDoorHazard = 324,
        [Structure("Metal Wall, Single, Panels",
            "_mdrn_pl_qwall04",
            "",
            true,
            1)]
        MetalWallSinglePanels = 325,
        [Structure("Swoop Bike, Black",
            "_mdrn_pl_hbike4",
            "",
            true,
            1)]
        SwoopBikeBlack = 326,
        [Structure("Swoop Bike, Grey",
            "_mdrn_pl_hbike1",
            "",
            true,
            1)]
        SwoopBikeGrey = 327,
        [Structure("Swoop Bike, Red",
            "_mdrn_pl_hbike3",
            "",
            true,
            1)]
        SwoopBikeRed = 328,
        [Structure("Swoop Bike, Yellow",
            "_mdrn_pl_hbike2",
            "",
            true,
            1)]
        SwoopBikeYellow = 329,
        [Structure("Speaker, Standing",
            "swlor_0011",
            "",
            true,
            1)]
        SpeakerStanding = 330,

        [Structure("Ashmark Field Cot",
            "swc_cot_ash",
            "",
            true,
            1)]
        AshmarkFieldCot = 331,

        [Structure("Ashmark Signal Lamp",
            "zep_firebowl001",
            "",
            true,
            1)]
        AshmarkSignalLamp = 332,

        [Structure("Ashmark Trophy Stand",
            "swc_tro_ash",
            "",
            true,
            1)]
        AshmarkTrophyStand = 333,

        [Structure("Ashmark Low Table",
            "_mdrn_pl_tableir",
            "",
            true,
            1)]
        AshmarkLowTable = 334,

        [Structure("Ashmark Wall Banner",
            "swp_banner0001",
            "",
            true,
            1)]
        AshmarkWallBanner = 335,

        [Structure("Ashmark Supply Locker",
            "_mdrn_pl_mtllkrs",
            "",
            true,
            1)]
        AshmarkSupplyLocker = 336,

        [Structure("Ashmark Floor Mat",
            "zep_skin001",
            "",
            true,
            1)]
        AshmarkFloorMat = 337,

        [Structure("Ashmark Data Console",
            "_mdrn_pl_conso05",
            "",
            true,
            1)]
        AshmarkDataConsole = 338,

        [Structure("Ashmark Altar",
            "_mdrn_pl_altarsi",
            "",
            true,
            1)]
        AshmarkAltar = 339,

        [Structure("Ashmark Work Stool",
            "swc_stl_ash",
            "",
            true,
            1)]
        AshmarkWorkStool = 340,

        [Structure("Guttermark Field Cot",
            "swc_cot_gutter",
            "",
            true,
            1)]
        GuttermarkFieldCot = 341,

        [Structure("Guttermark Signal Lamp",
            "_mdrn_pl_lampd05",
            "",
            true,
            1)]
        GuttermarkSignalLamp = 342,

        [Structure("Guttermark Trophy Stand",
            "swc_tro_gutter",
            "",
            true,
            1)]
        GuttermarkTrophyStand = 343,

        [Structure("Guttermark Low Table",
            "_mdrn_pl_rustedt",
            "",
            true,
            1)]
        GuttermarkLowTable = 344,

        [Structure("Guttermark Wall Banner",
            "swc_ban_gutter",
            "",
            true,
            1)]
        GuttermarkWallBanner = 345,

        [Structure("Guttermark Supply Locker",
            "_mdrn_pl_mtllkgg",
            "",
            true,
            1)]
        GuttermarkSupplyLocker = 346,

        [Structure("Guttermark Floor Mat",
            "zep_throwrug050",
            "",
            true,
            1)]
        GuttermarkFloorMat = 347,

        [Structure("Guttermark Data Console",
            "swlor_1119",
            "",
            true,
            1)]
        GuttermarkDataConsole = 348,

        [Structure("Guttermark Easel",
            "x3_plc_stand001",
            "",
            true,
            1)]
        GuttermarkEasel = 349,

        [Structure("Guttermark Work Stool",
            "_mdrn_pl_stool05",
            "",
            true,
            1)]
        GuttermarkWorkStool = 350,

        [Structure("Trailmark Field Cot",
            "_mdrn_pl_cotsimp",
            "",
            true,
            1)]
        TrailmarkFieldCot = 351,

        [Structure("Trailmark Signal Lamp",
            "_mdrn_pl_lamp5",
            "",
            true,
            1)]
        TrailmarkSignalLamp = 352,

        [Structure("Trailmark Trophy Stand",
            "swc_tro_trail",
            "",
            true,
            1)]
        TrailmarkTrophyStand = 353,

        [Structure("Trailmark Low Table",
            "zep_table012",
            "",
            true,
            1)]
        TrailmarkLowTable = 354,

        [Structure("Trailmark Wall Banner",
            "swc_ban_trail",
            "",
            true,
            1)]
        TrailmarkWallBanner = 355,

        [Structure("Trailmark Supply Locker",
            "_mdrn_pl_canten2",
            "",
            true,
            1)]
        TrailmarkSupplyLocker = 356,

        [Structure("Trailmark Floor Mat",
            "wookiee_rug",
            "",
            true,
            1)]
        TrailmarkFloorMat = 357,

        [Structure("Trailmark Data Console",
            "swlor_1080",
            "",
            true,
            1)]
        TrailmarkDataConsole = 358,

        [Structure("Trailmark Waystone",
            "x0_runestone1",
            "",
            true,
            1)]
        TrailmarkWaystone = 359,

        [Structure("Trailmark Work Stool",
            "_mdrn_pl_stool04",
            "",
            true,
            1)]
        TrailmarkWorkStool = 360,

        [Structure("Stonewake Field Cot",
            "swc_cot_stone",
            "",
            true,
            1)]
        StonewakeFieldCot = 361,

        [Structure("Stonewake Signal Lamp",
            "zep_ostnlntrn002",
            "",
            true,
            1)]
        StonewakeSignalLamp = 362,

        [Structure("Stonewake Trophy Stand",
            "swc_tro_stone",
            "",
            true,
            1)]
        StonewakeTrophyStand = 363,

        [Structure("Stonewake Low Table",
            "swp_table03",
            "",
            true,
            1)]
        StonewakeLowTable = 364,

        [Structure("Stonewake Wall Banner",
            "_mdrn_pl_bannr02",
            "",
            true,
            1)]
        StonewakeWallBanner = 365,

        [Structure("Stonewake Supply Locker",
            "_mdrn_pl_chest04",
            "",
            true,
            1)]
        StonewakeSupplyLocker = 366,

        [Structure("Stonewake Floor Mat",
            "zep_throwrug058",
            "",
            true,
            1)]
        StonewakeFloorMat = 367,

        [Structure("Stonewake Data Console",
            "_mdrn_pl_conso32",
            "",
            true,
            1)]
        StonewakeDataConsole = 368,

        [Structure("Stonewake Stone Altar",
            "zep_stones018",
            "",
            true,
            1)]
        StonewakeStoneAltar = 369,

        [Structure("Stonewake Work Stool",
            "_mdrn_pl_stool",
            "",
            true,
            1)]
        StonewakeWorkStool = 370,

        [Structure("Aegisline Field Cot",
            "_mdrn_pl_bedsgrd",
            "",
            true,
            1)]
        AegislineFieldCot = 371,

        [Structure("Aegisline Signal Lamp",
            "aswtor_130",
            "",
            true,
            1)]
        AegislineSignalLamp = 372,

        [Structure("Aegisline Trophy Stand",
            "swc_tro_aegis",
            "",
            true,
            1)]
        AegislineTrophyStand = 373,

        [Structure("Aegisline Low Table",
            "swp_table04",
            "",
            true,
            1)]
        AegislineLowTable = 374,

        [Structure("Aegisline Wall Banner",
            "swd3_ban005",
            "",
            true,
            1)]
        AegislineWallBanner = 375,

        [Structure("Aegisline Supply Locker",
            "_mdrn_pl_conta48",
            "",
            true,
            1)]
        AegislineSupplyLocker = 376,

        [Structure("Aegisline Floor Mat",
            "_mdrn_pl_matpurp",
            "",
            true,
            1)]
        AegislineFloorMat = 377,

        [Structure("Aegisline Data Console",
            "swd2_comp005",
            "",
            true,
            1)]
        AegislineDataConsole = 378,

        [Structure("Aegisline Sword Pedestal",
            "swc_ped_aegis",
            "",
            true,
            1)]
        AegislineSwordPedestal = 379,

        [Structure("Aegisline Work Stool",
            "swc_seat_steel",
            "",
            true,
            1)]
        AegislineWorkStool = 380,

        [Structure("Veilcarved Field Cot",
            "swc_cot_veil",
            "",
            true,
            1)]
        VeilcarvedFieldCot = 381,

        [Structure("Veilcarved Signal Lamp",
            "_mdrn_pl_brazier",
            "",
            true,
            1)]
        VeilcarvedSignalLamp = 382,

        [Structure("Veilcarved Trophy Stand",
            "swc_tro_veil",
            "",
            true,
            1)]
        VeilcarvedTrophyStand = 383,

        [Structure("Veilcarved Low Table",
            "_mdrn_pl_table07",
            "",
            true,
            1)]
        VeilcarvedLowTable = 384,

        [Structure("Veilcarved Wall Banner",
            "_mdrn_pl_bannr05",
            "",
            true,
            1)]
        VeilcarvedWallBanner = 385,

        [Structure("Veilcarved Supply Locker",
            "daf_sw309",
            "",
            true,
            1)]
        VeilcarvedSupplyLocker = 386,

        [Structure("Veilcarved Floor Mat",
            "zep_throwrug052",
            "",
            true,
            1)]
        VeilcarvedFloorMat = 387,

        [Structure("Veilcarved Data Console",
            "daf_sw315",
            "",
            true,
            1)]
        VeilcarvedDataConsole = 388,

        [Structure("Veilcarved Rune Pillar",
            "_mdrn_pl_pillr13",
            "",
            true,
            1)]
        VeilcarvedRunePillar = 389,

        [Structure("Veilcarved Work Stool",
            "swc_stl_veil",
            "",
            true,
            1)]
        VeilcarvedWorkStool = 390,

        [Structure("Surgewake Field Cot",
            "_mdrn_pl_bedmtl2",
            "",
            true,
            1)]
        SurgewakeFieldCot = 391,

        [Structure("Surgewake Signal Lamp",
            "_mdrn_pl_lamp3",
            "",
            true,
            1)]
        SurgewakeSignalLamp = 392,

        [Structure("Surgewake Trophy Stand",
            "swc_tro_surge",
            "",
            true,
            1)]
        SurgewakeTrophyStand = 393,

        [Structure("Surgewake Low Table",
            "swp_table01",
            "",
            true,
            1)]
        SurgewakeLowTable = 394,

        [Structure("Surgewake Wall Banner",
            "swc_ban_surge",
            "",
            true,
            1)]
        SurgewakeWallBanner = 395,

        [Structure("Surgewake Supply Locker",
            "aswtor_134",
            "",
            true,
            1)]
        SurgewakeSupplyLocker = 396,

        [Structure("Surgewake Floor Mat",
            "_mdrn_pl_carpt03",
            "",
            true,
            1)]
        SurgewakeFloorMat = 397,

        [Structure("Surgewake Data Console",
            "swd3_upter01",
            "",
            true,
            1)]
        SurgewakeDataConsole = 398,

        [Structure("Surgewake Holo Projector",
            "_mdrn_pl_holocon",
            "",
            true,
            1)]
        SurgewakeHoloProjector = 399,

        [Structure("Surgewake Work Stool",
            "swc_stl_surge",
            "",
            true,
            1)]
        SurgewakeWorkStool = 400,

        [Structure("Resonant Field Cot",
            "swc_cot_reson",
            "",
            true,
            1)]
        ResonantFieldCot = 401,

        [Structure("Resonant Signal Lamp",
            "zep_ostnlntrn004",
            "",
            true,
            1)]
        ResonantSignalLamp = 402,

        [Structure("Resonant Trophy Stand",
            "swc_tro_reson",
            "",
            true,
            1)]
        ResonantTrophyStand = 403,

        [Structure("Resonant Low Table",
            "_mdrn_pl_df_tblr",
            "",
            true,
            1)]
        ResonantLowTable = 404,

        [Structure("Resonant Wall Banner",
            "swc_ban_reson",
            "",
            true,
            1)]
        ResonantWallBanner = 405,

        [Structure("Resonant Supply Locker",
            "_mdrn_pl_rustdcr",
            "",
            true,
            1)]
        ResonantSupplyLocker = 406,

        [Structure("Resonant Floor Mat",
            "zep_throwrug054",
            "",
            true,
            1)]
        ResonantFloorMat = 407,

        [Structure("Resonant Data Console",
            "_mdrn_pl_conso33",
            "",
            true,
            1)]
        ResonantDataConsole = 408,

        [Structure("Resonant Crystal",
            "x2_plc_crystal",
            "",
            true,
            1)]
        ResonantCrystal = 409,

        [Structure("Resonant Work Stool",
            "zep_logsit001",
            "",
            true,
            1)]
        ResonantWorkStool = 410,

        [Structure("Fieldline Field Cot",
            "_mdrn_pl_bedenli",
            "",
            true,
            1)]
        FieldlineFieldCot = 411,

        [Structure("Fieldline Signal Lamp",
            "_mdrn_pl_lghtpl1",
            "",
            true,
            1)]
        FieldlineSignalLamp = 412,

        [Structure("Fieldline Trophy Stand",
            "swc_tro_field",
            "",
            true,
            1)]
        FieldlineTrophyStand = 413,

        [Structure("Fieldline Low Table",
            "_mdrn_pl_table08",
            "",
            true,
            1)]
        FieldlineLowTable = 414,

        [Structure("Fieldline Wall Banner",
            "swd3_ban006",
            "",
            true,
            1)]
        FieldlineWallBanner = 415,

        [Structure("Fieldline Supply Locker",
            "_mdrn_pl_mtllkcr",
            "",
            true,
            1)]
        FieldlineSupplyLocker = 416,

        [Structure("Fieldline Floor Mat",
            "_mdrn_pl_carpt04",
            "",
            true,
            1)]
        FieldlineFloorMat = 417,

        [Structure("Fieldline Data Console",
            "_mdrn_pl_conso07",
            "",
            true,
            1)]
        FieldlineDataConsole = 418,

        [Structure("Fieldline Display Case",
            "zep_displaycase1",
            "",
            true,
            1)]
        FieldlineDisplayCase = 419,

        [Structure("Fieldline Work Stool",
            "swc_stl_field",
            "",
            true,
            1)]
        FieldlineWorkStool = 420,

        [Structure("Commandline Field Cot",
            "swlor_0622",
            "",
            true,
            1)]
        CommandlineFieldCot = 421,

        [Structure("Commandline Signal Lamp",
            "_mdrn_pl_lampd08",
            "",
            true,
            1)]
        CommandlineSignalLamp = 422,

        [Structure("Commandline Trophy Stand",
            "swc_tro_cmd",
            "",
            true,
            1)]
        CommandlineTrophyStand = 423,

        [Structure("Commandline Low Table",
            "_mdrn_pl_table05",
            "",
            true,
            1)]
        CommandlineLowTable = 424,

        [Structure("Commandline Wall Banner",
            "swlor_1846",
            "",
            true,
            1)]
        CommandlineWallBanner = 425,

        [Structure("Commandline Supply Locker",
            "swd_ward01",
            "",
            true,
            1)]
        CommandlineSupplyLocker = 426,

        [Structure("Commandline Floor Mat",
            "_mdrn_pl_carpt02",
            "",
            true,
            1)]
        CommandlineFloorMat = 427,

        [Structure("Commandline Data Console",
            "swd3_comp009",
            "",
            true,
            1)]
        CommandlineDataConsole = 428,

        [Structure("Commandline Bust",
            "swc_bust_cmd",
            "",
            true,
            1)]
        CommandlineBust = 429,

        [Structure("Commandline Work Stool",
            "swc_stl_cmd",
            "",
            true,
            1)]
        CommandlineWorkStool = 430,

        [Structure("Rustline Data Terminal",
            "slc_rustterm",
            "A weathered analysis terminal recovered from a sealed slicing schematic.",
            true,
            1)]
        RustlineDataTerminal = 431,

        [Structure("Cipherfile Cabinet",
            "slc_ciphcab",
            "A secure cabinet with integrated cipher controls.",
            true,
            1)]
        CipherfileCabinet = 432,

        [Structure("Listening Post Monitor",
            "slc_listmon",
            "A compact signals monitor built from a recovered clandestine design.",
            true,
            1)]
        ListeningPostMonitor = 433,

        [Structure("Ghost-Channel Console",
            "slc_ghostcon",
            "A cold-channel communications console with shielded storage.",
            true,
            1)]
        GhostChannelConsole = 434,

        [Structure("Blacksite Analysis Station",
            "slc_blackstat",
            "A rare analysis station intended for compartmentalized field work.",
            true,
            1)]
        BlacksiteAnalysisStation = 435,

        [Structure("Espionage Workbench",
            "espionage_bench",
            "",
            true,
            0)]
        EspionageWorkbench = 436,

        [Structure("Bed - Bunk, Cream", "daf_sw378", "", true, 1)]
        SwtorBedBunkCream = 437,

        [Structure("Bed - Bunk, Open", "daf_sw367", "", true, 1)]
        SwtorBedBunkOpen = 438,

        [Structure("Bed - Bunk, Ship (Dark)", "daf_sw374", "", true, 1)]
        SwtorBedBunkShipDark = 439,

        [Structure("Bed - Bunk, Ship (Light)", "daf_sw375", "", true, 1)]
        SwtorBedBunkShipLight = 440,

        [Structure("Bed - Cream (Single)", "daf_sw379", "", true, 1)]
        SwtorBedCreamSingle = 441,

        [Structure("Bed - Double (Blue)", "daf_sw371", "", true, 1)]
        SwtorBedDoubleBlue = 442,

        [Structure("Bed - Double (Bronze)", "daf_sw370", "", true, 1)]
        SwtorBedDoubleBronze = 443,

        [Structure("Bed - Double (Orange)", "daf_sw372", "", true, 1)]
        SwtorBedDoubleOrange = 444,

        [Structure("Bed - Double (Purple)", "daf_sw369", "", true, 1)]
        SwtorBedDoublePurple = 445,

        [Structure("Bed - Grey Stripe (Double)", "daf_sw377", "", true, 1)]
        SwtorBedGreyStripeDouble = 446,

        [Structure("Bed - Jedi, Stone", "daf_sw366", "", true, 1)]
        SwtorBedJediStone = 447,

        [Structure("Bed - Mandalorian", "daf_sw368", "", true, 1)]
        SwtorBedMandalorian = 448,

        [Structure("Bed - Metal Post", "daf_sw380", "", true, 1)]
        SwtorBedMetalPost = 449,

        [Structure("Bed - Wood", "daf_sw373", "", true, 1)]
        SwtorBedWood = 450,

        [Structure("Ammo Box", "swd_ammobox1", "", true, 1)]
        SwtorAmmoBox = 451,

        [Structure("Animal Pen", "swd_animpe01", "", true, 1)]
        SwtorAnimalPen = 452,

        [Structure("Armory 001", "swd3_armry01", "", true, 1)]
        SwtorArmory001 = 453,

        [Structure("Armory 002", "swd3_armry002", "", true, 1)]
        SwtorArmory002 = 454,

        [Structure("Auction House 001 (On) (Decorative)", "swd_auchos01", "A decorative fixture for a player property.", true, 1)]
        SwtorAuctionHouse001On = 455,

        [Structure("Auction House 002 (Off) (Decorative)", "swd_auchos02", "A decorative fixture for a player property.", true, 1)]
        SwtorAuctionHouse002Off = 456,

        [Structure("Awning - Entrance (Blue)", "aswtor_172", "", true, 1)]
        SwtorAwningEntranceBlue = 457,

        [Structure("Awning - Wall Hanging (Blue)", "aswtor_173", "", true, 1)]
        SwtorAwningWallHangingBlue = 458,

        [Structure("Awning - Wall Hanging (Orange)", "aswtor_174", "", true, 1)]
        SwtorAwningWallHangingOrange = 459,

        [Structure("Backpack 001", "swd_backpk01", "", true, 1)]
        SwtorBackpack001 = 460,

        [Structure("Banner 001 (Czerka)", "swd3_czs003", "", true, 1)]
        SwtorBanner001Czerka = 461,

        [Structure("Banner 002 (Czerka)", "swd3_czs004", "", true, 1)]
        SwtorBanner002Czerka = 462,

        [Structure("Banner 003", "swd3_ban003", "", true, 1)]
        SwtorBanner003 = 463,

        [Structure("Banner 004", "swd3_ban004", "", true, 1)]
        SwtorBanner004 = 464,

        [Structure("Bar 001", "swd_bar001", "", true, 1)]
        SwtorBar001 = 465,

        [Structure("Bar 002", "swd_bar002", "", true, 1)]
        SwtorBar002 = 466,

        [Structure("Bar 003", "swd_bar003", "", true, 1)]
        SwtorBar003 = 467,

        [Structure("Bar 004", "swd_bar004", "", true, 1)]
        SwtorBar004 = 468,

        [Structure("Bar 005", "swd_bar005", "", true, 1)]
        SwtorBar005 = 469,

        [Structure("Bar 0052", "swd_bar0052", "", true, 1)]
        SwtorBar0052 = 470,

        [Structure("Bar 006", "swd_bar006", "", true, 1)]
        SwtorBar006 = 471,

        [Structure("Bar 007", "swd_bar0007", "", true, 1)]
        SwtorBar007 = 472,

        [Structure("Bar 008", "swd_bar008", "", true, 1)]
        SwtorBar008 = 473,

        [Structure("Bar 009", "swd_bar009", "", true, 1)]
        SwtorBar009 = 474,

        [Structure("Bar 010", "swd3_bar010", "", true, 1)]
        SwtorBar010 = 475,

        [Structure("Bar 011", "swd3_bar011", "", true, 1)]
        SwtorBar011 = 476,

        [Structure("Barrel 001", "swd_barrel01", "", true, 1)]
        SwtorBarrel001 = 477,

        [Structure("Barrel 002 (Stack)", "swd_barrel02", "", true, 1)]
        SwtorBarrel002Stack = 478,

        [Structure("Barrel 003", "swd2_barl003", "", true, 1)]
        SwtorBarrel003 = 479,

        [Structure("Barricade 001", "swd3_barri01", "", true, 1)]
        SwtorBarricade001 = 480,

        [Structure("Barricade 002", "swd3_barri002", "", true, 1)]
        SwtorBarricade002 = 481,

        [Structure("Barricade 003", "swd3_barri003", "", true, 1)]
        SwtorBarricade003 = 482,

        [Structure("Bench 001", "swd_bench01", "", true, 1)]
        SwtorBench001 = 483,

        [Structure("Bench 002", "swd3_bench02", "", true, 1)]
        SwtorBench002 = 484,

        [Structure("Bench 003", "swd3_bench003", "", true, 1)]
        SwtorBench003 = 485,

        [Structure("Biochemical Station (Decorative)", "aswtor_073", "A decorative fixture for a player property.", true, 1)]
        SwtorBiochemicalStation = 486,

        [Structure("Biomatter Storage", "swd_biosto01", "", true, 1)]
        SwtorBiomatterStorage = 487,

        [Structure("Bottle 001", "swd_bottle01", "", true, 1)]
        SwtorBottle001 = 488,

        [Structure("Bottle 002", "swd_bottle02", "", true, 1)]
        SwtorBottle002 = 489,

        [Structure("Bounty Board 001 (Decorative)", "swd3_bhbrd01", "A decorative fixture for a player property.", true, 1)]
        SwtorBountyBoard001 = 490,

        [Structure("Burlap Sack 001", "swd_bursac01", "", true, 1)]
        SwtorBurlapSack001 = 491,

        [Structure("Burlap Sack 002", "swd_bursac02", "", true, 1)]
        SwtorBurlapSack002 = 492,

        [Structure("Bus Stop", "swd_bussto01", "", true, 1)]
        SwtorBusStop = 493,

        [Structure("Canopy - Hanging", "aswtor_046", "", true, 1)]
        SwtorCanopyHanging = 494,

        [Structure("Canopy 001", "swd_canopy01", "", true, 1)]
        SwtorCanopy001 = 495,

        [Structure("Canopy 002", "swd_canopy02", "", true, 1)]
        SwtorCanopy002 = 496,

        [Structure("Canopy 003", "swd_canopy03", "", true, 1)]
        SwtorCanopy003 = 497,

        [Structure("Canopy 005", "swd_canopy05", "", true, 1)]
        SwtorCanopy005 = 498,

        [Structure("Cargo Container (Yellow)", "aswtor_177", "", true, 1)]
        SwtorCargoContainerYellow = 499,

        [Structure("Cargo Pallet", "aswtor_183", "", true, 1)]
        SwtorCargoPallet = 500,

        [Structure("Cargo Shelf (Empty)", "aswtor_107", "", true, 1)]
        SwtorCargoShelfEmpty = 501,

        [Structure("Cargo Shelf 01", "aswtor_106", "", true, 1)]
        SwtorCargoShelf01 = 502,

        [Structure("Cargo Stack 1", "aswtor_045", "", true, 1)]
        SwtorCargoStack1 = 503,

        [Structure("Cargo Stack 2", "aswtor_101", "", true, 1)]
        SwtorCargoStack2 = 504,

        [Structure("Chair - Cockpit, Black", "aswtor_141", "", true, 1)]
        SwtorChairCockpitBlack = 505,

        [Structure("Chair - Leather, Round", "aswtor_139", "", true, 1)]
        SwtorChairLeatherRound = 506,

        [Structure("Chair - Leather, Simple", "aswtor_140", "", true, 1)]
        SwtorChairLeatherSimple = 507,

        [Structure("Chair - Plasteel, Republic", "aswtor_137", "", true, 1)]
        SwtorChairPlasteelRepublic = 508,

        [Structure("Chair - Plasteel, Tan", "aswtor_138", "", true, 1)]
        SwtorChairPlasteelTan = 509,

        [Structure("Chair 001", "swd_chair001", "", true, 1)]
        SwtorChair001 = 510,

        [Structure("Chair 002", "swd_chair002", "", true, 1)]
        SwtorChair002 = 511,

        [Structure("Clothesline 01", "aswtor_123", "", true, 1)]
        SwtorClothesline01 = 512,

        [Structure("Clothesline 02", "aswtor_124", "", true, 1)]
        SwtorClothesline02 = 513,

        [Structure("Compactor 001", "swd3_compa01", "", true, 1)]
        SwtorCompactor001 = 514,

        [Structure("Computer 1", "swd_compu001", "", true, 1)]
        SwtorComputer1 = 515,

        [Structure("Computer 2", "swd_compu002", "", true, 1)]
        SwtorComputer2 = 516,

        [Structure("Computer 3", "swd_compu003", "", true, 1)]
        SwtorComputer3 = 517,

        [Structure("Computer 4", "swd2_comp004", "", true, 1)]
        SwtorComputer4 = 518,

        [Structure("Computer 6", "swd2_comp006", "", true, 1)]
        SwtorComputer6 = 519,

        [Structure("Computer 7", "swd3_comp7", "", true, 1)]
        SwtorComputer7 = 520,

        [Structure("Computer 8", "swd3_comp008", "", true, 1)]
        SwtorComputer8 = 521,

        [Structure("Construction Dumpster 001", "swd3_consd01", "", true, 1)]
        SwtorConstructionDumpster001 = 522,

        [Structure("Construction Dumpster 002", "swd3_consd002", "", true, 1)]
        SwtorConstructionDumpster002 = 523,

        [Structure("Construction Dumpster 003", "swd3_consd003", "", true, 1)]
        SwtorConstructionDumpster003 = 524,

        [Structure("Construction Dumpster 004", "swd3_consd004", "", true, 1)]
        SwtorConstructionDumpster004 = 525,

        [Structure("Construction Dumpster 005", "swd3_consd005", "", true, 1)]
        SwtorConstructionDumpster005 = 526,

        [Structure("Construction Dumpster 006", "swd3_consd006", "", true, 1)]
        SwtorConstructionDumpster006 = 527,

        [Structure("Construction Dumpster 007", "swd3_consd007", "", true, 1)]
        SwtorConstructionDumpster007 = 528,

        [Structure("Construction Dumpster 008", "swd3_consd008", "", true, 1)]
        SwtorConstructionDumpster008 = 529,

        [Structure("Container - Panel Access", "aswtor_181", "", true, 1)]
        SwtorContainerPanelAccess = 530,

        [Structure("Container 1", "swd_conta001", "", true, 1)]
        SwtorContainer1 = 531,

        [Structure("Container 2 (Stack)", "swd_conta002", "", true, 1)]
        SwtorContainer2Stack = 532,

        [Structure("Container 3", "swd_conta003", "", true, 1)]
        SwtorContainer3 = 533,

        [Structure("Container 4", "swd_conta004", "", true, 1)]
        SwtorContainer4 = 534,

        [Structure("Container 5 (Stack)", "swd_conta005", "", true, 1)]
        SwtorContainer5Stack = 535,

        [Structure("Container 6", "swd_conta006", "", true, 1)]
        SwtorContainer6 = 536,

        [Structure("Container 7 (Stack)", "swd_conta007", "", true, 1)]
        SwtorContainer7Stack = 537,

        [Structure("Container 8", "swd2_cont008", "", true, 1)]
        SwtorContainer8 = 538,

        [Structure("Container 9", "swd3_conta010", "", true, 1)]
        SwtorContainer9 = 539,

        [Structure("Control Tower 001", "swd3_contt01", "", true, 1)]
        SwtorControlTower001 = 540,

        [Structure("Conveyor", "swd_convay01", "", true, 1)]
        SwtorConveyor = 541,

        [Structure("Conveyor Screen (Off)", "swd_convay02", "", true, 1)]
        SwtorConveyorScreenOff = 542,

        [Structure("Conveyor Screen (On)", "swd_convay03", "", true, 1)]
        SwtorConveyorScreenOn = 543,

        [Structure("Couch 01", "aswtor_125", "", true, 1)]
        SwtorCouch01 = 544,

        [Structure("Crane 001", "swd2_crane001", "", true, 1)]
        SwtorCrane001 = 545,

        [Structure("Crate 03", "daf_sw320", "", true, 1)]
        SwtorCrate03 = 546,

        [Structure("Crate 04", "daf_sw321", "", true, 1)]
        SwtorCrate04 = 547,

        [Structure("Crate Pile, Rep 1", "aswtor_102", "", true, 1)]
        SwtorCratePileRep1 = 548,

        [Structure("Crate Pile, Rep 2", "aswtor_103", "", true, 1)]
        SwtorCratePileRep2 = 549,

        [Structure("Crate Pile, Rep 3", "aswtor_104", "", true, 1)]
        SwtorCratePileRep3 = 550,

        [Structure("Crate Pile, Rep 4", "aswtor_105", "", true, 1)]
        SwtorCratePileRep4 = 551,

        [Structure("Crystal 001", "swd_cryst01", "", true, 1)]
        SwtorCrystal001 = 552,

        [Structure("Crystal 002", "swd_cryst02", "", true, 1)]
        SwtorCrystal002 = 553,

        [Structure("Cup", "swd_cup01", "", true, 1)]
        SwtorCup = 554,

        [Structure("Czerka Sign 01", "swd3_czs001", "", true, 1)]
        SwtorCzerkaSign01 = 555,

        [Structure("Czerka Sign 02", "swd3_czs002", "", true, 1)]
        SwtorCzerkaSign02 = 556,

        [Structure("Datapad", "swd_datap001", "", true, 1)]
        SwtorDatapad = 557,

        [Structure("Debris Pile 001", "swd2_debr001", "", true, 1)]
        SwtorDebrisPile001 = 558,

        [Structure("Debris Pile 002", "swd3_dbpil02", "", true, 1)]
        SwtorDebrisPile002 = 559,

        [Structure("Decal: Paper Floor", "swd_decpap01", "", true, 1)]
        SwtorDecalPaperFloor = 560,

        [Structure("Door - Covered Entrance", "aswtor_175", "", true, 1)]
        SwtorDoorCoveredEntrance = 561,

        [Structure("Door - Imp 01", "daf_sw359", "", true, 1)]
        SwtorDoorImp01 = 562,

        [Structure("Door - Sealed Gate", "aswtor_176", "", true, 1)]
        SwtorDoorSealedGate = 563,

        [Structure("Door - Tatooine, Metal", "daf_sw304", "", true, 1)]
        SwtorDoorTatooineMetal = 564,

        [Structure("Droid Assembly Terminal (Decorative)", "aswtor_075", "A decorative fixture for a player property.", true, 1)]
        SwtorDroidAssemblyTerminal = 565,

        [Structure("Dumpster (Empty)", "swd_dump001", "", true, 1)]
        SwtorDumpsterEmpty = 566,

        [Structure("Dumpster 001", "swd_dump002", "", true, 1)]
        SwtorDumpster001 = 567,

        [Structure("Dumpster 002", "swd_dump003", "", true, 1)]
        SwtorDumpster002 = 568,

        [Structure("Elevator (Decorative)", "swd_eleva01", "A decorative fixture for a player property.", true, 1)]
        SwtorElevator = 569,

        [Structure("Elevator 002 (Decorative)", "swd2_elev002", "A decorative fixture for a player property.", true, 1)]
        SwtorElevator002 = 570,

        [Structure("Fabrication Terminal (Decorative)", "aswtor_074", "A decorative fixture for a player property.", true, 1)]
        SwtorFabricationTerminal = 571,

        [Structure("Fence", "swd_fence01", "", true, 1)]
        SwtorFence = 572,

        [Structure("Fence (Closed Door)", "swd2_fence003", "", true, 1)]
        SwtorFenceClosedDoor = 573,

        [Structure("Fence (Open Door)", "swd2_fence02", "", true, 1)]
        SwtorFenceOpenDoor = 574,

        [Structure("Fence (Pillar)", "swd2_fence008", "", true, 1)]
        SwtorFencePillar = 575,

        [Structure("Fence (Powered)", "swd2_fence004", "", true, 1)]
        SwtorFencePowered = 576,

        [Structure("Fence (Un-Powered 1)", "swd2_fence005", "", true, 1)]
        SwtorFenceUnPowered1 = 577,

        [Structure("Fence (Un-Powered 2)", "swd2_fence006", "", true, 1)]
        SwtorFenceUnPowered2 = 578,

        [Structure("Fence (Un-Powered 3)", "swd2_fence007", "", true, 1)]
        SwtorFenceUnPowered3 = 579,

        [Structure("Fence (Wide Door)", "swd2_fence009", "", true, 1)]
        SwtorFenceWideDoor = 580,

        [Structure("Fence (Wide Door, Closed)", "swd2_fence010", "", true, 1)]
        SwtorFenceWideDoorClosed = 581,

        [Structure("Fence - Angular, Large (Long)", "aswtor_036", "", true, 1)]
        SwtorFenceAngularLargeLong = 582,

        [Structure("Fence - Angular, Large (Short)", "aswtor_035", "", true, 1)]
        SwtorFenceAngularLargeShort = 583,

        [Structure("Fence - Angular, Small (Long)", "aswtor_034", "", true, 1)]
        SwtorFenceAngularSmallLong = 584,

        [Structure("Fence - Angular, Small (Short)", "aswtor_033", "", true, 1)]
        SwtorFenceAngularSmallShort = 585,

        [Structure("Fence - Geometric, Long", "aswtor_136", "", true, 1)]
        SwtorFenceGeometricLong = 586,

        [Structure("Fence - Geometric, Short", "aswtor_135", "", true, 1)]
        SwtorFenceGeometricShort = 587,

        [Structure("Fence - Lightposts", "aswtor_169", "", true, 1)]
        SwtorFenceLightposts = 588,

        [Structure("Fence - Railing", "aswtor_170", "", true, 1)]
        SwtorFenceRailing = 589,

        [Structure("Fence - Slat Barrier", "aswtor_171", "", true, 1)]
        SwtorFenceSlatBarrier = 590,

        [Structure("Floor (Metal Hex)", "swd_flormh01", "", true, 1)]
        SwtorFloorMetalHex = 591,

        [Structure("Floor (Metal)", "swd_floorm01", "", true, 1)]
        SwtorFloorMetal = 592,

        [Structure("Floor (Road End)", "swd_florre01", "", true, 1)]
        SwtorFloorRoadEnd = 593,

        [Structure("Floor (Road T)", "swd_florrt01", "", true, 1)]
        SwtorFloorRoadT = 594,

        [Structure("Floor (Road Turn)", "swd_florrt02", "", true, 1)]
        SwtorFloorRoadTurn = 595,

        [Structure("Floor (Road X)", "swd2_floorx", "", true, 1)]
        SwtorFloorRoadX = 596,

        [Structure("Floor (Road)", "swd_florrd01", "", true, 1)]
        SwtorFloorRoad = 597,

        [Structure("Floor - Cross Panel, Grey (1x1)", "aswtor_014", "", true, 1)]
        SwtorFloorCrossPanelGrey1x1 = 598,

        [Structure("Floor - Cross Panel, Grey (2x2)", "aswtor_015", "", true, 1)]
        SwtorFloorCrossPanelGrey2x2 = 599,

        [Structure("Floor - Duracrete Slab (1x1)", "aswtor_108", "", true, 1)]
        SwtorFloorDuracreteSlab1x1 = 600,

        [Structure("Floor - Duracrete Slab (2x2)", "aswtor_109", "", true, 1)]
        SwtorFloorDuracreteSlab2x2 = 601,

        [Structure("Floor - Duracrete Tile (1x1)", "aswtor_037", "", true, 1)]
        SwtorFloorDuracreteTile1x1 = 602,

        [Structure("Floor - Duracrete Tile (2x2)", "aswtor_038", "", true, 1)]
        SwtorFloorDuracreteTile2x2 = 603,

        [Structure("Floor - Durasteel Panel, Black (1x1)", "aswtor_088", "", true, 1)]
        SwtorFloorDurasteelPanelBlack1x1 = 604,

        [Structure("Floor - Durasteel Panel, Black (2x2)", "aswtor_089", "", true, 1)]
        SwtorFloorDurasteelPanelBlack2x2 = 605,

        [Structure("Floor - Durasteel Panel, Grey (1x1)", "aswtor_084", "", true, 1)]
        SwtorFloorDurasteelPanelGrey1x1 = 606,

        [Structure("Floor - Durasteel Panel, Grey (2x2)", "aswtor_085", "", true, 1)]
        SwtorFloorDurasteelPanelGrey2x2 = 607,

        [Structure("Floor - Durasteel Panel, Tan (1x1)", "aswtor_086", "", true, 1)]
        SwtorFloorDurasteelPanelTan1x1 = 608,

        [Structure("Floor - Durasteel Panel, Tan (2x2)", "aswtor_087", "", true, 1)]
        SwtorFloorDurasteelPanelTan2x2 = 609,

        [Structure("Floor - Industrial Yellow", "aswtor_100", "", true, 1)]
        SwtorFloorIndustrialYellow = 610,

        [Structure("Floor - Medical Tile (1x1)", "aswtor_069", "", true, 1)]
        SwtorFloorMedicalTile1x1 = 611,

        [Structure("Floor - Medical Tile (2x2)", "aswtor_070", "", true, 1)]
        SwtorFloorMedicalTile2x2 = 612,

        [Structure("Floor - Stone Carved (1x1)", "aswtor_096", "", true, 1)]
        SwtorFloorStoneCarved1x1 = 613,

        [Structure("Floor - Stone Carved (2x2)", "aswtor_097", "", true, 1)]
        SwtorFloorStoneCarved2x2 = 614,

        [Structure("Floor - Stone Pattern 1 (1x1)", "aswtor_092", "", true, 1)]
        SwtorFloorStonePattern11x1 = 615,

        [Structure("Floor - Stone Pattern 1 (2x2)", "aswtor_093", "", true, 1)]
        SwtorFloorStonePattern12x2 = 616,

        [Structure("Floor - Stone Pattern 2 (1x1)", "aswtor_094", "", true, 1)]
        SwtorFloorStonePattern21x1 = 617,

        [Structure("Floor - Stone Pattern 2 (2x2)", "aswtor_095", "", true, 1)]
        SwtorFloorStonePattern22x2 = 618,

        [Structure("Floor - Stone, Tiles (1x1)", "aswtor_098", "", true, 1)]
        SwtorFloorStoneTiles1x1 = 619,

        [Structure("Floor - Stone, Tiles (2x2)", "aswtor_099", "", true, 1)]
        SwtorFloorStoneTiles2x2 = 620,

        [Structure("Flower 001", "swd_flwr01", "", true, 1)]
        SwtorFlower001 = 621,

        [Structure("Flower 002", "swd_flwr02", "", true, 1)]
        SwtorFlower002 = 622,

        [Structure("Forklift 001", "swd3_forkl01", "", true, 1)]
        SwtorForklift001 = 623,

        [Structure("Generator (Lights)", "swd_genl01", "", true, 1)]
        SwtorGeneratorLights = 624,

        [Structure("Generator (Off)", "swd_geno01", "", true, 1)]
        SwtorGeneratorOff = 625,

        [Structure("Generator - Wall", "aswtor_180", "", true, 1)]
        SwtorGeneratorWall = 626,

        [Structure("Generator 003", "swd2_gene003", "", true, 1)]
        SwtorGenerator003 = 627,

        [Structure("Generator 004", "swd3_gener04", "", true, 1)]
        SwtorGenerator004 = 628,

        [Structure("Generator 05", "aswtor_145", "", true, 1)]
        SwtorGenerator05 = 629,

        [Structure("Generator 06", "aswtor_153", "", true, 1)]
        SwtorGenerator06 = 630,

        [Structure("Generator Pipes", "aswtor_178", "", true, 1)]
        SwtorGeneratorPipes = 631,

        [Structure("Grate 001", "swd_grate01", "", true, 1)]
        SwtorGrate001 = 632,

        [Structure("GSI Drop Box 001 (Decorative)", "swd3_gsidb01", "A decorative fixture for a player property.", true, 1)]
        SwtorGSIDropBox001 = 633,

        [Structure("GSI Sign", "swd3_gsisi01", "", true, 1)]
        SwtorGSISign = 634,

        [Structure("Gun Rack", "swd_gunrak01", "", true, 1)]
        SwtorGunRack = 635,

        [Structure("Holo - Cantina Sign", "aswtor_146", "", true, 1)]
        SwtorHoloCantinaSign = 636,

        [Structure("Hologram 001", "swd_holog01", "", true, 1)]
        SwtorHologram001 = 637,

        [Structure("Hologram 002", "swd_holog02", "", true, 1)]
        SwtorHologram002 = 638,

        [Structure("Hologram 003", "swd_holog03", "", true, 1)]
        SwtorHologram003 = 639,

        [Structure("Hologram 004", "swd_holog04", "", true, 1)]
        SwtorHologram004 = 640,

        [Structure("Hologram 005", "swd_holog05", "", true, 1)]
        SwtorHologram005 = 641,

        [Structure("Hologram 006", "swd_holog06", "", true, 1)]
        SwtorHologram006 = 642,

        [Structure("Hologram 007", "swd_holog07", "", true, 1)]
        SwtorHologram007 = 643,

        [Structure("Hologram 008", "swd_holog08", "", true, 1)]
        SwtorHologram008 = 644,

        [Structure("Hologram 009", "swd_holog09", "", true, 1)]
        SwtorHologram009 = 645,

        [Structure("Hologram 010", "swd_holog10", "", true, 1)]
        SwtorHologram010 = 646,

        [Structure("Holotree 1", "swd_holot01", "", true, 1)]
        SwtorHolotree1 = 647,

        [Structure("Holotree 2", "swd_holot02", "", true, 1)]
        SwtorHolotree2 = 648,

        [Structure("Hover Holo 002", "swd3_hovrh002", "", true, 1)]
        SwtorHoverHolo002 = 649,

        [Structure("Hutt Statue", "swd_hutst01", "", true, 1)]
        SwtorHuttStatue = 650,

        [Structure("Hutt Throne", "swd_huttr01", "", true, 1)]
        SwtorHuttThrone = 651,

        [Structure("Imperial Medical Shed", "swd_impmed01", "", true, 1)]
        SwtorImperialMedicalShed = 652,

        [Structure("Jedi Library 001", "swd2_jedl001", "", true, 1)]
        SwtorJediLibrary001 = 653,

        [Structure("Jedi Library 002", "swd2_jedl002", "", true, 1)]
        SwtorJediLibrary002 = 654,

        [Structure("Jedi Library 003", "swd2_jedl003", "", true, 1)]
        SwtorJediLibrary003 = 655,

        [Structure("Jedi Library 004", "swd2_jedl004", "", true, 1)]
        SwtorJediLibrary004 = 656,

        [Structure("Jedi Library 005", "swd2_jedl005", "", true, 1)]
        SwtorJediLibrary005 = 657,

        [Structure("Jedi Library 006", "swd2_jedl006", "", true, 1)]
        SwtorJediLibrary006 = 658,

        [Structure("Jukebox 001", "swd3_jukeb01", "", true, 1)]
        SwtorJukebox001 = 659,

        [Structure("Jukebox 002", "swd3_jukeb002", "", true, 1)]
        SwtorJukebox002 = 660,

        [Structure("Jukebox 003", "swd3_jukeb003", "", true, 1)]
        SwtorJukebox003 = 661,

        [Structure("Jukebox 004", "swd3_jukeb004", "", true, 1)]
        SwtorJukebox004 = 662,

        [Structure("Jukebox 005", "swd3_jukeb005", "", true, 1)]
        SwtorJukebox005 = 663,

        [Structure("Junk Pile 1", "swd_jkpl001", "", true, 1)]
        SwtorJunkPile1 = 664,

        [Structure("Junk Pile 2", "swd_jkpl002", "", true, 1)]
        SwtorJunkPile2 = 665,

        [Structure("Kiosk 001", "swd2_kiosk01", "", true, 1)]
        SwtorKiosk001 = 666,

        [Structure("Kiosk 002", "swd2_kiosk002", "", true, 1)]
        SwtorKiosk002 = 667,

        [Structure("Kiosk 003", "swd2_kiosk003", "", true, 1)]
        SwtorKiosk003 = 668,

        [Structure("Kiosk 004", "swd2_kiosk004", "", true, 1)]
        SwtorKiosk004 = 669,

        [Structure("Kiosk 005", "swd2_kiosk005", "", true, 1)]
        SwtorKiosk005 = 670,

        [Structure("Kiosk 006", "swd2_kiosk006", "", true, 1)]
        SwtorKiosk006 = 671,

        [Structure("Kolto Tank 01 (Decorative)", "aswtor_143", "A decorative fixture for a player property.", true, 1)]
        SwtorKoltoTank01 = 672,

        [Structure("Lamp 001", "swd_lamp01", "", true, 1)]
        SwtorLamp001 = 673,

        [Structure("Lightpost 01", "aswtor_022", "", true, 1)]
        SwtorLightpost01 = 674,

        [Structure("Locker - Green", "aswtor_126", "", true, 1)]
        SwtorLockerGreen = 675,

        [Structure("Locker - Grey", "aswtor_128", "", true, 1)]
        SwtorLockerGrey = 676,

        [Structure("Locker - Orange", "aswtor_127", "", true, 1)]
        SwtorLockerOrange = 677,

        [Structure("Mailbox", "swd_mailb01", "", true, 1)]
        SwtorMailBox = 678,

        [Structure("Market Item 1", "swd_mrkitm01", "", true, 1)]
        SwtorMarketItem1 = 679,

        [Structure("Market Item 2", "swd_mrkitm02", "", true, 1)]
        SwtorMarketItem2 = 680,

        [Structure("Market Stand 01 (Empty)", "swd_mstnd01", "", true, 1)]
        SwtorMarketStand01Empty = 681,

        [Structure("Market Stand 02 (Full)", "swd_mstnd02", "", true, 1)]
        SwtorMarketStand02Full = 682,

        [Structure("Market Stand 03 (Full)", "swd_mstnd03", "", true, 1)]
        SwtorMarketStand03Full = 683,

        [Structure("Market Stand 04 (Empty)", "swd_mstnd04", "", true, 1)]
        SwtorMarketStand04Empty = 684,

        [Structure("Market Stand 05 (Full)", "swd_mstnd05", "", true, 1)]
        SwtorMarketStand05Full = 685,

        [Structure("Market Stand 06 (Empty)", "swd_mstnd06", "", true, 1)]
        SwtorMarketStand06Empty = 686,

        [Structure("Market Stand 07 (Full)", "swd_mstnd07", "", true, 1)]
        SwtorMarketStand07Full = 687,

        [Structure("Market Stand 08 (Empty)", "swd_mstnd08", "", true, 1)]
        SwtorMarketStand08Empty = 688,

        [Structure("Market Stand 09 (Full)", "swd_mstnd09", "", true, 1)]
        SwtorMarketStand09Full = 689,

        [Structure("Market Stand 10 (Full)", "swd_mstnd10", "", true, 1)]
        SwtorMarketStand10Full = 690,

        [Structure("Mine (Decorative)", "swd_mine01", "A decorative fixture for a player property.", true, 1)]
        SwtorMine = 691,

        [Structure("Mining Drill 001 (Decorative)", "swd3_mindr01", "A decorative fixture for a player property.", true, 1)]
        SwtorMiningDrill001 = 692,

        [Structure("Pallet001", "swd_palet01", "", true, 1)]
        SwtorPallet001 = 693,

        [Structure("Parkingmeter", "swd_prkme01", "", true, 1)]
        SwtorParkingmeter = 694,

        [Structure("Phonebooth", "swd_phnb01", "", true, 1)]
        SwtorPhonebooth = 695,

        [Structure("Pickaxe 1", "swd_pick01", "", true, 1)]
        SwtorPickaxe1 = 696,

        [Structure("Pillar - Angled Brace", "aswtor_165", "", true, 1)]
        SwtorPillarAngledBrace = 697,

        [Structure("Pillar - Curved", "aswtor_166", "", true, 1)]
        SwtorPillarCurved = 698,

        [Structure("Pillar - Exterior Imp", "aswtor_081", "", true, 1)]
        SwtorPillarExteriorImp = 699,

        [Structure("Pillar - Exterior Imp (Gate)", "aswtor_083", "", true, 1)]
        SwtorPillarExteriorImpGate = 700,

        [Structure("Pillar - Exterior Imp (Turret)", "aswtor_082", "", true, 1)]
        SwtorPillarExteriorImpTurret = 701,

        [Structure("Pillar - Exterior Rep", "aswtor_005", "", true, 1)]
        SwtorPillarExteriorRep = 702,

        [Structure("Pillar - Grey Tech (Arch)", "aswtor_071", "", true, 1)]
        SwtorPillarGreyTechArch = 703,

        [Structure("Pillar - Grey Tech (Brace)", "aswtor_072", "", true, 1)]
        SwtorPillarGreyTechBrace = 704,

        [Structure("Pillar - Red Stripe", "aswtor_164", "", true, 1)]
        SwtorPillarRedStripe = 705,

        [Structure("Pillar - Red Vents", "aswtor_020", "", true, 1)]
        SwtorPillarRedVents = 706,

        [Structure("Pillar - Republic Interior", "aswtor_040", "", true, 1)]
        SwtorPillarRepublicInterior = 707,

        [Structure("Pillar - Round Durasteel", "aswtor_162", "", true, 1)]
        SwtorPillarRoundDurasteel = 708,

        [Structure("Pillar - Tatooine (Full)", "daf_sw306", "", true, 1)]
        SwtorPillarTatooineFull = 709,

        [Structure("Pillar - Tatooine (Half)", "daf_sw305", "", true, 1)]
        SwtorPillarTatooineHalf = 710,

        [Structure("Pillar - Yellow Trim", "aswtor_163", "", true, 1)]
        SwtorPillarYellowTrim = 711,

        [Structure("Pillar 001", "swd_pillar01", "", true, 1)]
        SwtorPillar001 = 712,

        [Structure("Pillar 002", "swd2_pilr002", "", true, 1)]
        SwtorPillar002 = 713,

        [Structure("Pillar 003", "swd2_pilr003", "", true, 1)]
        SwtorPillar003 = 714,

        [Structure("Pillar 004", "swd2_pilr004", "", true, 1)]
        SwtorPillar004 = 715,

        [Structure("Pillar 005", "swd2_pilr005", "", true, 1)]
        SwtorPillar005 = 716,

        [Structure("Pillar 006", "swd2_pilr006", "", true, 1)]
        SwtorPillar006 = 717,

        [Structure("Pillar 007", "swd3_pilla07", "", true, 1)]
        SwtorPillar007 = 718,

        [Structure("Pillar 008", "swd3_pilla008", "", true, 1)]
        SwtorPillar008 = 719,

        [Structure("Pillar 009", "swd3_pilla009", "", true, 1)]
        SwtorPillar009 = 720,

        [Structure("Pillar 010", "swd3_pilla010", "", true, 1)]
        SwtorPillar010 = 721,

        [Structure("Pipes 001", "swd_pipe01", "", true, 1)]
        SwtorPipes001 = 722,

        [Structure("Pipes 002", "swd_pipe02", "", true, 1)]
        SwtorPipes002 = 723,

        [Structure("Plant - Bush 01", "daf_sw323", "", true, 1)]
        SwtorPlantBush01 = 724,

        [Structure("Plant - Bush 02", "daf_sw324", "", true, 1)]
        SwtorPlantBush02 = 725,

        [Structure("Plant - Cattials", "aswtor_187", "", true, 1)]
        SwtorPlantCattials = 726,

        [Structure("Plant - Crops 01", "daf_sw339", "", true, 1)]
        SwtorPlantCrops01 = 727,

        [Structure("Plant - Crops 02", "daf_sw340", "", true, 1)]
        SwtorPlantCrops02 = 728,

        [Structure("Plant - Crops 03", "daf_sw343", "", true, 1)]
        SwtorPlantCrops03 = 729,

        [Structure("Plant - Crops 04", "daf_sw344", "", true, 1)]
        SwtorPlantCrops04 = 730,

        [Structure("Plant - Fern 01", "daf_sw325", "", true, 1)]
        SwtorPlantFern01 = 731,

        [Structure("Plant - Fern 02", "daf_sw326", "", true, 1)]
        SwtorPlantFern02 = 732,

        [Structure("Plant - Fern 03", "aswtor_186", "", true, 1)]
        SwtorPlantFern03 = 733,

        [Structure("Plant - Fern Group 01", "aswtor_185", "", true, 1)]
        SwtorPlantFernGroup01 = 734,

        [Structure("Plant - Fungus, Hanging 01", "daf_sw327", "", true, 1)]
        SwtorPlantFungusHanging01 = 735,

        [Structure("Plant - Fungus, Hanging 02", "daf_sw328", "", true, 1)]
        SwtorPlantFungusHanging02 = 736,

        [Structure("Plant - Grass, Dead 01", "daf_sw332", "", true, 1)]
        SwtorPlantGrassDead01 = 737,

        [Structure("Plant - Grass, Swamp 01", "daf_sw333", "", true, 1)]
        SwtorPlantGrassSwamp01 = 738,

        [Structure("Plant - Grass, Swamp 02", "daf_sw334", "", true, 1)]
        SwtorPlantGrassSwamp02 = 739,

        [Structure("Plant - Grass, Swamp 03", "daf_sw335", "", true, 1)]
        SwtorPlantGrassSwamp03 = 740,

        [Structure("Plant - Potpod 01", "daf_sw329", "", true, 1)]
        SwtorPlantPotpod01 = 741,

        [Structure("Plant - Silverfern", "aswtor_188", "", true, 1)]
        SwtorPlantSilverfern = 742,

        [Structure("Plant - Stalk 01", "daf_sw330", "", true, 1)]
        SwtorPlantStalk01 = 743,

        [Structure("Plant - Stalk 02", "daf_sw331", "", true, 1)]
        SwtorPlantStalk02 = 744,

        [Structure("Plant - Vinepost", "daf_sw345", "", true, 1)]
        SwtorPlantVinepost = 745,

        [Structure("Plant 1", "swd_plant01", "", true, 1)]
        SwtorPlant1 = 746,

        [Structure("Planter - Plasteel", "aswtor_167", "", true, 1)]
        SwtorPlanterPlasteel = 747,

        [Structure("Planter - White (Curve)", "aswtor_052", "", true, 1)]
        SwtorPlanterWhiteCurve = 748,

        [Structure("Planter - White (Endcap)", "aswtor_053", "", true, 1)]
        SwtorPlanterWhiteEndcap = 749,

        [Structure("Planter - White (Full Curve)", "aswtor_051", "", true, 1)]
        SwtorPlanterWhiteFullCurve = 750,

        [Structure("Planter - White (Straight)", "aswtor_054", "", true, 1)]
        SwtorPlanterWhiteStraight = 751,

        [Structure("Planter - White (Vase)", "aswtor_050", "", true, 1)]
        SwtorPlanterWhiteVase = 752,

        [Structure("Planter 1", "swd_planr01", "", true, 1)]
        SwtorPlanter1 = 753,

        [Structure("Plaque, Standing", "aswtor_144", "", true, 1)]
        SwtorPlaqueStanding = 754,

        [Structure("Poster - Adventure Awaits", "aswtor_117", "", true, 1)]
        SwtorPosterAdventureAwaits = 755,

        [Structure("Poster - Construction Off Limits", "aswtor_118", "", true, 1)]
        SwtorPosterConstructionOffLimits = 756,

        [Structure("Poster - Droid Recharge Section", "aswtor_116", "", true, 1)]
        SwtorPosterDroidRechargeSection = 757,

        [Structure("Poster - Must Register W/ Planet", "aswtor_114", "", true, 1)]
        SwtorPosterMustRegisterWPlanet = 758,

        [Structure("Poster - Neutral Station", "aswtor_113", "", true, 1)]
        SwtorPosterNeutralStation = 759,

        [Structure("Poster - Tow Zone", "aswtor_115", "", true, 1)]
        SwtorPosterTowZone = 760,

        [Structure("Poster 1", "swd_postr01", "", true, 1)]
        SwtorPoster1 = 761,

        [Structure("Poster 2", "swd_postr02", "", true, 1)]
        SwtorPoster2 = 762,

        [Structure("Power Core Pair", "swd_pwrcp01", "", true, 1)]
        SwtorPowerCorePair = 763,

        [Structure("Power Core Station", "swd_pwrcs01", "", true, 1)]
        SwtorPowerCoreStation = 764,

        [Structure("Projection Holo 001", "swd_pholo01", "", true, 1)]
        SwtorProjectionHolo001 = 765,

        [Structure("Projection Holo 002", "swd_pholo02", "", true, 1)]
        SwtorProjectionHolo002 = 766,

        [Structure("Projection Holo 003", "swd_pholo03", "", true, 1)]
        SwtorProjectionHolo003 = 767,

        [Structure("Projection Holo 004", "swd_pholo04", "", true, 1)]
        SwtorProjectionHolo004 = 768,

        [Structure("Projection Holo 005", "swd_pholo05", "", true, 1)]
        SwtorProjectionHolo005 = 769,

        [Structure("Projection Holo 006", "swd_pholo06", "", true, 1)]
        SwtorProjectionHolo006 = 770,

        [Structure("Radar - Wall Mounted", "aswtor_184", "", true, 1)]
        SwtorRadarWallMounted = 771,

        [Structure("Radar 001", "swd3_radar01", "", true, 1)]
        SwtorRadar001 = 772,

        [Structure("Refinery (Decorative)", "aswtor_076", "A decorative fixture for a player property.", true, 1)]
        SwtorRefinery = 773,

        [Structure("Repair Desk (Decorative)", "aswtor_068", "A decorative fixture for a player property.", true, 1)]
        SwtorRepairDesk = 774,

        [Structure("Road 1 (3-Way)", "aswtor_029", "", true, 1)]
        SwtorRoad13Way = 775,

        [Structure("Road 1 (4-Way)", "aswtor_030", "", true, 1)]
        SwtorRoad14Way = 776,

        [Structure("Road 1 (Curve)", "aswtor_028", "", true, 1)]
        SwtorRoad1Curve = 777,

        [Structure("Road 1 (Slab)", "aswtor_026", "", true, 1)]
        SwtorRoad1Slab = 778,

        [Structure("Road 1 (Straight)", "aswtor_027", "", true, 1)]
        SwtorRoad1Straight = 779,

        [Structure("Rug - Blue Geometry", "aswtor_160", "", true, 1)]
        SwtorRugBlueGeometry = 780,

        [Structure("Rug - Blue Geometry (Small)", "aswtor_161", "", true, 1)]
        SwtorRugBlueGeometrySmall = 781,

        [Structure("Rug - Blue Octagon", "aswtor_155", "", true, 1)]
        SwtorRugBlueOctagon = 782,

        [Structure("Rug - Cream Pattern", "aswtor_152", "", true, 1)]
        SwtorRugCreamPattern = 783,

        [Structure("Rug - Gold Pattern", "aswtor_159", "", true, 1)]
        SwtorRugGoldPattern = 784,

        [Structure("Rug - Green Octagon", "aswtor_158", "", true, 1)]
        SwtorRugGreenOctagon = 785,

        [Structure("Rug - Jedi Blue", "aswtor_156", "", true, 1)]
        SwtorRugJediBlue = 786,

        [Structure("Rug - Red/Orange Pattern", "aswtor_149", "", true, 1)]
        SwtorRugRedOrangePattern = 787,

        [Structure("Rug - Royal Dark Blue", "aswtor_157", "", true, 1)]
        SwtorRugRoyalDarkBlue = 788,

        [Structure("Rug 1", "swd_rug01", "", true, 1)]
        SwtorRug1 = 789,

        [Structure("Rug 2", "swd_rug02", "", true, 1)]
        SwtorRug2 = 790,

        [Structure("Rug 3", "swd_rug03", "", true, 1)]
        SwtorRug3 = 791,

        [Structure("Rug 4", "swd_rug04", "", true, 1)]
        SwtorRug4 = 792,

        [Structure("Rug 5", "swd_rug05", "", true, 1)]
        SwtorRug5 = 793,

        [Structure("Saber Remote", "swd_srem01", "", true, 1)]
        SwtorSaberRemote = 794,

        [Structure("Safe Box", "swd_sbox01", "", true, 1)]
        SwtorSafeBox = 795,

        [Structure("Salvage Pack", "swd_spack01", "", true, 1)]
        SwtorSalvagePack = 796,

        [Structure("Sandbag 001", "swd3_sandb01", "", true, 1)]
        SwtorSandbag001 = 797,

        [Structure("Sandbag 002", "swd3_sandb002", "", true, 1)]
        SwtorSandbag002 = 798,

        [Structure("Scaffold 001", "swd2_scaf001", "", true, 1)]
        SwtorScaffold001 = 799,

        [Structure("Scaffold 002", "swd2_scaf002", "", true, 1)]
        SwtorScaffold002 = 800,

        [Structure("Server 001", "swd3_servr01", "", true, 1)]
        SwtorServer001 = 801,

        [Structure("Sign - Hanging Tech", "aswtor_182", "", true, 1)]
        SwtorSignHangingTech = 802,

        [Structure("Sign - Street, Core Walkway", "aswtor_122", "", true, 1)]
        SwtorSignStreetCoreWalkway = 803,

        [Structure("Sign - Street, Day Moon", "aswtor_120", "", true, 1)]
        SwtorSignStreetDayMoon = 804,

        [Structure("Sign - Street, Direction", "aswtor_121", "", true, 1)]
        SwtorSignStreetDirection = 805,

        [Structure("Sign - Street, Watch", "aswtor_119", "", true, 1)]
        SwtorSignStreetWatch = 806,

        [Structure("Signpost - Taxi Holo", "aswtor_142", "", true, 1)]
        SwtorSignpostTaxiHolo = 807,

        [Structure("Slot Machine 1 (Light) (Decorative)", "swd_smach01", "A decorative fixture for a player property.", true, 1)]
        SwtorSlotMachine1Light = 808,

        [Structure("Slot Machine 2 (Light) (Decorative)", "swd_smach02", "A decorative fixture for a player property.", true, 1)]
        SwtorSlotMachine2Light = 809,

        [Structure("Slot Machine 3 (Light) (Decorative)", "swd_smach03", "A decorative fixture for a player property.", true, 1)]
        SwtorSlotMachine3Light = 810,

        [Structure("Slot Machine 4 (Light) (Decorative)", "swd_smach04", "A decorative fixture for a player property.", true, 1)]
        SwtorSlotMachine4Light = 811,

        [Structure("Slot Machine 5 (Dark) (Decorative)", "swd_smach05", "A decorative fixture for a player property.", true, 1)]
        SwtorSlotMachine5Dark = 812,

        [Structure("Slot Machine 6 (Dark) (Decorative)", "swd_smach06", "A decorative fixture for a player property.", true, 1)]
        SwtorSlotmachine6Dark = 813,

        [Structure("Smithing Bench (Decorative)", "aswtor_077", "A decorative fixture for a player property.", true, 1)]
        SwtorSmithingBench = 814,

        [Structure("Snow Pile 01", "daf_sw322", "", true, 1)]
        SwtorSnowPile01 = 815,

        [Structure("Sofa", "swd_sofa01", "", true, 1)]
        SwtorSofa = 816,

        [Structure("Sofa 2", "swd2_sofa002", "", true, 1)]
        SwtorSofa2 = 817,

        [Structure("Sofa 3", "swd2_sofa003", "", true, 1)]
        SwtorSofa3 = 818,

        [Structure("Solar Panel 001", "swd3_solar01", "", true, 1)]
        SwtorSolarPanel001 = 819,

        [Structure("Solar Panel 002", "swd3_solar002", "", true, 1)]
        SwtorSolarPanel002 = 820,

        [Structure("Statue - Veles Fountain", "aswtor_021", "", true, 1)]
        SwtorStatueVelesFountain = 821,

        [Structure("Statue 001", "swd2_stat001", "", true, 1)]
        SwtorStatue001 = 822,

        [Structure("Statue 002", "swd2_stat002", "", true, 1)]
        SwtorStatue002 = 823,

        [Structure("Stimpack", "swd_stim01", "", true, 1)]
        SwtorStimpack = 824,

        [Structure("Street Light", "swd_streel01", "", true, 1)]
        SwtorStreetLight = 825,

        [Structure("Table - Cantina, Large", "aswtor_148", "", true, 1)]
        SwtorTableCantinaLarge = 826,

        [Structure("Table - Cantina, Large (Full)", "aswtor_147", "", true, 1)]
        SwtorTableCantinaLargeFull = 827,

        [Structure("Table - Cantina, Small", "aswtor_151", "", true, 1)]
        SwtorTableCantinaSmall = 828,

        [Structure("Table - Cantina, Small (Full)", "aswtor_150", "", true, 1)]
        SwtorTableCantinaSmallFull = 829,

        [Structure("Table - Conference (Black)", "aswtor_133", "", true, 1)]
        SwtorTableConferenceBlack = 830,

        [Structure("Table - Conference (Brown)", "aswtor_131", "", true, 1)]
        SwtorTableConferenceBrown = 831,

        [Structure("Table - Conference (Grey)", "aswtor_132", "", true, 1)]
        SwtorTableConferenceGrey = 832,

        [Structure("Table - Imp 01 (Off)", "daf_sw313", "", true, 1)]
        SwtorTableImp01Off = 833,

        [Structure("Table - Imp 01 (On)", "daf_sw314", "", true, 1)]
        SwtorTableImp01On = 834,

        [Structure("Table - Imp Desk 01 (Off)", "daf_sw311", "", true, 1)]
        SwtorTableImpDesk01Off = 835,

        [Structure("Table - Imp Desk 01 (On)", "daf_sw312", "", true, 1)]
        SwtorTableImpDesk01On = 836,

        [Structure("Table 1", "swd_table01", "", true, 1)]
        SwtorTable1 = 837,

        [Structure("Table 2", "swd_table02", "", true, 1)]
        SwtorTable2 = 838,

        [Structure("Table 3", "swd_table03", "", true, 1)]
        SwtorTable3 = 839,

        [Structure("Table 5", "swd_table05", "", true, 1)]
        SwtorTable5 = 840,

        [Structure("Table 6", "swd2_table06", "", true, 1)]
        SwtorTable6 = 841,

        [Structure("Table 7", "swd2_table007", "", true, 1)]
        SwtorTable7 = 842,

        [Structure("Table 8", "swd3_table8", "", true, 1)]
        SwtorTable8 = 843,

        [Structure("Table 9", "swd3_table009", "", true, 1)]
        SwtorTable9 = 844,

        [Structure("Tablet", "swd_tablet01", "", true, 1)]
        SwtorTablet = 845,

        [Structure("Tank (Large)", "aswtor_032", "", true, 1)]
        SwtorTankLarge = 846,

        [Structure("Tank (Small)", "aswtor_031", "", true, 1)]
        SwtorTankSmall = 847,

        [Structure("Tank Trap 001", "swd3_tankt01", "", true, 1)]
        SwtorTankTrap001 = 848,

        [Structure("Tank Trap 002", "swd3_tankt002", "", true, 1)]
        SwtorTankTrap002 = 849,

        [Structure("Terminal - Imp 01 (On)", "daf_sw316", "", true, 1)]
        SwtorTerminalImp01On = 850,

        [Structure("Terminal - Imp Wall 01 (Off)", "daf_sw317", "", true, 1)]
        SwtorTerminalImpWall01Off = 851,

        [Structure("Terminal - Imp Wall 01 (On)", "daf_sw318", "", true, 1)]
        SwtorTerminalImpWall01On = 852,

        [Structure("Terminal - Market Kiosk", "aswtor_154", "", true, 1)]
        SwtorTerminalMarketKiosk = 853,

        [Structure("Tool", "swd_tool01", "", true, 1)]
        SwtorTool = 854,

        [Structure("Tool Box", "swd_toolb01", "", true, 1)]
        SwtorToolBox = 855,

        [Structure("Torture Chair", "daf_sw310", "", true, 1)]
        SwtorTortureChair = 856,

        [Structure("Toy", "swd_toy01", "", true, 1)]
        SwtorToy = 857,

        [Structure("Training Dummy (Decorative)", "swd_trdum01", "A decorative fixture for a player property.", true, 1)]
        SwtorTrainingDummy = 858,

        [Structure("Training Dummy (Side) (Decorative)", "swd_trdum02", "A decorative fixture for a player property.", true, 1)]
        SwtorTrainingDummySide = 859,

        [Structure("Training Dummy, Imp (Decorative)", "daf_sw319", "A decorative fixture for a player property.", true, 1)]
        SwtorTrainingDummyImp = 860,

        [Structure("Transmitter 1", "aswtor_048", "", true, 1)]
        SwtorTransmitter1 = 861,

        [Structure("Trash Can (SWTOR)", "swd_trash01", "", true, 1)]
        SwtorTrashCan = 862,

        [Structure("Trash Can 02", "aswtor_049", "", true, 1)]
        SwtorTrashCan02 = 863,

        [Structure("Turret 001 (Decorative)", "swd3_turet01", "A decorative fixture for a player property.", true, 1)]
        SwtorTurret001 = 864,

        [Structure("Turret 002 (Decorative)", "swd3_turet002", "A decorative fixture for a player property.", true, 1)]
        SwtorTurret002 = 865,

        [Structure("Turret 003 (Decorative)", "swd3_turet003", "A decorative fixture for a player property.", true, 1)]
        SwtorTurret003 = 866,

        [Structure("Turret 004 (Decorative)", "swd3_turet004", "A decorative fixture for a player property.", true, 1)]
        SwtorTurret004 = 867,

        [Structure("Turret 005 (Decorative)", "swd3_turet005", "A decorative fixture for a player property.", true, 1)]
        SwtorTurret005 = 868,

        [Structure("Upgrade Terminal 002 (Decorative)", "swd3_upter002", "A decorative fixture for a player property.", true, 1)]
        SwtorUpgradeTerminal002 = 869,

        [Structure("Vehicle 001 (Decorative)", "swd2_vehi001", "A decorative fixture for a player property.", true, 1)]
        SwtorVehicle001 = 870,

        [Structure("Vehicle 002 (Decorative)", "swd2_vehi002", "A decorative fixture for a player property.", true, 1)]
        SwtorVehicle002 = 871,

        [Structure("Vehicle 003 (Decorative)", "swd2_vehi003", "A decorative fixture for a player property.", true, 1)]
        SwtorVehicle003 = 872,

        [Structure("Vehicle 004 (Decorative)", "swd2_vehi004", "A decorative fixture for a player property.", true, 1)]
        SwtorVehicle004 = 873,

        [Structure("Vehicle 005 (Decorative)", "swd2_vehi005", "A decorative fixture for a player property.", true, 1)]
        SwtorVehicle005 = 874,

        [Structure("Vehicle 006 (Decorative)", "swd2_vehi006", "A decorative fixture for a player property.", true, 1)]
        SwtorVehicle006 = 875,

        [Structure("Vehicle 007 (Decorative)", "swd2_vehi007", "A decorative fixture for a player property.", true, 1)]
        SwtorVehicle007 = 876,

        [Structure("Vehicle 008 (Decorative)", "swd2_vehi008", "A decorative fixture for a player property.", true, 1)]
        SwtorVehicle008 = 877,

        [Structure("Vending Machine 01 (Decorative)", "daf_sw381", "A decorative fixture for a player property.", true, 1)]
        SwtorVendingMachine01 = 878,

        [Structure("Vendor - Armor (Decorative)", "aswtor_043", "A decorative fixture for a player property.", true, 1)]
        SwtorVendorArmor = 879,

        [Structure("Vendor - General 1 (Decorative)", "aswtor_041", "A decorative fixture for a player property.", true, 1)]
        SwtorVendorGeneral1 = 880,

        [Structure("Vendor - General 2 (Decorative)", "aswtor_042", "A decorative fixture for a player property.", true, 1)]
        SwtorVendorGeneral2 = 881,

        [Structure("Vendor - Weapons (Decorative)", "aswtor_044", "A decorative fixture for a player property.", true, 1)]
        SwtorVendorWeapons = 882,

        [Structure("Vendor Bank (Decorative)", "swd_vbank01", "A decorative fixture for a player property.", true, 1)]
        SwtorVendorBank = 883,

        [Structure("Vendor Canopy", "aswtor_047", "", true, 1)]
        SwtorVendorCanopy = 884,

        [Structure("Vendor Sign 1", "swd_vsign01", "", true, 1)]
        SwtorVendorSign1 = 885,

        [Structure("Vendor Sign 2", "swd_vsign02", "", true, 1)]
        SwtorVendorSign2 = 886,

        [Structure("Vendor Sign 3", "swd_vsign03", "", true, 1)]
        SwtorVendorSign3 = 887,

        [Structure("Vendor Sign 4", "swd3_vensi04", "", true, 1)]
        SwtorVendorSign4 = 888,

        [Structure("Vendor Sign 5", "swd3_vensi005", "", true, 1)]
        SwtorVendorSign5 = 889,

        [Structure("Vendor Table", "aswtor_129", "", true, 1)]
        SwtorVendorTable = 890,

        [Structure("Vent - Floor, Angled", "aswtor_179", "", true, 1)]
        SwtorVentFloorAngled = 891,

        [Structure("Vent - Wall Panel", "aswtor_168", "", true, 1)]
        SwtorVentWallPanel = 892,

        [Structure("Wall - Exterior Imp (Corner)", "aswtor_080", "", true, 1)]
        SwtorWallExteriorImpCorner = 893,

        [Structure("Wall - Exterior Imp (Long)", "aswtor_079", "", true, 1)]
        SwtorWallExteriorImpLong = 894,

        [Structure("Wall - Exterior Imp (Short)", "aswtor_078", "", true, 1)]
        SwtorWallExteriorImpShort = 895,

        [Structure("Wall - Exterior Pipes, Grey (Corner)", "aswtor_060", "", true, 1)]
        SwtorWallExteriorPipesGreyCorner = 896,

        [Structure("Wall - Exterior Pipes, Grey (Long)", "aswtor_059", "", true, 1)]
        SwtorWallExteriorPipesGreyLong = 897,

        [Structure("Wall - Exterior Pipes, Grey (Short)", "aswtor_058", "", true, 1)]
        SwtorWallExteriorPipesGreyShort = 898,

        [Structure("Wall - Exterior Pipes, Tan (Corner)", "aswtor_057", "", true, 1)]
        SwtorWallExteriorPipesTanCorner = 899,

        [Structure("Wall - Exterior Pipes, Tan (Gate)", "aswtor_110", "", true, 1)]
        SwtorWallExteriorPipesTanGate = 900,

        [Structure("Wall - Exterior Pipes, Tan (Long)", "aswtor_056", "", true, 1)]
        SwtorWallExteriorPipesTanLong = 901,

        [Structure("Wall - Exterior Pipes, Tan (Short)", "aswtor_055", "", true, 1)]
        SwtorWallExteriorPipesTanShort = 902,

        [Structure("Wall - Exterior Rep (Corner)", "aswtor_003", "", true, 1)]
        SwtorWallExteriorRepCorner = 903,

        [Structure("Wall - Exterior Rep (Gate)", "aswtor_004", "", true, 1)]
        SwtorWallExteriorRepGate = 904,

        [Structure("Wall - Exterior Rep (Long)", "aswtor_002", "", true, 1)]
        SwtorWallExteriorRepLong = 905,

        [Structure("Wall - Exterior Rep (Short)", "aswtor_001", "", true, 1)]
        SwtorWallExteriorRepShort = 906,

        [Structure("Wall - Red Vents (Door 1)", "aswtor_018", "", true, 1)]
        SwtorWallRedVentsDoor1 = 907,

        [Structure("Wall - Red Vents (Door 2)", "aswtor_019", "", true, 1)]
        SwtorWallRedVentsDoor2 = 908,

        [Structure("Wall - Red Vents (Long)", "aswtor_017", "", true, 1)]
        SwtorWallRedVentsLong = 909,

        [Structure("Wall - Red Vents (Short)", "aswtor_016", "", true, 1)]
        SwtorWallRedVentsShort = 910,

        [Structure("Wall - Republic Int (Arch)", "aswtor_039", "", true, 1)]
        SwtorWallRepublicIntArch = 911,

        [Structure("Wall - Republic Int (Door)", "aswtor_025", "", true, 1)]
        SwtorWallRepublicIntDoor = 912,

        [Structure("Wall - Republic Int (Long)", "aswtor_024", "", true, 1)]
        SwtorWallRepublicIntLong = 913,

        [Structure("Wall - Republic Int (Short)", "aswtor_023", "", true, 1)]
        SwtorWallRepublicIntShort = 914,

        [Structure("Wall - Tan Panel (Door 1)", "aswtor_012", "", true, 1)]
        SwtorWallTanPanelDoor1 = 915,

        [Structure("Wall - Tan Panel (Door 2)", "aswtor_013", "", true, 1)]
        SwtorWallTanPanelDoor2 = 916,

        [Structure("Wall - Tan Panel (Long)", "aswtor_011", "", true, 1)]
        SwtorWallTanPanelLong = 917,

        [Structure("Wall - Tan Panel (Short)", "aswtor_010", "", true, 1)]
        SwtorWallTanPanelShort = 918,

        [Structure("Wall - Tan Panel (Window)", "aswtor_061", "", true, 1)]
        SwtorWallTanPanelWindow = 919,

        [Structure("Wall - Tan Vents (Door 1)", "aswtor_008", "", true, 1)]
        SwtorWallTanVentsDoor1 = 920,

        [Structure("Wall - Tan Vents (Door 2)", "aswtor_009", "", true, 1)]
        SwtorWallTanVentsDoor2 = 921,

        [Structure("Wall - Tan Vents (Long)", "aswtor_007", "", true, 1)]
        SwtorWallTanVentsLong = 922,

        [Structure("Wall - Tan Vents (Short)", "aswtor_006", "", true, 1)]
        SwtorWallTanVentsShort = 923,

        [Structure("Wall - White Panel (Door 1)", "aswtor_064", "", true, 1)]
        SwtorWallWhitePanelDoor1 = 924,

        [Structure("Wall - White Panel (Door 2)", "aswtor_065", "", true, 1)]
        SwtorWallWhitePanelDoor2 = 925,

        [Structure("Wall - White Panel (Long)", "aswtor_063", "", true, 1)]
        SwtorWallWhitePanelLong = 926,

        [Structure("Wall - White Panel (Short)", "aswtor_062", "", true, 1)]
        SwtorWallWhitePanelShort = 927,

        [Structure("Wall - White Panel (Window)", "aswtor_066", "", true, 1)]
        SwtorWallWhitePanelWindow = 928,

        [Structure("Wall 001", "swd3_wall001", "", true, 1)]
        SwtorWall001 = 929,

        [Structure("Wall 002", "swd3_wall002", "", true, 1)]
        SwtorWall002 = 930,

        [Structure("Wall 003", "swd3_wall003", "", true, 1)]
        SwtorWall003 = 931,

        [Structure("Wall Decor 001", "swd3_walld01", "", true, 1)]
        SwtorWallDecor001 = 932,

        [Structure("Wall Decor 002", "swd3_walld002", "", true, 1)]
        SwtorWallDecor002 = 933,

        [Structure("Wardrobe 2", "swd_ward02", "", true, 1)]
        SwtorWardrobe2 = 934,

        [Structure("Wardrobe 3", "swd_ward03", "", true, 1)]
        SwtorWardrobe3 = 935,

        [Structure("Wardrobe 4", "swd_ward04", "", true, 1)]
        SwtorWardrobe4 = 936,

        [Structure("Water Tower", "aswtor_067", "", true, 1)]
        SwtorWaterTower = 937,

        [Structure("Waterfall 001", "swd_wafa01", "", true, 1)]
        SwtorWaterfall001 = 938,

        [Structure("Weapons Crate 001", "swd3_wepcr01", "", true, 1)]
        SwtorWeaponsCrate001 = 939,

        [Structure("Weapons Crate 002", "swd3_wepcr002", "", true, 1)]
        SwtorWeaponsCrate002 = 940,

        [Structure("Workbench (Decorative)", "swd_wrkbe01", "A decorative fixture for a player property.", true, 1)]
        SwtorWorkbench = 941,

        [Structure("Workbench 002 (Decorative)", "swd2_work002", "A decorative fixture for a player property.", true, 1)]
        SwtorWorkbench002 = 942,

        [Structure("Workbench 003 (Decorative)", "swd2_work003", "A decorative fixture for a player property.", true, 1)]
        SwtorWorkbench003 = 943,

        [Structure("Workbench 004 (Decorative)", "swd2_work004", "A decorative fixture for a player property.", true, 1)]
        SwtorWorkbench004 = 944,

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
}
