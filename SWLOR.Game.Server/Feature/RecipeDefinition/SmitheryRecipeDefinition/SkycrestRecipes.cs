using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class SkycrestRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Skycrest Harness
            _builder.Create(RecipeType.SkycrestHarness, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("sp_harness")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 4)
                .Component("fiberp_ruined", 2)
                .Component("stormpl_plume", 1);

            // Skycrest Talonwraps
            _builder.Create(RecipeType.SkycrestTalonwraps, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("sp_talonwrap")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("stormpl_plume", 1);

            // Skycrest Striders
            _builder.Create(RecipeType.SkycrestStriders, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("sp_striders")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("stormpl_plume", 1);

            // Skycrest Sash
            _builder.Create(RecipeType.SkycrestSash, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("sp_sash")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("stormpl_plume", 1);

            // Skycrest Mantle
            _builder.Create(RecipeType.SkycrestMantle, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("sp_mantle")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("stormpl_plume", 1);

            // Skycrest Gorget
            _builder.Create(RecipeType.SkycrestGorget, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("sp_gorget")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("stormpl_plume", 1);

            // Skycrest Band
            _builder.Create(RecipeType.SkycrestBand, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("sp_band")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("stormpl_plume", 1);

            // Skycrest Guard
            _builder.Create(RecipeType.SkycrestGuard, SkillType.Smithery)
                .Category(RecipeCategoryType.Bracer)
                .Resref("sp_guard")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("stormpl_plume", 1);

            // Skycrest Visor
            _builder.Create(RecipeType.SkycrestVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("sp_crestvis")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 3)
                .Component("fiberp_ruined", 2)
                .Component("stormpl_plume", 1);

            // Skycrest Charm
            _builder.Create(RecipeType.SkycrestCharm, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("sp_beakcharm")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("stormpl_plume", 1);

            // Skycrest Trophy Band
            _builder.Create(RecipeType.SkycrestTrophyBand, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("sp_trophy")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("stormpl_plume", 1);

            // Skycrest Braid
            _builder.Create(RecipeType.SkycrestBraid, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("sp_plumebraid")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("stormpl_plume", 1);
        }
    }
}
