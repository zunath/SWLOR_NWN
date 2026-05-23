namespace SWLOR.Game.Server.Service.CraftService
{
    public enum EnhancementSubType
    {
        [EnhancementSubType("Invalid")]
        Invalid = 0,
        [EnhancementSubType("Defense - Physical")]
        DefensePhysical = 1,
        [EnhancementSubType("Defense - Force")]
        DefenseForce = 2,
        [EnhancementSubType("Resistance - Fire")]
        ResistanceFire = 3,
        [EnhancementSubType("Resistance - Poison")]
        ResistancePoison = 4,
        [EnhancementSubType("Resistance - Electrical")]
        ResistanceElectrical = 5,
        [EnhancementSubType("Resistance - Ice")]
        ResistanceIce = 6,
        [EnhancementSubType("Evasion")]
        Evasion = 7,
        [EnhancementSubType("HP")]
        HP = 8,
        [EnhancementSubType("FP")]
        FP = 9,
        [EnhancementSubType("Stamina")]
        Stamina = 10,
        [EnhancementSubType("Vitality")]
        Vitality = 11,
        [EnhancementSubType("Social")]
        Social = 12,
        [EnhancementSubType("Willpower")]
        Willpower = 13,
        [EnhancementSubType("Control - Smithery")]
        ControlSmithery = 14,
        [EnhancementSubType("Craftsmanship - Smithery")]
        CraftsmanshipSmithery = 15,

        [EnhancementSubType("DMG")]
        DMG = 18,
        // 19-23 are free.
        [EnhancementSubType("Might")]
        Might = 24,
        [EnhancementSubType("Perception")]
        Perception = 25,
        [EnhancementSubType("Accuracy")]
        Accuracy = 26,
        [EnhancementSubType("Recast Reduction")]
        RecastReduction = 27,
        [EnhancementSubType("Structure Bonus")]
        StructureBonus = 28,
        [EnhancementSubType("Food Bonus - HP Regen")]
        FoodBonusHPRegen = 29,
        [EnhancementSubType("Food Bonus - FP Regen")]
        FoodBonusFPRegen = 30,
        [EnhancementSubType("Food Bonus - STM Regen")]
        FoodBonusSTMRegen = 31,
        [EnhancementSubType("Food Bonus - Rest Regen")]
        FoodBonusRestRegen = 32,
        [EnhancementSubType("Food Bonus - XP Bonus")]
        FoodBonusXPBonus = 33,
        [EnhancementSubType("Food Bonus - Recast Reduction")]
        FoodBonusRecastReduction = 34,
        [EnhancementSubType("Food Bonus - Duration")]
        FoodBonusDuration = 35,
        [EnhancementSubType("Food Bonus - HP")]
        FoodBonusHP = 36,
        [EnhancementSubType("Food Bonus - FP")]
        FoodBonusFP = 37,
        [EnhancementSubType("Food Bonus - STM")]
        FoodBonusSTM = 38,
        [EnhancementSubType("Control - Engineering")]
        ControlEngineering = 39,
        [EnhancementSubType("Craftsmanship - Engineering")]
        CraftsmanshipEngineering = 40,
        [EnhancementSubType("Control - Fabrication")]
        ControlFabrication = 41,
        [EnhancementSubType("Craftsmanship - Fabrication")]
        CraftsmanshipFabrication = 42,
        [EnhancementSubType("Control - Agriculture")]
        ControlAgriculture = 43,
        [EnhancementSubType("Craftsmanship - Agriculture")]
        CraftsmanshipAgriculture = 44,
        [EnhancementSubType("Module Bonus")]
        ModuleBonus = 45,
        [EnhancementSubType("Starship - Hull")]
        StarshipHull = 46,
        [EnhancementSubType("Starship - Capacitor")]
        StarshipCapacitor = 47,
        [EnhancementSubType("Starship - Shield")]
        StarshipShield = 48,
        [EnhancementSubType("Starship - Shield Recharge Rate")]
        StarshipShieldRechargeRate = 49,
        [EnhancementSubType("Starship - EM Damage")]
        StarshipEMDamage = 50,
        [EnhancementSubType("Starship - Thermal Damage")]
        StarshipThermalDamage = 51,
        [EnhancementSubType("Starship - Explosive Damage")]
        StarshipExplosiveDamage = 52,
        [EnhancementSubType("Starship - Accuracy")]
        StarshipAccuracy = 53,
        [EnhancementSubType("Starship - Evasion")]
        StarshipEvasion = 54,
        [EnhancementSubType("Starship - Thermal Defense")]
        StarshipThermalDefense = 55,
        [EnhancementSubType("Starship - Explosive Defense")]
        StarshipExplosiveDefense = 56,
        [EnhancementSubType("Starship - EM Defense")]
        StarshipEMDefense = 57,
        [EnhancementSubType("Starship - Agility")]
        Agility = 58,
        // 59 is free
        [EnhancementSubType("Food Bonus - Attack")]
        FoodBonusAttack = 60,
        [EnhancementSubType("Food Bonus - Accuracy")]
        FoodBonusAccuracy = 61,
        [EnhancementSubType("Food Bonus - Physical Defense")]
        FoodBonusPhysicalDefense = 62,
        [EnhancementSubType("Food Bonus - Force Defense")]
        FoodBonusForceDefense = 63,
        [EnhancementSubType("Food Bonus - Poison Resistance")]
        FoodBonusPoisonResistance = 64,
        [EnhancementSubType("Food Bonus - Fire Resistance")]
        FoodBonusFireResistance = 65,
        [EnhancementSubType("Food Bonus - Ice Resistance")]
        FoodBonusIceResistance = 66,
        [EnhancementSubType("Food Bonus - Electrical Resistance")]
        FoodBonusElectricalResistance = 67,
        [EnhancementSubType("Food Bonus - Evasion")]
        FoodBonusEvasion = 68,
        [EnhancementSubType("Food Bonus - Control - Smithery")]
        FoodBonusControlSmithery = 69,
        [EnhancementSubType("Food Bonus - Craftsmanship - Smithery")]
        FoodBonusCraftsmanshipSmithery = 70,
        [EnhancementSubType("Food Bonus - Control - Fabrication")]
        FoodBonusControlFabrication = 71,
        [EnhancementSubType("Food Bonus - Craftsmanship - Fabrication")]
        FoodBonusCraftsmanshipFabrication = 72,
        [EnhancementSubType("Food Bonus - Control - Engineering")]
        FoodBonusControlEngineering = 73,
        [EnhancementSubType("Food Bonus - Craftsmanship - Engineering")]
        FoodBonusCraftsmanshipEngineering = 74,
        [EnhancementSubType("Food Bonus - Control - Agriculture")]
        FoodBonusControlAgriculture = 75,
        [EnhancementSubType("Food Bonus - Craftsmanship - Agriculture")]
        FoodBonusCraftsmanshipAgriculture = 76,
        [EnhancementSubType("Food Bonus - Might")]
        FoodBonusMight = 77,
        [EnhancementSubType("Food Bonus - Perception")]
        FoodBonusPerception = 78,
        [EnhancementSubType("Food Bonus - Vitality")]
        FoodBonusVitality = 79,
        [EnhancementSubType("Food Bonus - Willpower")]
        FoodBonusWillpower = 80,
        [EnhancementSubType("Food Bonus - Agility")]
        FoodBonusAgility = 81,
        [EnhancementSubType("Food Bonus - Social")]
        FoodBonusSocial = 82,
        [EnhancementSubType("Attack")]
        Attack = 83,
        [EnhancementSubType("Force Attack")]
        ForceAttack = 84,
        [EnhancementSubType("Resistance - Mind")]
        ResistanceMind = 85,
        [EnhancementSubType("Resistance - Mobility")]
        ResistanceMobility = 86,
        [EnhancementSubType("Resistance - Trauma")]
        ResistanceTrauma = 87,
        [EnhancementSubType("Resistance - Disruption")]
        ResistanceDisruption = 88,
        [EnhancementSubType("Food Bonus - Mind Resistance")]
        FoodBonusMindResistance = 89,
        [EnhancementSubType("Food Bonus - Mobility Resistance")]
        FoodBonusMobilityResistance = 90,
        [EnhancementSubType("Food Bonus - Trauma Resistance")]
        FoodBonusTraumaResistance = 91,
        [EnhancementSubType("Food Bonus - Disruption Resistance")]
        FoodBonusDisruptionResistance = 92,

        [EnhancementSubType("Droid - Fire Resistance")]
        DroidResistanceFire = 93,
        [EnhancementSubType("Droid - Poison Resistance")]
        DroidResistancePoison = 94,
        [EnhancementSubType("Droid - Electrical Resistance")]
        DroidResistanceElectrical = 95,
        [EnhancementSubType("Droid - Ice Resistance")]
        DroidResistanceIce = 96,
        [EnhancementSubType("Droid - Mind Resistance")]
        DroidResistanceMind = 97,
        [EnhancementSubType("Droid - Mobility Resistance")]
        DroidResistanceMobility = 98,
        [EnhancementSubType("Droid - Trauma Resistance")]
        DroidResistanceTrauma = 99,
        [EnhancementSubType("Droid - Disruption Resistance")]
        DroidResistanceDisruption = 100,
        // 101 is free

        [EnhancementSubType("Droid - AI Slot")]
        DroidAISlot = 102,
        [EnhancementSubType("Droid - HP")]
        DroidHP = 103,
        [EnhancementSubType("Droid - STM")]
        DroidSTM = 104,
        [EnhancementSubType("Droid - MGT")]
        DroidMGT = 105,
        [EnhancementSubType("Droid - PER")]
        DroidPER = 106,
        [EnhancementSubType("Droid - VIT")]
        DroidVIT = 107,
        [EnhancementSubType("Droid - WIL")]
        DroidWIL = 108,
        [EnhancementSubType("Droid - AGI")]
        DroidAGI = 109,
        [EnhancementSubType("Droid - SOC")]
        DroidSOC = 110,
        // 111-114 are free.
        [EnhancementSubType("Droid - Vibroblade")]
        DroidVibroblade = 115,
        [EnhancementSubType("Droid - Vibroknife")]
        DroidVibroknife = 116,
        [EnhancementSubType("Droid - Lightsaber")]
        DroidLightsaber = 117,
        [EnhancementSubType("Droid - Heavy Vibroblade")]
        DroidHeavyVibroblade = 118,
        [EnhancementSubType("Droid - Spear")]
        DroidSpear = 119,
        [EnhancementSubType("Droid - Twin Blade")]
        DroidTwinBlade = 120,
        [EnhancementSubType("Droid - Saberstaff")]
        DroidSaberstaff = 121,
        [EnhancementSubType("Droid - Katar")]
        DroidKatar = 122,
        [EnhancementSubType("Droid - Staff")]
        DroidStaff = 123,
        [EnhancementSubType("Droid - Pistol")]
        DroidPistol = 124,
        [EnhancementSubType("Droid - Rifle")]
        DroidRifle = 125,
        [EnhancementSubType("Droid - Throwing")]
        DroidThrowing = 126,


    }

    public class EnhancementSubTypeAttribute : Attribute
    {
        public string Name { get; set; }

        public EnhancementSubTypeAttribute(string name)
        {
            Name = name;
        }
    }
}
