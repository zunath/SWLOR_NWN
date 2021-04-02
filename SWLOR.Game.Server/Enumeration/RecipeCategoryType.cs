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
        [RecipeCategory("Heavy Shield", true)]
        HeavyShield = 10,
        [RecipeCategory("Tunic", true)]
        Tunic = 11,
        [RecipeCategory("Cap", true)]
        Cap = 12,
        [RecipeCategory("Glove", true)]
        Glove = 13,
        [RecipeCategory("Boot", true)]
        Boot = 14,
        [RecipeCategory("Light Shield", true)]
        LightShield = 15,
        [RecipeCategory("Vibroblade", true)]
        Vibroblade = 16,
        [RecipeCategory("Finesse Vibroblade", true)]
        FinesseVibroblade = 17,
        [RecipeCategory("Lightsaber", true)]
        Lightsaber = 18,
        [RecipeCategory("Heavy Vibroblade", true)]
        HeavyVibroblade = 19,
        [RecipeCategory("Polearm", true)]
        Polearm = 20,
        [RecipeCategory("Twin Blade", true)]
        TwinBlade = 21,
        [RecipeCategory("Saberstaff", true)]
        Saberstaff = 22,
        [RecipeCategory("Martial", true)]
        Martial = 23,
        [RecipeCategory("Baton", true)]
        Baton = 24,
        [RecipeCategory("Pistol", true)]
        Pistol = 25,
        [RecipeCategory("Throwing", true)]
        Throwing = 26,
        [RecipeCategory("Cannon", true)]
        Cannon = 27,
        [RecipeCategory("Rifle", true)]
        Rifle = 28,
        [RecipeCategory("Neck Implant", true)]
        NeckImplant = 29,
        [RecipeCategory("Head Implant", true)]
        HeadImplant = 30,
        [RecipeCategory("Arm Implant", true)]
        ArmImplant = 31,
        [RecipeCategory("Leg Implant", true)]
        LegImplant = 32,
        [RecipeCategory("Foot Implant", true)]
        FootImplant = 33,
        [RecipeCategory("Chest Implant", true)]
        ChestImplant = 34,
        [RecipeCategory("Hand Implant", true)]
        HandImplant = 35,
        [RecipeCategory("Eye Implant", true)]
        EyeImplant = 36,
        [RecipeCategory("Furniture", true)]
        Furniture = 37,
        [RecipeCategory("Structure", true)]
        Structure = 38,
        [RecipeCategory("Starship", true)]
        Starship = 39,
        [RecipeCategory("Turret", true)]
        Turret = 40,
        [RecipeCategory("Reactor", true)]
        Reactor = 41,
        [RecipeCategory("Plating", true)]
        Plating = 42,
        [RecipeCategory("Mining", true)]
        Mining = 43,
        [RecipeCategory("Droid", true)]
        Droid = 44,
        [RecipeCategory("Harvester", true)]
        Harvester = 45,
        [RecipeCategory("Stim Pack", true)]
        StimPack = 46,
        [RecipeCategory("Recovery", true)]
        Recovery = 47,
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
