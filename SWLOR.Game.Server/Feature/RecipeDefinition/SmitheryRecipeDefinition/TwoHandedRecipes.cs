using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class TwoHandedRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            GreatSwords();
            Spears();

            return _builder.Build();
        }

        private void GreatSwords()
        {
            // Basic Great Sword
            _builder.Create(RecipeType.BasicGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("b_greatsword")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 1)
                .ModSlots(RecipeModType.Weapon, 1)
                .Component("ref_veldite", 9)
                .Component("wood", 5);

            // Titan Great Sword
            _builder.Create(RecipeType.TitanGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("tit_greatsword")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 2)
                .ModSlots(RecipeModType.Weapon, 1)
                .Component("ref_scordspar", 9)
                .Component("fine_wood", 5);

            // Delta Great Sword
            _builder.Create(RecipeType.DeltaGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("del_greatsword")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 3)
                .ModSlots(RecipeModType.Weapon, 2)
                .Component("ref_plagionite", 9)
                .Component("ancient_wood", 5);

            // Proto Great Sword
            _builder.Create(RecipeType.ProtoGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("proto_greatsword")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 4)
                .ModSlots(RecipeModType.Weapon, 2)
                .Component("ref_keromber", 9)
                .Component("aracia_wood", 5);

            // Ophidian Great Sword
            _builder.Create(RecipeType.OphidianGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("oph_greatsword")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 5)
                .ModSlots(RecipeModType.Weapon, 2)
                .Component("ref_jasioclase", 9)
                .Component("hyphae_wood", 5);
        }

        private void Spears()
        {
            // Basic Spear
            _builder.Create(RecipeType.BasicSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("b_spear")
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 1)
                .ModSlots(RecipeModType.Weapon, 1)
                .Component("ref_veldite", 8)
                .Component("wood", 4);

            // Titan Spear
            _builder.Create(RecipeType.TitanSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("tit_spear")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 2)
                .ModSlots(RecipeModType.Weapon, 1)
                .Component("ref_scordspar", 8)
                .Component("fine_wood", 4);

            // Delta Spear
            _builder.Create(RecipeType.DeltaSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("del_spear")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 3)
                .ModSlots(RecipeModType.Weapon, 2)
                .Component("ref_plagionite", 8)
                .Component("ancient_wood", 4);

            // Proto Spear
            _builder.Create(RecipeType.ProtoSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("proto_spear")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 4)
                .ModSlots(RecipeModType.Weapon, 2)
                .Component("ref_keromber", 8)
                .Component("aracia_wood", 4);

            // Ophidian Spear
            _builder.Create(RecipeType.OphidianSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("oph_spear")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 5)
                .ModSlots(RecipeModType.Weapon, 2)
                .Component("aracia_wood", 8)
                .Component("hyphae_wood", 4);
        }

    }
}