using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class OneHandedRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Knifes();
            Longswords();

            return _builder.Build();
        }

        private void Knifes()
        {
            // Basic Knife
            _builder.Create(RecipeType.BasicKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("b_knife")
                .Level(1)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 2)
                .Component("wood", 1);

            // Titan Knife
            _builder.Create(RecipeType.TitanKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("tit_knife")
                .Level(11)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 2)
                .Component("fine_wood", 1);

            // Delta Knife
            _builder.Create(RecipeType.DeltaKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("del_knife")
                .Level(21)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 2)
                .Component("ancient_wood", 1);

            // Proto Knife
            _builder.Create(RecipeType.ProtoKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("proto_knife")
                .Level(31)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 2)
                .Component("aracia_wood", 1);

            // Ophidian Knife
            _builder.Create(RecipeType.OphidianKnife, SkillType.Smithery)
                .Category(RecipeCategoryType.Knife)
                .Resref("oph_knife")
                .Level(41)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 2)
                .Component("hyphae_wood", 1);
        }

        private void Longswords()
        {
            // Basic Longsword
            _builder.Create(RecipeType.BasicLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("b_longsword")
                .Level(4)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 5)
                .Component("wood", 3);

            // Titan Longsword
            _builder.Create(RecipeType.TitanLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("tit_longsword")
                .Level(14)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 5)
                .Component("fine_wood", 3);

            // Delta Longsword
            _builder.Create(RecipeType.DeltaLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("del_longsword")
                .Level(24)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 5)
                .Component("ancient_wood", 3);

            // Proto Longsword
            _builder.Create(RecipeType.ProtoLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("pro_longsword")
                .Level(34)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 5)
                .Component("aracia_wood", 3);

            // Ophidian Longsword
            _builder.Create(RecipeType.OphidianLongsword, SkillType.Smithery)
                .Category(RecipeCategoryType.Longsword)
                .Resref("oph_longsword")
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.OneHandedBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 5)
                .Component("hyphae_wood", 3);
        }

    }
}