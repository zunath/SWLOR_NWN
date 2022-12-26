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
                .Component("elec_high", 20)
                .Component("ref_jasioclase", 20)
                .Component("chiro_shard", 1);

            // DHBE-005 C
            _builder.Create(RecipeType.DHBE005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Belt)
                .Resref("dhbe005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("elec_high", 20)
                .Component("ref_jasioclase", 20)
                .Component("chiro_shard", 1);

            // DHRG-005 C
            _builder.Create(RecipeType.DHRG005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Ring)
                .Resref("dhrg005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("elec_high", 20)
                .Component("ref_jasioclase", 20)
                .Component("chiro_shard", 1);

            // DHNK-005 C
            _builder.Create(RecipeType.DHNK005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Necklace)
                .Resref("dhnk005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("elec_high", 20)
                .Component("ref_jasioclase", 20)
                .Component("chiro_shard", 1);

            // DHAR-005 C
            _builder.Create(RecipeType.DHAR005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("dhar005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("elec_high", 20)
                .Component("ref_jasioclase", 20)
                .Component("chiro_shard", 1);

            // DHHL-005 C
            _builder.Create(RecipeType.DHHL005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Helmet)
                .Resref("dhhl005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("elec_high", 20)
                .Component("ref_jasioclase", 20)
                .Component("chiro_shard", 1);

            // DHBR-005 C
            _builder.Create(RecipeType.DHBR005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Bracer)
                .Resref("dhbr005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("elec_high", 20)
                .Component("ref_jasioclase", 20)
                .Component("chiro_shard", 1);

            // DHLG-005 C
            _builder.Create(RecipeType.DHLG005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Legging)
                .Resref("dhlg005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("elec_high", 20)
                .Component("ref_jasioclase", 20)
                .Component("chiro_shard", 1);

            // DLCL-005 C
            _builder.Create(RecipeType.DLCL005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Cloak)
                .Resref("dlcl005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("elec_high", 20)
                .Component("ref_jasioclase", 20)
                .Component("chiro_shard", 1);

            // DLBE-005 C
            _builder.Create(RecipeType.DLBE005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Belt)
                .Resref("dlbe005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("elec_high", 20)
                .Component("ref_jasioclase", 20)
                .Component("chiro_shard", 1);

            // DLRG-005 C
            _builder.Create(RecipeType.DLRG005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Ring)
                .Resref("dlrg005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("elec_high", 20)
                .Component("ref_jasioclase", 20)
                .Component("chiro_shard", 1);

            // DLNK-005 C
            _builder.Create(RecipeType.DLNK005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Necklace)
                .Resref("dlnk005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("elec_high", 20)
                .Component("ref_jasioclase", 20)
                .Component("chiro_shard", 1);

            // DLAR-005 C
            _builder.Create(RecipeType.DLAR005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("dlar005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("elec_high", 20)
                .Component("ref_jasioclase", 20)
                .Component("chiro_shard", 1);

            // DLHL-005 C
            _builder.Create(RecipeType.DLHL005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Helmet)
                .Resref("dlhl005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("elec_high", 20)
                .Component("ref_jasioclase", 20)
                .Component("chiro_shard", 1);

            // DLBR-005 C
            _builder.Create(RecipeType.DLBR005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Bracer)
                .Resref("dlbr005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("elec_high", 20)
                .Component("ref_jasioclase", 20)
                .Component("chiro_shard", 1);

            // DLLG-005 C
            _builder.Create(RecipeType.DLLG005C, SkillType.Engineering)
                .Category(RecipeCategoryType.Legging)
                .Resref("dllg005c")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("elec_high", 20)
                .Component("ref_jasioclase", 20)
                .Component("chiro_shard", 1);


        }
    }
}
