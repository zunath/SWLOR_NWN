using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
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
                .Component("ref_plagionite", 4)
                .Component("ref_scordspar", 4)
                .Component("elec_good", 4)
                .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 23, frameQualityPercent: 22, shellQualityPercent: 21, wearRate: 5);

            _builder.Create(RecipeType.ConstructionParts, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("const_parts")
                .Level(23)
                .Quantity(1)
                .Component("ref_plagionite", 3)
                .Component("ref_scordspar", 3)
                .Component("elec_good", 3)
                .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 27, frameQualityPercent: 29, shellQualityPercent: 29, wearRate: 6);

            _builder.Create(RecipeType.ReinforcedConstructionParts, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("r_const_parts")
                .Level(31)
                .Quantity(1)
                .Component("const_parts", 2)
                .Component("ref_keromber", 4)
                .Component("elec_imperfect", 3)
                .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 26, frameQualityPercent: 29, shellQualityPercent: 29, wearRate: 6);

            _builder.Create(RecipeType.ReinforcedPowerSupplyUnit, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("r_pow_supp_unit")
                .Level(32)
                .Quantity(1)
                .Component("pow_supp_unit", 2)
                .Component("ref_keromber", 4)
                .Component("elec_imperfect", 3)
                .FabricatorParameters(frameSizePercent: 27, shellSizePercent: 29, frameQualityPercent: 22, shellQualityPercent: 21, wearRate: 8);
        }

        private void Structures()
        {
            // City Hall
            _builder.Create(RecipeType.CityHallStyle1, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5000")
                .Level(25)
                .Quantity(1)
                .Component("const_parts", 3)
                .Component("pow_supp_unit", 2)
                .FabricatorParameters(frameSizePercent: 28, shellSizePercent: 25, frameQualityPercent: 23, shellQualityPercent: 21, wearRate: 6);

            // Small House - Style 1
            _builder.Create(RecipeType.SmallHouseStyle1, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5005")
                .Level(26)
                .Quantity(1)
                .Component("const_parts", 2)
                .Component("pow_supp_unit", 1)
                .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 30, frameQualityPercent: 23, shellQualityPercent: 25, wearRate: 6);

            // Small House - Style 2
            _builder.Create(RecipeType.SmallHouseStyle2, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5006")
                .Level(27)
                .Quantity(1)
                .Component("const_parts", 2)
                .Component("pow_supp_unit", 1)
                .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 27, frameQualityPercent: 20, shellQualityPercent: 21, wearRate: 4);

            // Small House - Style 3
            _builder.Create(RecipeType.SmallHouseStyle3, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5007")
                .Level(29)
                .Quantity(1)
                .Component("const_parts", 2)
                .Component("pow_supp_unit", 1)
                .FabricatorParameters(frameSizePercent: 27, shellSizePercent: 26, frameQualityPercent: 20, shellQualityPercent: 20, wearRate: 5);

            // Small House - Style 4
            _builder.Create(RecipeType.SmallHouseStyle4, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5008")
                .Level(30)
                .Quantity(1)
                .Component("const_parts", 2)
                .Component("pow_supp_unit", 1)
                .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 29, frameQualityPercent: 30, shellQualityPercent: 25, wearRate: 4);

            // Medium House - Style 1
            _builder.Create(RecipeType.MediumHouseStyle1, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5009")
                .Level(33)
                .Quantity(1)
                .Component("r_const_parts", 2)
                .Component("r_pow_supp_unit", 1)
                .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 21, frameQualityPercent: 22, shellQualityPercent: 26, wearRate: 6);

            // Medium House - Style 2
            _builder.Create(RecipeType.MediumHouseStyle2, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5010")
                .Level(34)
                .Quantity(1)
                .Component("r_const_parts", 2)
                .Component("r_pow_supp_unit", 1)
                .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 22, frameQualityPercent: 23, shellQualityPercent: 30, wearRate: 8);

            // Medium House - Style 3
            _builder.Create(RecipeType.MediumHouseStyle3, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5011")
                .Level(35)
                .Quantity(1)
                .Component("r_const_parts", 2)
                .Component("r_pow_supp_unit", 1)
                .FabricatorParameters(frameSizePercent: 26, shellSizePercent: 20, frameQualityPercent: 20, shellQualityPercent: 22, wearRate: 6);

            // Medium House - Style 4
            _builder.Create(RecipeType.MediumHouseStyle4, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5012")
                .Level(36)
                .Quantity(1)
                .Component("r_const_parts", 2)
                .Component("r_pow_supp_unit", 1)
                .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 28, frameQualityPercent: 25, shellQualityPercent: 24, wearRate: 6);

            // Cantina - Style 1
            _builder.Create(RecipeType.CantinaStyle1, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5004")
                .Level(40)
                .Quantity(1)
                .Component("r_const_parts", 2)
                .Component("r_pow_supp_unit", 1)
                .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 29, frameQualityPercent: 23, shellQualityPercent: 29, wearRate: 7);

            // Bank - Style 1
            _builder.Create(RecipeType.BankStyle1, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5001")
                .Level(41)
                .Quantity(1)
                .Component("r_const_parts", 4)
                .Component("r_pow_supp_unit", 2)
                .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 30, frameQualityPercent: 22, shellQualityPercent: 30, wearRate: 7);

            // Medical Center - Style 1
            _builder.Create(RecipeType.MedicalCenterStyle1, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5002")
                .Level(43)
                .Quantity(1)
                .Component("r_const_parts", 4)
                .Component("r_pow_supp_unit", 2)
                .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 25, frameQualityPercent: 28, shellQualityPercent: 26, wearRate: 8);

            // Large House - Style 1
            _builder.Create(RecipeType.LargeHouseStyle1, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5013")
                .Level(45)
                .Quantity(1)
                .Component("r_const_parts", 3)
                .Component("r_pow_supp_unit", 2)
                .FabricatorParameters(frameSizePercent: 27, shellSizePercent: 26, frameQualityPercent: 27, shellQualityPercent: 20, wearRate: 6);

            // Large House - Style 2
            _builder.Create(RecipeType.LargeHouseStyle2, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5014")
                .Level(46)
                .Quantity(1)
                .Component("r_const_parts", 3)
                .Component("r_pow_supp_unit", 2)
                .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 29, frameQualityPercent: 20, shellQualityPercent: 30, wearRate: 8);

            // Large House - Style 3
            _builder.Create(RecipeType.LargeHouseStyle3, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5015")
                .Level(47)
                .Quantity(1)
                .Component("r_const_parts", 3)
                .Component("r_pow_supp_unit", 2)
                .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 27, frameQualityPercent: 29, shellQualityPercent: 28, wearRate: 8);

            // Large House - Style 4
            _builder.Create(RecipeType.LargeHouseStyle4, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5016")
                .Level(48)
                .Quantity(1)
                .Component("r_const_parts", 3)
                .Component("r_pow_supp_unit", 2)
                .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 21, frameQualityPercent: 26, shellQualityPercent: 29, wearRate: 8);

            // Starport - Style 1
            _builder.Create(RecipeType.StarportStyle1, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5003")
                .Level(50)
                .Quantity(1)
                .Component("r_const_parts", 4)
                .Component("r_pow_supp_unit", 2)
                .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 27, frameQualityPercent: 24, shellQualityPercent: 26, wearRate: 7);

            // Lab - Style 1
            _builder.Create(RecipeType.LabStyle1, SkillType.Fabrication)
                .Category(RecipeCategoryType.Structure)
                .Resref("structure_5017")
                .Level(50)
                .Quantity(1)
                .Component("r_const_parts", 4)
                .Component("r_pow_supp_unit", 3)
                .Component("zinsiam", 8)
                .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 28, frameQualityPercent: 20, shellQualityPercent: 20, wearRate: 7);

        }
    }
}
