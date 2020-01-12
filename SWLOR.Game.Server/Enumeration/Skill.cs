using System;
using System.Security.Cryptography;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Enumeration
{
    public enum Skill
    {
        [Skill(SkillCategory.Invalid, "Unknown", 0, false, "Unused in-game.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Unknown = 0,
        [Skill(SkillCategory.MeleeCombat, "One-Handed", 100, true, "Ability to use one-handed weapons like vibroblades and batons. Higher skill levels unlock more powerful one-handed weapons.", Ability.Strength, Ability.Dexterity, Ability.Invalid, true)]
        OneHanded = 1,
        [Skill(SkillCategory.MeleeCombat, "Two-Handed", 100, true, "Ability to use heavy weapons like heavy vibroblades and polearms in combat. Higher skill levels unlock more powerful two-handed weapons.", Ability.Strength, Ability.Constitution, Ability.Invalid, true)]
        TwoHanded = 2,
        [Skill(SkillCategory.MeleeCombat, "Twin Blades", 100, true, "Ability to use double edged weapons like twin vibroblades in combat. Higher skill levels unlock more powerful twin blade weapons.", Ability.Strength, Ability.Constitution, Ability.Invalid, true)]
        TwinBlades = 3,
        [Skill(SkillCategory.MeleeCombat, "Martial Arts", 100, true, "Ability to fight using power gloves and staves in combat. Higher skill levels unlock more powerful power gloves and staves.", Ability.Strength, Ability.Dexterity, Ability.Invalid, true)]
        MartialArts = 4,
        [Skill(SkillCategory.RangedCombat, "Blasters", 100, true, "Ability to use blasters like pistols and rifles in combat. Higher skill levels unlock more powerful blasters.", Ability.Dexterity, Ability.Wisdom, Ability.Invalid, true)]
        Blasters = 5,
        [Skill(SkillCategory.RangedCombat, "Throwing", 100, true, "Ability to use throwing weapons like shurikens, grenades and slings in combat. Higher skill levels unlock more powerful throwing weapons.", Ability.Dexterity, Ability.Strength, Ability.Invalid, true)]
        Throwing = 6,
        [Skill(SkillCategory.Defense, "Light Armor", 100, true, "Ability to effectively defend against attacks while wearing light armor like tunics. Higher skill levels unlock more powerful light armors.", Ability.Dexterity, Ability.Constitution, Ability.Invalid, true)]
        LightArmor = 7,
        [Skill(SkillCategory.Defense, "Heavy Armor", 100, true, "Ability to effectively defend against attacks while wearing heavy armor like plate mail. Higher skill levels unlock more powerful heavy armors.", Ability.Constitution, Ability.Strength, Ability.Invalid, true)]
        HeavyArmor = 8,
        [Skill(SkillCategory.Defense, "Shields", 100, true, "Ability to effectively defend against attacks while using shields. Higher skill levels unlock more powerful shields.", Ability.Constitution, Ability.Strength, Ability.Invalid, true)]
        Shields = 9,
        [Skill(SkillCategory.Gathering, "Harvesting", 100, true, "Ability to harvest raw resources from ore veins, trees, etc. Higher skill levels increase yield and unlock more difficult resources.", Ability.Constitution, Ability.Strength, Ability.Invalid, true)]
        Harvesting = 10,
        [Skill(SkillCategory.Defense, "Force Armor", 100, true, "Ability to effectively defend against attacks while wearing force armor like Jedi robes. Higher skill levels unlock more powerful force armors.", Ability.Constitution, Ability.Charisma, Ability.Wisdom, true)]
        ForceArmor = 11,
        [Skill(SkillCategory.Crafting, "Weaponsmith", 50, true, "Ability to create weapons like axes and swords. Higher skill levels reduce crafting difficulty and unlock more recipes.", Ability.Dexterity, Ability.Intelligence, Ability.Invalid, true)]
        Weaponsmith = 12,
        [Skill(SkillCategory.Crafting, "Armorsmith", 50, true, "Ability to create armors like robes and plate mail. Higher skill levels reduce crafting difficulty and unlock more recipes.", Ability.Constitution, Ability.Intelligence, Ability.Invalid, true)]
        Armorsmith = 13,
        [Skill(SkillCategory.MeleeCombat, "Lightsaber", 100, true, "Ability to use lightsaber and saberstaff weapons. Higher skill levels unlock more powerful lightsabers and saberstaff weapons.", Ability.Dexterity, Ability.Charisma, Ability.Invalid, true)]
        Lightsaber = 14,
        [Skill(SkillCategory.Crafting, "Fabrication", 50, true, "Ability to create structures like furniture, buildings, control towers, etc. Higher skill levels reduce crafting difficulty and unlock more structures for building.", Ability.Intelligence, Ability.Constitution, Ability.Invalid, true)]
        Fabrication = 15,
        [Skill(SkillCategory.Crafting, "Cooking", 30, true, "Ability to create food items which can grant temporary stat increases. Higher skill levels reduce crafting difficulty and unlock more recipes.", Ability.Intelligence, Ability.Charisma, Ability.Invalid, true)]
        Cooking = 16,
        [Skill(SkillCategory.Crafting, "Medicine", 50, true, "Ability to treat bodily injuries in the field with healing kits. Also enables construction of medical items like stim packs. Higher skill levels increase effectiveness of healing kits and unlock new blueprints.", Ability.Intelligence, Ability.Wisdom, Ability.Charisma, true)]
        Medicine = 17,
        [Skill(SkillCategory.Languages, "Mirialan", 20, true, "Ability to speak the Mirialan language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Mirialan = 18,
        [Skill(SkillCategory.Force, "Force Alter", 100, true, "Ability to use alter-based force abilities like Force Confusion and Force Push. Higher skill levels unlock new abilities.", Ability.Intelligence, Ability.Wisdom, Ability.Charisma, true)]
        ForceAlter = 19,
        [Skill(SkillCategory.Force, "Force Control", 100, true, "Ability to use control-based force abilities like Force Speed and Rage. Higher skill levels unlock new abilities.", Ability.Wisdom, Ability.Intelligence, Ability.Charisma, true)]
        ForceControl = 20,
        [Skill(SkillCategory.Force, "Force Sense", 100, true, "Ability to use sense-based force abilities like Force Insight and Premonition. Higher skill levels unlock new abilities.", Ability.Charisma, Ability.Constitution, Ability.Dexterity, true)]
        ForceSense = 21,
        [Skill(SkillCategory.Crafting, "Engineering", 50, true, "Ability to process raw materials and create mechanical devices like firearms. Higher skill levels increase processing yield and allow more difficult ore to be smelted. Also reduces crafting difficulty and unlocks more blueprints.", Ability.Dexterity, Ability.Wisdom, Ability.Invalid, true)]
        Engineering = 22,
        [Skill(SkillCategory.Utility, "Farming", 50, false, "Ability to plant seeds, water plants, and harvest crops. Higher skill levels increase yield and unlock more crops.", Ability.Constitution, Ability.Charisma, Ability.Wisdom, true)]
        Farming = 23,
        [Skill(SkillCategory.Gathering, "Scavenging", 50, true, "Ability to search through junk to find useful supplies. Higher skill levels unlock more types of objects to scavenge.", Ability.Constitution, Ability.Wisdom, Ability.Invalid, true)]
        Scavenging = 24,
        [Skill(SkillCategory.Languages, "Bothese", 20, true, "Ability to speak the Bothese language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Bothese = 25,
        [Skill(SkillCategory.Languages, "Cheunh", 20, true, "Ability to speak the Cheunh language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Cheunh = 26,
        [Skill(SkillCategory.Languages, "Zabraki", 20, true, "Ability to speak the Zabraki language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Zabraki = 27,
        [Skill(SkillCategory.Languages, "Twi'leki (Ryl)", 20, true, "Ability to speak the Twi'leki (originally Ryl) language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Twileki = 28,
        [Skill(SkillCategory.Languages, "Catharese", 20, true, "Ability to speak the Catharese language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Catharese = 29,
        [Skill(SkillCategory.Languages, "Dosh", 20, true, "Ability to speak the Dosh language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Dosh = 30,
        [Skill(SkillCategory.Languages, "Shyriiwook (Wookiespeak)", 20, true, "Ability to speak the Shyriiwook (AKA Wookieespeak) language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Shyriiwook = 31,
        [Skill(SkillCategory.Languages, "Droidspeak", 20, true, "Ability to speak the Droid language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Droidspeak = 32,
        [Skill(SkillCategory.Languages, "Basic", 20, true, "Ability to speak the Galactic Basic language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Basic = 33,
        [Skill(SkillCategory.Languages, "Mandoa", 20, true, "Ability to speak the Mandoa language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Mandoa = 34,
        [Skill(SkillCategory.Languages, "Huttese", 20, true, "Ability to speak the Huttese language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Huttese = 35,
        [Skill(SkillCategory.Utility, "Piloting", 100, true, "Ability to pilot speeders and starships, follow navigation charts and control starship systems.", Ability.Dexterity, Ability.Charisma, Ability.Invalid, true)]
        Piloting = 36,
        [Skill(SkillCategory.Languages, "Mon Calamarian", 20, true, "Ability to speak the Mon Calamarian language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        MonCalamarian = 37,
        [Skill(SkillCategory.Languages, "Ugnaught", 20, true, "Ability to speak the Ugnaught language.", Ability.Invalid, Ability.Invalid, Ability.Invalid, false)]
        Ugnaught = 38
    }

    public class SkillAttribute : Attribute
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

        public SkillAttribute(SkillCategory category, string name, int maxRank, bool isActive, string description, Ability primary, Ability secondary, Ability tertiary, bool contributesToSkillCap)
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
