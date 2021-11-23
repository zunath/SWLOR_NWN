using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class TwoHandedRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            GreatSwords();
            Spears();
            TwinBlades();

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
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 9)
                .Component("wood", 5);

            // Titan Great Sword
            _builder.Create(RecipeType.TitanGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("tit_greatsword")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 9)
                .Component("fine_wood", 5);

            // Delta Great Sword
            _builder.Create(RecipeType.DeltaGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("del_greatsword")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 9)
                .Component("ancient_wood", 5);

            // Proto Great Sword
            _builder.Create(RecipeType.ProtoGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("proto_greatsword")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 9)
                .Component("aracia_wood", 5);

            // Ophidian Great Sword
            _builder.Create(RecipeType.OphidianGreatSword, SkillType.Smithery)
                .Category(RecipeCategoryType.GreatSword)
                .Resref("oph_greatsword")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
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
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 8)
                .Component("wood", 4);

            // Titan Spear
            _builder.Create(RecipeType.TitanSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("tit_spear")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 8)
                .Component("fine_wood", 4);

            // Delta Spear
            _builder.Create(RecipeType.DeltaSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("del_spear")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 8)
                .Component("ancient_wood", 4);

            // Proto Spear
            _builder.Create(RecipeType.ProtoSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("proto_spear")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 8)
                .Component("aracia_wood", 4);

            // Ophidian Spear
            _builder.Create(RecipeType.OphidianSpear, SkillType.Smithery)
                .Category(RecipeCategoryType.Spear)
                .Resref("oph_spear")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("aracia_wood", 8)
                .Component("hyphae_wood", 4);
        }

        private void TwinBlades()
        {
            // Basic Twin Blade
            _builder.Create(RecipeType.BasicTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("b_twinblade")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_veldite", 5)
                .Component("wood", 3);

            // Titan Twin Blade
            _builder.Create(RecipeType.TitanTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("tit_twinblade")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1)
                .Component("ref_scordspar", 5)
                .Component("fine_wood", 3);

            // Delta Twin Blade
            _builder.Create(RecipeType.DeltaTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("del_twinblade")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_plagionite", 5)
                .Component("ancient_wood", 3);

            // Proto Twin Blade
            _builder.Create(RecipeType.ProtoTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("proto_twinblade")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_keromber", 5)
                .Component("aracia_wood", 3);

            // Ophidian Twin Blade
            _builder.Create(RecipeType.OphidianTwinBlade, SkillType.Smithery)
                .Category(RecipeCategoryType.TwinBlade)
                .Resref("oph_twinblade")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.TwoHandedBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 2)
                .Component("ref_jasioclase", 5)
                .Component("hyphae_wood", 3);
        }

    }
}