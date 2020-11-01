using System;

namespace SWLOR.Game.Server.Enumeration
{
    public enum SkillCategoryType
    {
        [SkillCategory("Invalid", false, 0)]
        Invalid = 0,
        [PerkCategory("Melee Combat", true)]
        MeleeCombat = 1,
        [PerkCategory("Ranged Combat", true)]
        RangedCombat = 2,
        [PerkCategory("Defense", true)]
        Defense = 3,
        [PerkCategory("Crafting", true)]
        Crafting = 4,
        [PerkCategory("Gathering", true)]
        Gathering = 5,
        [PerkCategory("Utility", true)]
        Utility = 6,
        [PerkCategory("Force", true)]
        Force = 7,
        [PerkCategory("Languages", true)]
        Languages = 8,
    }

    public class SkillCategoryAttribute : Attribute
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public int Sequence { get; set; }

        public SkillCategoryAttribute(string name, bool isActive, int sequence)
        {
            Name = name;
            IsActive = isActive;
            Sequence = sequence;
        }
    }
}