using System;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.SkillService
{
    // Note: Corresponds to iprp_skill.2da
    // New additions or changes to this file should also be made to the 2DA.
    public enum SkillType
    {
        [Skill(SkillCategoryType.Invalid,
            "Invalid",
            0,
            false,
            "Unused in-game.",
            false,
            false)]
        Invalid = 0,

        [Skill(SkillCategoryType.Combat,
            "One-Handed",
            50,
            true,
            "Ability to use one-handed weapons like vibroblades, finesse vibroblades, and lightsabers.",
            true,
            false,
            CombatPointCategoryType.Weapon)]
        OneHanded = 1,

        [Skill(SkillCategoryType.Combat,
            "Two-Handed",
            50,
            true,
            "Ability to use heavy weapons like heavy vibroblades, polearms, and saberstaffs in combat.",
            true,
            false,
            CombatPointCategoryType.Weapon)]
        TwoHanded = 2,

        [Skill(SkillCategoryType.Combat,
            "Martial Arts", 50,
            true,
            "Ability to fight using katars and staves in combat.",
            true,
            false,
            CombatPointCategoryType.Weapon)]
        MartialArts = 3,

        [Skill(SkillCategoryType.Combat,
            "Ranged",
            50,
            true,
            "Ability to use ranged weapons like pistols, shurikens, and rifles in combat.",
            true,
            false,
            CombatPointCategoryType.Weapon)]
        Ranged = 4,

        [Skill(SkillCategoryType.Combat,
            "Force",
            50,
            true,
            "Ability to use Force abilities.",
            true,
            false,
            CombatPointCategoryType.Utility,
            CharacterType.ForceSensitive)]
        Force = 5,

        [Skill(SkillCategoryType.Combat,
            "Armor",
            50,
            true,
            "Ability to effectively wear and defend against attacks with armor.",
            true,
            false)]
        Armor = 6,

        [Skill(SkillCategoryType.Utility,
            "Piloting",
            50,
            true,
            "Ability to pilot starships, follow navigation charts, and control starship systems.",
            true,
            false)]
        Piloting = 7,

        [Skill(SkillCategoryType.Utility,
            "First Aid",
            50,
            true,
            "Ability to treat bodily injuries in the field with healing kits and stim packs.",
            true,
            false)]
        FirstAid = 8,

        [Skill(SkillCategoryType.Crafting,
            "Smithery",
            50,
            true,
            "Ability to create weapons and armor like vibroblades, blasters, and helmets.",
            true,
            true)]
        Smithery = 9,

        [Skill(SkillCategoryType.Crafting,
            "Fabrication",
            50,
            true,
            "Ability to create base structures and furniture.",
            true,
            true)]
        Fabrication = 10,

        [Skill(SkillCategoryType.Crafting,
            "Gathering",
            50,
            true,
            "Ability to harvest raw materials and scavenge for supplies.",
            true,
            false)]
        Gathering = 11,

        [Skill(SkillCategoryType.Utility,
            "Leadership",
            50,
            true,
            "Ability to handle people, negotiate, and manage relations.",
            true,
            false)]
        Leadership = 12,

        [Skill(SkillCategoryType.Combat,
            "Beast Mastery",
            50,
            true,
            "Ability to tame wild animals, raise them, and train them.",
            true,
            false)]
        BeastMastery = 13,

        [Skill(SkillCategoryType.Languages,
            "Mirialan",
            20,
            true,
            "Ability to speak the Mirialan language.",
            false,
            false)]
        Mirialan = 14,

        [Skill(SkillCategoryType.Languages,
            "Bothese",
            20,
            true,
            "Ability to speak the Bothese language.",
            false,
            false)]
        Bothese = 15,

        [Skill(SkillCategoryType.Languages,
            "Cheunh",
            20,
            true,
            "Ability to speak the Cheunh language.",
            false,
            false)]
        Cheunh = 16,


        [Skill(SkillCategoryType.Languages,
            "Zabraki",
            20,
            true,
            "Ability to speak the Zabraki language.",
            false,
            false)]
        Zabraki = 17,

        [Skill(SkillCategoryType.Languages,
            "Twi'leki (Ryl)",
            20,
            true,
            "Ability to speak the Twi'leki (Ryl) language.",
            false,
            false)]
        Twileki = 18,

        [Skill(SkillCategoryType.Languages,
            "Catharese", 20,
            true,
            "Ability to speak the Catharese language.",
            false,
            false)]
        Catharese = 19,

        [Skill(SkillCategoryType.Languages,
            "Dosh",
            20,
            true,
            "Ability to speak the Dosh language.",
            false,
            false)]
        Dosh = 20,

        [Skill(SkillCategoryType.Languages,
            "Shyriiwook",
            20,
            true,
            "Ability to speak the Shyriiwook (Wookieespeak) language.",
            false,
            false)]
        Shyriiwook = 21,

        [Skill(SkillCategoryType.Languages,
            "Droidspeak",
            20,
            true,
            "Ability to speak the Droidspeak language.",
            false,
            false)]
        Droidspeak = 22,

        [Skill(SkillCategoryType.Languages,
            "Basic",
            20,
            true,
            "Ability to speak the Galactic Basic language.",
            false,
            false)]
        Basic = 23,

        [Skill(SkillCategoryType.Languages,
            "Mandoa",
            20,
            true,
            "Ability to speak the Mandoa language.",
            false,
            false)]
        Mandoa = 24,

        [Skill(SkillCategoryType.Languages,
            "Huttese",
            20,
            true,
            "Ability to speak the Huttese language.",
            false,
            false)]
        Huttese = 25,

        [Skill(SkillCategoryType.Languages,
            "Mon Calamarian",
            20,
            true,
            "Ability to speak the Mon Calamarian language.",
            false,
            false)]
        MonCalamarian = 26,

        [Skill(SkillCategoryType.Languages,
            "Ugnaught",
            20,
            true,
            "Ability to speak the Ugnaught language.",
            false,
            false)]
        Ugnaught = 27,

        [Skill(SkillCategoryType.Languages,
            "Rodese",
            20,
            true,
            "Ability to speak the Rodese language.",
            false,
            false)]
        Rodese = 28,

        [Skill(SkillCategoryType.Languages,
            "Togruti",
            20,
            true,
            "Ability to speak the Togruti language.",
            false,
            false)]
        Togruti = 29,

        [Skill(SkillCategoryType.Languages,
            "Kel Dor",
            20,
            true,
            "Ability to speak the Kel Dor language.",
            false,
            false)]
        KelDor = 30,

        [Skill(SkillCategoryType.Crafting,
            "Agriculture",
            50,
            true,
            "Ability to farm, fish, and cook.",
            true,
            true)]
        Agriculture = 31,

        [Skill(SkillCategoryType.Crafting,
            "Engineering",
            50,
            true,
            "Ability to create starships, modules, droids, and other electronic & mechanical items.",
            true,
            true)]
        Engineering = 32,

        [Skill(SkillCategoryType.Combat,
            "Devices",
            50,
            true,
            "Ability to use grenades, bombs, and other electronics.",
            true,
            false,
            CombatPointCategoryType.Utility,
            CharacterType.Standard)]
        Devices = 33,
    }

    public class SkillAttribute : Attribute
    {
        public SkillCategoryType Category { get; set; }
        public string Name { get; set; }
        public int MaxRank { get; set; }
        public bool IsActive { get; set; }
        public string Description { get; set; }
        public bool ContributesToSkillCap { get; set; }
        public bool IsShownInCraftMenu { get; set; }
        public CharacterType CharacterTypeRestriction { get; set; }

        public CombatPointCategoryType CombatPointCategory { get; set; } 

        public SkillAttribute(
            SkillCategoryType category,
            string name,
            int maxRank,
            bool isActive,
            string description,
            bool contributesToSkillCap,
            bool isShownInCraftMenu,
            CombatPointCategoryType combatPointCategory = CombatPointCategoryType.Exempt,
            CharacterType characterTypeRestriction = CharacterType.Invalid)
        {
            Category = category;
            Name = name;
            MaxRank = maxRank;
            IsActive = isActive;
            Description = description;
            ContributesToSkillCap = contributesToSkillCap;
            IsShownInCraftMenu = isShownInCraftMenu;
            CharacterTypeRestriction = characterTypeRestriction;
            CombatPointCategory = combatPointCategory;
        }
    }
}
