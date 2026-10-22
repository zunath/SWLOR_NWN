using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class ShieldRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Shields();
            IntermediateShields();

            return _builder.Build();
        }

        private void Shields()
        {
            // Battlemaster Shield
            _builder.Create(RecipeType.BattlemasterShield, SkillType.Smithery)
                .Category(RecipeCategoryType.Shield)
                .Resref("bm_shield")
                .Level(6)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 4)
                .Component("wood", 2)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 52, coolingRate: 2, startingThermal: 79, shapeShare: 64);

            // Titan Shield
            _builder.Create(RecipeType.TitanShield, SkillType.Smithery)
                .Category(RecipeCategoryType.Shield)
                .Resref("tit_shield")
                .Level(16)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 4)
                .Component("fine_wood", 2)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 45, coolingRate: 3, startingThermal: 81, shapeShare: 62);

            // Quark Shield
            _builder.Create(RecipeType.QuarkShield, SkillType.Smithery)
                .Category(RecipeCategoryType.Shield)
                .Resref("qk_shield")
                .Level(26)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 4)
                .Component("ancient_wood", 2)
                .ForgeParameters(rangeCenter: 65, rangeWidth: 43, coolingRate: 3, startingThermal: 71, shapeShare: 59);

            // Argos Shield
            _builder.Create(RecipeType.ArgosShield, SkillType.Smithery)
                .Category(RecipeCategoryType.Shield)
                .Resref("ar_shield")
                .Level(36)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 4)
                .Component("aracia_wood", 2)
                .ForgeParameters(rangeCenter: 67, rangeWidth: 29, coolingRate: 5, startingThermal: 70, shapeShare: 62);

            // Eclipse Shield
            _builder.Create(RecipeType.EclipseShield, SkillType.Smithery)
                .Category(RecipeCategoryType.Shield)
                .Resref("ec_shield")
                .Level(46)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 4)
                .Component("hyphae_wood", 2)
                .ForgeParameters(rangeCenter: 64, rangeWidth: 25, coolingRate: 6, startingThermal: 69, shapeShare: 64);

            // Chaos Shield
            _builder.Create(RecipeType.ChaosShield, SkillType.Smithery)
                .Category(RecipeCategoryType.Shield)
                .Resref("ch_shield")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("hyphae_wood", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 65, rangeWidth: 27, coolingRate: 6, startingThermal: 60, shapeShare: 64);
        }

        private void IntermediateShields()
        {
            // Warden Shield
            _builder.Create(RecipeType.WardenShield, SkillType.Smithery)
                .Category(RecipeCategoryType.Shield)
                .Resref("fld_bul_shield")
                .Level(11)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 6)
                .Component("wood", 3)
                .Component("fiberp_ruined", 2)
                .ForgeParameters(rangeCenter: 64, rangeWidth: 50, coolingRate: 1, startingThermal: 81, shapeShare: 62);

            // Bastion Shield
            _builder.Create(RecipeType.BastionShield, SkillType.Smithery)
                .Category(RecipeCategoryType.Shield)
                .Resref("vet_bul_shield")
                .Level(21)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 6)
                .Component("fine_wood", 3)
                .Component("fiberp_flawed", 2)
                .ForgeParameters(rangeCenter: 60, rangeWidth: 42, coolingRate: 2, startingThermal: 75, shapeShare: 61);

            // Sentinel Shield
            _builder.Create(RecipeType.SentinelShield, SkillType.Smithery)
                .Category(RecipeCategoryType.Shield)
                .Resref("prm_bul_shield")
                .Level(31)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 6)
                .Component("ancient_wood", 3)
                .Component("fiberp_good", 2)
                .ForgeParameters(rangeCenter: 62, rangeWidth: 40, coolingRate: 4, startingThermal: 67, shapeShare: 64);

            // Aegis Shield
            _builder.Create(RecipeType.AegisShield, SkillType.Smithery)
                .Category(RecipeCategoryType.Shield)
                .Resref("asc_bul_shield")
                .Level(41)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 6)
                .Component("aracia_wood", 3)
                .Component("fiberp_imperfect", 2)
                .ForgeParameters(rangeCenter: 60, rangeWidth: 31, coolingRate: 5, startingThermal: 66, shapeShare: 59);

        }

    }
}
