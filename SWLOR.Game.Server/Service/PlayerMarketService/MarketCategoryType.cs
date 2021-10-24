using System;

namespace SWLOR.Game.Server.Service.PlayerMarketService
{
    public enum MarketCategoryType
    {
        [MarketCategory("Invalid", MarketGroupType.Invalid, false)]
        Invalid = 0,

        [MarketCategory("Vibroblade", MarketGroupType.Weapon, true)]
        Vibroblade = 1,
        [MarketCategory("Fin. Vibroblade", MarketGroupType.Weapon, true)]
        FinesseVibroblade = 2,
        [MarketCategory("Polearm", MarketGroupType.Weapon, true)]
        Polearm = 3,
        [MarketCategory("Katar", MarketGroupType.Weapon, true)]
        Katar = 4,
        [MarketCategory("Staff", MarketGroupType.Weapon, true)]
        Staff = 5,
        [MarketCategory("Pistol", MarketGroupType.Weapon, true)]
        Pistol = 6,
        [MarketCategory("Shuriken", MarketGroupType.Weapon, true)]
        Shuriken = 7,
        [MarketCategory("Twin Blade", MarketGroupType.Weapon, true)]
        TwinBlade = 8,
        [MarketCategory("Rifle", MarketGroupType.Weapon, true)]
        Rifle = 9,

        [MarketCategory("Shield", MarketGroupType.Armor, true)]
        Shield = 10,
        [MarketCategory("Cloak", MarketGroupType.Armor, true)]
        Cloak = 11,
        [MarketCategory("Belt", MarketGroupType.Armor, true)]
        Belt = 12,
        [MarketCategory("Ring", MarketGroupType.Armor, true)]
        Ring = 13,
        [MarketCategory("Necklace", MarketGroupType.Armor, true)]
        Necklace = 14,
        [MarketCategory("Breastplate", MarketGroupType.Armor, true)]
        Breastplate = 15,
        [MarketCategory("Helmet", MarketGroupType.Armor, true)]
        Helmet = 16,
        [MarketCategory("Bracer", MarketGroupType.Armor, true)]
        Bracer = 17,
        [MarketCategory("Legging", MarketGroupType.Armor, true)]
        Legging = 18,
        [MarketCategory("Tunic", MarketGroupType.Armor, true)]
        Tunic = 19,
        [MarketCategory("Cap", MarketGroupType.Armor, true)]
        Cap = 20,
        [MarketCategory("Glove", MarketGroupType.Armor, true)]
        Glove = 21,
        [MarketCategory("Boot", MarketGroupType.Armor, true)]
        Boot = 22,

        [MarketCategory("Recipe", MarketGroupType.Other, true)]
        Recipe = 23,
        [MarketCategory("Smithery", MarketGroupType.Other, true)]
        Smithery = 24,
        [MarketCategory("Fabrication", MarketGroupType.Other, true)]
        Fabrication = 25,
        [MarketCategory("Gathering", MarketGroupType.Other, true)]
        Gathering = 27,

        [MarketCategory("Starship", MarketGroupType.Other, true)]
        Starship = 28,
        [MarketCategory("First Aid", MarketGroupType.Other, true)]
        FirstAid = 29,


        [MarketCategory("Miscellaneous", MarketGroupType.Other, true)]
        Miscellaneous = 99
    }

    public class MarketCategoryAttribute : Attribute
    {
        public string Name { get; }
        public MarketGroupType Group { get; }
        public bool IsActive { get; }

        public MarketCategoryAttribute(string name, MarketGroupType group, bool isActive)
        {
            Name = name;
            Group = group;
            IsActive = isActive;
        }
    }
}
