using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EspionageRecipeDefinition
{
    public class TrapKitRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            // Trap Kit I
            _builder.Create(RecipeType.TrapKit1, SkillType.Espionage)
                .RequirementPerk(PerkType.Trapcraft, 1, "Trapcraft")
                .Category(RecipeCategoryType.Tool)
                .Resref("trap_kit_1")
                .Level(5)
                .Quantity(5)
                .Component("elec_ruined", 3)
                .Component("ref_veldite", 2);

            // Trap Kit II
            _builder.Create(RecipeType.TrapKit2, SkillType.Espionage)
                .RequirementPerk(PerkType.Trapcraft, 2, "Trapcraft")
                .Category(RecipeCategoryType.Tool)
                .Resref("trap_kit_2")
                .Level(18)
                .Quantity(5)
                .Component("elec_flawed", 3)
                .Component("ref_scordspar", 2);

            // Trap Kit III
            _builder.Create(RecipeType.TrapKit3, SkillType.Espionage)
                .RequirementPerk(PerkType.Trapcraft, 3, "Trapcraft")
                .Category(RecipeCategoryType.Tool)
                .Resref("trap_kit_3")
                .Level(30)
                .Quantity(5)
                .Component("elec_good", 3)
                .Component("ref_plagionite", 2);

            // Trap Kit IV
            _builder.Create(RecipeType.TrapKit4, SkillType.Espionage)
                .RequirementPerk(PerkType.Trapcraft, 4, "Trapcraft")
                .Category(RecipeCategoryType.Tool)
                .Resref("trap_kit_4")
                .Level(45)
                .Quantity(5)
                .Component("elec_imperfect", 3)
                .Component("ref_keromber", 2);

            // Trap Kit V (Master Saboteur capstone - Trapcraft has no fifth level)
            _builder.Create(RecipeType.TrapKit5, SkillType.Espionage)
                .RequirementPerk(PerkType.MasterSaboteur, 1, "Master Saboteur")
                .Category(RecipeCategoryType.Tool)
                .Resref("trap_kit_5")
                .Level(50)
                .Quantity(5)
                .Component("elec_high", 3)
                .Component("ref_jasioclase", 2);

            return _builder.Build();
        }
    }
}
