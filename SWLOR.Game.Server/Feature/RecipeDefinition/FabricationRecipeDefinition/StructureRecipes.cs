using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition
{
    public class StructureRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();
        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            StructureComponents();
            Structures();

            return _builder.Build();
        }

        private void StructureComponents()
        {
            _builder.Create(RecipeType.PowerSupplyUnit, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("pow_supp_unit")
                .Level(21)
                .Quantity(1)
                .RequirementPerk(PerkType.StructureBlueprints, 1)
                .Component("ref_plagionite", 4)
                .Component("ref_scordspar", 4)
                .Component("elec_good", 4);

            _builder.Create(RecipeType.ConstructionParts, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("const_parts")
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.StructureBlueprints, 1)
                .Component("ref_plagionite", 3)
                .Component("ref_scordspar", 3)
                .Component("elec_good", 3);

            _builder.Create(RecipeType.ReinforcedConstructionParts, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("r_const_parts")
                .Level(31)
                .Quantity(1)
                .RequirementPerk(PerkType.StructureBlueprints, 1)
                .Component("const_parts", 2)
                .Component("ref_keromber", 4)
                .Component("elec_imperfect", 3);

            _builder.Create(RecipeType.ReinforcedPowerSupplyUnit, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("r_pow_supp_unit")
                .Level(32)
                .Quantity(1)
                .RequirementPerk(PerkType.StructureBlueprints, 1)
                .Component("pow_supp_unit", 2)
                .Component("ref_keromber", 4)
                .Component("elec_imperfect", 3);
        }

        private void Structures()
        {
            // City Hall
            _builder.Create(RecipeType.CityHallStyle1, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5000")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StructureBlueprints, 1)
                .Component("const_parts", 3)
                .Component("pow_supp_unit", 2);

            // Small House - Style 1
            _builder.Create(RecipeType.SmallHouseStyle1, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5005")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.StructureBlueprints, 1)
                .Component("const_parts", 2)
                .Component("pow_supp_unit", 1);

            // Small House - Style 2
            _builder.Create(RecipeType.SmallHouseStyle2, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5006")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.StructureBlueprints, 1)
                .Component("const_parts", 2)
                .Component("pow_supp_unit", 1);

            // Small House - Style 3
            _builder.Create(RecipeType.SmallHouseStyle3, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5007")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.StructureBlueprints, 1)
                .Component("const_parts", 2)
                .Component("pow_supp_unit", 1);

            // Small House - Style 4
            _builder.Create(RecipeType.SmallHouseStyle4, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5008")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StructureBlueprints, 1)
                .Component("const_parts", 2)
                .Component("pow_supp_unit", 1);

            // Medium House - Style 1
            _builder.Create(RecipeType.MediumHouseStyle1, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5009")
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.StructureBlueprints, 1)
                .Component("r_const_parts", 2)
                .Component("r_pow_supp_unit", 1);

            // Medium House - Style 2
            _builder.Create(RecipeType.MediumHouseStyle2, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5010")
                .Level(34)
                .Quantity(1)
                .RequirementPerk(PerkType.StructureBlueprints, 1)
                .Component("r_const_parts", 2)
                .Component("r_pow_supp_unit", 1);

            // Medium House - Style 3
            _builder.Create(RecipeType.MediumHouseStyle3, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5011")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StructureBlueprints, 1)
                .Component("r_const_parts", 2)
                .Component("r_pow_supp_unit", 1);

            // Medium House - Style 4
            _builder.Create(RecipeType.MediumHouseStyle4, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5012")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.StructureBlueprints, 1)
                .Component("r_const_parts", 2)
                .Component("r_pow_supp_unit", 1);

            // Cantina - Style 1
            _builder.Create(RecipeType.CantinaStyle1, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StructureBlueprints, 2)
                .Component("r_const_parts", 2)
                .Component("r_pow_supp_unit", 1);

            // Bank - Style 1
            _builder.Create(RecipeType.BankStyle1, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5001")
                .Level(41)
                .Quantity(1)
                .RequirementPerk(PerkType.StructureBlueprints, 2)
                .Component("r_const_parts", 4)
                .Component("r_pow_supp_unit", 2);

            // Medical Center - Style 1
            _builder.Create(RecipeType.MedicalCenterStyle1, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5002")
                .Level(43)
                .Quantity(1)
                .RequirementPerk(PerkType.StructureBlueprints, 2)
                .Component("r_const_parts", 4)
                .Component("r_pow_supp_unit", 2);

            // Large House - Style 1
            _builder.Create(RecipeType.LargeHouseStyle1, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5013")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StructureBlueprints, 2)
                .Component("r_const_parts", 3)
                .Component("r_pow_supp_unit", 2);

            // Large House - Style 2
            _builder.Create(RecipeType.LargeHouseStyle2, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5014")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.StructureBlueprints, 2)
                .Component("r_const_parts", 3)
                .Component("r_pow_supp_unit", 2);

            // Large House - Style 3
            _builder.Create(RecipeType.LargeHouseStyle3, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5015")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.StructureBlueprints, 2)
                .Component("r_const_parts", 3)
                .Component("r_pow_supp_unit", 2);

            // Large House - Style 4
            _builder.Create(RecipeType.LargeHouseStyle4, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5016")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.StructureBlueprints, 2)
                .Component("r_const_parts", 3)
                .Component("r_pow_supp_unit", 2);

            // Starport - Style 1
            _builder.Create(RecipeType.StarportStyle1, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5003")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StructureBlueprints, 2)
                .Component("r_const_parts", 4)
                .Component("r_pow_supp_unit", 2);

            // Lab - Style 1
            _builder.Create(RecipeType.LabStyle1, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5017")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StructureBlueprints, 2)
                .Component("r_const_parts", 4)
                .Component("r_pow_supp_unit", 3)
                .Component("zinsiam", 8);

        }
    }
}
