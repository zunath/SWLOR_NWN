using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    // Field gear crafted from salvage recovered off the CZ-220 Breaker Yard rare elite droids.
    // Each recipe is unlocked from a dropped blueprint and requires an encounter-specific
    // salvage component plus common materials.
    public class SalvagedFieldGearRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Reactor-Forged Plating (chest)
            _builder.Create(RecipeType.SalvagedReactorPlate, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("reactor_plate")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("reactor_core", 1)
                .Component("elec_good", 4)
                .Component("fiberp_good", 2);

            // Piston-Driven Gauntlets (gloves)
            _builder.Create(RecipeType.SalvagedPistonGauntlet, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("piston_gaunt")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("crusher_piston", 1)
                .Component("elec_good", 3)
                .Component("fiberp_good", 2);

            // Siege Optics Visor (helmet)
            _builder.Create(RecipeType.SalvagedSiegeOptics, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("siege_optics")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("targeting_lens", 1)
                .Component("elec_good", 4)
                .Component("fiberp_good", 2);
        }
    }
}
