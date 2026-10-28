using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition;

public class PowerGloveRecipes : IRecipeListDefinition
{
    public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
    {
        var builder = new RecipeBuilder();

        builder.Create(RecipeType.BasicPowerGlove, SkillType.Smithery)
            .Category(RecipeCategoryType.PowerGlove)
            .Resref("b_pglove")
            .Level(3)
            .Quantity(1)
            .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
            .Component("ref_veldite", 2)
            .Component("wood", 1);

        builder.Create(RecipeType.FieldPowerGlove, SkillType.Smithery)
            .Category(RecipeCategoryType.PowerGlove)
            .Resref("fld_pglove")
            .Level(8)
            .Quantity(1)
            .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
            .Component("ref_veldite", 3)
            .Component("wood", 2)
            .Component("lth_ruined", 1);

        builder.Create(RecipeType.TitanPowerGlove, SkillType.Smithery)
            .Category(RecipeCategoryType.PowerGlove)
            .Resref("tit_pglove")
            .Level(13)
            .Quantity(1)
            .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
            .Component("ref_scordspar", 2)
            .Component("fine_wood", 1);

        builder.Create(RecipeType.SithPowerGlove, SkillType.Smithery)
            .Category(RecipeCategoryType.PowerGlove)
            .Resref("sith_pglove")
            .Level(18)
            .Quantity(1)
            .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
            .Component("ref_scordspar", 3)
            .Component("fine_wood", 2);

        builder.Create(RecipeType.VeteranPowerGlove, SkillType.Smithery)
            .Category(RecipeCategoryType.PowerGlove)
            .Resref("vet_pglove")
            .Level(18)
            .Quantity(1)
            .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
            .Component("ref_scordspar", 3)
            .Component("fine_wood", 2)
            .Component("lth_flawed", 1);

        builder.Create(RecipeType.DeltaPowerGlove, SkillType.Smithery)
            .Category(RecipeCategoryType.PowerGlove)
            .Resref("del_pglove")
            .Level(23)
            .Quantity(1)
            .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
            .Component("ref_plagionite", 2)
            .Component("ancient_wood", 1);

        builder.Create(RecipeType.PrimePowerGlove, SkillType.Smithery)
            .Category(RecipeCategoryType.PowerGlove)
            .Resref("prm_pglove")
            .Level(28)
            .Quantity(1)
            .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
            .Component("ref_plagionite", 3)
            .Component("ancient_wood", 2)
            .Component("lth_good", 1);

        builder.Create(RecipeType.ProtoPowerGlove, SkillType.Smithery)
            .Category(RecipeCategoryType.PowerGlove)
            .Resref("proto_pglove")
            .Level(33)
            .Quantity(1)
            .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
            .Component("ref_keromber", 2)
            .Component("aracia_wood", 1);

        builder.Create(RecipeType.AscendantPowerGlove, SkillType.Smithery)
            .Category(RecipeCategoryType.PowerGlove)
            .Resref("asc_pglove")
            .Level(38)
            .Quantity(1)
            .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
            .Component("ref_keromber", 3)
            .Component("aracia_wood", 2)
            .Component("lth_imperfect", 1);

        builder.Create(RecipeType.OphidianPowerGlove, SkillType.Smithery)
            .Category(RecipeCategoryType.PowerGlove)
            .Resref("oph_pglove")
            .Level(43)
            .Quantity(1)
            .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
            .Component("ref_jasioclase", 2)
            .Component("hyphae_wood", 1);

        builder.Create(RecipeType.ChiroPowerGlove, SkillType.Smithery)
            .Category(RecipeCategoryType.PowerGlove)
            .Resref("chi_pglove")
            .Level(52)
            .Quantity(1)
            .RequirementUnlocked()
            .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
            .ResearchCostModifier(0.2f)
            .Component("ref_arkoxit", 2)
            .Component("ref_jasioclase", 10)
            .Component("hyphae_wood", 20)
            .Component("chiro_shard", 2)
            .Component("ref_veldite", 5)
            .Component("ref_scordspar", 5)
            .Component("ref_plagionite", 5)
            .Component("ref_keromber", 5);

        return builder.Build();
    }
}
