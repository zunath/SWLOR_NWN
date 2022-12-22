using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class ShieldRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Shields();

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
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 4)
                .Component("wood", 2);

            // Titan Shield
            _builder.Create(RecipeType.TitanShield, SkillType.Smithery)
                .Category(RecipeCategoryType.Shield)
                .Resref("tit_shield")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 4)
                .Component("fine_wood", 2);

            // Quark Shield
            _builder.Create(RecipeType.QuarkShield, SkillType.Smithery)
                .Category(RecipeCategoryType.Shield)
                .Resref("qk_shield")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 4)
                .Component("ancient_wood", 2);

            // Argos Shield
            _builder.Create(RecipeType.ArgosShield, SkillType.Smithery)
                .Category(RecipeCategoryType.Shield)
                .Resref("ar_shield")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 4)
                .Component("aracia_wood", 2);

            // Eclipse Shield
            _builder.Create(RecipeType.EclipseShield, SkillType.Smithery)
                .Category(RecipeCategoryType.Shield)
                .Resref("ec_shield")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 4)
                .Component("hyphae_wood", 2);

            // Chaos Shield
            _builder.Create(RecipeType.ChaosShield, SkillType.Smithery)
                .Category(RecipeCategoryType.Shield)
                .Resref("ch_shield")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 20)
                .Component("hyphae_wood", 10)
                .Component("chiro_shard", 2);
        }

    }
}