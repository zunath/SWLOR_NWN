using System;

namespace SWLOR.Game.Server.Enumeration
{
    public enum RecipeCategoryType
    {
        [RecipeCategory("Invalid", false)]
        Invalid = 0,
        [RecipeCategory("Uncategorized", true)]
        Uncategorized = 1,
        [RecipeCategory("Heavy Armor", true)]
        HeavyArmor = 2,
        [RecipeCategory("Light Armor", true)]
        LightArmor = 3,
        [RecipeCategory("Mystic Armor", true)]
        MysticArmor = 4,
        [RecipeCategory("Longsword", true)]
        Longsword = 5,
        [RecipeCategory("Knuckles", true)]
        Knuckles = 6,
        [RecipeCategory("Dagger", true)]
        Dagger = 7,
        [RecipeCategory("Katana", true)]
        Katana = 8,
        [RecipeCategory("Gunblade", true)]
        Gunblade = 9,
        [RecipeCategory("Rifle", true)]
        Rifle = 10,
        [RecipeCategory("Rapier", true)]
        Rapier = 11,
        [RecipeCategory("Great Sword", true)]
        GreatSword = 12,
        [RecipeCategory("Cloak", true)]
        Cloak = 13,
        [RecipeCategory("Helmet", true)]
        Helmet = 14,
        [RecipeCategory("Boots", true)]
        Boots = 15,
        [RecipeCategory("Gloves", true)]
        Gloves = 16,
        [RecipeCategory("Belt", true)]
        Belt = 17,
        [RecipeCategory("Leather", true)]
        Leather = 18,
        [RecipeCategory("Potion", true)]
        Potion = 19,
        [RecipeCategory("Medicine", true)]
        Medicine = 20,
        [RecipeCategory("Staff", true)]
        Staff = 21,
        [RecipeCategory("Rod", true)]
        Rod = 22,
        [RecipeCategory("Ring", true)]
        Ring = 23,
        [RecipeCategory("Grenade", true)]
        Grenade = 24,
        [RecipeCategory("Ingredient", true)]
        Ingredient = 25,
        [RecipeCategory("Meal", true)]
        Meal = 26,
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
