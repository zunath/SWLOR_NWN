using System;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Enumeration
{
    public enum SkillType
    {
        [Skill(SkillCategoryType.Invalid, "Invalid", 0, false, "Unused in-game.", false, AbilityType.Invalid, AbilityType.Invalid)]
        Invalid = 0,

        // Ability
        [Skill(SkillCategoryType.Ability, "Chivalry", 50, true, "Ability to use bash, cleave, and other knight-related actions.", true, AbilityType.Invalid, AbilityType.Invalid)]
        Chivalry = 1,
        [Skill(SkillCategoryType.Ability, "Chi", 50, true, "Ability to use self-buff and restoration techniques.", true, AbilityType.Invalid, AbilityType.Invalid)]
        Chi = 2,
        [Skill(SkillCategoryType.Ability, "Thievery", 50, true, "Ability to use stealth, steal, and other thievery-related actions.", true, AbilityType.Invalid, AbilityType.Invalid)]
        Thievery = 3,
        [Skill(SkillCategoryType.Ability, "Black Magic", 50, true, "Ability to use fire, blizzard, and other black magic-related actions.", true, AbilityType.Invalid, AbilityType.Invalid)]
        BlackMagic = 4,
        [Skill(SkillCategoryType.Ability, "White Magic", 50, true, "Ability to use cure, raise, and other white magic-related actions.", true, AbilityType.Invalid, AbilityType.Invalid)]
        WhiteMagic = 5,
        [Skill(SkillCategoryType.Ability, "Red Magic", 50, true, "Ability to use poison, convert, and other red magic-related actions.", true, AbilityType.Invalid, AbilityType.Invalid)]
        RedMagic = 6,

        // 7 is free

        [Skill(SkillCategoryType.Ability, "Ninjitsu", 50, true, "Ability to use utsusemi, raiton, and other ninja-related actions.", true, AbilityType.Invalid, AbilityType.Invalid)]
        Ninjitsu = 8,
        [Skill(SkillCategoryType.Ability, "Swordplay", 50, true, "Ability to use renzokuken, royal guard, and other specialist-related actions.", true, AbilityType.Invalid, AbilityType.Invalid)]
        Swordplay = 9,
        [Skill(SkillCategoryType.Ability, "Marksmanship", 50, true, "Ability to use heat bullet, tranquilizer, and other sniper-related actions..", true, AbilityType.Invalid, AbilityType.Invalid)]
        Marksmanship = 10,
        [Skill(SkillCategoryType.Ability, "Darkness", 50, true, "Ability to use souleater, last resort, and other darkness-related actions..", true, AbilityType.Invalid, AbilityType.Invalid)]
        Darkness = 11,

        // Armor
        [Skill(SkillCategoryType.Armor, "Heavy Armor", 50, true, "Ability to use heavy armor.", true, AbilityType.Constitution, AbilityType.Strength)]
        HeavyArmor = 12,
        [Skill(SkillCategoryType.Armor, "Light Armor", 50, true, "Ability to use light armor.", true, AbilityType.Constitution, AbilityType.Dexterity)]
        LightArmor = 13,
        [Skill(SkillCategoryType.Armor, "Mystic Armor", 50, true, "Ability to use mystic armor.", true, AbilityType.Charisma, AbilityType.Constitution)]
        MysticArmor = 14,

        // Weapon
        [Skill(SkillCategoryType.Weapon, "Longsword", 50, true, "Ability to use longswords.", true, AbilityType.Strength, AbilityType.Constitution)]
        Longsword = 15,
        [Skill(SkillCategoryType.Weapon, "Knuckles", 50, true, "Ability to use fist knuckle weapons.", true, AbilityType.Strength, AbilityType.Wisdom)]
        Knuckles = 16,
        [Skill(SkillCategoryType.Weapon, "Dagger", 50, true, "Ability to use daggers.", true, AbilityType.Dexterity, AbilityType.Strength)]
        Dagger = 17,
        [Skill(SkillCategoryType.Weapon, "Staff", 50, true, "Ability to use staves.", true, AbilityType.Intelligence, AbilityType.Charisma)]
        Staff = 18,
        [Skill(SkillCategoryType.Weapon, "Rod", 50, true, "Ability to use rods.", true, AbilityType.Wisdom, AbilityType.Charisma)]
        Rod = 19,

        // 20 is free

        [Skill(SkillCategoryType.Weapon, "Katana", 50, true, "Ability to use katanas.", true, AbilityType.Strength, AbilityType.Dexterity)]
        Katana = 21,
        [Skill(SkillCategoryType.Weapon, "Gunblade", 50, true, "Ability to use gunblades.", true, AbilityType.Strength, AbilityType.Constitution)]
        Gunblade = 22,
        [Skill(SkillCategoryType.Weapon, "Rifle", 50, true, "Ability to use rifles.", true, AbilityType.Dexterity, AbilityType.Constitution)]
        Rifle = 23,
        [Skill(SkillCategoryType.Weapon, "Rapier", 50, true, "Ability to use rapiers.", true, AbilityType.Dexterity, AbilityType.Intelligence)]
        Rapier = 24,
        [Skill(SkillCategoryType.Weapon, "Great Sword", 50, true, "Ability to use great swords.", true, AbilityType.Strength, AbilityType.Intelligence)]
        GreatSword = 25,

        // Crafting
        [Skill(SkillCategoryType.Crafting, "Blacksmithing", 50, true, "Ability to create metal-based weapons and heavy armors.", true, AbilityType.Invalid, AbilityType.Invalid)]
        Blacksmithing = 26,
        [Skill(SkillCategoryType.Crafting, "Leathercraft", 50, true, "Ability to create leather-based items and light armors.", true, AbilityType.Invalid, AbilityType.Invalid)]
        Leathercraft = 27,
        [Skill(SkillCategoryType.Crafting, "Alchemy", 50, true, "Ability to create magic-based weapons and mystic armors.", true, AbilityType.Invalid, AbilityType.Invalid)]
        Alchemy = 28,

        // 29 is free

        [Skill(SkillCategoryType.Crafting, "Cooking", 50, true, "Ability to create food items which grant temporary buffs when consumed.", true, AbilityType.Invalid, AbilityType.Invalid)]
        Cooking = 30,
        [Skill(SkillCategoryType.Crafting, "Mining", 50, true, "Ability to gather raw ore and refine it.", true, AbilityType.Invalid, AbilityType.Invalid)]
        Mining = 31,
        [Skill(SkillCategoryType.Crafting, "Botany", 50, true, "Ability to gather raw lumber and refine it.", true, AbilityType.Invalid, AbilityType.Invalid)]
        Botany = 32,

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
