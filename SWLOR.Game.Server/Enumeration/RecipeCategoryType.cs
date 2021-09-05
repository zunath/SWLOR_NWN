using System;

namespace SWLOR.Game.Server.Enumeration
{
    public enum RecipeCategoryType
    {
        [RecipeCategory("Invalid", false)]
        Invalid = 0,
        [RecipeCategory("Uncategorized", true)]
        Uncategorized = 1,
        [RecipeCategory("Cloak", true)]
        Cloak = 2,
        [RecipeCategory("Belt", true)]
        Belt = 3,
        [RecipeCategory("Ring", true)]
        Ring = 4,
        [RecipeCategory("Necklace", true)]
        Necklace = 5,
        [RecipeCategory("Breastplate", true)]
        Breastplate = 6,
        [RecipeCategory("Helmet", true)]
        Helmet = 7,
        [RecipeCategory("Bracer", true)]
        Bracer = 8,
        [RecipeCategory("Legging", true)]
        Legging = 9,
        [RecipeCategory("Shield", true)]
        Shield = 10,
        [RecipeCategory("Tunic", true)]
        Tunic = 11,
        [RecipeCategory("Cap", true)]
        Cap = 12,
        [RecipeCategory("Glove", true)]
        Glove = 13,
        [RecipeCategory("Boots", true)]
        Boots = 14,
        [RecipeCategory("Longsword", true)]
        Longsword = 15,
        [RecipeCategory("Knife", true)]
        Knife = 16,
        [RecipeCategory("Great Sword", true)]
        GreatSword = 17,
        [RecipeCategory("Spear", true)]
        Spear = 18,
        [RecipeCategory("Katar", true)]
        Katar = 19,
        [RecipeCategory("Staff", true)]
        Staff = 20,
        [RecipeCategory("Pistol", true)]
        Pistol = 21,
        [RecipeCategory("Shuriken", true)]
        Shuriken = 22,
        [RecipeCategory("Rifle", true)]
        Rifle = 23,
        [RecipeCategory("Twin Blade", true)]
        TwinBlade = 24,

        [RecipeCategory("Furniture", true)]
        Furniture = 25,
        [RecipeCategory("Structure", true)]
        Structure = 26,
        [RecipeCategory("Starship", true)]
        Starship = 27,
        [RecipeCategory("Turret", true)]
        Turret = 28,
        [RecipeCategory("Reactor", true)]
        Reactor = 29,
        [RecipeCategory("Plating", true)]
        Plating = 30,
        [RecipeCategory("Mining", true)]
        Mining = 31,
        [RecipeCategory("Droid", true)]
        Droid = 32,
        [RecipeCategory("Harvester", true)]
        Harvester = 33,
    }

    public class RecipeCategoryAttribute : Attribute
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }

        public RecipeCategoryAttribute(string name, bool isActive)
        {
            Name = name;
            IsActive = isActive;
        }
    }
}
