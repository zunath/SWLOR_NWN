using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SWLOR.Game.Server.Enumeration
{
    public enum MarketCategory
    {
        [Description("Invalid")]
        Invalid = 0,
        [Description("Heavy Vibroblade GA")]
        HeavyVibrobladeGA = 1,
        [Description("Vibroblade BA")]
        VibrobladeBA = 2,
        [Description("Vibroblade BS")]
        VibrobladeBS = 3,
        [Description("Finesse Vibroblade D")]
        FinesseVibrobladeD = 4,
        [Description("Heavy Vibroblade GS")]
        HeavyVibrobladeGS = 5,
        [Description("Lightsaber")]
        Lightsaber = 6,
        [Description("Vibroblade LS")]
        VibrobladeLS = 7,
        [Description("Finesse Vibroblade R")]
        FinesseVibrobladeR = 8,
        [Description("Vibroblade K")]
        VibrobladeK = 9,
        [Description("Vibroblade SS")]
        VibrobladeSS = 10,
        [Description("Baton C")]
        BatonC = 11,
        [Description("Baton M")]
        BatonM = 12,
        [Description("Baton MS")]
        BatonMS = 13,
        [Description("Saberstaff")]
        Saberstaff = 14,
        [Description("Quarterstaff")]
        Quarterstaff = 15,
        [Description("Twin Vibroblade DA")]
        TwinVibrobladeDA = 16,
        [Description("Twin Vibroblade TS")]
        TwinVibrobladeTS = 17,
        [Description("Finesse Vibroblade K")]
        FinesseVibrobladeK = 18,
        [Description("Polearm H")]
        PolearmH = 19,
        [Description("Polearm S")]
        PolearmS = 20,
        [Description("Blaster Rifle")]
        BlasterRifle = 21,
        [Description("Blaster Pistol")]
        BlasterPistol = 22,
        [Description("Clothing")]
        Clothing = 23,
        [Description("Light Armor")]
        LightArmor = 24,
        [Description("Force Armor")]
        ForceArmor = 25,
        [Description("Heavy Armor")]
        HeavyArmor = 26,
        [Description("Helmet")]
        Helmet = 27,
        [Description("Shield")]
        Shield = 28,
        [Description("Book")]
        Book = 29,
        [Description("Power Glove")]
        PowerGlove = 30,
        [Description("Scanner")]
        Scanner = 31,
        [Description("Harvester")]
        Harvester = 32,
        [Description("Component (Raw Ore)")]
        ComponentRawOre = 33,
        [Description("Component (Metal)")]
        ComponentMetal = 34,
        [Description("Component (Organic)")]
        ComponentOrganic = 35,
        [Description("Component (Small Blade)")]
        ComponentSmallBlade = 36,
        [Description("Component (Medium Blade)")]
        ComponentMediumBlade = 37,
        [Description("Component (Large Blade)")]
        ComponentLargeBlade = 38,
        [Description("Component (Shaft)")]
        ComponentShaft = 39,
        [Description("Component (Small Handle)")]
        ComponentSmallHandle = 40,
        [Description("Component (Medium Handle)")]
        ComponentMediumHandle = 41,
        [Description("Component (Large Handle)")]
        ComponentLargeHandle = 42,
        [Description("Component (Enhancement)")]
        ComponentEnhancement = 43,
        [Description("Component (Fiberplast)")]
        ComponentFiberplast = 44,
        [Description("Component (Leather)")]
        ComponentLeather = 45,
        [Description("Component (Padding)")]
        ComponentPadding = 46,
        [Description("Component (Electronics)")]
        ComponentElectronics = 47,
        [Description("Component (Wood Baton Frame)")]
        ComponentWoodBatonFrame = 48,
        [Description("Component (Metal Baton Frame)")]
        ComponentMetalBatonFrame = 49,
        [Description("Component (Ranged Weapon Core)")]
        ComponentRangedWeaponCore = 50,
        [Description("Component (Rifle Barrel)")]
        ComponentRifleBarrel = 51,
        [Description("Component (Pistol Barrel)")]
        ComponentPistolBarrel = 52,
        [Description("Component (Power Crystal)")]
        ComponentPowerCrystal = 53,
        [Description("Component (Saber Hilt)")]
        ComponentSaberHilt = 54,
        [Description("Component (Seeds)")]
        ComponentSeeds = 55,
        [Description("Component (Blue Crystal)")]
        ComponentBlueCrystal = 56,
        [Description("Component (Red Crystal)")]
        ComponentRedCrystal = 57,
        [Description("Component (Green Crystal)")]
        ComponentGreenCrystal = 58,
        [Description("Component (Yellow Crystal)")]
        ComponentYellowCrystal = 59,
        [Description("Component (Blue Crystal Cluster)")]
        ComponentBlueCrystalCluster = 60,
        [Description("Component (Red Crystal Cluster)")]
        ComponentRedCrystalCluster = 61,
        [Description("Component (Green Crystal Cluster)")]
        ComponentGreenCrystalCluster = 62,
        [Description("Component (Yellow Crystal Cluster)")]
        ComponentYellowCrystalCluster = 63,
        [Description("Component (Power Crystal Cluster)")]
        ComponentPowerCrystalCluster = 64,
        [Description("Component (Heavy Armor Core)")]
        ComponentHeavyArmorCore = 65,
        [Description("Component (Light Armor Core)")]
        ComponentLightArmorCore = 66,
        [Description("Component (Force Armor Core)")]
        ComponentForceArmorCore = 67,
        [Description("Component (Heavy Armor Segment)")]
        ComponentHeavyArmorSegment = 68,
        [Description("Component (Light Armor Segment)")]
        ComponentLightArmorSegment = 69,
        [Description("Component (Force Armor Segment)")]
        ComponentForceArmorSegment = 70,
        [Description("Component (Small Structure Frame)")]
        ComponentSmallStructureFrame = 71,
        [Description("Component (Medium Structure Frame)")]
        ComponentMediumStructureFrame = 72,
        [Description("Component (Large Structure Frame)")]
        ComponentLargeStructureFrame = 73,
        [Description("Component (Computing Module)")]
        ComponentComputingModule = 74,
        [Description("Component (Construction Parts)")]
        ComponentConstructionParts = 75,
        [Description("Component (Mainframe)")]
        ComponentMainframe = 76,
        [Description("Component (Power Relay)")]
        ComponentPowerRelay = 77,
        [Description("Component (Power Core)")]
        ComponentPowerCore = 78,
        [Description("Component (Ingredient)")]
        ComponentIngredient = 79,
        [Description("Component (Herb)")]
        ComponentHerb = 80,
        [Description("Component (Carbosyrup)")]
        ComponentCarbosyrup = 81,
        [Description("Component (Meat)")]
        ComponentMeat = 82,
        [Description("Component (Cereal)")]
        ComponentCereal = 83,
        [Description("Component (Grain)")]
        ComponentGrain = 84,
        [Description("Component (Vegetable)")]
        ComponentVegetable = 85,
        [Description("Component (Water)")]
        ComponentWater = 86,
        [Description("Component (Curry Paste)")]
        ComponentCurryPaste = 87,
        [Description("Component (Soup)")]
        ComponentSoup = 88,
        [Description("Component (Spiced Milk)")]
        ComponentSpicedMilk = 89,
        [Description("Component (Dough)")]
        ComponentDough = 90,
        [Description("Component (Butter)")]
        ComponentButter = 91,
        [Description("Component (Noodles)")]
        ComponentNoodles = 92,
        [Description("Component (Eggs)")]
        ComponentEggs = 93,
        [Description("Component (Emitter)")]
        ComponentEmitter = 94,
        [Description("Component (Hyperdrive)")]
        ComponentHyperdrive = 95,
        [Description("Component (Hull Plating)")]
        ComponentHullPlating = 96,
        [Description("Component (Starship Weapon)")]
        ComponentStarshipWeapon = 97,
        [Description("Blue Mod")]
        BlueMod = 98,
        [Description("Green Mod")]
        GreenMod = 99,
        [Description("Red Mod")]
        RedMod = 100,
        [Description("Yellow Mod")]
        YellowMod = 101,
        [Description("Necklace")]
        Necklace = 102,
        [Description("Ring")]
        Ring = 103,
        [Description("Repair Kit")]
        RepairKit = 104,
        [Description("Stim Pack")]
        StimPack = 105,
        [Description("Force Pack")]
        ForcePack = 106,
        [Description("Healing Kit")]
        HealingKit = 107,
        [Description("Resuscitation Device")]
        ResuscitationDevice = 108,
        [Description("Starchart")]
        Starchart = 109,
        [Description("Fuel")]
        Fuel = 110,
        [Description("Control Tower")]
        ControlTower = 111,
        [Description("Drill")]
        Drill = 112,
        [Description("Resource Silo")]
        ResourceSilo = 113,
        [Description("Turret")]
        Turret = 114,
        [Description("Building")]
        Building = 115,
        [Description("Mass Production")]
        MassProduction = 116,
        [Description("Starship Production")]
        StarshipProduction = 117,
        [Description("Furniture")]
        Furniture = 118,
        [Description("Stronidium Silo")]
        StronidiumSilo = 119,
        [Description("Fuel Silo")]
        FuelSilo = 120,
        [Description("Crafting Device")]
        CraftingDevice = 121,
        [Description("Persistent Storage")]
        PersistentStorage = 122,
        [Description("Starship")]
        Starship = 123,
        [Description("Starship Equipment")]
        StarshipEquipment = 124,
    }
}
