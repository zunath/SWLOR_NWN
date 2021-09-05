using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class RingRecipes : IRecipeListDefinition
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
            // Battlemaster Ring
            _builder.Create(RecipeType.BattlemasterRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("bm_ring")
                .Level(1)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .ModSlots(RecipeModType.Armor, 1)
                .Component("ref_veldite", 2)
                .Component("jade", 1);

            // Spiritmaster Ring
            _builder.Create(RecipeType.SpiritmasterRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("sm_ring")
                .Level(1)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .ModSlots(RecipeModType.Armor, 1)
                .Component("ref_veldite", 2)
                .Component("jade", 1);

            // Combat Ring
            _builder.Create(RecipeType.CombatRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("com_ring")
                .Level(1)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .ModSlots(RecipeModType.Armor, 1)
                .Component("ref_veldite", 2)
                .Component("jade", 1);
        }

        private void Tier2()
        {
            // Titan Ring
            _builder.Create(RecipeType.TitanRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("tit_ring")
                .Level(11)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                .ModSlots(RecipeModType.Armor, 1)
                .Component("ref_scordspar", 2)
                .Component("agate", 1);

            // Vivid Ring
            _builder.Create(RecipeType.VividRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("viv_ring")
                .Level(11)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                .ModSlots(RecipeModType.Armor, 1)
                .Component("ref_scordspar", 2)
                .Component("agate", 1);

            // Valor Ring
            _builder.Create(RecipeType.ValorRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("val_ring")
                .Level(11)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                .ModSlots(RecipeModType.Armor, 1)
                .Component("ref_scordspar", 2)
                .Component("agate", 1);
        }

        private void Tier3()
        {
            // Quark Ring
            _builder.Create(RecipeType.QuarkRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("qk_ring")
                .Level(21)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_plagionite", 2)
                .Component("citrine", 1);

            // Reginal Ring
            _builder.Create(RecipeType.ReginalRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("reg_ring")
                .Level(21)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_plagionite", 2)
                .Component("citrine", 1);

            // Forza Ring
            _builder.Create(RecipeType.ForzaRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("for_ring")
                .Level(21)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_plagionite", 2)
                .Component("citrine", 1);
        }

        private void Tier4()
        {
            // Argos Ring
            _builder.Create(RecipeType.ArgosRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("ar_ring")
                .Level(31)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_keromber", 2)
                .Component("ruby", 1);

            // Grenada Ring
            _builder.Create(RecipeType.GrenadaRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("gr_ring")
                .Level(31)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_keromber", 2)
                .Component("ruby", 1);

            // Survival Ring
            _builder.Create(RecipeType.SurvivalRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("sur_ring")
                .Level(31)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_keromber", 2)
                .Component("ruby", 1);
        }

        private void Tier5()
        {
            // Eclipse Ring
            _builder.Create(RecipeType.EclipseRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("ec_ring")
                .Level(41)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_jasioclase", 2)
                .Component("emerald", 1);

            // Transcendent Ring
            _builder.Create(RecipeType.TranscendentRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("tran_ring")
                .Level(41)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_jasioclase", 2)
                .Component("emerald", 1);

            // Supreme Ring
            _builder.Create(RecipeType.SupremeRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("sup_ring")
                .Level(41)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_jasioclase", 2)
                .Component("emerald", 1);
        }

    }
}