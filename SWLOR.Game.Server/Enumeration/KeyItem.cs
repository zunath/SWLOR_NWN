using System;

namespace SWLOR.Game.Server.Enumeration
{
    public enum KeyItem
    {
        [KeyItem("Invalid", "Invalid", KeyItemCategoryType.Unknown)]
        Invalid = 0,
        [KeyItem("Avix Tatham's Work Receipt", "You received this work receipt from Avix Tatham, mining coordinator on CZ-220.", KeyItemCategoryType.QuestItems)]
        AvixTathamWorkReceipt = 1,
        [KeyItem("Halron Linth's Work Receipt", "You received this work receipt from Halron Linth, security officer on CZ-220.", KeyItemCategoryType.QuestItems)]
        HalronLinthWorkReceipt = 2,
        [KeyItem("Crafting Terminal Droid Operator's Work Receipt", "You received this work receipt from the Crafting Terminal Droid Operator on CZ-220.", KeyItemCategoryType.QuestItems)]
        CraftingTerminalDroidOperatorWorkReceipt = 3,
        [KeyItem("Crafting Terminal Droid Operator's Work Order", "This is a work order you received from the droid responsible for item construction on CZ-220. Obtain the item(s) requested and return them to him.", KeyItemCategoryType.QuestItems)]
        CraftingTerminalDroidOperatorWorkOrder = 4,
        [KeyItem("CZ-220 Shuttle Pass", "This shuttle pass enables you to travel between CZ-220 and planet Viscara.", KeyItemCategoryType.QuestItems)]
        CZ220ShuttlePass = 5,
        [KeyItem("CZ-220 Experiment Room Key", "This unlocks the door leading to the experiment room, where the Colicoid should be located.", KeyItemCategoryType.Keys)]
        CZ220ExperimentRoomKey = 6,
        [KeyItem("Mandalorian Facility Key", "This key unlocks the door to the Mandalorian facility in the Viscara Wildlands.", KeyItemCategoryType.Keys)]
        MandalorianFacilityKey = 7,
        [KeyItem("Yellow Key Card", "This yellow key card can be used somewhere in the Mandalorian facility on Viscara.", KeyItemCategoryType.Keys)]
        YellowKeyCard = 8,
        [KeyItem("Red Key Card", "This red key card can be used somewhere in the Mandalorian facility on Viscara.", KeyItemCategoryType.Keys)]
        RedKeyCard = 9,
        [KeyItem("Blue Key Card", "This blue key card can be used somewhere in the Mandalorian facility on Viscara.", KeyItemCategoryType.Keys)]
        BlueKeyCard = 10,
        [KeyItem("Slicing Program", "A data disc with a program used to slice the terminals in the Mandalorian facility.", KeyItemCategoryType.QuestItems)]
        SlicingProgram = 11,
        [KeyItem("Data Disc #1", "The first disc containing data on the Mandalorian Facility.", KeyItemCategoryType.QuestItems)]
        DataDisc1 = 12,
        [KeyItem("Data Disc #2", "The second disc containing data on the Mandalorian Facility.", KeyItemCategoryType.QuestItems)]
        DataDisc2 = 13,
        [KeyItem("Data Disc #3", "The third disc containing data on the Mandalorian Facility.", KeyItemCategoryType.QuestItems)]
        DataDisc3 = 14,
        [KeyItem("Data Disc #4", "The fourth disc containing data on the Mandalorian Facility.", KeyItemCategoryType.QuestItems)]
        DataDisc4 = 15,
        [KeyItem("Data Disc #5", "The fifth disc containing data on the Mandalorian Facility.", KeyItemCategoryType.QuestItems)]
        DataDisc5 = 16,
        [KeyItem("Data Disc #6", "The sixth disc containing data on the Mandalorian Facility.", KeyItemCategoryType.QuestItems)]
        DataDisc6 = 17,
        [KeyItem("Package for Denam Reyholm", "Roy Moss gave you this package to deliver to Denam Reyholm.", KeyItemCategoryType.QuestItems)]
        PackageForDenamReyholm = 18,
        [KeyItem("Old Tome", "A man known only as 'L' gave you this tome. It's very old and the words have faded.", KeyItemCategoryType.Documents)]
        OldTome = 19,
        [KeyItem("Coxxion Base Key", "This key will unlock the doors to the Coxxion base located in the deep mountains of Viscara.", KeyItemCategoryType.Keys)]
        CoxxionBaseKey = 20,
    }

    public class KeyItemAttribute : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public KeyItemCategoryType Category { get; set; }

        public KeyItemAttribute(string name, string description, KeyItemCategoryType category)
        {
            Name = name;
            Description = description;
            Category = category;
        }
    }
}
