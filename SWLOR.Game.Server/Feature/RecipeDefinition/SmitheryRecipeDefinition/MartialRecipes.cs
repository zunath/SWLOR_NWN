using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService;

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
                .ModSlots(RecipeModType.Weapon, 1)
                .Component("ref_veldite", 4)
                .Component("wood", 2);

            // Titan Katar
            _builder.Create(RecipeType.TitanKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("tit_katar")
                .Level(13)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 2)
                .ModSlots(RecipeModType.Weapon, 1)
                .Component("ref_scordspar", 4)
                .Component("fine_wood", 2);

            // Delta Katar
            _builder.Create(RecipeType.DeltaKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("del_katar")
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 3)
                .ModSlots(RecipeModType.Weapon, 2)
                .Component("ref_plagionite", 4)
                .Component("ancient_wood", 2);

            // Proto Katar
            _builder.Create(RecipeType.ProtoKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("proto_katar")
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 4)
                .ModSlots(RecipeModType.Weapon, 2)
                .Component("ref_keromber", 4)
                .Component("aracia_wood", 2);

            // Ophidian Katar
            _builder.Create(RecipeType.OphidianKatar, SkillType.Smithery)
                .Category(RecipeCategoryType.Katar)
                .Resref("oph_katar")
                .Level(43)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 5)
                .ModSlots(RecipeModType.Weapon, 2)
                .Component("ref_jasioclase", 4)
                .Component("hyphae_wood", 2);
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
                .ModSlots(RecipeModType.Weapon, 1)
                .Component("ref_veldite", 6)
                .Component("wood", 3);

            // Titan Staff
            _builder.Create(RecipeType.TitanStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("tit_staff")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 2)
                .ModSlots(RecipeModType.Weapon, 1)
                .Component("ref_scordspar", 6)
                .Component("fine_wood", 3);

            // Delta Staff
            _builder.Create(RecipeType.DeltaStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("del_staff")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 3)
                .ModSlots(RecipeModType.Weapon, 2)
                .Component("ref_plagionite", 6)
                .Component("ancient_wood", 3);

            // Proto Staff
            _builder.Create(RecipeType.ProtoStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("proto_staff")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 4)
                .ModSlots(RecipeModType.Weapon, 2)
                .Component("ref_keromber", 6)
                .Component("aracia_wood", 3);

            // Ophidian Staff
            _builder.Create(RecipeType.OphidianStaff, SkillType.Smithery)
                .Category(RecipeCategoryType.Staff)
                .Resref("oph_staff")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.MartialBlueprints, 5)
                .ModSlots(RecipeModType.Weapon, 2)
                .Component("ref_jasioclase", 6)
                .Component("hyphae_wood", 3);
        }

    }
}