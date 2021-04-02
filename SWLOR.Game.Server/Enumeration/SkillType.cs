using System;

namespace SWLOR.Game.Server.Enumeration
{
    public enum SkillType
    {
        [Skill(SkillCategoryType.Invalid, 
            "Invalid", 
            0, 
            false, 
            "Unused in-game.", 
            false)]
        Invalid = 0,

        // Melee Combat
        [Skill(SkillCategoryType.Combat, 
            "One-Handed", 
            50, 
            true, 
            "Ability to use one-handed weapons like vibroblades, finesse vibroblades, and lightsabers.", 
            true)]
        OneHanded = 1,

        [Skill(SkillCategoryType.Combat, 
            "Two-Handed", 
            50, 
            true, 
            "Ability to use heavy weapons like heavy vibroblades, polearms, and saberstaffs in combat.", 
            true)]
        TwoHanded = 2,

        [Skill(SkillCategoryType.Combat, 
            "Martial Arts", 50, 
            true, 
            "Ability to fight using knuckles and staves in combat.", 
            true)]
        MartialArts = 3,

        [Skill(SkillCategoryType.Combat, 
            "Ranged", 
            50, 
            true, 
            "Ability to use ranged weapons like pistols, cannons, and rifles in combat.", 
            true)]
        Ranged = 4,

        [Skill(SkillCategoryType.Combat,
            "Force",
            50,
            true,
            "Ability to use Force abilities. Only available to 'Force Sensitive' character types.",
            true)]
        Force = 5,

        [Skill(SkillCategoryType.Combat, 
            "Armor", 
            50, 
            true,
            "Ability to effectively wear and defend against attacks with armor.", 
            true)]
        Armor = 6,

        // Utility
        [Skill(SkillCategoryType.Utility,
            "Piloting",
            50,
            true,
            "Ability to pilot starships, follow navigation charts, and control starship systems.",
            true)]
        Piloting = 7,

        [Skill(SkillCategoryType.Utility,
            "First Aid",
            50,
            true,
            "Ability to treat bodily injuries in the field with healing kits and stim packs.",
            true)]
        FirstAid = 8,

        // Crafting
        [Skill(SkillCategoryType.Crafting, 
            "Smithery", 
            50, 
            true, 
            "Ability to create weapons and armor like vibroblades, blasters, and helmets.", 
            true)]
        Smithery = 9,
        
        [Skill(SkillCategoryType.Crafting,
            "Cybertech", 
            50,
            true,
            "Ability to create and install implants.",
            true)]
        Cybertech = 11,

        [Skill(SkillCategoryType.Crafting, 
            "Fabrication", 
            50, 
            true, 
            "Ability to create base structures, furniture, and starships.", 
            true)]
        Fabrication = 12,

        [Skill(SkillCategoryType.Crafting, 
            "Gathering", 
            50, 
            true, 
            "Ability to harvest raw materials and scavenge for supplies.", 
            true)]
        Gathering = 13,


        // Languages
        [Skill(SkillCategoryType.Languages, 
            "Mirialan", 
            20, 
            true, 
            "Ability to speak the Mirialan language.", 
            false)]
        Mirialan = 14,

        [Skill(SkillCategoryType.Languages, 
            "Bothese", 
            20, 
            true, 
            "Ability to speak the Bothese language.", 
            false)]
        Bothese = 15,

        [Skill(SkillCategoryType.Languages, 
            "Cheunh", 
            20, 
            true, 
            "Ability to speak the Cheunh language.", 
            false)]
        Cheunh = 16,

        [Skill(SkillCategoryType.Languages, 
            "Zabraki", 
            20, 
            true, 
            "Ability to speak the Zabraki language.", 
            false)]
        Zabraki = 17,

        [Skill(SkillCategoryType.Languages, 
            "Twi'leki (Ryl)", 
            20, 
            true, 
            "Ability to speak the Twi'leki (Ryl) language.", 
            false)]
        Twileki = 18,

        [Skill(SkillCategoryType.Languages, 
            "Catharese", 20, 
            true, 
            "Ability to speak the Catharese language.", 
            false)]
        Catharese = 19,

        [Skill(SkillCategoryType.Languages, 
            "Dosh", 
            20, 
            true, 
            "Ability to speak the Dosh language.", 
            false)]
        Dosh = 20,

        [Skill(SkillCategoryType.Languages, 
            "Shyriiwook (Wookieespeak)", 
            20, 
            true, 
            "Ability to speak the Shyriiwook (Wookieespeak) language.", 
            false)]
        Shyriiwook = 21,

        [Skill(SkillCategoryType.Languages, 
            "Droidspeak", 
            20, 
            true, 
            "Ability to speak the Droidspeak language.", 
            false)]
        Droidspeak = 22,

        [Skill(SkillCategoryType.Languages, 
            "Basic", 
            20, 
            true, 
            "Ability to speak the Galactic Basic language.", 
            false)]
        Basic = 23,

        [Skill(SkillCategoryType.Languages, 
            "Mandoa", 
            20, 
            true, 
            "Ability to speak the Mandoa language.", 
            false)]
        Mandoa = 24,

        [Skill(SkillCategoryType.Languages, 
            "Huttese", 
            20, 
            true, 
            "Ability to speak the Huttese language.", 
            false)]
        Huttese = 25,

        [Skill(SkillCategoryType.Languages, 
            "Mon Calamarian", 
            20, 
            true, 
            "Ability to speak the Mon Calamarian language.", 
            false)]
        MonCalamarian = 26,

        [Skill(SkillCategoryType.Languages, 
            "Ugnaught", 
            20, 
            true, 
            "Ability to speak the Ugnaught language.", 
            false)]
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

        public SkillAttribute(
            SkillCategoryType category,
            string name,
            int maxRank,
            bool isActive,
            string description,
            bool contributesToSkillCap)
        {
            Category = category;
            Name = name;
            MaxRank = maxRank;
            IsActive = isActive;
            Description = description;
            ContributesToSkillCap = contributesToSkillCap;
        }
    }
}
