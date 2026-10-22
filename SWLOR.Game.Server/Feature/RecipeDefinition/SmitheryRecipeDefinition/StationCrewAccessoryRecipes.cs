using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    // Accessories for the Abandoned Station's Containment, Gantry and Biolab gear lines. Blueprints drop from the
    // Station Director; each recipe matches the level-25 crafted accessory of its slot plus a Quarantine Seal.
    public class StationCrewAccessoryRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Containment();
            Gantry();
            Biolab();

            return _builder.Build();
        }

        private void Containment()
        {
            // Containment Cloak
            _builder.Create(RecipeType.ContainmentCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("cnt_cloak")
                .Level(33)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 8)
                .Component("fiberp_good", 5)
                .Component("ref_plagionite", 2)
                .Component("quar_seal", 1)
                .ForgeParameters(rangeCenter: 52, rangeWidth: 31, coolingRate: 4, startingThermal: 57, shapeShare: 52);

            // Containment Belt
            _builder.Create(RecipeType.ContainmentBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("cnt_belt")
                .Level(34)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 8)
                .Component("fiberp_good", 5)
                .Component("ref_plagionite", 2)
                .Component("quar_seal", 1)
                .ForgeParameters(rangeCenter: 46, rangeWidth: 38, coolingRate: 5, startingThermal: 55, shapeShare: 52);

            // Containment Ring
            _builder.Create(RecipeType.ContainmentRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("cnt_ring")
                .Level(26)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 2)
                .Component("citrine", 2)
                .Component("lth_good", 1)
                .Component("quar_seal", 1)
                .ForgeParameters(rangeCenter: 45, rangeWidth: 44, coolingRate: 4, startingThermal: 57, shapeShare: 44);

            // Containment Necklace
            _builder.Create(RecipeType.ContainmentNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("cnt_neck")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 3)
                .Component("citrine", 2)
                .Component("lth_good", 1)
                .Component("quar_seal", 1)
                .ForgeParameters(rangeCenter: 52, rangeWidth: 39, coolingRate: 5, startingThermal: 56, shapeShare: 44);
        }

        private void Gantry()
        {
            // Gantry Cloak
            _builder.Create(RecipeType.GantryCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("gantry_cloak")
                .Level(33)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 8)
                .Component("fiberp_good", 5)
                .Component("elec_good", 2)
                .Component("quar_seal", 1)
                .ForgeParameters(rangeCenter: 40, rangeWidth: 47, coolingRate: 5, startingThermal: 46, shapeShare: 53);

            // Gantry Belt
            _builder.Create(RecipeType.GantryBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("gantry_belt")
                .Level(34)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 8)
                .Component("fiberp_good", 5)
                .Component("elec_good", 2)
                .Component("quar_seal", 1)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 54, coolingRate: 3, startingThermal: 44, shapeShare: 50);

            // Gantry Ring
            _builder.Create(RecipeType.GantryRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("gantry_ring")
                .Level(26)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 2)
                .Component("citrine", 2)
                .Component("elec_good", 1)
                .Component("quar_seal", 1)
                .ForgeParameters(rangeCenter: 44, rangeWidth: 36, coolingRate: 4, startingThermal: 55, shapeShare: 45);

            // Gantry Necklace
            _builder.Create(RecipeType.GantryNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("gantry_neck")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 3)
                .Component("citrine", 2)
                .Component("elec_good", 1)
                .Component("quar_seal", 1)
                .ForgeParameters(rangeCenter: 45, rangeWidth: 40, coolingRate: 3, startingThermal: 55, shapeShare: 44);
        }

        private void Biolab()
        {
            // Biolab Cloak
            _builder.Create(RecipeType.BiolabCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("biolab_cloak")
                .Level(33)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 8)
                .Component("fiberp_good", 5)
                .Component("citrine", 2)
                .Component("quar_seal", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 54, coolingRate: 3, startingThermal: 45, shapeShare: 51);

            // Biolab Belt
            _builder.Create(RecipeType.BiolabBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("biolab_belt")
                .Level(34)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 8)
                .Component("fiberp_good", 5)
                .Component("citrine", 2)
                .Component("quar_seal", 1)
                .ForgeParameters(rangeCenter: 43, rangeWidth: 54, coolingRate: 5, startingThermal: 45, shapeShare: 50);

            // Biolab Ring
            _builder.Create(RecipeType.BiolabRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("biolab_ring")
                .Level(26)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 2)
                .Component("citrine", 2)
                .Component("fiberp_good", 1)
                .Component("quar_seal", 1)
                .ForgeParameters(rangeCenter: 46, rangeWidth: 38, coolingRate: 5, startingThermal: 56, shapeShare: 46);

            // Biolab Necklace
            _builder.Create(RecipeType.BiolabNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("biolab_neck")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 3)
                .Component("citrine", 2)
                .Component("fiberp_good", 1)
                .Component("quar_seal", 1)
                .ForgeParameters(rangeCenter: 45, rangeWidth: 37, coolingRate: 4, startingThermal: 60, shapeShare: 47);
        }
    }
}
