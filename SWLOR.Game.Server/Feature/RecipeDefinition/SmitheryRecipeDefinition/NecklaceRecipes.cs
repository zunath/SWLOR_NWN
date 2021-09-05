using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService;

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
                .ModSlots(RecipeModType.Armor, 1)
                .Component("ref_veldite", 3)
                .Component("jade", 2);

            // Spiritmaster Necklace
            _builder.Create(RecipeType.SpiritmasterNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("sm_necklace")
                .Level(2)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .ModSlots(RecipeModType.Armor, 1)
                .Component("ref_veldite", 3)
                .Component("jade", 2);

            // Combat Necklace
            _builder.Create(RecipeType.CombatNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("com_necklace")
                .Level(2)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .ModSlots(RecipeModType.Armor, 1)
                .Component("ref_veldite", 3)
                .Component("jade", 2);
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
                .ModSlots(RecipeModType.Armor, 1)
                .Component("ref_scordspar", 3)
                .Component("agate", 2);

            // Vivid Necklace
            _builder.Create(RecipeType.VividNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("viv_necklace")
                .Level(12)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                .ModSlots(RecipeModType.Armor, 1)
                .Component("ref_scordspar", 3)
                .Component("agate", 2);

            // Valor Necklace
            _builder.Create(RecipeType.ValorNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("val_necklace")
                .Level(12)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                .ModSlots(RecipeModType.Armor, 1)
                .Component("ref_scordspar", 3)
                .Component("agate", 2);
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
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_plagionite", 3)
                .Component("citrine", 2);

            // Reginal Necklace
            _builder.Create(RecipeType.ReginalNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("reg_necklace")
                .Level(22)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_plagionite", 3)
                .Component("citrine", 2);

            // Forza Necklace
            _builder.Create(RecipeType.ForzaNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("for_necklace")
                .Level(22)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_plagionite", 3)
                .Component("citrine", 2);
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
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_keromber", 3)
                .Component("ruby", 2);

            // Grenada Necklace
            _builder.Create(RecipeType.GrenadaNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("gr_necklace")
                .Level(32)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_keromber", 3)
                .Component("ruby", 2);

            // Survival Necklace
            _builder.Create(RecipeType.SurvivalNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("sur_necklace")
                .Level(32)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_keromber", 3)
                .Component("ruby", 2);
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
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_jasioclase", 3)
                .Component("emerald", 2);

            // Transcendent Necklace
            _builder.Create(RecipeType.TranscendentNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("tran_necklace")
                .Level(42)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_jasioclase", 3)
                .Component("emerald", 2);

            // Supreme Necklace
            _builder.Create(RecipeType.SupremeNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("sup_necklace")
                .Level(42)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_jasioclase", 3)
                .Component("emerald", 2);
        }

    }
}