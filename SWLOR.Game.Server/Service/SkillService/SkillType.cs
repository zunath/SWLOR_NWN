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
            false,
            false)]
        Invalid = 0,

        [Skill(SkillCategoryType.Combat,
            "Force",
            50,
            true,
            "Ability to use Force abilities.",
            true,
            false,
            false,
            CombatPointCategoryType.Utility,
            CharacterType.ForceSensitive,
            BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        Force = 5,

        [Skill(SkillCategoryType.Combat,
            "Armor",
            50,
            true,
            "Ability to effectively wear and defend against attacks with armor.",
            true,
            false,
            false,
            BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        Armor = 6,

        [Skill(SkillCategoryType.Utility,
            "Piloting",
            50,
            true,
            "Ability to pilot starships, follow navigation charts, and control starship systems.",
            true,
            false,
            false)]
        Piloting = 7,

        [Skill(SkillCategoryType.Utility,
            "First Aid",
            50,
            true,
            "Ability to treat bodily injuries in the field with healing kits and stim packs.",
            true,
            false,
            false,
            BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        FirstAid = 8,

        [Skill(SkillCategoryType.Crafting,
            "Smithery",
            50,
            true,
            "Ability to create weapons and armor like vibroblades, blasters, and helmets.",
            true,
            true,
            true)]
        Smithery = 9,

        [Skill(SkillCategoryType.Crafting,
            "Fabrication",
            50,
            true,
            "Ability to create base structures and furniture.",
            true,
            true,
            false)]
        Fabrication = 10,

        [Skill(SkillCategoryType.Crafting,
            "Gathering",
            50,
            true,
            "Ability to harvest raw materials and scavenge for supplies.",
            true,
            false,
            false)]
        Gathering = 11,

        [Skill(SkillCategoryType.Utility,
            "Leadership",
            50,
            true,
            "Ability to handle people, negotiate, and manage relations.",
            true,
            false,
            false)]
        Leadership = 12,

        [Skill(SkillCategoryType.Combat,
            "Beast Mastery",
            50,
            true,
            "Ability to tame wild animals, raise them, and train them.",
            true,
            false,
            false,
            BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        BeastMastery = 13,

        [Skill(SkillCategoryType.Languages,
            "Mirialan",
            20,
            true,
            "Ability to speak the Mirialan language.",
            false,
            false,
            false)]
        Mirialan = 14,

        [Skill(SkillCategoryType.Languages,
            "Bothese",
            20,
            true,
            "Ability to speak the Bothese language.",
            false,
            false,
            false)]
        Bothese = 15,

        [Skill(SkillCategoryType.Languages,
            "Cheunh",
            20,
            true,
            "Ability to speak the Cheunh language.",
            false,
            false,
            false)]
        Cheunh = 16,


        [Skill(SkillCategoryType.Languages,
            "Zabraki",
            20,
            true,
            "Ability to speak the Zabraki language.",
            false,
            false,
            false)]
        Zabraki = 17,

        [Skill(SkillCategoryType.Languages,
            "Twi'leki (Ryl)",
            20,
            true,
            "Ability to speak the Twi'leki (Ryl) language.",
            false,
            false,
            false)]
        Twileki = 18,

        [Skill(SkillCategoryType.Languages,
            "Catharese", 20,
            true,
            "Ability to speak the Catharese language.",
            false,
            false,
            false)]
        Catharese = 19,

        [Skill(SkillCategoryType.Languages,
            "Dosh",
            20,
            true,
            "Ability to speak the Dosh language.",
            false,
            false,
            false)]
        Dosh = 20,

        [Skill(SkillCategoryType.Languages,
            "Shyriiwook",
            20,
            true,
            "Ability to speak the Shyriiwook (Wookieespeak) language.",
            false,
            false,
            false)]
        Shyriiwook = 21,

        [Skill(SkillCategoryType.Languages,
            "Droidspeak",
            20,
            true,
            "Ability to speak the Droidspeak language.",
            false,
            false,
            false)]
        Droidspeak = 22,

        [Skill(SkillCategoryType.Languages,
            "Basic",
            20,
            true,
            "Ability to speak the Galactic Basic language.",
            false,
            false,
            false)]
        Basic = 23,

        [Skill(SkillCategoryType.Languages,
            "Mandoa",
            20,
            true,
            "Ability to speak the Mandoa language.",
            false,
            false,
            false)]
        Mandoa = 24,

        [Skill(SkillCategoryType.Languages,
            "Huttese",
            20,
            true,
            "Ability to speak the Huttese language.",
            false,
            false,
            false)]
        Huttese = 25,

        [Skill(SkillCategoryType.Languages,
            "Mon Calamarian",
            20,
            true,
            "Ability to speak the Mon Calamarian language.",
            false,
            false,
            false)]
        MonCalamarian = 26,

        [Skill(SkillCategoryType.Languages,
            "Ugnaught",
            20,
            true,
            "Ability to speak the Ugnaught language.",
            false,
            false,
            false)]
        Ugnaught = 27,

        [Skill(SkillCategoryType.Languages,
            "Rodese",
            20,
            true,
            "Ability to speak the Rodese language.",
            false,
            false,
            false)]
        Rodese = 28,

        [Skill(SkillCategoryType.Languages,
            "Togruti",
            20,
            true,
            "Ability to speak the Togruti language.",
            false,
            false,
            false)]
        Togruti = 29,

        [Skill(SkillCategoryType.Languages,
            "Kel Dor",
            20,
            true,
            "Ability to speak the Kel Dor language.",
            false,
            false,
            false)]
        KelDor = 30,

        [Skill(SkillCategoryType.Crafting,
            "Agriculture",
            50,
            true,
            "Ability to farm, fish, and cook.",
            true,
            true,
            true)]
        Agriculture = 31,

        [Skill(SkillCategoryType.Crafting,
            "Engineering",
            50,
            true,
            "Ability to create starships, modules, droids, and other electronic & mechanical items.",
            true,
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
            false,
            CombatPointCategoryType.Utility,
            CharacterType.Standard)]
        Devices = 33,

        [Skill(SkillCategoryType.Languages,
            "Nautila",
            20,
            true,
            "Ability to speak the Nautila language.",
            false,
            false,
            false)]
        Nautila = 34,

        [Skill(SkillCategoryType.Languages,
            "Ewokese",
            20,
            true,
           "Ability to speak the Ewok language.",
           false,
           false,
           false)]
        Ewokese = 35,

        [Skill(SkillCategoryType.Combat,
            "Vibroblade",
            50,
            true,
            "Ability to use vibroblades in combat.",
            true,
            false,
            false,
            CombatPointCategoryType.Weapon,
            BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        Vibroblade = 36,

        [Skill(SkillCategoryType.Combat,
            "Vibroknife",
            50,
            true,
            "Ability to use vibroknives in combat.",
            true,
            false,
            false,
            CombatPointCategoryType.Weapon,
            BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        Vibroknife = 37,

        [Skill(SkillCategoryType.Combat,
            "Lightsaber",
            50,
            true,
            "Ability to use lightsabers in combat.",
            true,
            false,
            false,
            CombatPointCategoryType.Weapon,
            CharacterType.ForceSensitive,
            BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        Lightsaber = 38,

        [Skill(SkillCategoryType.Combat,
            "Heavy Vibroblade",
            50,
            true,
            "Ability to use heavy vibroblades in combat.",
            true,
            false,
            false,
            CombatPointCategoryType.Weapon,
            BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        HeavyVibroblade = 39,

        [Skill(SkillCategoryType.Combat,
            "Spear",
            50,
            true,
            "Ability to use spears and polearms in combat.",
            true,
            false,
            false,
            CombatPointCategoryType.Weapon,
            BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        Spear = 40,

        [Skill(SkillCategoryType.Combat,
            "Twin Blade",
            50,
            true,
            "Ability to use twin blades in combat.",
            true,
            false,
            false,
            CombatPointCategoryType.Weapon,
            BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        TwinBlade = 41,

        [Skill(SkillCategoryType.Combat,
            "Saberstaff",
            50,
            true,
            "Ability to use saberstaffs in combat.",
            true,
            false,
            false,
            CombatPointCategoryType.Weapon,
            CharacterType.ForceSensitive,
            BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        Saberstaff = 42,

        [Skill(SkillCategoryType.Combat,
            "Katar",
            50,
            true,
            "Ability to use katars in combat.",
            true,
            false,
            false,
            CombatPointCategoryType.Weapon,
            BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        Katar = 43,

        [Skill(SkillCategoryType.Combat,
            "Staff",
            50,
            true,
            "Ability to use staves in combat.",
            true,
            false,
            false,
            CombatPointCategoryType.Weapon,
            BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        Staff = 44,

        [Skill(SkillCategoryType.Combat,
            "Pistol",
            50,
            true,
            "Ability to use pistols in combat.",
            true,
            false,
            false,
            CombatPointCategoryType.Weapon,
            BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        Pistol = 45,

        [Skill(SkillCategoryType.Combat,
            "Rifle",
            50,
            true,
            "Ability to use rifles in combat.",
            true,
            false,
            false,
            CombatPointCategoryType.Weapon,
            BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        Rifle = 46,

        [Skill(SkillCategoryType.Combat,
            "Throwing",
            50,
            true,
            "Ability to use throwing weapons in combat.",
            true,
            false,
            false,
            CombatPointCategoryType.Weapon,
            BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        Throwing = 47,

        [Skill(SkillCategoryType.Combat,
            "Mimicry",
            50,
            true,
            "Ability to analyze enemy creatures and replicate their combat techniques.",
            true,
            false,
            false,
            CombatPointCategoryType.Utility,
            BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        Mimicry = 48,

        [Skill(SkillCategoryType.Utility,
            "Espionage",
            50,
            true,
            "Ability to craft poisons, traps, employ stealth, and slice security systems.",
            true,
            true,
            false,
            CombatPointCategoryType.Utility)]
        Espionage = 49,

        [Skill(SkillCategoryType.Languages,
            "Arkanian",
            20,
            true,
            "Ability to speak the Arkanian language.",
            false,
            false,
            false)]
        Arkanian = 50,

        [Skill(SkillCategoryType.Combat,
            "Unarmed",
            50,
            true,
            "Ability to fight with equipped Power Gloves and hand-to-hand striking weapons.",
            true,
            false,
            false,
            CombatPointCategoryType.Weapon,
            BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        Unarmed = 51,

        [Skill(SkillCategoryType.Combat,
            "Twin Pistol",
            50,
            true,
            "Ability to fight with a pistol and a companion pistol.",
            true,
            false,
            false,
            CombatPointCategoryType.Weapon,
            BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        TwinPistol = 52,

        [Skill(SkillCategoryType.Utility,
            "Performance",
            50,
            true,
            "Ability to entertain an audience through dance and music, inspiring those who stay to watch.",
            true,
            false,
            false,
            BodyFrameRestriction = BodyFrameRestriction.HumanoidOnly)]
        Performance = 60,

        [Skill(SkillCategoryType.Combat,
            "Chassis",
            50,
            true,
            "Ability of an astromech to withstand attacks through its frame, plating, and internal modules.",
            true,
            false,
            false,
            BodyFrameRestriction = BodyFrameRestriction.AstromechOnly)]
        Chassis = 61,

        [Skill(SkillCategoryType.Utility,
            "Utility Systems",
            50,
            true,
            "Ability of an astromech to repair, restore, and reinforce itself and its allies in the field.",
            true,
            false,
            false,
            BodyFrameRestriction = BodyFrameRestriction.AstromechOnly)]
        UtilitySystems = 62,

        [Skill(SkillCategoryType.Combat,
            "Armaments",
            50,
            true,
            "Ability of an astromech to fight with the tools built into its chassis: the shock prod and the blaster mount.",
            true,
            false,
            false,
            CombatPointCategoryType.Weapon,
            BodyFrameRestriction = BodyFrameRestriction.AstromechOnly)]
        Armaments = 63,
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
        public bool IsShownInResearchMenu { get; set; }
        public CharacterType CharacterTypeRestriction { get; set; }

        /// <summary>
        /// Limits the skill to one body frame. Skills that need a humanoid body are closed to astromechs.
        /// </summary>
        public BodyFrameRestriction BodyFrameRestriction { get; set; }

        public bool IsAvailableToCharacterType(CharacterType characterType)
        {
            return CharacterTypeRestriction == CharacterType.Invalid ||
                   CharacterTypeRestriction == characterType;
        }

        /// <summary>
        /// Whether a character of this type and body frame can train the skill at all.
        /// </summary>
        public bool IsAvailableTo(CharacterType characterType, BodyFrame bodyFrame)
        {
            return IsAvailableToCharacterType(characterType) && BodyFrameRestriction.Allows(bodyFrame);
        }

        public CombatPointCategoryType CombatPointCategory { get; set; }

        public SkillAttribute(
            SkillCategoryType category,
            string name,
            int maxRank,
            bool isActive,
            string description,
            bool contributesToSkillCap,
            bool isShownInCraftMenu,
            bool isShownInResearchMenu,
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
            IsShownInResearchMenu = isShownInResearchMenu;
            CharacterTypeRestriction = characterTypeRestriction;
            CombatPointCategory = combatPointCategory;
        }
    }
}
