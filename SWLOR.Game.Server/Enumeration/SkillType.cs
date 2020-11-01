using System;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Enumeration
{
    public enum SkillType
    {
        [Skill(SkillCategoryType.Invalid, "Invalid", 0, 0, false, "Unused in-game.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Invalid = 0,

        // Melee Combat
        [Skill(SkillCategoryType.MeleeCombat, "One-Handed", 100, 50, true, "Ability to use one-handed weapons like vibroblades and batons. Higher Skill levels unlock more powerful one-handed weapons.", true, AbilityType.Strength, AbilityType.Dexterity)]
        OneHanded = 1,
        [Skill(SkillCategoryType.MeleeCombat, "Two-Handed", 100, 50, true, "Ability to use heavy weapons like heavy vibroblades and polearms in combat. Higher Skill levels unlock more powerful two-handed weapons.", true, AbilityType.Strength, AbilityType.Constitution)]
        TwoHanded = 2,
        [Skill(SkillCategoryType.MeleeCombat, "Twin Blades", 100, 50, true, "Ability to use double edged weapons like twin vibroblades in combat. Higher Skill levels unlock more powerful twin blade weapons.", true, AbilityType.Strength, AbilityType.Constitution)]
        TwinBlades = 3,
        [Skill(SkillCategoryType.MeleeCombat, "Martial Arts", 100, 100, true, "Ability to fight using power gloves and staves in combat. Higher Skill levels unlock more powerful power gloves and staves.", true, AbilityType.Strength, AbilityType.Dexterity)]
        MartialArts = 4,
        [Skill(SkillCategoryType.MeleeCombat, "Lightsabers", 0, 100, true, "Ability to use lightsaber and saberstaff weapons. Higher Skill levels unlock more powerful lightsabers and saberstaff weapons.", true, AbilityType.Dexterity, AbilityType.Charisma)]
        Lightsabers = 5,

        // Ranged Combat
        [Skill(SkillCategoryType.RangedCombat, "Blasters", 100, 25, true, "Ability to use blasters like pistols and rifles in combat. Higher Skill levels unlock more powerful blasters.", true, AbilityType.Dexterity, AbilityType.Wisdom)]
        Blasters = 6,
        [Skill(SkillCategoryType.RangedCombat, "Throwing", 100, 25, true, "Ability to use throwing weapons like shurikens, grenades and slings in combat. Higher Skill levels unlock more powerful throwing weapons.", true, AbilityType.Dexterity, AbilityType.Strength)]
        Throwing = 7,

        // Defense
        [Skill(SkillCategoryType.Defense, "Light Armor", 100, 25, true, "Ability to effectively defend against attacks while wearing light armor like tunics. Higher Skill levels unlock more powerful light armors.", true, AbilityType.Dexterity, AbilityType.Constitution)]
        LightArmor = 8,
        [Skill(SkillCategoryType.Defense, "Heavy Armor", 100, 25, true, "Ability to effectively defend against attacks while wearing heavy armor like plate mail. Higher Skill levels unlock more powerful heavy armors.", true, AbilityType.Constitution, AbilityType.Strength)]
        HeavyArmor = 9,
        [Skill(SkillCategoryType.Defense, "Force Armor", 0, 100, true, "Ability to effectively defend against attacks while wearing force armor like Jedi robes. Higher Skill levels unlock more powerful force armors.", true, AbilityType.Charisma, AbilityType.Wisdom)]
        ForceArmor = 10,

        // Crafting
        [Skill(SkillCategoryType.Crafting, "Weaponsmith", 50, 50, true, "Ability to create weapons like axes and swords. Higher Skill levels reduce crafting difficulty and unlock more recipes.", true, AbilityType.Invalid, AbilityType.Invalid)]
        Weaponsmith = 11,
        [Skill(SkillCategoryType.Crafting, "Armorsmith", 50, 50, true, "Ability to create armors like robes and plate mail. Higher Skill levels reduce crafting difficulty and unlock more recipes.", true, AbilityType.Invalid, AbilityType.Invalid)]
        Armorsmith = 12,
        [Skill(SkillCategoryType.Crafting, "Fabrication", 50, 50, true, "Ability to create structures like furniture, buildings, control towers, etc. Higher Skill levels reduce crafting difficulty and unlock more structures for building.", true, AbilityType.Invalid, AbilityType.Invalid)]
        Fabrication = 13,
        [Skill(SkillCategoryType.Crafting, "Medicine", 50, 50, true, "Ability to treat bodily injuries in the field with healing kits. Also enables construction of medical items like stim packs. Higher Skill levels increase effectiveness of healing kits and unlock new blueprints.", true, AbilityType.Invalid, AbilityType.Invalid)]
        Medicine = 14,
        [Skill(SkillCategoryType.Crafting, "Engineering", 50, 50, true, "Ability to process raw materials and create mechanical devices like firearms. Higher Skill levels increase processing yield and allow more difficult ore to be smelted. Also reduces crafting difficulty and unlocks more blueprints.", true, AbilityType.Invalid, AbilityType.Invalid)]
        Engineering = 15,

        // Gathering
        [Skill(SkillCategoryType.Gathering, "Harvesting", 100, 100, true, "Ability to harvest raw resources from ore veins, trees, etc. Higher Skill levels increase yield and unlock more difficult resources.", true, AbilityType.Invalid, AbilityType.Invalid)]
        Harvesting = 16,
        [Skill(SkillCategoryType.Gathering, "Scavenging", 50, 50, true, "Ability to search through junk to find useful supplies. Higher Skill levels unlock more types of objects to scavenge.", true, AbilityType.Invalid, AbilityType.Invalid)]
        Scavenging = 17,

        // Utility
        [Skill(SkillCategoryType.Utility, "Piloting", 100, 100, true, "Ability to pilot speeders and starships, follow navigation charts and control starship systems.", true, AbilityType.Dexterity, AbilityType.Charisma)]
        Piloting = 18,

        // Force
        [Skill(SkillCategoryType.Force, "Force Alter", 0, 100, true, "Ability to use alter-based force abilities like Force Confusion and Force Push. Higher Skill levels unlock new abilities.", true, AbilityType.Intelligence, AbilityType.Wisdom)]
        ForceAlter = 18,
        [Skill(SkillCategoryType.Force, "Force Control", 0, 100, true, "Ability to use control-based force abilities like Force Speed and Rage. Higher Skill levels unlock new abilities.", true, AbilityType.Wisdom, AbilityType.Intelligence)]
        ForceControl = 19,
        [Skill(SkillCategoryType.Force, "Force Sense", 0, 100, true, "Ability to use sense-based force abilities like Force Insight and Premonition. Higher Skill levels unlock new abilities.", true, AbilityType.Charisma, AbilityType.Constitution)]
        ForceSense = 20,

        // Languages
        [Skill(SkillCategoryType.Languages, "Mirialan", 20, 20, true, "Ability to speak the Mirialan language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Mirialan = 21,
        [Skill(SkillCategoryType.Languages, "Bothese", 20, 20, true, "Ability to speak the Bothese language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Bothese = 22,
        [Skill(SkillCategoryType.Languages, "Cheunh", 20, 20, true, "Ability to speak the Cheunh language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Cheunh = 23,
        [Skill(SkillCategoryType.Languages, "Zabraki", 20, 20, true, "Ability to speak the Zabraki language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Zabraki = 24,
        [Skill(SkillCategoryType.Languages, "Twi'leki (Ryl)", 20, 20, true, "Ability to speak the Twi'leki (Ryl) language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Twileki = 25,
        [Skill(SkillCategoryType.Languages, "Catharese", 20, 20, true, "Ability to speak the Catharese language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Catharese = 26,
        [Skill(SkillCategoryType.Languages, "Dosh", 20, 20, true, "Ability to speak the Dosh language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Dosh = 27,
        [Skill(SkillCategoryType.Languages, "Shyriiwook (Wookieespeak)", 20, 20, true, "Ability to speak the Shyriiwook (Wookieespeak) language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Shyriiwook = 28,
        [Skill(SkillCategoryType.Languages, "Droidspeak", 20, 20, true, "Ability to speak the Droidspeak language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Droidspeak = 29,
        [Skill(SkillCategoryType.Languages, "Basic", 20, 20, true, "Ability to speak the Galactic Basic language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Basic = 30,
        [Skill(SkillCategoryType.Languages, "Mandoa", 20, 20, true, "Ability to speak the Mandoa language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Mandoa = 31,
        [Skill(SkillCategoryType.Languages, "Huttese", 20, 20, true, "Ability to speak the Huttese language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Huttese = 32,
        [Skill(SkillCategoryType.Languages, "Mon Calamarian", 20, 20, true, "Ability to speak the Mon Calamarian language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        MonCalamarian = 33,
        [Skill(SkillCategoryType.Languages, "Ugnaught ", 20, 20, true, "Ability to speak the Ugnaught language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Ugnaught = 34,
    }

    public class SkillAttribute : Attribute
    {
        public SkillCategoryType Category { get; set; }
        public string Name { get; set; }
        public int MaxRankStandard { get; set; }
        public int MaxRankForceSensitive { get; set; }
        public bool IsActive { get; set; }
        public string Description { get; set; }
        public bool ContributesToSkillCap { get; set; }
        public AbilityType PrimaryStat { get; set; }
        public AbilityType SecondaryStat { get; set; }

        public SkillAttribute(
            SkillCategoryType category, 
            string name, 
            int maxRankStandard, 
            int maxRankForceSensitive,
            bool isActive, 
            string description, 
            bool contributesToSkillCap, 
            AbilityType primaryStat, 
            AbilityType secondaryStat)
        {
            Category = category;
            Name = name;
            MaxRankStandard = maxRankStandard;
            MaxRankForceSensitive = maxRankForceSensitive;
            IsActive = isActive;
            Description = description;
            ContributesToSkillCap = contributesToSkillCap;
            PrimaryStat = primaryStat;
            SecondaryStat = secondaryStat;
        }
    }
}
