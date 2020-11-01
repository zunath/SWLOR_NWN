using System;

namespace SWLOR.Game.Server.Enumeration
{
    public enum SkillCategoryType
    {
        [SkillCategory("Invalid", false, 0)]
        Invalid = 0,
        [SkillCategory("Weapon", true, 1)]
        Weapon = 1,
        [SkillCategory("Armor", true, 2)]
        Armor = 3,
        [SkillCategory("Ability", true, 3)]
        Ability = 4,
        [SkillCategory("Crafting", true, 4)]
        Crafting = 5,
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