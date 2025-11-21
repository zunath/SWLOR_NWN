using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class MartialRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Katars();
            Staffs();

            return _builder.Build();
        }

        private void Katars()
        {
            // Basic Katar
            _builder.Create(RecipeType.BasicKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("b_katar")
                .Level(3)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 2)
                .Component("wood", 1);

            // Titan Katar
            _builder.Create(RecipeType.TitanKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("tit_katar")
                .Level(13)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 2)
                .Component("fine_wood", 1);

            // Sith Katar
            _builder.Create(RecipeType.SithKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("sith_katar")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 3)
                .Component("fine_wood", 2);

            // Delta Katar
            _builder.Create(RecipeType.DeltaKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("del_katar")
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 2)
                .Component("ancient_wood", 1);

            // Proto Katar
            _builder.Create(RecipeType.ProtoKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("proto_katar")
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 2)
                .Component("aracia_wood", 1);

            // Ophidian Katar
            _builder.Create(RecipeType.OphidianKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("oph_katar")
                .Level(43)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 2)
                .Component("hyphae_wood", 1);

            // Chiro Katar
            _builder.Create(RecipeType.ChiroKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("chi_katar")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 5)
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
        }

        private void Staffs()
        {
            // Basic Staff
            _builder.Create(RecipeType.BasicStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("b_staff")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 3)
                .Component("wood", 2);

            // Titan Staff
            _builder.Create(RecipeType.TitanStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("tit_staff")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_scordspar", 3)
                .Component("fine_wood", 2);

            // Sith Staff
            _builder.Create(RecipeType.SithStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("sith_staff")
                .Level(12)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 2)
                .Component("fine_wood", 1);

            // Delta Staff
            _builder.Create(RecipeType.DeltaStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("del_staff")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 3)
                .Component("ancient_wood", 2);

            // Proto Staff
            _builder.Create(RecipeType.ProtoStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("proto_staff")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 3)
                .Component("aracia_wood", 2);

            // Ophidian Staff
            _builder.Create(RecipeType.OphidianStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("oph_staff")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 3)
                .Component("hyphae_wood", 2);

            // Chiro Staff
            _builder.Create(RecipeType.ChiroStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("chi_staff")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 5)
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

        }

    }
}
