using System;

namespace SWLOR.Game.Server.Service.PerkService
{
    public enum PerkCategoryType
    {
        [PerkCategory("Invalid", false)]
        Invalid = 0,

        [PerkCategory("One Handed - General", true)]
        OneHandedGeneral = 1,

        [PerkCategory("One Handed - Vibroblade", true)]
        OneHandedVibroblade = 2,

        [PerkCategory("One Handed - Finesse Vibroblade", true)]
        OneHandedFinesseVibroblade = 3,

        [PerkCategory("One Handed - Lightsaber", true)]
        OneHandedLightsaber = 4,

        [PerkCategory("Two Handed - General", true)]
        TwoHandedGeneral = 5,

        [PerkCategory("Two Handed - Heavy Vibroblade", true)]
        TwoHandedHeavyVibroblade = 6,

        [PerkCategory("Two Handed - Polearm", true)]
        TwoHandedPolearm = 7,

        [PerkCategory("Two Handed - Twin Blade", true)]
        TwoHandedTwinBlade = 8,

        [PerkCategory("Two Handed - Saberstaff", true)]
        TwoHandedSaberstaff = 9,

        [PerkCategory("Martial Arts - General", true)]
        MartialArtsGeneral = 10,

        [PerkCategory("Martial Arts - Katars", true)]
        MartialArtsKatars = 11,

        [PerkCategory("Martial Arts - Staff", true)]
        MartialArtsStaff = 12,

        [PerkCategory("Ranged - General", true)]
        RangedGeneral = 13,

        [PerkCategory("Ranged - Pistol", true)]
        RangedPistol = 14,

        [PerkCategory("Ranged - Throwing", true)]
        RangedThrowing = 15,

        [PerkCategory("Ranged - Cannon", true)]
        RangedCannon = 16,

        [PerkCategory("Ranged - Rifle", true)]
        RangedRifle = 17,

        [PerkCategory("Force - Universal", true)]
        ForceUniversal = 18,

        [PerkCategory("Armor - General", true)]
        ArmorGeneral = 19,

        [PerkCategory("Armor - Heavy", true)]
        ArmorHeavy = 20,

        [PerkCategory("Armor - Light", true)]
        ArmorLight = 21,

        [PerkCategory("Piloting", true)]
        Piloting = 22,

        [PerkCategory("First Aid", true)]
        FirstAid = 23,

        [PerkCategory("Smithery", true)]
        Smithery = 24,

        [PerkCategory("Cybertech", true)]
        Cybertech = 25,

        [PerkCategory("Fabrication", true)]
        Fabrication = 26,

        [PerkCategory("Gathering", true)]
        Gathering = 27,

        [PerkCategory("Leadership", true)]
        Leadership = 28,

        [PerkCategory("Force - Light", true)]
        ForceLight = 29,

        [PerkCategory("Force - Dark", true)]
        ForceDark = 30,

        [PerkCategory("General Perks", true)]
        General = 31,

        [PerkCategory("Agriculture", true)]
        Agriculture = 32,

        [PerkCategory("Engineering", true)]
        Engineering = 33,

        [PerkCategory("Devices", true)]
        Devices = 34,

        [PerkCategory("One Handed - Shield", true)]
        OneHandedShield = 35,

        [PerkCategory("Beast Mastery - Training", true)]
        BeastMasteryTraining = 36,

        [PerkCategory("Beast Mastery - Incubation", true)]
        BeastMasteryIncubation = 37,

        [PerkCategory("Beast - General", true)]
        BeastGeneral = 38,

        [PerkCategory("Beast - Damage", true)]
        BeastDamage = 39,

        [PerkCategory("Beast - Tank", true)]
        BeastTank = 40,

        [PerkCategory("Beast - Balanced", true)]
        BeastBalanced = 41,

        [PerkCategory("Beast - Bruiser", true)]
        BeastBruiser = 42,

        [PerkCategory("Beast - Evasion", true)]
        BeastEvasion = 43,

        [PerkCategory("Beast - Force", true)]
        BeastForce = 44,
    }

    public class PerkCategoryAttribute : Attribute
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }

        public PerkCategoryAttribute(string name, bool isActive)
        {
            Name = name;
            IsActive = isActive;
        }
    }
}
