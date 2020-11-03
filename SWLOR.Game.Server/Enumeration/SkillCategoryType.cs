using System;

namespace SWLOR.Game.Server.Enumeration
{
    public enum SkillCategoryType
    {
        [SkillCategory("Invalid", false, 0)]
        Invalid = 0,
        [SkillCategory("Melee Combat", true, 1)]
        MeleeCombat = 1,
        [SkillCategory("Ranged Combat", true, 2)]
        RangedCombat = 2,
        [SkillCategory("Defense", true, 3)]
        Defense = 3,
        [SkillCategory("Crafting", true, 4)]
        Crafting = 4,
        [SkillCategory("Gathering", true, 5)]
        Gathering = 5,
        [SkillCategory("Utility", true, 6)]
        Utility = 6,
        [SkillCategory("Force", true, 7)]
        Force = 7,
        [SkillCategory("Languages", true, 8)]
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