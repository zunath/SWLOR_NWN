using System;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Enumeration
{
    public enum BaseStructure
    {
        [BaseStructure(BaseStructureType.Invalid, "Invalid", "", "", false, 0, 0, 0, 0, 0, 7200, false, 0, 0, 0, StructureModeType.None)]
        Invalid = 0,
        [BaseStructure(BaseStructureType.ControlTower, "Small Control Tower", "c_tower_small", "control_tower", 1, 50, 20, 20, 340, 0, 7200, 1, 10, 0, 1, StructureModeType.None)]
        SmallControlTower = 1,
        [BaseStructure(BaseStructureType.ControlTower, "Medium Control Tower", "c_tower_medium", "control_tower", 1, 125, 50, 40, 360, 0, 7200, 1, 15, 0, 2, StructureModeType.None)]
        MediumControlTower = 2,
        [BaseStructure(BaseStructureType.ControlTower, "Large Control Tower", "c_tower_large", "control_tower", 1, 265, 120, 60, 380, 0, 7200, 1, 20, 0, 3, StructureModeType.None)]
        LargeControlTower = 3,
        [BaseStructure(BaseStructureType.Furniture, "Obelisk, Large", "x0_obelisk", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        ObeliskLarge = 4,
        [BaseStructure(BaseStructureType.Furniture, "Jukebox", "jukebox", "furniture", 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, StructureModeType.None)]
        Jukebox = 5,
        [BaseStructure(BaseStructureType.Furniture, "Obelisk, Small", "nw_plc_seaobelsk", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        ObeliskSmall = 6,
        [BaseStructure(BaseStructureType.Furniture, "Ladder, Light", "x0_weatheredladd", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        LadderLight = 7,
        [BaseStructure(BaseStructureType.Furniture, "Ladder, Dark", "x0_ladder", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        LadderDark = 8,
        [BaseStructure(BaseStructureType.Furniture, "Statue, Huge", "x2_plc_statue_h", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        StatueHuge = 9,
        [BaseStructure(BaseStructureType.Furniture, "Mining Well", "x2_plc_minewell", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        MiningWell = 10,
        [BaseStructure(BaseStructureType.CraftingDevice, "Molecular Reassembly Terminal", "atom_reass", "furniture", 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, StructureModeType.None)]
        MolecularReassemblyTerminal = 11,
        [BaseStructure(BaseStructureType.Furniture, "Statue, Wizard", "x3_plc_gg001", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        StatueWizard = 12,
        [BaseStructure(BaseStructureType.Furniture, "Gong", "plc_gong", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        Gong = 13,
        [BaseStructure(BaseStructureType.Furniture, "Statue, Monster", "plc_statue3", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        StatueMonster = 14,
        [BaseStructure(BaseStructureType.Furniture, "Brazier, Stone", "plc_brazier", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        BrazierStone = 17,
        [BaseStructure(BaseStructureType.Furniture, "Statue, Guardian", "plc_statue1", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        StatueGuardian = 18,
        [BaseStructure(BaseStructureType.Furniture, "Doorway, Metal", "x0_doormetal", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        DoorwayMetal = 19,
        [BaseStructure(BaseStructureType.Furniture, "Flaming Statue", "x2_plc_statue_fl", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        FlamingStatue = 20,
        [BaseStructure(BaseStructureType.Furniture, "Brazier, Round", "x3_plc_brazier", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        BrazierRound = 21,
        [BaseStructure(BaseStructureType.Furniture, "Pedestal", "plc_pedestal", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        Pedestal = 22,
        [BaseStructure(BaseStructureType.Furniture, "Rope Coil", "nw_plc_ropecoil1", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        RopeCoil = 23,
        [BaseStructure(BaseStructureType.Furniture, "Statue, Wyvern", "x3_plc_statuew", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        StatueWyvern = 24,
        [BaseStructure(BaseStructureType.Furniture, "Pedestal, Evil", "x3_plc_pedestal", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        PedestalEvil = 25,
        [BaseStructure(BaseStructureType.Furniture, "Birdbath", "plc_birdbath", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        Birdbath = 26,
        [BaseStructure(BaseStructureType.Furniture, "Sphinx Statue", "x0_sphinxstatue", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        SphinxStatue = 27,
        [BaseStructure(BaseStructureType.Furniture, "Mining Well Platform", "x2_plc_minewellb", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        MiningWellPlatform = 28,
        [BaseStructure(BaseStructureType.Furniture, "Pedestal, Sword", "x3_plc_pedsword", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        PedestalSword = 29,
        [BaseStructure(BaseStructureType.Furniture, "Doorway, Stone", "x0_stonewalldoor", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        DoorwayStone = 30,
        [BaseStructure(BaseStructureType.Furniture, "Female Statue", "x2_plc_statue_f", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        FemaleStatue = 31,
        [BaseStructure(BaseStructureType.Furniture, "Gnomish Contraption", "plc_gnomcntrptn", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        GnomishContraption = 32,
        [BaseStructure(BaseStructureType.Furniture, "Pillar, Stone", "plc_pillar3", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        PillarStone = 33,
        [BaseStructure(BaseStructureType.Furniture, "Statue of Lathander", "x2_plc_statue_la", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        StatueofLathander = 35,
        [BaseStructure(BaseStructureType.Furniture, "Pillar, Flame", "plc_pillar1", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        PillarFlame = 36,
        [BaseStructure(BaseStructureType.Furniture, "Cage", "plc_animalcage", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        Cage = 37,
        [BaseStructure(BaseStructureType.Furniture, "Lamp Post", "plc_lamppost", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        LampPost = 38,
        [BaseStructure(BaseStructureType.Furniture, "Torch Bracket", "plc_freetorch", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        TorchBracket = 39,
        [BaseStructure(BaseStructureType.Furniture, "Pillar, Wood", "plc_pillar2", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        PillarWood = 40,
        [BaseStructure(BaseStructureType.Furniture, "Statue, Cyric", "x3_plc_statuec", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        StatueCyric = 41,
        [BaseStructure(BaseStructureType.Furniture, "Sea Idol", "nw_plc_seaidol", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        SeaIdol = 42,
        [BaseStructure(BaseStructureType.Furniture, "Fountain", "plc_fountain", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        Fountain = 43,
        [BaseStructure(BaseStructureType.Furniture, "Monster Statue", "x2_plc_statue_mo", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        MonsterStatue = 44,
        [BaseStructure(BaseStructureType.Furniture, "Easel", "plc_pntingeasel", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        Easel = 45,
        [BaseStructure(BaseStructureType.Furniture, "Keg", "plc_keg", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        Keg = 47,
        [BaseStructure(BaseStructureType.Furniture, "Dran Statue", "x2_plc_statu_dra", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        DranStatue = 48,
        [BaseStructure(BaseStructureType.Furniture, "Net", "nw_plc_net", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        Net = 51,
        [BaseStructure(BaseStructureType.Furniture, "Bed, Extra Large", "x0_largebed", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        BedExtraLarge = 52,
        [BaseStructure(BaseStructureType.Furniture, "Carpet, Round, Blue", "x0_roundrugorien", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        CarpetRoundBlue = 54,
        [BaseStructure(BaseStructureType.Furniture, "Altar, Evil", "plc_altrevil", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        AltarEvil = 55,
        [BaseStructure(BaseStructureType.Furniture, "Table, Wood, Large", "x3_plc_table002", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        TableWoodLarge = 56,
        [BaseStructure(BaseStructureType.Furniture, "Table, Wood, With Fish", "x3_plc_table001", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        TableWoodWithFish = 57,
        [BaseStructure(BaseStructureType.Furniture, "Table, Stone, Small", "nw_plc_seatable", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        TableStoneSmall = 58,
        [BaseStructure(BaseStructureType.Furniture, "Altar, Hand", "plc_altrod", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        AltarHand = 59,
        [BaseStructure(BaseStructureType.Furniture, "Window", "x0_window", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        Window = 60,
        [BaseStructure(BaseStructureType.Furniture, "Cushions", "x0_cushions", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        Cushions = 61,
        [BaseStructure(BaseStructureType.Furniture, "Candle", "nw_plc_candle1", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        Candle = 62,
        [BaseStructure(BaseStructureType.Furniture, "Bear Skin Rug", "x0_bearskinrug1", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        BearSkinRug = 64,
        [BaseStructure(BaseStructureType.Furniture, "Chandelier", "x0_chandelier", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        Chandelier = 65,
        [BaseStructure(BaseStructureType.Furniture, "Urn", "plc_urn", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        Urn = 67,
        [BaseStructure(BaseStructureType.Furniture, "Altar, Stone", "plc_altrneutral", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        AltarStone = 68,
        [BaseStructure(BaseStructureType.Furniture, "Cot", "plc_cot", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        Cot = 69,
        [BaseStructure(BaseStructureType.Furniture, "Table, Wood", "plc_table", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        TableWood = 70,
        [BaseStructure(BaseStructureType.Furniture, "Throw Rug", "plc_throwrug", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        ThrowRug = 72,
        [BaseStructure(BaseStructureType.Furniture, "Table, Stone, Large", "nw_plc_dwarftabl", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        TableStoneLarge = 73,
        [BaseStructure(BaseStructureType.Furniture, "Bed, Stone, Yellow", "nw_plc_dwarfbed", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        BedStoneYellow = 74,
        [BaseStructure(BaseStructureType.Furniture, "Bed, Large", "x0_beddouble", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        BedLarge = 75,
        [BaseStructure(BaseStructureType.Furniture, "Vase, Rounded", "x0_vaseflower", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        VaseRounded = 76,
        [BaseStructure(BaseStructureType.Furniture, "Carpet", "x0_ruglarge", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        Carpet = 77,
        [BaseStructure(BaseStructureType.Furniture, "Bed, Wood, Yellow", "plc_bed", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        BedWoodYellow = 78,
        [BaseStructure(BaseStructureType.Furniture, "Overgrown Pillar", "x0_overgrownrui", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        OvergrownPillar = 79,
        [BaseStructure(BaseStructureType.Furniture, "Tome", "x0_tome", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        Tome = 80,
        [BaseStructure(BaseStructureType.Furniture, "Bird Cage", "plc_birdcage", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        BirdCage = 81,
        [BaseStructure(BaseStructureType.Furniture, "Pillar, Wood, Dark", "x3_plc_pillar1", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        PillarWoodDark = 82,
        [BaseStructure(BaseStructureType.Furniture, "Bunk Bed", "x2_plc_bunkbed", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        BunkBed = 83,
        [BaseStructure(BaseStructureType.Furniture, "Vase, Tall", "x0_bigvase", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        VaseTall = 84,
        [BaseStructure(BaseStructureType.Furniture, "Bed Roll", "plc_bedrolls", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        BedRoll = 85,
        [BaseStructure(BaseStructureType.Furniture, "Ottoman", "x0_ottoman", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        Ottoman = 86,
        [BaseStructure(BaseStructureType.Furniture, "Pillar, Rounded", "x0_ruinedpillar", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        PillarRounded = 88,
        [BaseStructure(BaseStructureType.Furniture, "Painting 2", "x0_painting2", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        Painting2 = 89,
        [BaseStructure(BaseStructureType.Furniture, "Candelabra", "plc_candelabra", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        Candelabra = 90,
        [BaseStructure(BaseStructureType.Furniture, "Potted Plant", "plc_pottedplant", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        PottedPlant = 91,
        [BaseStructure(BaseStructureType.Furniture, "Painting 1", "x0_painting", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        Painting1 = 92,
        [BaseStructure(BaseStructureType.Furniture, "Carpet, Fancy", "x0_rugoriental", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        CarpetFancy = 93,
        [BaseStructure(BaseStructureType.Furniture, "Illithid Table", "x2_plc_tablemind", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        IllithidTable = 95,
        [BaseStructure(BaseStructureType.Furniture, "Carpet, Fancy, Smaller", "x0_rugoriental2", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        CarpetFancySmaller = 96,
        [BaseStructure(BaseStructureType.Furniture, "Drow Altar", "x2_plc_drowaltar", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        DrowAltar = 97,
        [BaseStructure(BaseStructureType.Furniture, "Dartboard", "x3_plc_dartbrd", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        Dartboard = 98,
        [BaseStructure(BaseStructureType.Furniture, "Map", "x0_maps", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        Map = 99,
        [BaseStructure(BaseStructureType.Furniture, "Floor-anchored shackles", "plc_flrshackles", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        Flooranchoredshackles = 100,
        [BaseStructure(BaseStructureType.Furniture, "Round Wooden Table", "x2_plc_tablernd", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        RoundWoodenTable = 101,
        [BaseStructure(BaseStructureType.Furniture, "Drow Bar", "x2_plc_drowbar", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        DrowBar = 102,
        [BaseStructure(BaseStructureType.Furniture, "Shrine of Umberlee", "nw_plc_shrnumbr1", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        ShrineofUmberlee = 103,
        [BaseStructure(BaseStructureType.Furniture, "Rune Pillar", "x0_runepillar", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        RunePillar = 104,
        [BaseStructure(BaseStructureType.Furniture, "Mirror", "x2_plc_mirror", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        Mirror = 106,
        [BaseStructure(BaseStructureType.Furniture, "Footstool", "plc_footstool", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        Footstool = 107,
        [BaseStructure(BaseStructureType.Furniture, "Drow Table", "x2_plc_tabledrow", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        DrowTable = 108,
        [BaseStructure(BaseStructureType.Furniture, "Bench, Stone, Dwarven", "bench_stonedwarf", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        BenchStoneDwarven = 111,
        [BaseStructure(BaseStructureType.Furniture, "Illithid Chair", "illithid_chair", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        IllithidChair = 112,
        [BaseStructure(BaseStructureType.Furniture, "Bench, Wood", "bench_wood", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        BenchWood = 113,
        [BaseStructure(BaseStructureType.Furniture, "Chair, Wood, Medium", "chair_med_wood", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        ChairWoodMedium = 114,
        [BaseStructure(BaseStructureType.Furniture, "Bench, Large", "bench_large", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        BenchLarge = 115,
        [BaseStructure(BaseStructureType.Furniture, "Drow Chair", "drow_chair", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        DrowChair = 116,
        [BaseStructure(BaseStructureType.Furniture, "Bench, Wood, Large", "bench_largewood", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        BenchWoodLarge = 117,
        [BaseStructure(BaseStructureType.Furniture, "Bench, Wood, Small", "bench_woodsmall", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        BenchWoodSmall = 118,
        [BaseStructure(BaseStructureType.Furniture, "Chair, Wood", "chair_wood", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        ChairWood = 119,
        [BaseStructure(BaseStructureType.Furniture, "Chair, Wood, Small", "chair_wood_small", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        ChairWoodSmall = 120,
        [BaseStructure(BaseStructureType.Furniture, "Throne, Wood", "thronewood001", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        ThroneWood = 121,
        [BaseStructure(BaseStructureType.Furniture, "Chair, Stone", "chair_stone", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        ChairStone = 122,
        [BaseStructure(BaseStructureType.Furniture, "Bench, Wood, Small 2", "bench_wood2", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        BenchWoodSmall2 = 123,
        [BaseStructure(BaseStructureType.Furniture, "Chair, Shell", "chair_shell", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        ChairShell = 124,
        [BaseStructure(BaseStructureType.Furniture, "Couch, Wood, Yellow", "couch_wood", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        CouchWoodYellow = 125,
        [BaseStructure(BaseStructureType.PersistentStorage, "Barrel", "elm_barrel", "furniture", 1, 0, 0, 0, 10, 0, 0, 0, 0, 0, 0, StructureModeType.None)]
        Barrel = 126,
        [BaseStructure(BaseStructureType.PersistentStorage, "Crate", "elm_barrel001", "furniture", 1, 0, 0, 0, 20, 0, 0, 0, 0, 0, 0, StructureModeType.None)]
        Crate = 129,
        [BaseStructure(BaseStructureType.PersistentStorage, "Book Shelf", "elm_barrel002", "furniture", 1, 0, 0, 0, 15, 0, 0, 0, 0, 0, 0, StructureModeType.None)]
        BookShelf = 132,
        [BaseStructure(BaseStructureType.PersistentStorage, "Chest", "elm_barrel003", "furniture", 1, 0, 0, 0, 30, 0, 0, 0, 0, 0, 0, StructureModeType.None)]
        Chest = 133,
        [BaseStructure(BaseStructureType.PersistentStorage, "Desk", "elm_barrel004", "furniture", 1, 0, 0, 0, 15, 0, 0, 0, 0, 0, 0, StructureModeType.None)]
        Desk = 136,
        [BaseStructure(BaseStructureType.CraftingDevice, "Refinery", "cft_forge", "furniture", 1, 0, 0, 0, 0, 1, 0, 1, 0, 0, 0, StructureModeType.None)]
        Refinery = 146,
        [BaseStructure(BaseStructureType.CraftingDevice, "Armorsmith Workbench", "armorsmith_bench", "furniture", 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, StructureModeType.None)]
        ArmorsmithWorkbench = 147,
        [BaseStructure(BaseStructureType.CraftingDevice, "Weaponsmith Bench", "weapon_bench", "furniture", 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, StructureModeType.None)]
        WeaponsmithBench = 148,
        [BaseStructure(BaseStructureType.CraftingDevice, "Cookpot", "cookpot", "furniture", 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, StructureModeType.None)]
        Cookpot = 149,
        [BaseStructure(BaseStructureType.CraftingDevice, "Engineering Bench", "engineer_bench", "furniture", 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, StructureModeType.None)]
        EngineeringBench = 150,
        [BaseStructure(BaseStructureType.Furniture, "Mysterious Obelisk", "myst_obelisk", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        MysteriousObelisk = 152,
        [BaseStructure(BaseStructureType.Building, "Small Building", "", "building", 1, 5, 2, 5, 55, 0, 0, 1, 0, 0, 0, StructureModeType.Residence)]
        SmallBuilding = 153,
        [BaseStructure(BaseStructureType.Building, "Medium Building", "", "building", 1, 11, 5, 15, 65, 0, 0, 1, 0, 0, 0, StructureModeType.Residence)]
        MediumBuilding = 154,
        [BaseStructure(BaseStructureType.Building, "Large Building", "", "building", 1, 17, 10, 25, 75, 0, 0, 1, 0, 0, 0, StructureModeType.Residence)]
        LargeBuilding = 155,
        [BaseStructure(BaseStructureType.StronidiumSilo, "Basic Stronidium Silo", "stron_silo", "silo", 1, 5, 2, 5, 5, 0, 0, 1, 0, 0, 0, StructureModeType.None)]
        BasicStronidiumSilo = 156,
        [BaseStructure(BaseStructureType.StronidiumSilo, "Stronidium Silo I", "stron_silo", "silo", 1, 9, 3, 5, 10, 0, 0, 1, 0, 0, 0, StructureModeType.None)]
        StronidiumSiloI = 157,
        [BaseStructure(BaseStructureType.StronidiumSilo, "Stronidium Silo II", "stron_silo", "silo", 1, 14, 4, 5, 15, 0, 0, 1, 0, 0, 0, StructureModeType.None)]
        StronidiumSiloII = 158,
        [BaseStructure(BaseStructureType.StronidiumSilo, "Stronidium Silo III", "stron_silo", "silo", 1, 18, 5, 5, 20, 0, 0, 1, 0, 0, 0, StructureModeType.None)]
        StronidiumSiloIII = 159,
        [BaseStructure(BaseStructureType.StronidiumSilo, "Stronidium Silo IV", "stron_silo", "silo", 1, 24, 6, 5, 25, 0, 0, 1, 0, 0, 0, StructureModeType.None)]
        StronidiumSiloIV = 160,
        [BaseStructure(BaseStructureType.FuelSilo, "Basic Fuel Silo", "fuel_silo", "silo", 1, 5, 2, 5, 5, 0, 0, 1, 0, 0, 0, StructureModeType.None)]
        BasicFuelSilo = 161,
        [BaseStructure(BaseStructureType.FuelSilo, "Fuel Silo I", "fuel_silo", "silo", 1, 9, 3, 5, 10, 0, 0, 1, 0, 0, 0, StructureModeType.None)]
        FuelSiloI = 162,
        [BaseStructure(BaseStructureType.FuelSilo, "Fuel Silo II", "fuel_silo", "silo", 1, 14, 4, 5, 15, 0, 0, 1, 0, 0, 0, StructureModeType.None)]
        FuelSiloII = 163,
        [BaseStructure(BaseStructureType.FuelSilo, "Fuel Silo III", "fuel_silo", "silo", 1, 18, 5, 5, 20, 0, 0, 1, 0, 0, 0, StructureModeType.None)]
        FuelSiloIII = 164,
        [BaseStructure(BaseStructureType.FuelSilo, "Fuel Silo IV", "fuel_silo", "silo", 1, 24, 6, 5, 25, 0, 0, 1, 0, 0, 0, StructureModeType.None)]
        FuelSiloIV = 165,
        [BaseStructure(BaseStructureType.ResourceSilo, "Basic Resource Silo", "resource_silo", "silo", 1, 5, 2, 5, 5, 0, 0, 1, 0, 0, 0, StructureModeType.None)]
        BasicResourceSilo = 166,
        [BaseStructure(BaseStructureType.ResourceSilo, "Resource Silo I", "resource_silo", "silo", 1, 9, 3, 5, 10, 0, 0, 1, 0, 0, 0, StructureModeType.None)]
        ResourceSiloI = 167,
        [BaseStructure(BaseStructureType.ResourceSilo, "Resource Silo II", "resource_silo", "silo", 1, 14, 4, 5, 15, 0, 0, 1, 0, 0, 0, StructureModeType.None)]
        ResourceSiloII = 168,
        [BaseStructure(BaseStructureType.ResourceSilo, "Resource Silo III", "resource_silo", "silo", 1, 18, 5, 5, 20, 0, 0, 1, 0, 0, 0, StructureModeType.None)]
        ResourceSiloIII = 169,
        [BaseStructure(BaseStructureType.ResourceSilo, "Resource Silo IV", "resource_silo", "silo", 1, 24, 6, 5, 25, 0, 0, 1, 0, 0, 0, StructureModeType.None)]
        ResourceSiloIV = 170,
        [BaseStructure(BaseStructureType.Drill, "Basic Resource Drill", "resource_drill", "drill", 1, 25, 15, 5, 0, 0, 0, 1, 0, 1, 0, StructureModeType.None)]
        BasicResourceDrill = 171,
        [BaseStructure(BaseStructureType.Drill, "Resource Drill I", "resource_drill", "drill", 1, 30, 20, 5, 0, 0, 0, 1, 0, 2, 0, StructureModeType.None)]
        ResourceDrillI = 172,
        [BaseStructure(BaseStructureType.Drill, "Resource Drill II", "resource_drill", "drill", 1, 35, 25, 5, 0, 0, 0, 1, 0, 3, 0, StructureModeType.None)]
        ResourceDrillII = 173,
        [BaseStructure(BaseStructureType.Drill, "Resource Drill III", "resource_drill", "drill", 1, 40, 30, 5, 0, 0, 0, 1, 0, 4, 0, StructureModeType.None)]
        ResourceDrillIII = 174,
        [BaseStructure(BaseStructureType.Drill, "Resource Drill IV", "resource_drill", "drill", 1, 45, 35, 5, 0, 0, 0, 1, 0, 5, 0, StructureModeType.None)]
        ResourceDrillIV = 175,
        [BaseStructure(BaseStructureType.CraftingDevice, "Fabrication Terminal", "fabrication_term", "furniture", 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, StructureModeType.None)]
        FabricationTerminal = 176,
        [BaseStructure(BaseStructureType.CraftingDevice, "Medical Terminal", "medical_term", "furniture", 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, StructureModeType.None)]
        MedicalTerminal = 177,
        [BaseStructure(BaseStructureType.Furniture, "Wookiee Rug", "wookiee_rug", "furniture", 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, StructureModeType.None)]
        WookieeRug = 178,
        [BaseStructure(BaseStructureType.StarshipProduction, "Starship Dock", "starship_dock", "silo", 1, 30, 5, 15, 0, 0, 0, 1, 0, 0, 0, StructureModeType.None)]
        StarshipDock = 179,
        [BaseStructure(BaseStructureType.Starship, "Starship: Light Freighter 1", "starship1", "starship", 1, 0, 0, 50, 400, 0, 100, 0, 30, 0, 0, StructureModeType.None)]
        StarshipLightFreighter1 = 180,
        [BaseStructure(BaseStructureType.Starship, "Starship: Light Escort 1", "starship2", "starship", 1, 0, 0, 50, 400, 0, 300, 0, 10, 0, 0, StructureModeType.None)]
        StarshipLightEscort1 = 181,
    }


    public class BaseStructureAttribute : Attribute
    {
        public BaseStructureType BaseStructureType { get; set; }
        public string Name { get; set; }
        public string PlaceableResref { get; set; }
        public string ItemResref { get; set; }
        public bool IsActive { get; set; }
        public int Power { get; set; }
        public int CPU { get; set; }
        public int Durability { get; set; }
        public int Storage { get; set; }
        public bool HasAtmosphere { get; set; }
        public int ReinforcedStorage { get; set; }
        public bool RequiresBasePower { get; set; }
        public int ResourceStorage { get; set; }
        public int RetrievalRating { get; set; }
        public int FuelRating { get; set; }
        public StructureModeType DefaultStructureMode { get; set; }

        public BaseStructureAttribute(
            BaseStructureType baseStructureType, 
            string name, 
            string placeableResref, 
            string itemResref, 
            bool isActive, 
            int power, 
            int cpu,
            int durability, 
            int storage, 
            bool hasAtmosphere, 
            int reinforcedStorage, 
            bool requiresBasePower, 
            int resourceStorage, 
            int retrievalRating,
            int fuelRating, 
            StructureModeType defaultStructureMode)
        {
            BaseStructureType = baseStructureType;
            Name = name;
            PlaceableResref = placeableResref;
            ItemResref = itemResref;
            IsActive = isActive;
            Power = power;
            CPU = cpu;
            Durability = durability;
            Storage = storage;
            HasAtmosphere = hasAtmosphere;
            ReinforcedStorage = reinforcedStorage;
            RequiresBasePower = requiresBasePower;
            ResourceStorage = resourceStorage;
            RetrievalRating = retrievalRating;
            FuelRating = fuelRating;
            DefaultStructureMode = defaultStructureMode;
        }
    }
}
