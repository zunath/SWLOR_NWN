using System;

namespace SWLOR.Game.Server.Service.PlayerMarketService
{
    public enum MarketCategoryType
    {
        [MarketCategory("Invalid",  false)]
        Invalid = 0,

        [MarketCategory("Vibroblade",  true)]
        Vibroblade = 1,
        [MarketCategory("Fin. Vibroblade",  true)]
        FinesseVibroblade = 2,
        [MarketCategory("Hvy. Vibroblade", true)]
        HeavyVibroblade = 3,
        [MarketCategory("Polearm",  true)]
        Polearm = 4,
        [MarketCategory("Katar",  true)]
        Katar = 5,
        [MarketCategory("Staff",  true)]
        Staff = 6,
        [MarketCategory("Pistol",  true)]
        Pistol = 7,
        [MarketCategory("Throwing",  true)]
        Throwing = 8,
        [MarketCategory("Twin Blade",  true)]
        TwinBlade = 9,
        [MarketCategory("Rifle",  true)]
        Rifle = 10,
        [MarketCategory("Shield",  true)]
        Shield = 11,
        [MarketCategory("Cloak",  true)]
        Cloak = 12,
        [MarketCategory("Belt",  true)]
        Belt = 13,
        [MarketCategory("Ring",  true)]
        Ring = 14,
        [MarketCategory("Necklace",  true)]
        Necklace = 15,
        [MarketCategory("Breastplate",  true)]
        Breastplate = 16,
        [MarketCategory("Helmet",  true)]
        Helmet = 17,
        [MarketCategory("Bracer",  true)]
        Bracer = 18,
        [MarketCategory("Legging",  true)]
        Legging = 19,
        [MarketCategory("Tunic",  true)]
        Tunic = 20,
        [MarketCategory("Cap",  true)]
        Cap = 21,
        [MarketCategory("Glove",  true)]
        Glove = 22,
        [MarketCategory("Boot",  true)]
        Boot = 23,
        [MarketCategory("Recipe",  true)]
        Recipe = 24,
        [MarketCategory("Components",  true)]
        Components = 25,
        [MarketCategory("Starship",  true)]
        Starship = 26,
        [MarketCategory("Starship Parts", true)]
        StarshipParts = 27,
        [MarketCategory("Enhancement", true)]
        Enhancement = 28,
        [MarketCategory("Structure", true)]
        Structure = 29,
        [MarketCategory("Lightsaber", true)]
        Lightsaber = 30,
        [MarketCategory("Saberstaff", true)]
        Saberstaff = 31,
        [MarketCategory("Food", true)]
        Food = 32,
        [MarketCategory("Fishing", true)]
        Fishing = 33,
        [MarketCategory("Pet Food", true)]
        PetFood = 34,

        [MarketCategory("Miscellaneous", true)]
        Miscellaneous = 99
    }

    public class MarketCategoryAttribute : Attribute
    {
        public string Name { get; }
        public bool IsActive { get; }

        public MarketCategoryAttribute(string name, bool isActive)
        {
            Name = name;
            IsActive = isActive;
        }
    }
}
