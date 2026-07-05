using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    public class FieldToolRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Faultline Capacitor
            _builder.Create(RecipeType.FaultlineCapacitor, SkillType.Engineering)
                .Category(RecipeCategoryType.Tool)
                .Resref("sr_jrcell")
                .Level(12)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("elec_ruined", 3)
                .Component("ref_veldite", 2)
                .Component("sr_token", 1);

            // Ghostkey Relay
            _builder.Create(RecipeType.GhostkeyRelay, SkillType.Engineering)
                .Category(RecipeCategoryType.Tool)
                .Resref("nv_relay")
                .Level(14)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("elec_ruined", 3)
                .Component("ref_veldite", 2)
                .Component("nv_pin", 1);

            // Wayfinder Sensor
            _builder.Create(RecipeType.WayfinderSensor, SkillType.Engineering)
                .Category(RecipeCategoryType.Tool)
                .Resref("tk_sensor")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("obsidian", 3)
                .Component("elec_good", 3)
                .Component("tk_badge", 1);

            // Stonewake Relay
            _builder.Create(RecipeType.StonewakeRelay, SkillType.Engineering)
                .Category(RecipeCategoryType.Tool)
                .Resref("vs_relay")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("obsidian", 3)
                .Component("elec_good", 3)
                .Component("vs_mask", 1);

            // Kinetic Harness
            _builder.Create(RecipeType.KineticHarness, SkillType.Engineering)
                .Category(RecipeCategoryType.Tool)
                .Resref("hv_servo")
                .Level(31)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("obsidian", 3)
                .Component("elec_good", 3)
                .Component("hv_plate", 1);

            // Lucid Splice
            _builder.Create(RecipeType.LucidSplice, SkillType.Engineering)
                .Category(RecipeCategoryType.Tool)
                .Resref("mg_splice")
                .Level(35)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("crystal", 4)
                .Component("elec_imperfect", 3)
                .Component("mg_totem", 1);

            // Stormcore Matrix
            _builder.Create(RecipeType.StormcoreMatrix, SkillType.Engineering)
                .Category(RecipeCategoryType.Tool)
                .Resref("vx_matrix")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("diamond", 5)
                .Component("elec_high", 2)
                .Component("vx_core", 1);

            // Tidecall Beacon
            _builder.Create(RecipeType.TidecallBeacon, SkillType.Engineering)
                .Category(RecipeCategoryType.Tool)
                .Resref("tc_beacon")
                .Level(34)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("elec_imperfect", 3)
                .Component("ref_arkoxit", 2)
                .Component("command_key", 1);

            // Flux Diverter
            _builder.Create(RecipeType.FluxDiverter, SkillType.Engineering)
                .Category(RecipeCategoryType.Tool)
                .Resref("fx_diverter")
                .Level(29)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("elec_good", 3)
                .Component("ref_veldite", 2)
                .Component("field_chip", 1);
        }
    }
}
