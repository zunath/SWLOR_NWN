using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    // Tusken war gear worked from trophies recovered in the Tatooine Tusken caves.
    // Each recipe is unlocked from a dropped blueprint and requires a cave-specific
    // component plus High Quality materials.
    public class TuskenWarGearRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Sun-Cured Hide Cuirass (heavy armor)
            _builder.Create(RecipeType.SunCuredHideCuirass, SkillType.Smithery)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("hide_cuirass")
                .Level(42)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("sun_bantha_hide", 1)
                .Component("lth_high", 4)
                .Component("fiberp_high", 2);

            // Scopewright Cap (light armor)
            _builder.Create(RecipeType.ScopewrightCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("scopewright_cap")
                .Level(43)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("cycler_scope", 1)
                .Component("elec_high", 3)
                .Component("fiberp_high", 2);

            // Windcaller Mantle (cloak)
            _builder.Create(RecipeType.WindcallerMantle, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("windcall_mantle")
                .Level(44)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("bone_fetish", 1)
                .Component("fiberp_high", 3)
                .Component("elec_high", 1);

            // Warband Helm (heavy helmet)
            _builder.Create(RecipeType.WarbandHelm, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("warband_helm")
                .Level(45)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("bone_totem", 1)
                .Component("lth_high", 3)
                .Component("elec_high", 2);
        }
    }
}
