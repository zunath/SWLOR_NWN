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
                .ForgeParameters(rangeCenter: 44, rangeWidth: 22, coolingRate: 7, startingThermal: 47, shapeShare: 54);

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
                .ForgeParameters(rangeCenter: 51, rangeWidth: 22, coolingRate: 7, startingThermal: 54, shapeShare: 54);

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
                .ForgeParameters(rangeCenter: 48, rangeWidth: 25, coolingRate: 8, startingThermal: 49, shapeShare: 38);

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
                .ForgeParameters(rangeCenter: 48, rangeWidth: 21, coolingRate: 5, startingThermal: 39, shapeShare: 38);
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
                .ForgeParameters(rangeCenter: 40, rangeWidth: 42, coolingRate: 6, startingThermal: 46, shapeShare: 54);

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
                .ForgeParameters(rangeCenter: 40, rangeWidth: 37, coolingRate: 6, startingThermal: 45, shapeShare: 54);

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
                .ForgeParameters(rangeCenter: 48, rangeWidth: 19, coolingRate: 9, startingThermal: 40, shapeShare: 38);

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
                .ForgeParameters(rangeCenter: 48, rangeWidth: 25, coolingRate: 5, startingThermal: 53, shapeShare: 38);
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
                .ForgeParameters(rangeCenter: 40, rangeWidth: 40, coolingRate: 6, startingThermal: 45, shapeShare: 54);

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
                .ForgeParameters(rangeCenter: 40, rangeWidth: 43, coolingRate: 6, startingThermal: 46, shapeShare: 54);

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
                .ForgeParameters(rangeCenter: 48, rangeWidth: 20, coolingRate: 3, startingThermal: 50, shapeShare: 38);

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
                .ForgeParameters(rangeCenter: 48, rangeWidth: 25, coolingRate: 3, startingThermal: 31, shapeShare: 38);
        }
    }
}
