using System;

namespace SWLOR.Game.Server.Service.KeyItemService
{
    public enum KeyItemType
    {
        [KeyItem(KeyItemCategoryType.Invalid, "Invalid", false, "")]
        Invalid = 0,
        [KeyItem(KeyItemCategoryType.QuestItems, "Avix Tatham's Work Receipt", true, "You received this work receipt from Avix Tatham, mining coordinator on CZ-220.")]
        AvixTathamsWorkReceipt = 1,
        [KeyItem(KeyItemCategoryType.QuestItems, "Halron Linth's Work Receipt", true, "You received this work receipt from Halron Linth, security officer on CZ-220.")]
        HalronLinthsWorkReceipt = 2,
        [KeyItem(KeyItemCategoryType.QuestItems, "Crafting Terminal Droid Operator's Work Receipt", true, "You received this work receipt from the Crafting Terminal Droid Operator on CZ-220.")]
        CraftingTerminalDroidOperatorsWorkReceipt = 3,
        [KeyItem(KeyItemCategoryType.QuestItems, "Crafting Terminal Droid Operator's Work Order", true, "This is a work order you received from the droid responsible for item construction on CZ-220. Obtain the item(s) requested and return them to him.")]
        CraftingTerminalDroidOperatorsWorkOrder = 4,
        [KeyItem(KeyItemCategoryType.Keys, "CZ-220 Shuttle Pass", true, "This shuttle pass enables you to travel between CZ-220 and planet Viscara.")]
        CZ220ShuttlePass = 5,
        [KeyItem(KeyItemCategoryType.Keys, "CZ-220 Experiment Room Key", true, "This unlocks the door leading to the experiment room, where the Colicoid should be located.")]
        CZ220ExperimentRoomKey = 6,
        [KeyItem(KeyItemCategoryType.Keys, "Mandalorian Facility Key", true, "This key unlocks the door to the Mandalorian facility in the Viscara Wildlands.")]
        MandalorianFacilityKey = 7,
        [KeyItem(KeyItemCategoryType.Keys, "Yellow Key Card", true, "This yellow key card can be used somewhere in the Mandalorian facility on Viscara.")]
        YellowKeyCard = 8,
        [KeyItem(KeyItemCategoryType.Keys, "Red Key Card", true, "This red key card can be used somewhere in the Mandalorian facility on Viscara.")]
        RedKeyCard = 9,
        [KeyItem(KeyItemCategoryType.Keys, "Blue Key Card", true, "This blue key card can be used somewhere in the Mandalorian facility on Viscara.")]
        BlueKeyCard = 10,
        [KeyItem(KeyItemCategoryType.QuestItems, "Slicing Program", true, "A data disc with a program used to slice the terminals in the Mandalorian facility.")]
        SlicingProgram = 11,
        [KeyItem(KeyItemCategoryType.QuestItems, "Data Disc #1", true, "The first disc containing data on the Mandalorian Facility.")]
        DataDisc1 = 12,
        [KeyItem(KeyItemCategoryType.QuestItems, "Data Disc #2", true, "The second disc containing data on the Mandalorian Facility.")]
        DataDisc2 = 13,
        [KeyItem(KeyItemCategoryType.QuestItems, "Data Disc #3", true, "The third disc containing data on the Mandalorian Facility.")]
        DataDisc3 = 14,
        [KeyItem(KeyItemCategoryType.QuestItems, "Data Disc #4", true, "The fourth disc containing data on the Mandalorian Facility.")]
        DataDisc4 = 15,
        [KeyItem(KeyItemCategoryType.QuestItems, "Data Disc #5", true, "The fifth disc containing data on the Mandalorian Facility.")]
        DataDisc5 = 16,
        [KeyItem(KeyItemCategoryType.QuestItems, "Data Disc #6", true, "The sixth disc containing data on the Mandalorian Facility.")]
        DataDisc6 = 17,
        [KeyItem(KeyItemCategoryType.QuestItems, "Package for Denam Reyholm", true, "Roy Moss gave you this package to deliver to Denam Reyholm.")]
        PackageForDenamReyholm = 18,
        [KeyItem(KeyItemCategoryType.Documents, "Old Tome", true, "A man known only as \"L\" gave you this tome. It's very old and the words have faded.")]
        OldTome = 19,
        [KeyItem(KeyItemCategoryType.Keys, "Coxxion Base Key", true, "This key will unlock the doors to the Coxxion base located in the deep mountains of Viscara.")]
        CoxxionBaseKey = 20,
        [KeyItem(KeyItemCategoryType.Keys, "Taxi Hailing Device", true, "This device will enable you to call upon a taxi to quickly transport you across a region.")]
        TaxiHailingDevice = 21,

        [KeyItem(KeyItemCategoryType.Maps, "CZ-220 - Maintenance Level Map", true, "Map of the CZ-220 Maintenance Level.")]
        CZ220MaintenanceLevelMap = 22,
        [KeyItem(KeyItemCategoryType.Maps, "CZ-220 - Offices & Labs Map", true, "Map of the CZ-220 Offices & Labs.")]
        CZ220OfficesAndLabsMap = 23,
        [KeyItem(KeyItemCategoryType.Maps, "Hutlar - Outpost Map", true, "Map of the Hutlar Outpost.")]
        HutlarOutpostMap = 24,
        [KeyItem(KeyItemCategoryType.Maps, "Hutlar - Qion Tundra Map", true, "Map of Qion Tundra.")]
        QionTundraMap = 25,
        [KeyItem(KeyItemCategoryType.Maps, "Hutlar - Qion Valley Map", true, "Map of Qion Valley.")]
        QionValleyMap = 26,
        [KeyItem(KeyItemCategoryType.Maps, "Mon Cala - Coral Isles Facility Map", true, "Map of the Coral Isles Facility.")]
        CoralIslesFacilityMap = 27,
        [KeyItem(KeyItemCategoryType.Maps, "Mon Cala - Coral Isles Inner Map", true, "Map of the Inner Coral Isles.")]
        CoralIslesInnerMap = 28,
        [KeyItem(KeyItemCategoryType.Maps, "Mon Cala - Coral Isles Outer Map", true, "Map of the Outer Coral Isles.")]
        CoralIslesOuterMap = 29,
        [KeyItem(KeyItemCategoryType.Maps, "Mon Cala - The 'Elite' Hotel Map", true, "Map of the 'Elite' Hotel.")]
        EliteHotelMap = 30,

        [KeyItem(KeyItemCategoryType.Maps, "Hutlar Orbit Map", true, "Map of the space surrounding Hutlar.")]
        HutlarOrbitMap = 31,
        [KeyItem(KeyItemCategoryType.Maps, "Mon Cala Orbit Map", true, "Map of the space surrounding Mon Cala.")]
        MonCalaOrbitMap = 32,
        [KeyItem(KeyItemCategoryType.Maps, "Tatooine Orbit Map", true, "Map of the space surrounding Tatooine.")]
        TatooineOrbitMap = 33,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara Orbit Map", true, "Map of the space surrounding Viscara and CZ-220.")]
        ViscaraOrbitMap = 34,

        [KeyItem(KeyItemCategoryType.Maps, "Tatooine - Anchorhead Map", true, "Map of Anchorhead.")]
        AnchorheadMap = 35,
        [KeyItem(KeyItemCategoryType.Maps, "Tatooine - Desert Map", true, "Map of the Tatooine deserts.")]
        TatooineDesertMap = 36,
        [KeyItem(KeyItemCategoryType.Maps, "Tatooine - Mos Esper Map", true, "Map of Mos Esper.")]
        MosEsperMap = 37,
        [KeyItem(KeyItemCategoryType.Maps, "Tatooine - Tusken Raider Cave Map", true, "Map of Tusken Raider cave.")]
        TuskenRaiderCaveMap = 38,

        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Cavern Map", true, "Map of Viscara caverns.")]
        ViscaraCavernMap = 39,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Veles Colony Map", true, "Map of Veles Colony.")]
        VelesColonyMap = 40,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Coxxion Base Map", true, "Map of the Coxxion Base.")]
        CoxxionBaseMap = 41,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Deep Mountains Map", true, "Map of the Deep Mountains.")]
        DeepMountainsMap = 42,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Deepwoods Map", true, "Map of the Deepwoods.")]
        DeepwoodsMap = 43,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Druzer Map", true, "Map of Druzer.")]
        DruzerMap = 44,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Swamplands Map", true, "Map of Swamplands.")]
        ViscaraSwamplandsMap = 45,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Jedi Temple Map", true, "Map of the Viscara Jedi Temple.")]
        ViscaraJediTempleMap = 46,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Wildlands Map", true, "Map of the Wildlands.")]
        WildlandsMap = 47,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Mandalorian Facility Map", true, "Map of the Mandalorian Facility.")]
        MandalorianFacilityMap = 48,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Mountain Valley Map", true, "Map of the Mountain Valley.")]
        MountainValleyMap = 49,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Republic Base Map", true, "Map of the Viscara Republic Base.")]
        ViscaraRepublicBaseMap = 50,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Sith Lake Outpost Map", true, "Map of the Viscara Sith Lake Outpost.")]
        SithLakeOutpostMap = 51,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Sewers Map", true, "Map of the Viscara Sewers.")]
        ViscaraSewersMap = 52,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Wildwoods Map", true, "Map of the Wildwoods.")]
        WildwoodsMap = 53,

        [KeyItem(KeyItemCategoryType.Maps, "Korriban Orbit Map", true, "Map of the space surrounding Korriban.")]
        KorribanOrbitMap = 54,
        [KeyItem(KeyItemCategoryType.Maps, "Korriban - Wastelands Map", true, "Map of the wastelands on Korriban.")]
        KorribanWastelandsMap = 55,
        [KeyItem(KeyItemCategoryType.Maps, "Korriban - Sith Crypt Map", true, "Map of the Sith crypt on Korriban.")]
        KorribanSithCryptMap = 56,
        [KeyItem(KeyItemCategoryType.Maps, "Korriban - Caverns Map", true, "Map of the caverns on Korriban.")]
        KorribanCavernsMap = 57,

        [KeyItem(KeyItemCategoryType.Maps, "Mon Cala - Sunkenhead Swamps Map", true, "Map of the Sunkenhead Swamps on Mon Cala.")]
        MonCalaSunkenhedgeSwampsMap = 58,
        [KeyItem(KeyItemCategoryType.Maps, "Mon Cala - Sharptooth Jungles Map", true, "Map of the Sharptooth Jungles on Mon Cala.")]
        MonCalaSharptoothJunglesMap = 59,
        [KeyItem(KeyItemCategoryType.Maps, "Mon Cala - Sharptooth Caverns Map", true, "Map of the Sharptooth Caverns on Mon Cala.")]
        MonCalaSharptoothCavernsMap = 60,

        [KeyItem(KeyItemCategoryType.Maps, "Dathomir Orbit Map", true, "Map of the space surrounding Dathomir.")]
        DathomirOrbitMap = 61,
        [KeyItem(KeyItemCategoryType.Maps, "Dathomir - Cave Ruins Map", true, "Map of the Cave Ruins on Dathomir.")]
        DathomirCaveRuinsMap = 62,
        [KeyItem(KeyItemCategoryType.Maps, "Dathomir - Desert Map", true, "Map of the Deserts on Dathomir.")]
        DathomirDesertMap = 63,
        [KeyItem(KeyItemCategoryType.Maps, "Dathomir - Grottos Map", true, "Map of the Grottos on Dathomir.")]
        DathomirGrottosMap = 64,
        [KeyItem(KeyItemCategoryType.Maps, "Dathomir - Grotto Caverns Map", true, "Map of the Grotto Caverns on Dathomir.")]
        DathomirGrottoCavernsMap = 65,
        [KeyItem(KeyItemCategoryType.Maps, "Dathomir - Jungles Map", true, "Map of the Jungles on Dathomir.")]
        DathomirJunglesMap = 66,
        [KeyItem(KeyItemCategoryType.Maps, "Dathomir - Mountains Map", true, "Map of the Mountain region on Dathomir.")]
        DathomirMountainsMap = 67,
        [KeyItem(KeyItemCategoryType.Maps, "Dathomir - Ruins Base Map", true, "Map of the Ruins Base on Dathomir.")]
        DathomirRuinsBaseMap = 68,
        [KeyItem(KeyItemCategoryType.Maps, "Dathomir - Tribes Village Map", true, "Map of the Tribes Village on Dathomir.")]
        DathomirTribeVillageMap = 69,

        [KeyItem(KeyItemCategoryType.Maps, "Dantooine Orbit Map", true, "Map of the space surrounding Dantooine.")]
        DantooineOrbitMap = 70,
        [KeyItem(KeyItemCategoryType.Maps, "Dantooine - Lake Map", true, "Map of the Lake on Dantooine.")]
        DantooineLakeMap = 71,
        [KeyItem(KeyItemCategoryType.Maps, "Dantooine - Kinrath Cave Map", true, "Map of the Kinrath Tunnels on Dantooine.")]
        DantooineKinrathMap = 72,
        [KeyItem(KeyItemCategoryType.Maps, "Dantooine - Tribal Map", true, "Map of the South Plains Tribes on Dantooine.")]
        DantooineTribalMap = 73,
        [KeyItem(KeyItemCategoryType.Maps, "Dantooine - Forsaken Jungles Map", true, "Map of the Forsaken Jungles Caverns on Dantooine.")]
        DantooineForsakenJungleMap = 74,
        [KeyItem(KeyItemCategoryType.Maps, "Dantooine - Mountain Jungles Map", true, "Map of the Mountain Jungles on Dantooine.")]
        DantooineMountainJunglesMap = 75,
        [KeyItem(KeyItemCategoryType.Maps, "Dantooine - Crystal Cave Map", true, "Map of the Crystal Cave on Dantooine.")]
        DantooineCrystalCaveMap = 76,
        [KeyItem(KeyItemCategoryType.Maps, "Dantooine - Abandoned Warehouse Map", true, "Map of the Abandoned Warehouse Base on Dantooine.")]
        DantooineWarehouseMap = 77,
        [KeyItem(KeyItemCategoryType.Maps, "Dantooine - Canyon River Map", true, "Map of the Canyon Rivers on Dantooine.")]
        DantooineCanyonRiverMap = 78,
    }

    public class KeyItemAttribute : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public KeyItemCategoryType Category { get; set; }
        public bool IsActive { get; set; }

        public KeyItemAttribute(KeyItemCategoryType category, string name, bool isActive, string description)
        {
            Category = category;
            Name = name;
            IsActive = isActive;
            Description = description;
        }
    }
}
