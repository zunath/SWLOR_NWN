using System;
using System.Security.Cryptography;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Enumeration
{
    public enum Skill
    {
        [SkillType(SkillCategory.Invalid, "Unknown", 0, false, "Unused in-game.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Unknown = 0,
        [SkillType(SkillCategory.MeleeCombat, "One-Handed", 100, true, "Ability to use one-handed weapons like vibroblades and batons. Higher skill levels unlock more powerful one-handed weapons.", Ability.Strength, Ability.Dexterity, Ability.Invalid, true)]
        OneHanded = 1,
        [SkillType(SkillCategory.MeleeCombat, "Two-Handed", 100, true, "Ability to use heavy weapons like heavy vibroblades and polearms in combat. Higher skill levels unlock more powerful two-handed weapons.", Ability.Strength, Ability.Constitution, Ability.Invalid, true)]
        TwoHanded = 2,
        [SkillType(SkillCategory.MeleeCombat, "Twin Blades", 100, true, "Ability to use double edged weapons like twin vibroblades in combat. Higher skill levels unlock more powerful twin blade weapons.", Ability.Strength, Ability.Constitution, Ability.Invalid, true)]
        TwinBlades = 3,
        [SkillType(SkillCategory.MeleeCombat, "Martial Arts", 100, true, "Ability to fight using power gloves and staves in combat. Higher skill levels unlock more powerful power gloves and staves.", Ability.Strength, Ability.Dexterity, Ability.Invalid, true)]
        MartialArts = 4,
        [SkillType(SkillCategory.RangedCombat, "Blasters", 100, true, "Ability to use blasters like pistols and rifles in combat. Higher skill levels unlock more powerful blasters.", Ability.Dexterity, Ability.Wisdom, Ability.Invalid, true)]
        Blasters = 5,
        [SkillType(SkillCategory.RangedCombat, "Throwing", 100, true, "Ability to use throwing weapons like shurikens, grenades and slings in combat. Higher skill levels unlock more powerful throwing weapons.", Ability.Dexterity, Ability.Strength, Ability.Invalid, true)]
        Throwing = 6,
        [SkillType(SkillCategory.Defense, "Light Armor", 100, true, "Ability to effectively defend against attacks while wearing light armor like tunics. Higher skill levels unlock more powerful light armors.", Ability.Dexterity, Ability.Constitution, Ability.Invalid, true)]
        LightArmor = 7,
        [SkillType(SkillCategory.Defense, "Heavy Armor", 100, true, "Ability to effectively defend against attacks while wearing heavy armor like plate mail. Higher skill levels unlock more powerful heavy armors.", Ability.Constitution, Ability.Strength, Ability.Invalid, true)]
        HeavyArmor = 8,
        [SkillType(SkillCategory.Defense, "Shields", 100, true, "Ability to effectively defend against attacks while using shields. Higher skill levels unlock more powerful shields.", Ability.Constitution, Ability.Strength, Ability.Invalid, true)]
        Shields = 9,
        [SkillType(SkillCategory.Gathering, "Harvesting", 100, true, "Ability to harvest raw resources from ore veins, trees, etc. Higher skill levels increase yield and unlock more difficult resources.", Ability.Constitution, Ability.Strength, Ability.Invalid, true)]
        Harvesting = 10,
        [SkillType(SkillCategory.Defense, "Force Armor", 100, true, "Ability to effectively defend against attacks while wearing force armor like Jedi robes. Higher skill levels unlock more powerful force armors.", Ability.Constitution, Ability.Charisma, Ability.Wisdom, true)]
        ForceArmor = 11,
        [SkillType(SkillCategory.Crafting, "Weaponsmith", 50, true, "Ability to create weapons like axes and swords. Higher skill levels reduce crafting difficulty and unlock more recipes.", Ability.Dexterity, Ability.Intelligence, Ability.Invalid, true)]
        Weaponsmith = 12,
        [SkillType(SkillCategory.Crafting, "Armorsmith", 50, true, "Ability to create armors like robes and plate mail. Higher skill levels reduce crafting difficulty and unlock more recipes.", Ability.Constitution, Ability.Intelligence, Ability.Invalid, true)]
        Armorsmith = 13,
        [SkillType(SkillCategory.MeleeCombat, "Lightsaber", 100, true, "Ability to use lightsaber and saberstaff weapons. Higher skill levels unlock more powerful lightsabers and saberstaff weapons.", Ability.Dexterity, Ability.Charisma, Ability.Invalid, true)]
        Lightsaber = 14,
        [SkillType(SkillCategory.Crafting, "Fabrication", 50, true, "Ability to create structures like furniture, buildings, control towers, etc. Higher skill levels reduce crafting difficulty and unlock more structures for building.", Ability.Intelligence, Ability.Constitution, Ability.Invalid, true)]
        Fabrication = 15,
        [SkillType(SkillCategory.Crafting, "Cooking", 30, true, "Ability to create food items which can grant temporary stat increases. Higher skill levels reduce crafting difficulty and unlock more recipes.", Ability.Intelligence, Ability.Charisma, Ability.Invalid, true)]
        Cooking = 16,
        [SkillType(SkillCategory.Crafting, "Medicine", 50, true, "Ability to treat bodily injuries in the field with healing kits. Also enables construction of medical items like stim packs. Higher skill levels increase effectiveness of healing kits and unlock new blueprints.", Ability.Intelligence, Ability.Wisdom, Ability.Charisma, true)]
        Medicine = 17,
        [SkillType(SkillCategory.Languages, "Mirialan", 20, true, "Ability to speak the Mirialan language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Mirialan = 18,
        [SkillType(SkillCategory.Force, "Force Alter", 100, true, "Ability to use alter-based force abilities like Force Confusion and Force Push. Higher skill levels unlock new abilities.", Ability.Intelligence, Ability.Wisdom, Ability.Charisma, true)]
        ForceAlter = 19,
        [SkillType(SkillCategory.Force, "Force Control", 100, true, "Ability to use control-based force abilities like Force Speed and Rage. Higher skill levels unlock new abilities.", Ability.Wisdom, Ability.Intelligence, Ability.Charisma, true)]
        ForceControl = 20,
        [SkillType(SkillCategory.Force, "Force Sense", 100, true, "Ability to use sense-based force abilities like Force Insight and Premonition. Higher skill levels unlock new abilities.", Ability.Charisma, Ability.Constitution, Ability.Dexterity, true)]
        ForceSense = 21,
        [SkillType(SkillCategory.Crafting, "Engineering", 50, true, "Ability to process raw materials and create mechanical devices like firearms. Higher skill levels increase processing yield and allow more difficult ore to be smelted. Also reduces crafting difficulty and unlocks more blueprints.", Ability.Dexterity, Ability.Wisdom, Ability.Invalid, true)]
        Engineering = 22,
        [SkillType(SkillCategory.Utility, "Farming", 50, false, "Ability to plant seeds, water plants, and harvest crops. Higher skill levels increase yield and unlock more crops.", Ability.Constitution, Ability.Charisma, Ability.Wisdom, true)]
        Farming = 23,
        [SkillType(SkillCategory.Gathering, "Scavenging", 50, true, "Ability to search through junk to find useful supplies. Higher skill levels unlock more types of objects to scavenge.", Ability.Constitution, Ability.Wisdom, Ability.Invalid, true)]
        Scavenging = 24,
        [SkillType(SkillCategory.Languages, "Bothese", 20, true, "Ability to speak the Bothese language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Bothese = 25,
        [SkillType(SkillCategory.Languages, "Cheunh", 20, true, "Ability to speak the Cheunh language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Cheunh = 26,
        [SkillType(SkillCategory.Languages, "Zabraki", 20, true, "Ability to speak the Zabraki language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Zabraki = 27,
        [SkillType(SkillCategory.Languages, "Twi'leki (Ryl)", 20, true, "Ability to speak the Twi'leki (originally Ryl) language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Twileki = 28,
        [SkillType(SkillCategory.Languages, "Catharese", 20, true, "Ability to speak the Catharese language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Catharese = 29,
        [SkillType(SkillCategory.Languages, "Dosh", 20, true, "Ability to speak the Dosh language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Dosh = 30,
        [SkillType(SkillCategory.Languages, "Shyriiwook (Wookiespeak)", 20, true, "Ability to speak the Shyriiwook (AKA Wookieespeak) language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Shyriiwook = 31,
        [SkillType(SkillCategory.Languages, "Droidspeak", 20, true, "Ability to speak the Droid language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Droidspeak = 32,
        [SkillType(SkillCategory.Languages, "Basic", 20, true, "Ability to speak the Galactic Basic language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Basic = 33,
        [SkillType(SkillCategory.Languages, "Mandoa", 20, true, "Ability to speak the Mandoa language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Mandoa = 34,
        [SkillType(SkillCategory.Languages, "Huttese", 20, true, "Ability to speak the Huttese language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Huttese = 35,
        [SkillType(SkillCategory.Utility, "Piloting", 100, true, "Ability to pilot speeders and starships, follow navigation charts and control starship systems.", Ability.Dexterity, Ability.Charisma, Ability.Invalid, true)]
        Piloting = 36,
        [SkillType(SkillCategory.Languages, "Mon Calamarian", 20, true, "Ability to speak the Mon Calamarian language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        MonCalamarian = 37,
        [SkillType(SkillCategory.Languages, "Ugnaught", 20, true, "Ability to speak the Ugnaught language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Ugnaught = 38
    }

    public class SkillTypeAttribute : Attribute
    {
        public SkillCategory Category { get; set; }
        public string Name { get; set; }
        public int MaxRank { get; set; }
        public bool IsActive { get; set; }
        public string Description { get; set; }
        public Ability Primary { get; set; }
        public Ability Secondary { get; set; }
        public Ability Tertiary { get; set; }
        public bool ContributesToSkillCap { get; set; }

        public SkillTypeAttribute(SkillCategory category, string name, int maxRank, bool isActive, string description, Ability primary, Ability secondary, Ability tertiary, bool contributesToSkillCap)
        {
            Category = category;
            Name = name;
            MaxRank = maxRank;
            IsActive = isActive;
            Description = description;
            Primary = primary;
            Secondary = secondary;
            Tertiary = tertiary;
            ContributesToSkillCap = contributesToSkillCap;
        }
    }
}
