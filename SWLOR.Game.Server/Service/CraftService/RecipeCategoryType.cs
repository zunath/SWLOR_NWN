using System;

namespace SWLOR.Game.Server.Service.CraftService
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
        [RecipeCategory("Bed", true)]
        Bed = 25,
        [RecipeCategory("Misc. Furniture", true)]
        MiscellaneousFurniture = 25,
        [RecipeCategory("Flooring", true)]
        Flooring = 26,
        [RecipeCategory("Seating", true)]
        Seating = 27,
        [RecipeCategory("Surface", true)]
        Surfaces = 28,
        [RecipeCategory("Statue", true)]
        Statues = 29,
        [RecipeCategory("Door", true)]
        Doors = 30,
        [RecipeCategory("Lighting", true)]
        Lighting = 31,
        [RecipeCategory("Fixture", true)]
        Fixtures = 32,
        [RecipeCategory("Electronic", true)]
        Electronics = 33,
        [RecipeCategory("Structure", true)]
        Structure = 34,
        [RecipeCategory("Starship", true)]
        Starship = 35,
        [RecipeCategory("Ship Module", true)]
        ShipModule = 36,
        [RecipeCategory("Food", true)]
        Food = 37,
        [RecipeCategory("Ingredient", true)]
        Ingredients = 38,
        [RecipeCategory("Lightsaber", true)]
        Lightsaber = 39,
        [RecipeCategory("Crafting", true)]
        Crafting = 40,
        [RecipeCategory("Weapon Enhancement", true)]
        WeaponEnhancement = 41,
        [RecipeCategory("Saberstaff", true)]
        Saberstaff = 42,
        [RecipeCategory("Wall", true)]
        Wall = 43,
        [RecipeCategory("Armor Enhancement", true)]
        ArmorEnhancement = 44,
        [RecipeCategory("Cooking Enhancement", true)]
        CookingEnhancement = 45,
        [RecipeCategory("Starship Enhancement", true)]
        StarshipEnhancement = 46,
        [RecipeCategory("Module Enhancement", true)]
        ModuleEnhancement = 47,
        [RecipeCategory("Droid Enhancement", true)]
        DroidEnhancement = 48,
        [RecipeCategory("Structure Enhancement", true)]
        StructureEnhancement = 49,
        [RecipeCategory("Droid Component", true)]
        DroidComponent = 50,
        [RecipeCategory("Droid CPU", true)]
        DroidCPU = 51,
        [RecipeCategory("Droid Head", true)]
        DroidHead = 52,
        [RecipeCategory("Droid Arms", true)]
        DroidArms = 53,
        [RecipeCategory("Droid Legs", true)]
        DroidLegs = 54,
        [RecipeCategory("Droid Body", true)]
        DroidBody = 55,
        [RecipeCategory("Droid Instruction", true)]
        DroidInstruction = 56,
        [RecipeCategory("Pet Food", true)]
        PetFood = 57,

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
