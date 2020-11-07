using System;

namespace SWLOR.Game.Server.Enumeration
{
    public enum PerkCategoryType
    {
        [PerkCategory("Invalid", false)]
        Invalid = 0,
        [PerkCategory("General", true)]
        General = 1,
        [PerkCategory("Armor", true)] // light/heavy/force/shields
        Armor = 2,
        [PerkCategory("Crafting", true)]
        Crafting = 3,
        [PerkCategory("Gathering", true)] // harvesting
        Gathering = 4,
        [PerkCategory("Force Alter", true)]
        ForceAlter = 6,
        [PerkCategory("Force Control", true)]
        ForceControl = 7,
        [PerkCategory("Force Sense", true)]
        ForceSense = 8,
        [PerkCategory("Medicine", true)]
        Medicine = 5,
        [PerkCategory("Piloting", true)]
        Piloting = 5,
        [PerkCategory("Weapon - Blaster", true)]
        Blaster = 5,
        [PerkCategory("Weapon - Lightsaber", true)]
        Lightsaber = 5,
        [PerkCategory("Weapon - Martial Arts", true)]
        MartialArts = 5,
        [PerkCategory("Weapon - One Handed", true)]
        OneHanded = 5,
        [PerkCategory("Weapon - Throwing", true)]
        Throwing = 5,

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
