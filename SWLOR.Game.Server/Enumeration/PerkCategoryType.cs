using System;

namespace SWLOR.Game.Server.Enumeration
{
    public enum PerkCategoryType
    {
        [PerkCategory("Invalid", false)]
        Invalid = 0,
        [PerkCategory("General", true)]
        General = 1,
        [PerkCategory("Knight", true)]
        Knight = 2,
        [PerkCategory("Monk", true)]
        Monk = 3,
        [PerkCategory("Thief", true)]
        Thief = 4,
        [PerkCategory("Black Mage", true)]
        BlackMage = 5,
        [PerkCategory("White Mage", true)]
        WhiteMage = 6,
        [PerkCategory("Red Mage", true)]
        RedMage = 7,
        [PerkCategory("Ninja", true)]
        Ninja = 8,
        [PerkCategory("Specialist", true)]
        Specialist = 9,
        [PerkCategory("Sniper", true)]
        Sniper = 10,
        [PerkCategory("Dark Knight", true)]
        DarkKnight = 11,
        [PerkCategory("Blacksmith", true)]
        Blacksmith = 12,
        [PerkCategory("Leatherworker", true)]
        Leatherworker = 13,
        [PerkCategory("Alchemist", true)]
        Alchemist = 14,
        [PerkCategory("Culinarian", true)]
        Culinarian = 15

    }

    public class PerkCategoryAttribute : Attribute
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }

        public PerkCategoryAttribute(string name, bool isActive)
        {
            Name = name;
            IsActive = isActive;
        }
    }
}
