using System;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Enumeration
{
    public enum SkillType
    {
        [Skill(SkillCategoryType.Invalid, "Invalid", 0, false, "Unused in-game.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Invalid = 0,

        // Melee Combat
        [Skill(SkillCategoryType.MeleeCombat, "One-Handed", 100, true, "Ability to use one-handed weapons like vibroblades, batons and lightsabers. Higher Skill levels unlock more powerful one-handed weapons.", true, AbilityType.Strength, AbilityType.Dexterity)]
        OneHanded = 1,
        [Skill(SkillCategoryType.MeleeCombat, "Two-Handed", 100, true, "Ability to use heavy weapons like heavy vibroblades, polearms and saberstaffs in combat. Higher Skill levels unlock more powerful two-handed weapons.", true, AbilityType.Strength, AbilityType.Constitution)]
        TwoHanded = 2,
        [Skill(SkillCategoryType.MeleeCombat, "Martial Arts", 100, true, "Ability to fight using power gloves and staves in combat. Higher Skill levels unlock more powerful power gloves and staves.", true, AbilityType.Strength, AbilityType.Dexterity)]
        MartialArts = 3,

        // Ranged Combat
        [Skill(SkillCategoryType.RangedCombat, "Blasters", 100, true, "Ability to use blasters like pistols and rifles in combat. Higher Skill levels unlock more powerful blasters.", true, AbilityType.Dexterity, AbilityType.Wisdom)]
        Blasters = 4,
        [Skill(SkillCategoryType.RangedCombat, "Throwing", 100, true, "Ability to use throwing weapons like shurikens, grenades and slings in combat. Higher Skill levels unlock more powerful throwing weapons.", true, AbilityType.Dexterity, AbilityType.Strength)]
        Throwing = 5,

        // Defense
        [Skill(SkillCategoryType.Defense, "Light Armor", 100, true, "Ability to effectively defend against attacks while wearing light armor like tunics. Higher Skill levels unlock more powerful light armors.", true, AbilityType.Dexterity, AbilityType.Constitution)]
        LightArmor = 6,
        [Skill(SkillCategoryType.Defense, "Heavy Armor", 100, true, "Ability to effectively defend against attacks while wearing heavy armor like plate mail. Higher Skill levels unlock more powerful heavy armors.", true, AbilityType.Constitution, AbilityType.Strength)]
        HeavyArmor = 7,

        // Crafting
        [Skill(SkillCategoryType.Crafting, "Weaponsmith", 50, true, "Ability to create weapons like axes and swords. Higher Skill levels reduce crafting difficulty and unlock more recipes.", true, AbilityType.Invalid, AbilityType.Invalid)]
        Weaponsmith = 8,
        [Skill(SkillCategoryType.Crafting, "Armorsmith", 50, true, "Ability to create armors like robes and plate mail. Higher Skill levels reduce crafting difficulty and unlock more recipes.", true, AbilityType.Invalid, AbilityType.Invalid)]
        Armorsmith = 9,
        [Skill(SkillCategoryType.Crafting, "Fabrication", 50, true, "Ability to create structures like furniture, buildings, control towers, etc. Higher Skill levels reduce crafting difficulty and unlock more structures for building.", true, AbilityType.Invalid, AbilityType.Invalid)]
        Fabrication = 10,
        [Skill(SkillCategoryType.Crafting, "Engineering", 50, true, "Ability to process raw materials and create mechanical devices like firearms. Higher Skill levels increase processing yield and allow more difficult ore to be smelted. Also reduces crafting difficulty and unlocks more blueprints.", true, AbilityType.Invalid, AbilityType.Invalid)]
        Engineering = 11,

        // Gathering
        [Skill(SkillCategoryType.Gathering, "Harvesting", 100, true, "Ability to harvest raw resources from ore veins, trees, etc. Higher Skill levels increase yield and unlock more difficult resources.", true, AbilityType.Invalid, AbilityType.Invalid)]
        Harvesting = 12,
        [Skill(SkillCategoryType.Gathering, "Scavenging", 50, true, "Ability to search through junk to find useful supplies. Higher Skill levels unlock more types of objects to scavenge.", true, AbilityType.Invalid, AbilityType.Invalid)]
        Scavenging = 13,

        // Utility
        [Skill(SkillCategoryType.Utility, "Piloting", 100, true, "Ability to pilot speeders and starships, follow navigation charts and control starship systems.", true, AbilityType.Dexterity, AbilityType.Charisma)]
        Piloting = 14,
        [Skill(SkillCategoryType.Utility, "Medicine", 50, true, "Ability to treat bodily injuries in the field with healing kits. Also enables construction of medical items like stim packs. Higher Skill levels increase effectiveness of healing kits and unlock new blueprints.", true, AbilityType.Invalid, AbilityType.Invalid)]
        Medicine = 15,

        // Force
        [Skill(SkillCategoryType.Force, "Force Alter", 100, true, "Ability to use alter-based force abilities like Force Confusion and Force Push. Higher Skill levels unlock new abilities.", true, AbilityType.Intelligence, AbilityType.Wisdom)]
        ForceAlter = 16,
        [Skill(SkillCategoryType.Force, "Force Control", 100, true, "Ability to use control-based force abilities like Force Speed and Rage. Higher Skill levels unlock new abilities.", true, AbilityType.Wisdom, AbilityType.Intelligence)]
        ForceControl = 17,
        [Skill(SkillCategoryType.Force, "Force Sense", 100, true, "Ability to use sense-based force abilities like Force Insight and Premonition. Higher Skill levels unlock new abilities.", true, AbilityType.Charisma, AbilityType.Constitution)]
        ForceSense = 18,

        // Languages
        [Skill(SkillCategoryType.Languages, "Mirialan", 20, true, "Ability to speak the Mirialan language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Mirialan = 19,
        [Skill(SkillCategoryType.Languages, "Bothese", 20, true, "Ability to speak the Bothese language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Bothese = 20,
        [Skill(SkillCategoryType.Languages, "Cheunh", 20, true, "Ability to speak the Cheunh language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Cheunh = 21,
        [Skill(SkillCategoryType.Languages, "Zabraki", 20, true, "Ability to speak the Zabraki language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Zabraki = 22,
        [Skill(SkillCategoryType.Languages, "Twi'leki (Ryl)", 20, true, "Ability to speak the Twi'leki (Ryl) language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Twileki = 23,
        [Skill(SkillCategoryType.Languages, "Catharese", 20, true, "Ability to speak the Catharese language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Catharese = 24,
        [Skill(SkillCategoryType.Languages, "Dosh", 20, true, "Ability to speak the Dosh language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Dosh = 25,
        [Skill(SkillCategoryType.Languages, "Shyriiwook (Wookieespeak)", 20, true, "Ability to speak the Shyriiwook (Wookieespeak) language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Shyriiwook = 26,
        [Skill(SkillCategoryType.Languages, "Droidspeak", 20, true, "Ability to speak the Droidspeak language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Droidspeak = 27,
        [Skill(SkillCategoryType.Languages, "Basic", 20, true, "Ability to speak the Galactic Basic language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Basic = 28,
        [Skill(SkillCategoryType.Languages, "Mandoa", 20, true, "Ability to speak the Mandoa language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Mandoa = 29,
        [Skill(SkillCategoryType.Languages, "Huttese", 20, true, "Ability to speak the Huttese language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Huttese = 30,
        [Skill(SkillCategoryType.Languages, "Mon Calamarian", 20, true, "Ability to speak the Mon Calamarian language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        MonCalamarian = 31,
        [Skill(SkillCategoryType.Languages, "Ugnaught ", 20, true, "Ability to speak the Ugnaught language.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Ugnaught = 32,
    }

    public class SkillAttribute : Attribute
    {
        public SkillCategoryType Category { get; set; }
        public string Name { get; set; }
        public int MaxRank { get; set; }
        public bool IsActive { get; set; }
        public string Description { get; set; }
        public bool ContributesToSkillCap { get; set; }
        public AbilityType PrimaryStat { get; set; }
        public AbilityType SecondaryStat { get; set; }

        public SkillAttribute(
            SkillCategoryType category,
            string name,
            int maxRank,
            bool isActive,
            string description,
            bool contributesToSkillCap,
            AbilityType primaryStat,
            AbilityType secondaryStat)
        {
            Category = category;
            Name = name;
            MaxRank = maxRank;
            IsActive = isActive;
            Description = description;
            ContributesToSkillCap = contributesToSkillCap;
            PrimaryStat = primaryStat;
            SecondaryStat = secondaryStat;
        }
    }
}
