using System;

namespace SWLOR.Game.Server.Enumeration
{
    public enum KeyItemType
    {
        [KeyItem(KeyItemCategoryType.Invalid, "Invalid", false)]
        Invalid = 0,
        [KeyItem(KeyItemCategoryType.Invalid, "Avix Tatham's Work Receipt", true)]
        AvixTathamsWorkReceipt = 1,
        [KeyItem(KeyItemCategoryType.Invalid, "Halron Linth's Work Receipt", true)]
        HalronLinthsWorkReceipt = 2,
        [KeyItem(KeyItemCategoryType.Invalid, "Crafting Terminal Droid Operator's Work Receipt", true)]
        CraftingTerminalDroidOperatorsWorkReceipt = 3,
        [KeyItem(KeyItemCategoryType.Invalid, "Crafting Terminal Droid Operator's Work Order", true)]
        CraftingTerminalDroidOperatorsWorkOrder = 4,
        [KeyItem(KeyItemCategoryType.Invalid, "CZ-220 Shuttle Pass", true)]
        CZ220ShuttlePass = 5,
        [KeyItem(KeyItemCategoryType.Invalid, "CZ-220 Experiment Room Key", true)]
        CZ220ExperimentRoomKey = 6,
        [KeyItem(KeyItemCategoryType.Invalid, "Mandalorian Facility Key", true)]
        MandalorianFacilityKey = 7,
        [KeyItem(KeyItemCategoryType.Invalid, "Yellow Key Card", true)]
        YellowKeyCard = 8,
        [KeyItem(KeyItemCategoryType.Invalid, "Red Key Card", true)]
        RedKeyCard = 9,
        [KeyItem(KeyItemCategoryType.Invalid, "Blue Key Card", true)]
        BlueKeyCard = 10,
        [KeyItem(KeyItemCategoryType.Invalid, "Slicing Program", true)]
        SlicingProgram = 11,
        [KeyItem(KeyItemCategoryType.Invalid, "Data Disc #1", true)]
        DataDisc1 = 12,
        [KeyItem(KeyItemCategoryType.Invalid, "Data Disc #2", true)]
        DataDisc2 = 13,
        [KeyItem(KeyItemCategoryType.Invalid, "Data Disc #3", true)]
        DataDisc3 = 14,
        [KeyItem(KeyItemCategoryType.Invalid, "Data Disc #4", true)]
        DataDisc4 = 15,
        [KeyItem(KeyItemCategoryType.Invalid, "Data Disc #5", true)]
        DataDisc5 = 16,
        [KeyItem(KeyItemCategoryType.Invalid, "Data Disc #6", true)]
        DataDisc6 = 17,
        [KeyItem(KeyItemCategoryType.Invalid, "Package for Denam Reyholm", true)]
        PackageForDenamReyholm = 18,
        [KeyItem(KeyItemCategoryType.Invalid, "Old Tome", true)]
        OldTome = 19,
        [KeyItem(KeyItemCategoryType.Invalid, "Coxxion Base Key", true)]
        CoxxionBaseKey = 20,
    }

    public class KeyItemAttribute : Attribute
    {
        public string Name { get; set; }
        public KeyItemCategoryType Category { get; set; }
        public bool IsActive { get; set; }

        public KeyItemAttribute(KeyItemCategoryType category, string name, bool isActive)
        {
            Category = category;
            Name = name;
            IsActive = isActive;
        }
    }
}
