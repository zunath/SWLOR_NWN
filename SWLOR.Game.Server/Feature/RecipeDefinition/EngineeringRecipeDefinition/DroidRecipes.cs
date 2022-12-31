using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    public class DroidRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

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
			// DHCL-001
			_builder.Create(RecipeType.DHCL001, SkillType.Engineering)
				.Category(RecipeCategoryType.Cloak)
				.Resref("dhcl001")
				.Level(8)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_ruined", 5)
				.Component("ref_veldite", 3);

			// DHBE-001
			_builder.Create(RecipeType.DHBE001, SkillType.Engineering)
				.Category(RecipeCategoryType.Belt)
				.Resref("dhbe001")
				.Level(9)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_ruined", 5)
				.Component("ref_veldite", 3);

			// DHRG-001
			_builder.Create(RecipeType.DHRG001, SkillType.Engineering)
				.Category(RecipeCategoryType.Ring)
				.Resref("dhrg001")
				.Level(1)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_ruined", 1)
				.Component("ref_veldite", 1);

			// DHNK-001
			_builder.Create(RecipeType.DHNK001, SkillType.Engineering)
				.Category(RecipeCategoryType.Necklace)
				.Resref("dhnk001")
				.Level(2)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_ruined", 2)
				.Component("ref_veldite", 1);

			// DHAR-001
			_builder.Create(RecipeType.DHAR001, SkillType.Engineering)
				.Category(RecipeCategoryType.Breastplate)
				.Resref("dhar001")
				.Level(7)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_ruined", 4)
				.Component("ref_veldite", 2);

			// DHHL-001
			_builder.Create(RecipeType.DHHL001, SkillType.Engineering)
				.Category(RecipeCategoryType.Helmet)
				.Resref("dhhl001")
				.Level(4)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_ruined", 3)
				.Component("ref_veldite", 2);

			// DHBR-001
			_builder.Create(RecipeType.DHBR001, SkillType.Engineering)
				.Category(RecipeCategoryType.Bracer)
				.Resref("dhbr001")
				.Level(3)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_ruined", 2)
				.Component("ref_veldite", 1);

			// DHLG-001
			_builder.Create(RecipeType.DHLG001, SkillType.Engineering)
				.Category(RecipeCategoryType.Legging)
				.Resref("dhlg001")
				.Level(5)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_ruined", 3)
				.Component("ref_veldite", 2);

			// DLCL-001
			_builder.Create(RecipeType.DLCL001, SkillType.Engineering)
				.Category(RecipeCategoryType.Cloak)
				.Resref("dlcl001")
				.Level(8)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_ruined", 5)
				.Component("ref_veldite", 3);

			// DLBE-001
			_builder.Create(RecipeType.DLBE001, SkillType.Engineering)
				.Category(RecipeCategoryType.Belt)
				.Resref("dlbe001")
				.Level(9)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_ruined", 5)
				.Component("ref_veldite", 3);

			// DLRG-001
			_builder.Create(RecipeType.DLRG001, SkillType.Engineering)
				.Category(RecipeCategoryType.Ring)
				.Resref("dlrg001")
				.Level(1)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_ruined", 1)
				.Component("ref_veldite", 1);

			// DLNK-001
			_builder.Create(RecipeType.DLNK001, SkillType.Engineering)
				.Category(RecipeCategoryType.Necklace)
				.Resref("dlnk001")
				.Level(2)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_ruined", 2)
				.Component("ref_veldite", 1);

			// DLAR-001
			_builder.Create(RecipeType.DLAR001, SkillType.Engineering)
				.Category(RecipeCategoryType.Breastplate)
				.Resref("dlar001")
				.Level(7)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_ruined", 4)
				.Component("ref_veldite", 2);

			// DLHL-001
			_builder.Create(RecipeType.DLHL001, SkillType.Engineering)
				.Category(RecipeCategoryType.Helmet)
				.Resref("dlhl001")
				.Level(4)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_ruined", 3)
				.Component("ref_veldite", 2);

			// DLBR-001
			_builder.Create(RecipeType.DLBR001, SkillType.Engineering)
				.Category(RecipeCategoryType.Bracer)
				.Resref("dlbr001")
				.Level(3)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_ruined", 2)
				.Component("ref_veldite", 1);

			// DLLG-001
			_builder.Create(RecipeType.DLLG001, SkillType.Engineering)
				.Category(RecipeCategoryType.Legging)
				.Resref("dllg001")
				.Level(5)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_ruined", 3)
				.Component("ref_veldite", 2);

            // DSCL-001
            _builder.Create(RecipeType.DSCL001, SkillType.Engineering)
                .Category(RecipeCategoryType.Cloak)
                .Resref("dscl001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DECL-001
            _builder.Create(RecipeType.DECL001, SkillType.Engineering)
                .Category(RecipeCategoryType.Cloak)
                .Resref("decl001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DACL-001
            _builder.Create(RecipeType.DACL001, SkillType.Engineering)
                .Category(RecipeCategoryType.Cloak)
                .Resref("dacl001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DFCL-001
            _builder.Create(RecipeType.DFCL001, SkillType.Engineering)
                .Category(RecipeCategoryType.Cloak)
                .Resref("dfcl001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DSBE-001
            _builder.Create(RecipeType.DSBE001, SkillType.Engineering)
                .Category(RecipeCategoryType.Belt)
                .Resref("dsbe001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DEBE-001
            _builder.Create(RecipeType.DEBE001, SkillType.Engineering)
                .Category(RecipeCategoryType.Belt)
                .Resref("debe001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DABE-001
            _builder.Create(RecipeType.DABE001, SkillType.Engineering)
                .Category(RecipeCategoryType.Belt)
                .Resref("dabe001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

			// DFBE-001
			_builder.Create(RecipeType.DFBE001, SkillType.Engineering)
				.Category(RecipeCategoryType.Belt)
                .Resref("dfbe001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DSRG-001
            _builder.Create(RecipeType.DSRG001, SkillType.Engineering)
                .Category(RecipeCategoryType.Ring)
                .Resref("dsrg001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DERG-001
            _builder.Create(RecipeType.DERG001, SkillType.Engineering)
                .Category(RecipeCategoryType.Ring)
                .Resref("derg001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DARG-001
            _builder.Create(RecipeType.DARG001, SkillType.Engineering)
                .Category(RecipeCategoryType.Ring)
                .Resref("darg001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DFRG-001
            _builder.Create(RecipeType.DFRG001, SkillType.Engineering)
                .Category(RecipeCategoryType.Ring)
                .Resref("dfrg001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DSNK-001
            _builder.Create(RecipeType.DSNK001, SkillType.Engineering)
                .Category(RecipeCategoryType.Necklace)
                .Resref("dsnk001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DENK-001
            _builder.Create(RecipeType.DENK001, SkillType.Engineering)
                .Category(RecipeCategoryType.Necklace)
                .Resref("denk001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DANK-001
            _builder.Create(RecipeType.DANK001, SkillType.Engineering)
                .Category(RecipeCategoryType.Necklace)
                .Resref("dank001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DFNK-001
            _builder.Create(RecipeType.DFNK001, SkillType.Engineering)
                .Category(RecipeCategoryType.Necklace)
                .Resref("dfnk001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DSAR-001
            _builder.Create(RecipeType.DSAR001, SkillType.Engineering)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("dsar001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DEAR-001
            _builder.Create(RecipeType.DEAR001, SkillType.Engineering)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("dear001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DAAR-001
            _builder.Create(RecipeType.DAAR001, SkillType.Engineering)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("daar001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DFAR-001
            _builder.Create(RecipeType.DFAR001, SkillType.Engineering)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("dfar001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DSHL-001
            _builder.Create(RecipeType.DSHL001, SkillType.Engineering)
                .Category(RecipeCategoryType.Helmet)
                .Resref("dshl001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DEHL-001
            _builder.Create(RecipeType.DEHL001, SkillType.Engineering)
                .Category(RecipeCategoryType.Helmet)
                .Resref("dehl001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DAHL-001
            _builder.Create(RecipeType.DAHL001, SkillType.Engineering)
                .Category(RecipeCategoryType.Helmet)
                .Resref("dahl001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DFHL-001
            _builder.Create(RecipeType.DFHL001, SkillType.Engineering)
                .Category(RecipeCategoryType.Helmet)
                .Resref("dfhl001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DSBR-001
            _builder.Create(RecipeType.DSBR001, SkillType.Engineering)
                .Category(RecipeCategoryType.Bracer)
                .Resref("dsbr001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DEBR-001
            _builder.Create(RecipeType.DEBR001, SkillType.Engineering)
                .Category(RecipeCategoryType.Bracer)
                .Resref("debr001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DABR-001
            _builder.Create(RecipeType.DABR001, SkillType.Engineering)
                .Category(RecipeCategoryType.Bracer)
                .Resref("dabr001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DFBR-001
            _builder.Create(RecipeType.DFBR001, SkillType.Engineering)
                .Category(RecipeCategoryType.Bracer)
                .Resref("dfbr001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DSLG-001
            _builder.Create(RecipeType.DSLG001, SkillType.Engineering)
                .Category(RecipeCategoryType.Legging)
                .Resref("dslg001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DELG-001
            _builder.Create(RecipeType.DELG001, SkillType.Engineering)
                .Category(RecipeCategoryType.Legging)
                .Resref("delg001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DALG-001
            _builder.Create(RecipeType.DALG001, SkillType.Engineering)
                .Category(RecipeCategoryType.Legging)
                .Resref("dalg001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // DFLG-001
            _builder.Create(RecipeType.DFLG001, SkillType.Engineering)
                .Category(RecipeCategoryType.Legging)
                .Resref("dflg001")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);
        }

		private void Tier2()
        {
			// DHCL-002
			_builder.Create(RecipeType.DHCL002, SkillType.Engineering)
				.Category(RecipeCategoryType.Cloak)
				.Resref("dhcl002")
				.Level(18)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_flawed", 5)
				.Component("ref_scordspar", 3);

			// DHBE-002
			_builder.Create(RecipeType.DHBE002, SkillType.Engineering)
				.Category(RecipeCategoryType.Belt)
				.Resref("dhbe002")
				.Level(19)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_flawed", 5)
				.Component("ref_scordspar", 3);

			// DHRG-002
			_builder.Create(RecipeType.DHRG002, SkillType.Engineering)
				.Category(RecipeCategoryType.Ring)
				.Resref("dhrg002")
				.Level(11)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_flawed", 1)
				.Component("ref_scordspar", 1);

			// DHNK-002
			_builder.Create(RecipeType.DHNK002, SkillType.Engineering)
				.Category(RecipeCategoryType.Necklace)
				.Resref("dhnk002")
				.Level(12)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_flawed", 2)
				.Component("ref_scordspar", 1);

			// DHAR-002
			_builder.Create(RecipeType.DHAR002, SkillType.Engineering)
				.Category(RecipeCategoryType.Breastplate)
				.Resref("dhar002")
				.Level(17)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_flawed", 4)
				.Component("ref_scordspar", 2);

			// DHHL-002
			_builder.Create(RecipeType.DHHL002, SkillType.Engineering)
				.Category(RecipeCategoryType.Helmet)
				.Resref("dhhl002")
				.Level(14)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_flawed", 3)
				.Component("ref_scordspar", 2);

			// DHBR-002
			_builder.Create(RecipeType.DHBR002, SkillType.Engineering)
				.Category(RecipeCategoryType.Bracer)
				.Resref("dhbr002")
				.Level(13)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_flawed", 2)
				.Component("ref_scordspar", 1);

			// DHLG-002
			_builder.Create(RecipeType.DHLG002, SkillType.Engineering)
				.Category(RecipeCategoryType.Legging)
				.Resref("dhlg002")
				.Level(15)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_flawed", 3)
				.Component("ref_scordspar", 2);

			// DLCL-002
			_builder.Create(RecipeType.DLCL002, SkillType.Engineering)
				.Category(RecipeCategoryType.Cloak)
				.Resref("dlcl002")
				.Level(18)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_flawed", 5)
				.Component("ref_scordspar", 3);

			// DLBE-002
			_builder.Create(RecipeType.DLBE002, SkillType.Engineering)
				.Category(RecipeCategoryType.Belt)
				.Resref("dlbe002")
				.Level(19)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_flawed", 5)
				.Component("ref_scordspar", 3);

			// DLRG-002
			_builder.Create(RecipeType.DLRG002, SkillType.Engineering)
				.Category(RecipeCategoryType.Ring)
				.Resref("dlrg002")
				.Level(11)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_flawed", 1)
				.Component("ref_scordspar", 1);

			// DLNK-002
			_builder.Create(RecipeType.DLNK002, SkillType.Engineering)
				.Category(RecipeCategoryType.Necklace)
				.Resref("dlnk002")
				.Level(12)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_flawed", 2)
				.Component("ref_scordspar", 1);

			// DLAR-002
			_builder.Create(RecipeType.DLAR002, SkillType.Engineering)
				.Category(RecipeCategoryType.Breastplate)
				.Resref("dlar002")
				.Level(17)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_flawed", 4)
				.Component("ref_scordspar", 2);

			// DLHL-002
			_builder.Create(RecipeType.DLHL002, SkillType.Engineering)
				.Category(RecipeCategoryType.Helmet)
				.Resref("dlhl002")
				.Level(14)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_flawed", 3)
				.Component("ref_scordspar", 2);

			// DLBR-002
			_builder.Create(RecipeType.DLBR002, SkillType.Engineering)
				.Category(RecipeCategoryType.Bracer)
				.Resref("dlbr002")
				.Level(13)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_flawed", 2)
				.Component("ref_scordspar", 1);

			// DLLG-002
			_builder.Create(RecipeType.DLLG002, SkillType.Engineering)
				.Category(RecipeCategoryType.Legging)
				.Resref("dllg002")
				.Level(15)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Armor, 1)
				.Component("elec_flawed", 3)
				.Component("ref_scordspar", 2);

            /* DSCL-002
            _builder.Create(RecipeType.DSCL002, SkillType.Engineering)
                .Category(RecipeCategoryType.Cloak)
                .Resref("dscl002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DECL-002
            _builder.Create(RecipeType.DECL002, SkillType.Engineering)
                .Category(RecipeCategoryType.Cloak)
                .Resref("decl002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DACL-002
            _builder.Create(RecipeType.DACL002, SkillType.Engineering)
                .Category(RecipeCategoryType.Cloak)
                .Resref("dacl002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DFCL-002
            _builder.Create(RecipeType.DFCL002, SkillType.Engineering)
                .Category(RecipeCategoryType.Cloak)
                .Resref("dfcl002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DSBE-002
            _builder.Create(RecipeType.DSBE002, SkillType.Engineering)
                .Category(RecipeCategoryType.Belt)
                .Resref("dsbe001")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DEBE-002
            _builder.Create(RecipeType.DEBE002, SkillType.Engineering)
                .Category(RecipeCategoryType.Belt)
                .Resref("debe002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DABE-002
            _builder.Create(RecipeType.DABE002, SkillType.Engineering)
                .Category(RecipeCategoryType.Belt)
                .Resref("dabe002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DFBE-002
            _builder.Create(RecipeType.DFBE002, SkillType.Engineering)
                .Category(RecipeCategoryType.Belt)
                .Resref("dfbe002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DSRG-002
            _builder.Create(RecipeType.DSRG002, SkillType.Engineering)
                .Category(RecipeCategoryType.Ring)
                .Resref("dsrg002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DERG-002
            _builder.Create(RecipeType.DERG002, SkillType.Engineering)
                .Category(RecipeCategoryType.Ring)
                .Resref("derg002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DARG-002
            _builder.Create(RecipeType.DARG002, SkillType.Engineering)
                .Category(RecipeCategoryType.Ring)
                .Resref("darg002")
                .Level(20)
                .Quantity(2)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DFRG-002
            _builder.Create(RecipeType.DFRG002, SkillType.Engineering)
                .Category(RecipeCategoryType.Ring)
                .Resref("dfrg002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DSNK-002
            _builder.Create(RecipeType.DSNK002, SkillType.Engineering)
                .Category(RecipeCategoryType.Necklace)
                .Resref("dsnk002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DENK-002
            _builder.Create(RecipeType.DENK002, SkillType.Engineering)
                .Category(RecipeCategoryType.Necklace)
                .Resref("denk002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DANK-002
            _builder.Create(RecipeType.DANK002, SkillType.Engineering)
                .Category(RecipeCategoryType.Necklace)
                .Resref("dank002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DFNK-002
            _builder.Create(RecipeType.DFNK002, SkillType.Engineering)
                .Category(RecipeCategoryType.Necklace)
                .Resref("dfnk002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DSAR-002
            _builder.Create(RecipeType.DSAR002, SkillType.Engineering)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("dsar002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DEAR-002
            _builder.Create(RecipeType.DEAR002, SkillType.Engineering)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("dear002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DAAR-002
            _builder.Create(RecipeType.DAAR002, SkillType.Engineering)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("daar002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DFAR-002
            _builder.Create(RecipeType.DFAR002, SkillType.Engineering)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("dfar002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DSHL-002
            _builder.Create(RecipeType.DSHL002, SkillType.Engineering)
                .Category(RecipeCategoryType.Helmet)
                .Resref("dshl001")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DEHL-002
            _builder.Create(RecipeType.DEHL002, SkillType.Engineering)
                .Category(RecipeCategoryType.Helmet)
                .Resref("dehl002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DAHL-002
            _builder.Create(RecipeType.DAHL002, SkillType.Engineering)
                .Category(RecipeCategoryType.Helmet)
                .Resref("dahl002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DFHL-002
            _builder.Create(RecipeType.DFHL002, SkillType.Engineering)
                .Category(RecipeCategoryType.Helmet)
                .Resref("dfhl002")
                .Level (20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DSBR-002
            _builder.Create(RecipeType.DSBR002, SkillType.Engineering)
                .Category(RecipeCategoryType.Bracer)
                .Resref("dsbr002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DEBR-002
            _builder.Create(RecipeType.DEBR002, SkillType.Engineering)
                .Category(RecipeCategoryType.Bracer)
                .Resref("debr002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DABR-002
            _builder.Create(RecipeType.DABR002, SkillType.Engineering)
                .Category(RecipeCategoryType.Bracer)
                .Resref("dabr002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DFBR-002
            _builder.Create(RecipeType.DFBR002, SkillType.Engineering)
                .Category(RecipeCategoryType.Bracer)
                .Resref("dfbr002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DSLG-002
            _builder.Create(RecipeType.DSLG002, SkillType.Engineering)
                .Category(RecipeCategoryType.Legging)
                .Resref("dslg002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DELG-002
            _builder.Create(RecipeType.DELG002, SkillType.Engineering)
                .Category(RecipeCategoryType.Legging)
                .Resref("delg002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DALG-002
            _builder.Create(RecipeType.DALG002, SkillType.Engineering)
                .Category(RecipeCategoryType.Legging)
                .Resref("dalg002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // DFLG-002
            _builder.Create(RecipeType.DFLG002, SkillType.Engineering)
                .Category(RecipeCategoryType.Legging)
                .Resref("dflg002")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3); */
        }

        private void Tier3()
		{
            // DHCL-003
			_builder.Create(RecipeType.DHCL003, SkillType.Engineering)
				.Category(RecipeCategoryType.Cloak)
				.Resref("dhcl003")
				.Level(28)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_good", 5)
				.Component("ref_plagionite", 3);

			// DHBE-003
			_builder.Create(RecipeType.DHBE003, SkillType.Engineering)
				.Category(RecipeCategoryType.Belt)
				.Resref("dhbe003")
				.Level(29)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_good", 5)
				.Component("ref_plagionite", 3);

			// DHRG-003
			_builder.Create(RecipeType.DHRG003, SkillType.Engineering)
				.Category(RecipeCategoryType.Ring)
				.Resref("dhrg003")
				.Level(21)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_good", 1)
				.Component("ref_plagionite", 1);

			// DHNK-003
			_builder.Create(RecipeType.DHNK003, SkillType.Engineering)
				.Category(RecipeCategoryType.Necklace)
				.Resref("dhnk003")
				.Level(22)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_good", 2)
				.Component("ref_plagionite", 1);

			// DHAR-003
			_builder.Create(RecipeType.DHAR003, SkillType.Engineering)
				.Category(RecipeCategoryType.Breastplate)
				.Resref("dhar003")
				.Level(27)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_good", 4)
				.Component("ref_plagionite", 2);

			// DHHL-003
			_builder.Create(RecipeType.DHHL003, SkillType.Engineering)
				.Category(RecipeCategoryType.Helmet)
				.Resref("dhhl003")
				.Level(24)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_good", 3)
				.Component("ref_plagionite", 2);

			// DHBR-003
			_builder.Create(RecipeType.DHBR003, SkillType.Engineering)
				.Category(RecipeCategoryType.Bracer)
				.Resref("dhbr003")
				.Level(23)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_good", 2)
				.Component("ref_plagionite", 1);

			// DHLG-003
			_builder.Create(RecipeType.DHLG003, SkillType.Engineering)
				.Category(RecipeCategoryType.Legging)
				.Resref("dhlg003")
				.Level(25)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_good", 3)
				.Component("ref_plagionite", 2);

			// DLCL-003
			_builder.Create(RecipeType.DLCL003, SkillType.Engineering)
				.Category(RecipeCategoryType.Cloak)
				.Resref("dlcl003")
				.Level(28)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_good", 5)
				.Component("ref_plagionite", 3);

			// DLBE-003
			_builder.Create(RecipeType.DLBE003, SkillType.Engineering)
				.Category(RecipeCategoryType.Belt)
				.Resref("dlbe003")
				.Level(29)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_good", 5)
				.Component("ref_plagionite", 3);

			// DLRG-003
			_builder.Create(RecipeType.DLRG003, SkillType.Engineering)
				.Category(RecipeCategoryType.Ring)
				.Resref("dlrg003")
				.Level(21)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_good", 1)
				.Component("ref_plagionite", 1);

			// DLNK-003
			_builder.Create(RecipeType.DLNK003, SkillType.Engineering)
				.Category(RecipeCategoryType.Necklace)
				.Resref("dlnk003")
				.Level(22)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_good", 2)
				.Component("ref_plagionite", 1);

			// DLAR-003
			_builder.Create(RecipeType.DLAR003, SkillType.Engineering)
				.Category(RecipeCategoryType.Breastplate)
				.Resref("dlar003")
				.Level(27)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_good", 4)
				.Component("ref_plagionite", 2);

			// DLHL-003
			_builder.Create(RecipeType.DLHL003, SkillType.Engineering)
				.Category(RecipeCategoryType.Helmet)
				.Resref("dlhl003")
				.Level(24)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_good", 3)
				.Component("ref_plagionite", 2);

			// DLBR-003
			_builder.Create(RecipeType.DLBR003, SkillType.Engineering)
				.Category(RecipeCategoryType.Bracer)
				.Resref("dlbr003")
				.Level(23)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_good", 2)
				.Component("ref_plagionite", 1);

			// DLLG-003
			_builder.Create(RecipeType.DLLG003, SkillType.Engineering)
				.Category(RecipeCategoryType.Legging)
				.Resref("dllg003")
				.Level(25)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_good", 3)
				.Component("ref_plagionite", 2);

            // DSCL-003
            _builder.Create(RecipeType.DSCL003, SkillType.Engineering)
                .Category(RecipeCategoryType.Cloak)
                .Resref("dscl003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DECL-003
            _builder.Create(RecipeType.DECL003, SkillType.Engineering)
                .Category(RecipeCategoryType.Cloak)
                .Resref("decl003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DACL-003
            _builder.Create(RecipeType.DACL003, SkillType.Engineering)
                .Category(RecipeCategoryType.Cloak)
                .Resref("dacl003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DFCL-003
            _builder.Create(RecipeType.DFCL003, SkillType.Engineering)
                .Category(RecipeCategoryType.Cloak)
                .Resref("dfcl003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DSBE-003
            _builder.Create(RecipeType.DSBE003, SkillType.Engineering)
                .Category(RecipeCategoryType.Belt)
                .Resref("dsbe003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DEBE-003
            _builder.Create(RecipeType.DEBE003, SkillType.Engineering)
                .Category(RecipeCategoryType.Belt)
                .Resref("debe003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DABE-003
            _builder.Create(RecipeType.DABE003, SkillType.Engineering)
                .Category(RecipeCategoryType.Belt)
                .Resref("dabe003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DFBE-003
            _builder.Create(RecipeType.DFBE003, SkillType.Engineering)
                .Category(RecipeCategoryType.Belt)
                .Resref("dfbe003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DSRG-003
            _builder.Create(RecipeType.DSRG003, SkillType.Engineering)
                .Category(RecipeCategoryType.Ring)
                .Resref("dsrg003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DERG-003
            _builder.Create(RecipeType.DERG003, SkillType.Engineering)
                .Category(RecipeCategoryType.Ring)
                .Resref("derg003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DARG-003
            _builder.Create(RecipeType.DARG003, SkillType.Engineering)
                .Category(RecipeCategoryType.Ring)
                .Resref("darg003")
                .Level(30)
                .Quantity(2)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DFRG-003
            _builder.Create(RecipeType.DFRG003, SkillType.Engineering)
                .Category(RecipeCategoryType.Ring)
                .Resref("dfrg003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DSNK-003
            _builder.Create(RecipeType.DSNK003, SkillType.Engineering)
                .Category(RecipeCategoryType.Necklace)
                .Resref("dsnk003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DENK-003
            _builder.Create(RecipeType.DENK003, SkillType.Engineering)
                .Category(RecipeCategoryType.Necklace)
                .Resref("denk003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DANK-003
            _builder.Create(RecipeType.DANK003, SkillType.Engineering)
                .Category(RecipeCategoryType.Necklace)
                .Resref("dank003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DFNK-003
            _builder.Create(RecipeType.DFNK003, SkillType.Engineering)
                .Category(RecipeCategoryType.Necklace)
                .Resref("dfnk003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DSAR-003
            _builder.Create(RecipeType.DSAR003, SkillType.Engineering)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("dsar003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DEAR-003
            _builder.Create(RecipeType.DEAR003, SkillType.Engineering)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("dear003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DAAR-003
            _builder.Create(RecipeType.DAAR003, SkillType.Engineering)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("daar003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DFAR-003
            _builder.Create(RecipeType.DFAR003, SkillType.Engineering)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("dfar003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DSHL-003
            _builder.Create(RecipeType.DSHL003, SkillType.Engineering)
                .Category(RecipeCategoryType.Helmet)
                .Resref("dshl003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DEHL-003
            _builder.Create(RecipeType.DEHL003, SkillType.Engineering)
                .Category(RecipeCategoryType.Helmet)
                .Resref("dehl003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DAHL-003
            _builder.Create(RecipeType.DAHL003, SkillType.Engineering)
                .Category(RecipeCategoryType.Helmet)
                .Resref("dahl003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DFHL-003
            _builder.Create(RecipeType.DFHL003, SkillType.Engineering)
                .Category(RecipeCategoryType.Helmet)
                .Resref("dfhl003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DSBR-003
            _builder.Create(RecipeType.DSBR003, SkillType.Engineering)
                .Category(RecipeCategoryType.Bracer)
                .Resref("dsbr003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DEBR-003
            _builder.Create(RecipeType.DEBR003, SkillType.Engineering)
                .Category(RecipeCategoryType.Bracer)
                .Resref("debr003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DABR-003
            _builder.Create(RecipeType.DABR003, SkillType.Engineering)
                .Category(RecipeCategoryType.Bracer)
                .Resref("dabr003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DFBR-003
            _builder.Create(RecipeType.DFBR003, SkillType.Engineering)
                .Category(RecipeCategoryType.Bracer)
                .Resref("dfbr003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DSLG-003
            _builder.Create(RecipeType.DSLG003, SkillType.Engineering)
                .Category(RecipeCategoryType.Legging)
                .Resref("dslg003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DELG-003
            _builder.Create(RecipeType.DELG003, SkillType.Engineering)
                .Category(RecipeCategoryType.Legging)
                .Resref("delg003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DALG-003
            _builder.Create(RecipeType.DALG003, SkillType.Engineering)
                .Category(RecipeCategoryType.Legging)
                .Resref("dalg003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // DFLG-003
            _builder.Create(RecipeType.DFLG003, SkillType.Engineering)
                .Category(RecipeCategoryType.Legging)
                .Resref("dflg003")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);
        }

        private void Tier4()
		{
            // DHCL-004
			_builder.Create(RecipeType.DHCL004, SkillType.Engineering)
				.Category(RecipeCategoryType.Cloak)
				.Resref("dhcl004")
				.Level(38)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_imperfect", 5)
				.Component("ref_keromber", 3);

			// DHBE-004
			_builder.Create(RecipeType.DHBE004, SkillType.Engineering)
				.Category(RecipeCategoryType.Belt)
				.Resref("dhbe004")
				.Level(39)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_imperfect", 5)
				.Component("ref_keromber", 3);

			// DHRG-004
			_builder.Create(RecipeType.DHRG004, SkillType.Engineering)
				.Category(RecipeCategoryType.Ring)
				.Resref("dhrg004")
				.Level(31)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_imperfect", 1)
				.Component("ref_keromber", 1);

			// DHNK-004
			_builder.Create(RecipeType.DHNK004, SkillType.Engineering)
				.Category(RecipeCategoryType.Necklace)
				.Resref("dhnk004")
				.Level(32)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_imperfect", 2)
				.Component("ref_keromber", 1);

			// DHAR-004
			_builder.Create(RecipeType.DHAR004, SkillType.Engineering)
				.Category(RecipeCategoryType.Breastplate)
				.Resref("dhar004")
				.Level(37)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_imperfect", 4)
				.Component("ref_keromber", 2);

			// DHHL-004
			_builder.Create(RecipeType.DHHL004, SkillType.Engineering)
				.Category(RecipeCategoryType.Helmet)
				.Resref("dhhl004")
				.Level(34)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_imperfect", 3)
				.Component("ref_keromber", 2);

			// DHBR-004
			_builder.Create(RecipeType.DHBR004, SkillType.Engineering)
				.Category(RecipeCategoryType.Bracer)
				.Resref("dhbr004")
				.Level(33)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_imperfect", 2)
				.Component("ref_keromber", 1);

			// DHLG-004
			_builder.Create(RecipeType.DHLG004, SkillType.Engineering)
				.Category(RecipeCategoryType.Legging)
				.Resref("dhlg004")
				.Level(35)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_imperfect", 3)
				.Component("ref_keromber", 2);

			// DLCL-004
			_builder.Create(RecipeType.DLCL004, SkillType.Engineering)
				.Category(RecipeCategoryType.Cloak)
				.Resref("dlcl004")
				.Level(38)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_imperfect", 5)
				.Component("ref_keromber", 3);

			// DLBE-004
			_builder.Create(RecipeType.DLBE004, SkillType.Engineering)
				.Category(RecipeCategoryType.Belt)
				.Resref("dlbe004")
				.Level(39)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_imperfect", 5)
				.Component("ref_keromber", 3);

			// DLRG-004
			_builder.Create(RecipeType.DLRG004, SkillType.Engineering)
				.Category(RecipeCategoryType.Ring)
				.Resref("dlrg004")
				.Level(31)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_imperfect", 1)
				.Component("ref_keromber", 1);

			// DLNK-004
			_builder.Create(RecipeType.DLNK004, SkillType.Engineering)
				.Category(RecipeCategoryType.Necklace)
				.Resref("dlnk004")
				.Level(32)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_imperfect", 2)
				.Component("ref_keromber", 1);

			// DLAR-004
			_builder.Create(RecipeType.DLAR004, SkillType.Engineering)
				.Category(RecipeCategoryType.Breastplate)
				.Resref("dlar004")
				.Level(37)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_imperfect", 4)
				.Component("ref_keromber", 2);

			// DLHL-004
			_builder.Create(RecipeType.DLHL004, SkillType.Engineering)
				.Category(RecipeCategoryType.Helmet)
				.Resref("dlhl004")
				.Level(34)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_imperfect", 3)
				.Component("ref_keromber", 2);

			// DLBR-004
			_builder.Create(RecipeType.DLBR004, SkillType.Engineering)
				.Category(RecipeCategoryType.Bracer)
				.Resref("dlbr004")
				.Level(33)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_imperfect", 2)
				.Component("ref_keromber", 1);

			// DLLG-004
			_builder.Create(RecipeType.DLLG004, SkillType.Engineering)
				.Category(RecipeCategoryType.Legging)
				.Resref("dllg004")
				.Level(35)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_imperfect", 3)
				.Component("ref_keromber", 2);

            // DSCL-004
            _builder.Create(RecipeType.DSCL004, SkillType.Engineering)
                .Category(RecipeCategoryType.Cloak)
                .Resref("dscl004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DECL-004
            _builder.Create(RecipeType.DECL004, SkillType.Engineering)
                .Category(RecipeCategoryType.Cloak)
                .Resref("decl004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DACL-004
            _builder.Create(RecipeType.DACL004, SkillType.Engineering)
                .Category(RecipeCategoryType.Cloak)
                .Resref("dacl004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DFCL-004
            _builder.Create(RecipeType.DFCL004, SkillType.Engineering)
                .Category(RecipeCategoryType.Cloak)
                .Resref("dfcl004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DSBE-004
            _builder.Create(RecipeType.DSBE004, SkillType.Engineering)
                .Category(RecipeCategoryType.Belt)
                .Resref("dsbe004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DEBE-004
            _builder.Create(RecipeType.DEBE004, SkillType.Engineering)
                .Category(RecipeCategoryType.Belt)
                .Resref("debe004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DABE-004
            _builder.Create(RecipeType.DABE004, SkillType.Engineering)
                .Category(RecipeCategoryType.Belt)
                .Resref("dabe004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DFBE-004
            _builder.Create(RecipeType.DFBE004, SkillType.Engineering)
                .Category(RecipeCategoryType.Belt)
                .Resref("dfbe004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DSRG-004
            _builder.Create(RecipeType.DSRG004, SkillType.Engineering)
                .Category(RecipeCategoryType.Ring)
                .Resref("dsrg004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DERG-004
            _builder.Create(RecipeType.DERG004, SkillType.Engineering)
                .Category(RecipeCategoryType.Ring)
                .Resref("derg004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DARG-004
            _builder.Create(RecipeType.DARG004, SkillType.Engineering)
                .Category(RecipeCategoryType.Ring)
                .Resref("darg004")
                .Level(40)
                .Quantity(2)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DFRG-004
            _builder.Create(RecipeType.DFRG004, SkillType.Engineering)
                .Category(RecipeCategoryType.Ring)
                .Resref("dfrg004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DSNK-004
            _builder.Create(RecipeType.DSNK004, SkillType.Engineering)
                .Category(RecipeCategoryType.Necklace)
                .Resref("dsnk004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DENK-004
            _builder.Create(RecipeType.DENK004, SkillType.Engineering)
                .Category(RecipeCategoryType.Necklace)
                .Resref("denk004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DANK-004
            _builder.Create(RecipeType.DANK004, SkillType.Engineering)
                .Category(RecipeCategoryType.Necklace)
                .Resref("dank004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DFNK-004
            _builder.Create(RecipeType.DFNK004, SkillType.Engineering)
                .Category(RecipeCategoryType.Necklace)
                .Resref("dfnk004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DSAR-004
            _builder.Create(RecipeType.DSAR004, SkillType.Engineering)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("dsar004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DEAR-004
            _builder.Create(RecipeType.DEAR004, SkillType.Engineering)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("dear004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DAAR-004
            _builder.Create(RecipeType.DAAR004, SkillType.Engineering)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("daar004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DFAR-004
            _builder.Create(RecipeType.DFAR004, SkillType.Engineering)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("dfar004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DSHL-004
            _builder.Create(RecipeType.DSHL004, SkillType.Engineering)
                .Category(RecipeCategoryType.Helmet)
                .Resref("dshl004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DEHL-004
            _builder.Create(RecipeType.DEHL004, SkillType.Engineering)
                .Category(RecipeCategoryType.Helmet)
                .Resref("dehl004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DAHL-004
            _builder.Create(RecipeType.DAHL004, SkillType.Engineering)
                .Category(RecipeCategoryType.Helmet)
                .Resref("dahl004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DFHL-004
            _builder.Create(RecipeType.DFHL004, SkillType.Engineering)
                .Category(RecipeCategoryType.Helmet)
                .Resref("dfhl004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DSBR-004
            _builder.Create(RecipeType.DSBR004, SkillType.Engineering)
                .Category(RecipeCategoryType.Bracer)
                .Resref("dsbr004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DEBR-004
            _builder.Create(RecipeType.DEBR004, SkillType.Engineering)
                .Category(RecipeCategoryType.Bracer)
                .Resref("debr004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DABR-004
            _builder.Create(RecipeType.DABR004, SkillType.Engineering)
                .Category(RecipeCategoryType.Bracer)
                .Resref("dabr004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DFBR-004
            _builder.Create(RecipeType.DFBR004, SkillType.Engineering)
                .Category(RecipeCategoryType.Bracer)
                .Resref("dfbr004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DSLG-004
            _builder.Create(RecipeType.DSLG004, SkillType.Engineering)
                .Category(RecipeCategoryType.Legging)
                .Resref("dslg004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DELG-004
            _builder.Create(RecipeType.DELG004, SkillType.Engineering)
                .Category(RecipeCategoryType.Legging)
                .Resref("delg004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DALG-004
            _builder.Create(RecipeType.DALG004, SkillType.Engineering)
                .Category(RecipeCategoryType.Legging)
                .Resref("dalg004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DFLG-004
            _builder.Create(RecipeType.DFLG004, SkillType.Engineering)
                .Category(RecipeCategoryType.Legging)
                .Resref("dflg004")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);
        }

        private void Tier5()
		{
            // DHCL-005
			_builder.Create(RecipeType.DHCL005, SkillType.Engineering)
				.Category(RecipeCategoryType.Cloak)
				.Resref("dhcl005")
				.Level(48)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_high", 5)
				.Component("ref_jasioclase", 3);

			// DHBE-005
			_builder.Create(RecipeType.DHBE005, SkillType.Engineering)
				.Category(RecipeCategoryType.Belt)
				.Resref("dhbe005")
				.Level(49)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_high", 5)
				.Component("ref_jasioclase", 3);

			// DHRG-005
			_builder.Create(RecipeType.DHRG005, SkillType.Engineering)
				.Category(RecipeCategoryType.Ring)
				.Resref("dhrg005")
				.Level(41)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_high", 1)
				.Component("ref_jasioclase", 1);

			// DHNK-005
			_builder.Create(RecipeType.DHNK005, SkillType.Engineering)
				.Category(RecipeCategoryType.Necklace)
				.Resref("dhnk005")
				.Level(42)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_high", 2)
				.Component("ref_jasioclase", 1);

			// DHAR-005
			_builder.Create(RecipeType.DHAR005, SkillType.Engineering)
				.Category(RecipeCategoryType.Breastplate)
				.Resref("dhar005")
				.Level(47)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_high", 4)
				.Component("ref_jasioclase", 2);

			// DHHL-005
			_builder.Create(RecipeType.DHHL005, SkillType.Engineering)
				.Category(RecipeCategoryType.Helmet)
				.Resref("dhhl005")
				.Level(44)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_high", 3)
				.Component("ref_jasioclase", 2);

			// DHBR-005
			_builder.Create(RecipeType.DHBR005, SkillType.Engineering)
				.Category(RecipeCategoryType.Bracer)
				.Resref("dhbr005")
				.Level(43)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_high", 2)
				.Component("ref_jasioclase", 1);

			// DHLG-005
			_builder.Create(RecipeType.DHLG005, SkillType.Engineering)
				.Category(RecipeCategoryType.Legging)
				.Resref("dhlg005")
				.Level(45)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_high", 3)
				.Component("ref_jasioclase", 2);

			// DLCL-005
			_builder.Create(RecipeType.DLCL005, SkillType.Engineering)
				.Category(RecipeCategoryType.Cloak)
				.Resref("dlcl005")
				.Level(48)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_high", 5)
				.Component("ref_jasioclase", 3);

			// DLBE-005
			_builder.Create(RecipeType.DLBE005, SkillType.Engineering)
				.Category(RecipeCategoryType.Belt)
				.Resref("dlbe005")
				.Level(49)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_high", 5)
				.Component("ref_jasioclase", 3);

			// DLRG-005
			_builder.Create(RecipeType.DLRG005, SkillType.Engineering)
				.Category(RecipeCategoryType.Ring)
				.Resref("dlrg005")
				.Level(41)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_high", 1)
				.Component("ref_jasioclase", 1);

			// DLNK-005
			_builder.Create(RecipeType.DLNK005, SkillType.Engineering)
				.Category(RecipeCategoryType.Necklace)
				.Resref("dlnk005")
				.Level(42)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_high", 2)
				.Component("ref_jasioclase", 1);

			// DLAR-005
			_builder.Create(RecipeType.DLAR005, SkillType.Engineering)
				.Category(RecipeCategoryType.Breastplate)
				.Resref("dlar005")
				.Level(47)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_high", 4)
				.Component("ref_jasioclase", 2);

			// DLHL-005
			_builder.Create(RecipeType.DLHL005, SkillType.Engineering)
				.Category(RecipeCategoryType.Helmet)
				.Resref("dlhl005")
				.Level(44)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_high", 3)
				.Component("ref_jasioclase", 2);

			// DLBR-005
			_builder.Create(RecipeType.DLBR005, SkillType.Engineering)
				.Category(RecipeCategoryType.Bracer)
				.Resref("dlbr005")
				.Level(43)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_high", 2)
				.Component("ref_jasioclase", 1);

			// DLLG-005
			_builder.Create(RecipeType.DLLG005, SkillType.Engineering)
				.Category(RecipeCategoryType.Legging)
				.Resref("dllg005")
				.Level(45)
				.Quantity(1)
				.RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Armor, 2)
				.Component("elec_high", 3)
				.Component("ref_jasioclase", 2);

            // DHCL-005 C
            _builder.Create(RecipeType.DHCL005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Cloak)
                .Resref("dhcl005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("ref_keromber", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_veldite", 5)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2);

            // DHBE-005 C
            _builder.Create(RecipeType.DHBE005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Belt)
                .Resref("dhbe005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("ref_keromber", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_veldite", 5)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2);

            // DHRG-005 C
            _builder.Create(RecipeType.DHRG005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Ring)
                .Resref("dhrg005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("ref_keromber", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_veldite", 5)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2);

            // DHNK-005 C
            _builder.Create(RecipeType.DHNK005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Necklace)
                .Resref("dhnk005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("ref_keromber", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_veldite", 5)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2);

            // DHAR-005 C
            _builder.Create(RecipeType.DHAR005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("dhar005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("ref_keromber", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_veldite", 5)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2);

            // DHHL-005 C
            _builder.Create(RecipeType.DHHL005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Helmet)
                .Resref("dhhl005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("ref_keromber", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_veldite", 5)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2);

            // DHBR-005 C
            _builder.Create(RecipeType.DHBR005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Bracer)
                .Resref("dhbr005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("ref_keromber", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_veldite", 5)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2);

            // DHLG-005 C
            _builder.Create(RecipeType.DHLG005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Legging)
                .Resref("dhlg005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("ref_keromber", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_veldite", 5)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2);

            // DLCL-005 C
            _builder.Create(RecipeType.DLCL005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Cloak)
                .Resref("dlcl005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("ref_keromber", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_veldite", 5)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2);

            // DLBE-005 C
            _builder.Create(RecipeType.DLBE005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Belt)
                .Resref("dlbe005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("ref_keromber", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_veldite", 5)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2);

            // DLRG-005 C
            _builder.Create(RecipeType.DLRG005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Ring)
                .Resref("dlrg005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("ref_keromber", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_veldite", 5)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2);

            // DLNK-005 C
            _builder.Create(RecipeType.DLNK005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Necklace)
                .Resref("dlnk005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("ref_keromber", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_veldite", 5)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2);

            // DLAR-005 C
            _builder.Create(RecipeType.DLAR005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("dlar005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("ref_keromber", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_veldite", 5)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2);

            // DLHL-005 C
            _builder.Create(RecipeType.DLHL005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Helmet)
                .Resref("dlhl005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("ref_keromber", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_veldite", 5)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2);

            // DLBR-005 C
            _builder.Create(RecipeType.DLBR005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Bracer)
                .Resref("dlbr005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("ref_keromber", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_veldite", 5)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2);

            // DLLG-005 C
            _builder.Create(RecipeType.DLLG005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Legging)
                .Resref("dllg005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("ref_keromber", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_veldite", 5)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2);

            // DSCL-005
            _builder.Create(RecipeType.DSCL005, SkillType.Engineering)
                .Category(RecipeCategoryType.Cloak)
                .Resref("dscl005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DECL-005
            _builder.Create(RecipeType.DECL005, SkillType.Engineering)
                .Category(RecipeCategoryType.Cloak)
                .Resref("decl005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DACL-005
            _builder.Create(RecipeType.DACL005, SkillType.Engineering)
                .Category(RecipeCategoryType.Cloak)
                .Resref("dacl005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DFCL-005
            _builder.Create(RecipeType.DFCL005, SkillType.Engineering)
                .Category(RecipeCategoryType.Cloak)
                .Resref("dfcl005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);
            
            // DSBE-005
            _builder.Create(RecipeType.DSBE005, SkillType.Engineering)
                .Category(RecipeCategoryType.Belt)
                .Resref("dsbe005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DEBE-005
            _builder.Create(RecipeType.DEBE005, SkillType.Engineering)
                .Category(RecipeCategoryType.Belt)
                .Resref("debe005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DABE-005
            _builder.Create(RecipeType.DABE005, SkillType.Engineering)
                .Category(RecipeCategoryType.Belt)
                .Resref("dabe005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DFBE-005
            _builder.Create(RecipeType.DFBE005, SkillType.Engineering)
                .Category(RecipeCategoryType.Belt)
                .Resref("dfbe005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DSRG-005
            _builder.Create(RecipeType.DSRG005, SkillType.Engineering)
                .Category(RecipeCategoryType.Ring)
                .Resref("dsrg005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DERG-005
            _builder.Create(RecipeType.DERG005, SkillType.Engineering)
                .Category(RecipeCategoryType.Ring)
                .Resref("derg005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DARG-005
            _builder.Create(RecipeType.DARG005, SkillType.Engineering)
                .Category(RecipeCategoryType.Ring)
                .Resref("darg005")
                .Level(50)
                .Quantity(2)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DFRG-005
            _builder.Create(RecipeType.DFRG005, SkillType.Engineering)
                .Category(RecipeCategoryType.Ring)
                .Resref("dfrg005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DSNK-005
            _builder.Create(RecipeType.DSNK005, SkillType.Engineering)
                .Category(RecipeCategoryType.Necklace)
                .Resref("dsnk005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DENK-005
            _builder.Create(RecipeType.DENK005, SkillType.Engineering)
                .Category(RecipeCategoryType.Necklace)
                .Resref("denk005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DANK-005
            _builder.Create(RecipeType.DANK005, SkillType.Engineering)
                .Category(RecipeCategoryType.Necklace)
                .Resref("dank005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DFNK-005
            _builder.Create(RecipeType.DFNK005, SkillType.Engineering)
                .Category(RecipeCategoryType.Necklace)
                .Resref("dfnk005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DSAR-005
            _builder.Create(RecipeType.DSAR005, SkillType.Engineering)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("dsar005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DEAR-005
            _builder.Create(RecipeType.DEAR005, SkillType.Engineering)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("dear005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);
            
            // DAAR-005
            _builder.Create(RecipeType.DAAR005, SkillType.Engineering)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("daar005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DFAR-005
            _builder.Create(RecipeType.DFAR005, SkillType.Engineering)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("dfar005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DSHL-005
            _builder.Create(RecipeType.DSHL005, SkillType.Engineering)
                .Category(RecipeCategoryType.Helmet)
                .Resref("dshl005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DEHL-005
            _builder.Create(RecipeType.DEHL005, SkillType.Engineering)
                .Category(RecipeCategoryType.Helmet)
                .Resref("dehl005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DAHL-005
            _builder.Create(RecipeType.DAHL005, SkillType.Engineering)
                .Category(RecipeCategoryType.Helmet)
                .Resref("dahl005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DFHL-005
            _builder.Create(RecipeType.DFHL005, SkillType.Engineering)
                .Category(RecipeCategoryType.Helmet)
                .Resref("dfhl005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DSBR-005
            _builder.Create(RecipeType.DSBR005, SkillType.Engineering)
                .Category(RecipeCategoryType.Bracer)
                .Resref("dsbr005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DEBR-005
            _builder.Create(RecipeType.DEBR005, SkillType.Engineering)
                .Category(RecipeCategoryType.Bracer)
                .Resref("debr005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DABR-005
            _builder.Create(RecipeType.DABR005, SkillType.Engineering)
                .Category(RecipeCategoryType.Bracer)
                .Resref("dabr005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DFBR-005
            _builder.Create(RecipeType.DFBR005, SkillType.Engineering)
                .Category(RecipeCategoryType.Bracer)
                .Resref("dfbr005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DSLG-005
            _builder.Create(RecipeType.DSLG005, SkillType.Engineering)
                .Category(RecipeCategoryType.Legging)
                .Resref("dslg005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DELG-005
            _builder.Create(RecipeType.DELG005, SkillType.Engineering)
                .Category(RecipeCategoryType.Legging)
                .Resref("delg005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DALG-005
            _builder.Create(RecipeType.DALG005, SkillType.Engineering)
                .Category(RecipeCategoryType.Legging)
                .Resref("dalg005")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // DFLG-005
            _builder.Create(RecipeType.DFLG005, SkillType.Engineering)
                .Category(RecipeCategoryType.Legging)
                .Resref("dflg005")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);
        }
    }
}
