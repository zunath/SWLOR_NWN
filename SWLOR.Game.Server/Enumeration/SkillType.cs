using System;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Enumeration
{
    public enum SkillType
    {
        [Skill(SkillCategoryType.Invalid, 
            "Invalid", 
            0, 
            false, 
            "Unused in-game.", 
            false, 
            AbilityType.Invalid, 
            AbilityType.Invalid)]
        Invalid = 0,

        // Melee Combat
        [Skill(SkillCategoryType.Combat, 
            "One-Handed", 
            50, 
            true, 
            "Ability to use one-handed weapons like vibroblades, finesse vibroblades, and lightsabers.", 
            true, 
            AbilityType.Strength, 
            AbilityType.Dexterity)]
        OneHanded = 1,

        [Skill(SkillCategoryType.Combat, 
            "Two-Handed", 
            50, 
            true, 
            "Ability to use heavy weapons like heavy vibroblades, polearms, and saberstaffs in combat.", 
            true, 
            AbilityType.Strength, 
            AbilityType.Constitution)]
        TwoHanded = 2,

        [Skill(SkillCategoryType.Combat, 
            "Martial Arts", 50, 
            true, 
            "Ability to fight using knuckles and staves in combat.", 
            true, 
            AbilityType.Strength, 
            AbilityType.Dexterity)]
        MartialArts = 3,

        [Skill(SkillCategoryType.Combat, 
            "Ranged", 
            50, 
            true, 
            "Ability to use ranged weapons like pistols, cannons, and rifles in combat.", 
            true, 
            AbilityType.Dexterity, 
            AbilityType.Wisdom)]
        Ranged = 4,

        [Skill(SkillCategoryType.Combat,
            "Force",
            50,
            true,
            "Ability to use Force abilities. Only available to 'Force Sensitive' character types.",
            true,
            AbilityType.Intelligence,
            AbilityType.Wisdom)]
        Force = 5,

        [Skill(SkillCategoryType.Combat, 
            "Armor", 
            50, 
            true,
            "Ability to effectively wear and defend against attacks with armor.", 
            true, 
            AbilityType.Constitution, 
            AbilityType.Dexterity)]
        Armor = 6,

        // Utility
        [Skill(SkillCategoryType.Utility,
            "Piloting",
            50,
            true,
            "Ability to pilot starships, follow navigation charts, and control starship systems.",
            true,
            AbilityType.Intelligence,
            AbilityType.Constitution)]
        Piloting = 7,

        [Skill(SkillCategoryType.Utility,
            "First Aid",
            50,
            true,
            "Ability to treat bodily injuries in the field with healing kits and stim packs.",
            true,
            AbilityType.Intelligence,
            AbilityType.Wisdom)]
        FirstAid = 8,

        // Crafting
        [Skill(SkillCategoryType.Crafting, 
            "Weaponsmith", 
            50, 
            true, 
            "Ability to create weapons like vibroblades, polearms, and blasters.", 
            true, 
            AbilityType.Invalid, 
            AbilityType.Invalid)]
        Weaponsmith = 9,

        [Skill(SkillCategoryType.Crafting, 
            "Armorsmith", 
            50, 
            true, 
            "Ability to create equipment like helmets, belts, and armor.", 
            true, 
            AbilityType.Invalid, 
            AbilityType.Invalid)]
        Armorsmith = 10,

        [Skill(SkillCategoryType.Crafting,
            "Cybertech", 
            50,
            true,
            "Ability to create and install implants.",
            true,
            AbilityType.Invalid,
            AbilityType.Invalid)]
        Cybertech = 11,

        [Skill(SkillCategoryType.Crafting, 
            "Fabrication", 
            50, 
            true, 
            "Ability to create base structures, furniture, and starships.", 
            true, 
            AbilityType.Invalid, 
            AbilityType.Invalid)]
        Fabrication = 12,

        [Skill(SkillCategoryType.Crafting, 
            "Gathering", 
            50, 
            true, 
            "Ability to harvest raw materials and scavenge for supplies.", 
            true, 
            AbilityType.Invalid, 
            AbilityType.Invalid)]
        Harvesting = 13,


        // Languages
        [Skill(SkillCategoryType.Languages, 
            "Mirialan", 
            20, 
            true, 
            "Ability to speak the Mirialan language.", 
            false, 
            AbilityType.Invalid, 
            AbilityType.Invalid)]
        Mirialan = 14,

        [Skill(SkillCategoryType.Languages, 
            "Bothese", 
            20, 
            true, 
            "Ability to speak the Bothese language.", 
            false, 
            AbilityType.Invalid, 
            AbilityType.Invalid)]
        Bothese = 15,

        [Skill(SkillCategoryType.Languages, 
            "Cheunh", 
            20, 
            true, 
            "Ability to speak the Cheunh language.", 
            false, 
            AbilityType.Invalid, 
            AbilityType.Invalid)]
        Cheunh = 16,

        [Skill(SkillCategoryType.Languages, 
            "Zabraki", 
            20, 
            true, 
            "Ability to speak the Zabraki language.", 
            false, 
            AbilityType.Invalid, 
            AbilityType.Invalid)]
        Zabraki = 17,

        [Skill(SkillCategoryType.Languages, 
            "Twi'leki (Ryl)", 
            20, 
            true, 
            "Ability to speak the Twi'leki (Ryl) language.", 
            false, 
            AbilityType.Invalid, 
            AbilityType.Invalid)]
        Twileki = 18,

        [Skill(SkillCategoryType.Languages, 
            "Catharese", 20, 
            true, 
            "Ability to speak the Catharese language.", 
            false, 
            AbilityType.Invalid, 
            AbilityType.Invalid)]
        Catharese = 19,

        [Skill(SkillCategoryType.Languages, 
            "Dosh", 
            20, 
            true, 
            "Ability to speak the Dosh language.", 
            false, 
            AbilityType.Invalid, 
            AbilityType.Invalid)]
        Dosh = 20,

        [Skill(SkillCategoryType.Languages, 
            "Shyriiwook (Wookieespeak)", 
            20, 
            true, 
            "Ability to speak the Shyriiwook (Wookieespeak) language.", 
            false, 
            AbilityType.Invalid, 
            AbilityType.Invalid)]
        Shyriiwook = 21,

        [Skill(SkillCategoryType.Languages, 
            "Droidspeak", 
            20, 
            true, 
            "Ability to speak the Droidspeak language.", 
            false, 
            AbilityType.Invalid, 
            AbilityType.Invalid)]
        Droidspeak = 22,

        [Skill(SkillCategoryType.Languages, 
            "Basic", 
            20, 
            true, 
            "Ability to speak the Galactic Basic language.", 
            false, 
            AbilityType.Invalid, 
            AbilityType.Invalid)]
        Basic = 23,

        [Skill(SkillCategoryType.Languages, 
            "Mandoa", 
            20, 
            true, 
            "Ability to speak the Mandoa language.", 
            false, 
            AbilityType.Invalid, 
            AbilityType.Invalid)]
        Mandoa = 24,

        [Skill(SkillCategoryType.Languages, 
            "Huttese", 
            20, 
            true, 
            "Ability to speak the Huttese language.", 
            false, 
            AbilityType.Invalid, 
            AbilityType.Invalid)]
        Huttese = 25,

        [Skill(SkillCategoryType.Languages, 
            "Mon Calamarian", 
            20, 
            true, 
            "Ability to speak the Mon Calamarian language.", 
            false, 
            AbilityType.Invalid, 
            AbilityType.Invalid)]
        MonCalamarian = 26,

        [Skill(SkillCategoryType.Languages, 
            "Ugnaught", 
            20, 
            true, 
            "Ability to speak the Ugnaught language.", 
            false, 
            AbilityType.Invalid, 
            AbilityType.Invalid)]
        Ugnaught = 27,
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
