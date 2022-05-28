using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class NecklaceRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Tier1();
            Tier2();
            Tier3();
            Tier4();
            Tier5();

            return _builder.Build();
        }

        private void Tier1()
        {
            // Battlemaster Necklace
            _builder.Create(RecipeType.BattlemasterNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("bm_necklace")
                .Level(2)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 2)
                .Component("jade", 1);

            // Spiritmaster Necklace
            _builder.Create(RecipeType.SpiritmasterNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("sm_necklace")
                .Level(2)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 2)
                .Component("jade", 1);

            // Combat Necklace
            _builder.Create(RecipeType.CombatNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("com_necklace")
                .Level(2)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 2)
                .Component("jade", 1);
        }

        private void Tier2()
        {
            // Titan Necklace
            _builder.Create(RecipeType.TitanNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("tit_necklace")
                .Level(12)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 2)
                .Component("agate", 1);

            // Vivid Necklace
            _builder.Create(RecipeType.VividNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("viv_necklace")
                .Level(12)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 2)
                .Component("agate", 1);

            // Valor Necklace
            _builder.Create(RecipeType.ValorNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("val_necklace")
                .Level(12)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 2)
                .Component("agate", 1);
        }

        private void Tier3()
        {
            // Quark Necklace
            _builder.Create(RecipeType.QuarkNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("qk_necklace")
                .Level(22)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 2)
                .Component("citrine", 1);

            // Reginal Necklace
            _builder.Create(RecipeType.ReginalNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("reg_necklace")
                .Level(22)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 2)
                .Component("citrine", 1);

            // Forza Necklace
            _builder.Create(RecipeType.ForzaNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("for_necklace")
                .Level(22)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 2)
                .Component("citrine", 1);
        }

        private void Tier4()
        {
            // Argos Necklace
            _builder.Create(RecipeType.ArgosNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("ar_necklace")
                .Level(32)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 2)
                .Component("ruby", 1);

            // Grenada Necklace
            _builder.Create(RecipeType.GrenadaNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("gr_necklace")
                .Level(32)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 2)
                .Component("ruby", 1);

            // Survival Necklace
            _builder.Create(RecipeType.SurvivalNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("sur_necklace")
                .Level(32)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 2)
                .Component("ruby", 1);
        }

        private void Tier5()
        {
            // Eclipse Necklace
            _builder.Create(RecipeType.EclipseNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("ec_necklace")
                .Level(42)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 2)
                .Component("emerald", 1);

            // Transcendent Necklace
            _builder.Create(RecipeType.TranscendentNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("tran_necklace")
                .Level(42)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 2)
                .Component("emerald", 1);

            // Supreme Necklace
            _builder.Create(RecipeType.SupremeNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("sup_necklace")
                .Level(42)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 2)
                .Component("emerald", 1);
        }

    }
}