using System;

namespace SWLOR.Game.Server.Enumeration
{
    public enum ComponentType
    {
        [ComponentType("None", "")]
        None = -1,
        [ComponentType("Raw Ore", "")]
        RawOre = 1,
        [ComponentType("Metal", "ass_metal")]
        Metal = 2,
        [ComponentType("Organic", "ass_organic")]
        Organic = 3,
        [ComponentType("Small Blade", "")]
        SmallBlade = 4,
        [ComponentType("Medium Blade", "")]
        MediumBlade = 5,
        [ComponentType("Large Blade", "")]
        LargeBlade = 6,
        [ComponentType("Shaft", "")]
        Shaft = 7,
        [ComponentType("Small Handle", "")]
        SmallHandle = 8,
        [ComponentType("Medium Handle", "")]
        MediumHandle = 9,
        [ComponentType("Large Handle", "")]
        LargeHandle = 10,
        [ComponentType("Enhancement", "")]
        Enhancement = 11,
        [ComponentType("Fiberplast", "ass_fiberplast")]
        Fiberplast = 12,
        [ComponentType("Leather", "ass_leather")]
        Leather = 13,
        [ComponentType("Cloth", "")]
        Cloth = 14,
        [ComponentType("Electronics", "ass_electronics")]
        Electronics = 15,
        [ComponentType("Wood Baton Frame", "")]
        WoodBatonFrame = 16,
        [ComponentType("Metal Baton Frame", "")]
        MetalBatonFrame = 17,
        [ComponentType("Ranged Weapon Core", "")]
        RangedWeaponCore = 18,
        [ComponentType("Rifle Barrel", "")]
        RifleBarrel = 19,
        [ComponentType("Pistol Barrel", "")]
        PistolBarrel = 20,
        [ComponentType("Power Crystal", "ass_powcry")]
        PowerCrystal = 21,
        [ComponentType("Saber Hilt", "")]
        SaberHilt = 22,
        [ComponentType("Seeds", "")]
        Seeds = 23,
        [ComponentType("Blue Crystal", "ass_bluecry")]
        BlueCrystal = 24,
        [ComponentType("Red Crystal", "ass_redcry")]
        RedCrystal = 25,
        [ComponentType("Green Crystal", "ass_greencry")]
        GreenCrystal = 26,
        [ComponentType("Yellow Crystal", "ass_yellowcry")]
        YellowCrystal = 27,
        [ComponentType("Blue Cluster", "")]
        BlueCluster = 28,
        [ComponentType("Red Cluster", "")]
        RedCluster = 29,
        [ComponentType("Green Cluster", "")]
        GreenCluster = 30,
        [ComponentType("Yellow Cluster", "")]
        YellowCluster = 31,
        [ComponentType("Power Cluster", "")]
        PowerCluster = 32,
        [ComponentType("Heavy Armor Core", "")]
        HeavyArmorCore = 33,
        [ComponentType("Light Armor Core", "")]
        LightArmorCore = 34,
        [ComponentType("Force Armor Core", "")]
        ForceArmorCore = 35,
        [ComponentType("Heavy Armor Segment", "")]
        HeavyArmorSegment = 36,
        [ComponentType("Light Armor Segment", "")]
        LightArmorSegment = 37,
        [ComponentType("Force Armor Segment", "")]
        ForceArmorSegment = 38,
        [ComponentType("Small Structure Frame", "")]
        SmallStructureFrame = 39,
        [ComponentType("Medium Structure Frame", "")]
        MediumStructureFrame = 40,
        [ComponentType("Large Structure Frame", "")]
        LargeStructureFrame = 41,
        [ComponentType("Computing Module", "")]
        ComputingModule = 42,
        [ComponentType("Construction Parts", "")]
        ConstructionParts = 43,
        [ComponentType("Mainframe", "")]
        Mainframe = 44,
        [ComponentType("Power Relay", "")]
        PowerRelay = 45,
        [ComponentType("Power Core", "")]
        PowerCore = 46,
        [ComponentType("Ingredient", "")]
        Ingredient = 47,
        [ComponentType("Herb", "ass_herb")]
        Herb = 48,
        [ComponentType("Carbosyrup", "")]
        Carbosyrup = 49,
        [ComponentType("Meat", "")]
        Meat = 50,
        [ComponentType("Cereal", "")]
        Cereal = 51,
        [ComponentType("Grain", "")]
        Grain = 52,
        [ComponentType("Vegetable", "")]
        Vegetable = 53,
        [ComponentType("Water", "")]
        Water = 54,
        [ComponentType("Curry Paste", "")]
        CurryPaste = 55,
        [ComponentType("Soup", "")]
        Soup = 56,
        [ComponentType("Spiced Milk", "")]
        SpicedMilk = 57,
        [ComponentType("Dough", "")]
        Dough = 58,
        [ComponentType("Butter", "")]
        Butter = 59,
        [ComponentType("Noodles", "")]
        Noodles = 60,
        [ComponentType("Eggs", "")]
        Eggs = 61,
        [ComponentType("Emitter", "")]
        Emitter = 62,
        [ComponentType("Hyperdrive", "")]
        Hyperdrive = 63,
        [ComponentType("Hull Plating", "")]
        HullPlating = 64,
        [ComponentType("Starship Weapon", "")]
        StarshipWeapon = 65
    }

    public class ComponentTypeAttribute: Attribute
    {
        public string Name { get; set; }
        public string ReassembledResref { get; set; }

        public ComponentTypeAttribute(string name, string reassembledResref)
        {
            Name = name;
            ReassembledResref = reassembledResref;
        }
    }

}
