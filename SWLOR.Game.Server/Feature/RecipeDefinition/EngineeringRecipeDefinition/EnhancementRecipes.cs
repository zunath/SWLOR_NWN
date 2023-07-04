using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    public class EnhancementRecipes: IRecipeListDefinition
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
			// Armor Enhancement - Control - Smithery I
			_builder.Create(RecipeType.ArmorEnhancementControlSmithery1, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_con_smth1")
				.Level(1)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 1)
				.Component("ref_veldite", 1)
				.Component("elec_ruined", 1)
				;

			// Armor Enhancement - Control - Engineering I
			_builder.Create(RecipeType.ArmorEnhancementControlEngineering1, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_con_eng1")
				.Level(2)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 2)
				.Component("ref_veldite", 1)
				.Component("elec_ruined", 1)
				;

			// Armor Enhancement - Control - Fabrication I
			_builder.Create(RecipeType.ArmorEnhancementControlFabrication1, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_con_fab1")
				.Level(3)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 2)
				.Component("ref_veldite", 1)
				.Component("elec_ruined", 1)
				;

			// Armor Enhancement - Control - Agriculture I
			_builder.Create(RecipeType.ArmorEnhancementControlAgriculture1, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_con_agr1")
				.Level(4)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 3)
				.Component("ref_veldite", 2)
				.Component("elec_ruined", 1)
				;

			// Armor Enhancement - Craftsmanship - Smithery I
			_builder.Create(RecipeType.ArmorEnhancementCraftsmanshipSmithery1, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_cft_smth1")
				.Level(1)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 1)
				.Component("ref_veldite", 1)
				.Component("elec_ruined", 1)
				;

			// Armor Enhancement - Craftsmanship - Engineering I
			_builder.Create(RecipeType.ArmorEnhancementCraftsmanshipEngineering1, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_cft_eng1")
				.Level(2)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 2)
				.Component("ref_veldite", 1)
				.Component("elec_ruined", 1)
				;

			// Armor Enhancement - Craftsmanship - Fabrication I
			_builder.Create(RecipeType.ArmorEnhancementCraftsmanshipFabrication1, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_cft_fab1")
				.Level(3)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 2)
				.Component("ref_veldite", 1)
				.Component("elec_ruined", 1)
				;

			// Armor Enhancement - Craftsmanship - Agriculture I
			_builder.Create(RecipeType.ArmorEnhancementCraftsmanshipAgriculture1, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_cft_agr1")
				.Level(4)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 3)
				.Component("ref_veldite", 2)
				.Component("elec_ruined", 1)
				;

			// Armor Enhancement - Defense - Physical I
			_builder.Create(RecipeType.ArmorEnhancementDefensePhysical1, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_phy1")
				.Level(5)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 3)
				.Component("ref_veldite", 2)
				.Component("elec_ruined", 2)
				;

			// Armor Enhancement - Defense - Force I
			_builder.Create(RecipeType.ArmorEnhancementDefenseForce1, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_for1")
				.Level(6)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 4)
				.Component("ref_veldite", 2)
				.Component("elec_ruined", 2)
				;

			// Armor Enhancement - Defense - Poison I
			_builder.Create(RecipeType.ArmorEnhancementDefensePoison1, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_psn1")
				.Level(7)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 4)
				.Component("ref_veldite", 2)
				.Component("elec_ruined", 2)
				;

			// Armor Enhancement - Defense - Fire I
			_builder.Create(RecipeType.ArmorEnhancementDefenseFire1, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_fir1")
				.Level(8)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 5)
				.Component("ref_veldite", 3)
				.Component("elec_ruined", 2)
				;

			// Armor Enhancement - Defense - Ice I
			_builder.Create(RecipeType.ArmorEnhancementDefenseIce1, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_ice1")
				.Level(9)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 5)
				.Component("ref_veldite", 3)
				.Component("elec_ruined", 2)
				;

			// Armor Enhancement - Defense - Electrical I
			_builder.Create(RecipeType.ArmorEnhancementDefenseElectrical1, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_elec1")
				.Level(5)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 3)
				.Component("ref_veldite", 2)
				.Component("elec_ruined", 2)
				;

			// Armor Enhancement - Evasion I
			_builder.Create(RecipeType.ArmorEnhancementEvasion1, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_eva1")
				.Level(8)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 5)
				.Component("ref_veldite", 3)
				.Component("elec_ruined", 2)
				;

            // Armor Enhancement - Attack I
            _builder.Create(RecipeType.ArmorEnhancementAttack1, SkillType.Engineering)
                .Category(RecipeCategoryType.ArmorEnhancement)
                .Resref("aen_atk1")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 1)
                .Component("ref_tilarium", 5)
                .Component("ref_veldite", 3)
                .Component("elec_ruined", 2)
                ;

            // Armor Enhancement - Force Attack I
            _builder.Create(RecipeType.ArmorEnhancementForceAttack1, SkillType.Engineering)
                .Category(RecipeCategoryType.ArmorEnhancement)
                .Resref("aen_frcatk1")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 1)
                .Component("ref_tilarium", 5)
                .Component("ref_veldite", 3)
                .Component("elec_ruined", 2)
                ;

            // Armor Enhancement - FP I
            _builder.Create(RecipeType.ArmorEnhancementFP1, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_fp1")
				.Level(5)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 3)
				.Component("ref_veldite", 2)
				.Component("elec_ruined", 2)
				;

			// Armor Enhancement - HP I
			_builder.Create(RecipeType.ArmorEnhancementHP1, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_hp1")
				.Level(6)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 4)
				.Component("ref_veldite", 2)
				.Component("elec_ruined", 2)
				;

			// Armor Enhancement - Recast Reduction I
			_builder.Create(RecipeType.ArmorEnhancementRecastReduction1, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_recast1")
				.Level(9)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 5)
				.Component("ref_veldite", 3)
				.Component("elec_ruined", 2)
				;

			// Armor Enhancement - STM I
			_builder.Create(RecipeType.ArmorEnhancementSTM1, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_stm1")
				.Level(7)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 4)
				.Component("ref_veldite", 2)
				.Component("elec_ruined", 2)
				;

			// Weapon Enhancement - Attack Bonus I
			_builder.Create(RecipeType.WeaponEnhancementAttackBonus1, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_atk1")
				.Level(8)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 5)
				.Component("ref_veldite", 3)
				.Component("elec_ruined", 2)
				;

            // Weapon Enhancement - Force Attack I
            _builder.Create(RecipeType.WeaponEnhancementForceAttack1, SkillType.Engineering)
                .Category(RecipeCategoryType.WeaponEnhancement)
                .Resref("wen_frcatk1")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 1)
                .Component("ref_tilarium", 5)
                .Component("ref_veldite", 3)
                .Component("elec_ruined", 2)
                ;

			// Weapon Enhancement - Control - Smithery I
			_builder.Create(RecipeType.WeaponEnhancementControlSmithery1, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_con_smth1")
				.Level(4)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 3)
				.Component("ref_veldite", 2)
				.Component("elec_ruined", 1)
				;

			// Weapon Enhancement - Control - Engineering I
			_builder.Create(RecipeType.WeaponEnhancementControlEngineering1, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_con_eng1")
				.Level(3)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 2)
				.Component("ref_veldite", 1)
				.Component("elec_ruined", 1)
				;

			// Weapon Enhancement - Control - Fabrication I
			_builder.Create(RecipeType.WeaponEnhancementControlFabrication1, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_con_fab1")
				.Level(2)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 2)
				.Component("ref_veldite", 1)
				.Component("elec_ruined", 1)
				;

			// Weapon Enhancement - Control - Agriculture I
			_builder.Create(RecipeType.WeaponEnhancementControlAgriculture1, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_con_agr1")
				.Level(1)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 1)
				.Component("ref_veldite", 1)
				.Component("elec_ruined", 1)
				;

			// Weapon Enhancement - Craftsmanship - Smithery I
			_builder.Create(RecipeType.WeaponEnhancementCraftsmanshipSmithery1, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_cft_smth1")
				.Level(3)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 2)
				.Component("ref_veldite", 1)
				.Component("elec_ruined", 1)
				;

			// Weapon Enhancement - Craftsmanship - Engineering I
			_builder.Create(RecipeType.WeaponEnhancementCraftsmanshipEngineering1, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_cft_eng1")
				.Level(4)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 3)
				.Component("ref_veldite", 2)
				.Component("elec_ruined", 1)
				;

			// Weapon Enhancement - Craftsmanship - Fabrication I
			_builder.Create(RecipeType.WeaponEnhancementCraftsmanshipFabrication1, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_cft_fab1")
				.Level(1)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 1)
				.Component("ref_veldite", 1)
				.Component("elec_ruined", 1)
				;

			// Weapon Enhancement - Craftsmanship - Agriculture I
			_builder.Create(RecipeType.WeaponEnhancementCraftsmanshipAgriculture1, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_cft_agr1")
				.Level(2)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 2)
				.Component("ref_veldite", 1)
				.Component("elec_ruined", 1)
				;

			// Weapon Enhancement - DMG - Physical I
			_builder.Create(RecipeType.WeaponEnhancementDMGPhysical1, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_dmg_phy1")
				.Level(9)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 5)
				.Component("ref_veldite", 3)
				.Component("elec_ruined", 2)
				;

			// Weapon Enhancement - DMG - Force I
			_builder.Create(RecipeType.WeaponEnhancementDMGForce1, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_dmg_for1")
				.Level(8)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 5)
				.Component("ref_veldite", 3)
				.Component("elec_ruined", 2)
				;

            // Weapon Enhancement - Evasion I
			_builder.Create(RecipeType.WeaponEnhancementEvasion1, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_eva1")
				.Level(8)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 5)
				.Component("ref_veldite", 3)
				.Component("elec_ruined", 2)
				;

			// Weapon Enhancement - FP I
			_builder.Create(RecipeType.WeaponEnhancementFP1, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_fp1")
				.Level(8)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 5)
				.Component("ref_veldite", 3)
				.Component("elec_ruined", 2)
				;

			// Weapon Enhancement - HP I
			_builder.Create(RecipeType.WeaponEnhancementHP1, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_hp1")
				.Level(9)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 5)
				.Component("ref_veldite", 3)
				.Component("elec_ruined", 2)
				;

			// Weapon Enhancement - STM I
			_builder.Create(RecipeType.WeaponEnhancementSTM1, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_stm1")
				.Level(7)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 4)
				.Component("ref_veldite", 2)
				.Component("elec_ruined", 2)
				;

            // Weapon Enhancement - Accuracy I
            _builder.Create(RecipeType.WeaponEnhancementAccuracy1, SkillType.Engineering)
                .Category(RecipeCategoryType.WeaponEnhancement)
                .Resref("wen_acc1")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 1)
                .Component("ref_tilarium", 5)
                .Component("ref_veldite", 3)
                .Component("elec_ruined", 2)
                ;

            // Structure Enhancement - Structure Bonus I
            _builder.Create(RecipeType.StructureEnhancementStructureBonus1, SkillType.Engineering)
				.Category(RecipeCategoryType.StructureEnhancement)
				.Resref("sten_sb1")
				.Level(9)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 5)
				.Component("ref_veldite", 3)
				.Component("elec_ruined", 2)
				;

			// Starship Enhancement - Accuracy I
			_builder.Create(RecipeType.StarshipEnhancementAccuracy1, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_acc1")
				.Level(2)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 2)
				.Component("ref_veldite", 1)
				.Component("elec_ruined", 1)
				;

			// Starship Enhancement - Armor I
			_builder.Create(RecipeType.StarshipEnhancementArmor1, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_armor1")
				.Level(3)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 2)
				.Component("ref_veldite", 1)
				.Component("elec_ruined", 1)
				;

			// Starship Enhancement - Capacitor I
			_builder.Create(RecipeType.StarshipEnhancementCapacitor1, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_cap1")
				.Level(1)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 1)
				.Component("ref_veldite", 1)
				.Component("elec_ruined", 1)
				;

			// Starship Enhancement - EM Damage I
			_builder.Create(RecipeType.StarshipEnhancementEMDamage1, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_emdmg1")
				.Level(5)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 3)
				.Component("ref_veldite", 2)
				.Component("elec_ruined", 1)
				;

			// Starship Enhancement - EM Defense I
			_builder.Create(RecipeType.StarshipEnhancementEMDefense1, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_emdef1")
				.Level(4)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 3)
				.Component("ref_veldite", 2)
				.Component("elec_ruined", 1)
				;

			// Starship Enhancement - Evasion I
			_builder.Create(RecipeType.StarshipEnhancementEvasion1, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_eva1")
				.Level(6)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 4)
				.Component("ref_veldite", 2)
				.Component("elec_ruined", 2)
				;

			// Starship Enhancement - Explosive Damage I
			_builder.Create(RecipeType.StarshipEnhancementExplosiveDamage1, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_expdmg1")
				.Level(8)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 5)
				.Component("ref_veldite", 3)
				.Component("elec_ruined", 2)
				;

			// Starship Enhancement - Explosive Defense I
			_builder.Create(RecipeType.StarshipEnhancementDefense1, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_expdef1")
				.Level(7)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 4)
				.Component("ref_veldite", 2)
				.Component("elec_ruined", 2)
				;

			// Starship Enhancement - Shield I
			_builder.Create(RecipeType.StarshipEnhancementShield1, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_shield1")
				.Level(6)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 4)
				.Component("ref_veldite", 2)
				.Component("elec_ruined", 2)
				;

			// Starship Enhancement - Shield Recharge Rate I
			_builder.Create(RecipeType.StarshipEnhancementShieldRechargeRate1, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_shrech1")
				.Level(9)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 5)
				.Component("ref_veldite", 3)
				.Component("elec_ruined", 2)
				;

			// Starship Enhancement - Thermal Damage I
			_builder.Create(RecipeType.StarshipEnhancementThermalDamage1, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_thermdmg1")
				.Level(7)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 4)
				.Component("ref_veldite", 2)
				.Component("elec_ruined", 2)
				;

			// Starship Enhancement - Thermal Defense I
			_builder.Create(RecipeType.StarshipEnhancementThermalDefense1, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_thermdef1")
				.Level(8)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 5)
				.Component("ref_veldite", 3)
				.Component("elec_ruined", 2)
				;

			// Module Enhancement - Module Bonus I
			_builder.Create(RecipeType.ModuleEnhancementModuleBonus1, SkillType.Engineering)
				.Category(RecipeCategoryType.ModuleEnhancement)
				.Resref("men_mod1")
				.Level(9)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 1)
				.Component("ref_tilarium", 5)
				.Component("ref_veldite", 3)
				.Component("elec_ruined", 2)
				;


		}

		private void Tier2()
        {
			// Armor Enhancement - Control - Smithery II
			_builder.Create(RecipeType.ArmorEnhancementControlSmithery2, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_con_smth2")
				.Level(11)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 1)
				.Component("ref_scordspar", 1)
				.Component("elec_flawed", 1)
				;

			// Armor Enhancement - Control - Engineering II
			_builder.Create(RecipeType.ArmorEnhancementControlEngineering2, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_con_eng2")
				.Level(12)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 2)
				.Component("ref_scordspar", 1)
				.Component("elec_flawed", 1)
				;

			// Armor Enhancement - Control - Fabrication II
			_builder.Create(RecipeType.ArmorEnhancementControlFabrication2, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_con_fab2")
				.Level(13)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 2)
				.Component("ref_scordspar", 1)
				.Component("elec_flawed", 1)
				;

			// Armor Enhancement - Control - Agriculture II
			_builder.Create(RecipeType.ArmorEnhancementControlAgriculture2, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_con_agr2")
				.Level(14)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 3)
				.Component("ref_scordspar", 2)
				.Component("elec_flawed", 1)
				;

			// Armor Enhancement - Craftsmanship - Smithery II
			_builder.Create(RecipeType.ArmorEnhancementCraftsmanshipSmithery2, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_cft_smth2")
				.Level(11)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 1)
				.Component("ref_scordspar", 1)
				.Component("elec_flawed", 1)
				;

			// Armor Enhancement - Craftsmanship - Engineering II
			_builder.Create(RecipeType.ArmorEnhancementCraftsmanshipEngineering2, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_cft_eng2")
				.Level(12)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 2)
				.Component("ref_scordspar", 1)
				.Component("elec_flawed", 1)
				;

			// Armor Enhancement - Craftsmanship - Fabrication II
			_builder.Create(RecipeType.ArmorEnhancementCraftsmanshipFabrication2, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_cft_fab2")
				.Level(13)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 2)
				.Component("ref_scordspar", 1)
				.Component("elec_flawed", 1)
				;

			// Armor Enhancement - Craftsmanship - Agriculture II
			_builder.Create(RecipeType.ArmorEnhancementCraftsmanshipAgriculture2, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_cft_agr2")
				.Level(14)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 3)
				.Component("ref_scordspar", 2)
				.Component("elec_flawed", 1)
				;

			// Armor Enhancement - Defense - Physical II
			_builder.Create(RecipeType.ArmorEnhancementDefensePhysical2, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_phy2")
				.Level(15)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 3)
				.Component("ref_scordspar", 2)
				.Component("elec_flawed", 2)
				;

			// Armor Enhancement - Defense - Force II
			_builder.Create(RecipeType.ArmorEnhancementDefenseForce2, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_for2")
				.Level(16)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 4)
				.Component("ref_scordspar", 2)
				.Component("elec_flawed", 2)
				;

			// Armor Enhancement - Defense - Poison II
			_builder.Create(RecipeType.ArmorEnhancementDefensePoison2, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_psn2")
				.Level(17)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 4)
				.Component("ref_scordspar", 2)
				.Component("elec_flawed", 2)
				;

			// Armor Enhancement - Defense - Fire II
			_builder.Create(RecipeType.ArmorEnhancementDefenseFire2, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_fir2")
				.Level(18)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 5)
				.Component("ref_scordspar", 3)
				.Component("elec_flawed", 2)
				;

			// Armor Enhancement - Defense - Ice II
			_builder.Create(RecipeType.ArmorEnhancementDefenseIce2, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_ice2")
				.Level(19)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 5)
				.Component("ref_scordspar", 3)
				.Component("elec_flawed", 2)
				;

			// Armor Enhancement - Defense - Electrical II
			_builder.Create(RecipeType.ArmorEnhancementDefenseElectrical2, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_elec2")
				.Level(15)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 3)
				.Component("ref_scordspar", 2)
				.Component("elec_flawed", 2)
				;

			// Armor Enhancement - Evasion II
			_builder.Create(RecipeType.ArmorEnhancementEvasion2, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_eva2")
				.Level(18)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 5)
				.Component("ref_scordspar", 3)
				.Component("elec_flawed", 2)
				;

            // Armor Enhancement - Attack II
            _builder.Create(RecipeType.ArmorEnhancementAttack2, SkillType.Engineering)
                .Category(RecipeCategoryType.ArmorEnhancement)
                .Resref("aen_atk2")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 2)
                .Component("ref_currian", 5)
                .Component("ref_scordspar", 3)
                .Component("elec_flawed", 2)
                ;

            // Armor Enhancement - Force Attack II
            _builder.Create(RecipeType.ArmorEnhancementForceAttack2, SkillType.Engineering)
                .Category(RecipeCategoryType.ArmorEnhancement)
                .Resref("aen_frcatk2")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 2)
                .Component("ref_currian", 5)
                .Component("ref_scordspar", 3)
                .Component("elec_flawed", 2)
                ;

            // Armor Enhancement - FP II
            _builder.Create(RecipeType.ArmorEnhancementFP2, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_fp2")
				.Level(15)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 3)
				.Component("ref_scordspar", 2)
				.Component("elec_flawed", 2)
				;

			// Armor Enhancement - HP II
			_builder.Create(RecipeType.ArmorEnhancementHP2, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_hp2")
				.Level(16)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 4)
				.Component("ref_scordspar", 2)
				.Component("elec_flawed", 2)
				;

			// Armor Enhancement - Recast Reduction II
			_builder.Create(RecipeType.ArmorEnhancementRecastReduction2, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_recast2")
				.Level(19)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 5)
				.Component("ref_scordspar", 3)
				.Component("elec_flawed", 2)
				;

			// Armor Enhancement - STM II
			_builder.Create(RecipeType.ArmorEnhancementSTM2, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_stm2")
				.Level(17)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 4)
				.Component("ref_scordspar", 2)
				.Component("elec_flawed", 2)
				;

			// Weapon Enhancement - Attack Bonus II
			_builder.Create(RecipeType.WeaponEnhancementAttackBonus2, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_atk2")
				.Level(18)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 5)
				.Component("ref_scordspar", 3)
				.Component("elec_flawed", 2)
				;

            // Weapon Enhancement - Force Attack II
            _builder.Create(RecipeType.WeaponEnhancementForceAttack2, SkillType.Engineering)
                .Category(RecipeCategoryType.WeaponEnhancement)
                .Resref("wen_frcatk2")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 2)
                .Component("ref_currian", 5)
                .Component("ref_scordspar", 3)
                .Component("elec_flawed", 2)
                ;

            // Weapon Enhancement - Control - Smithery II
            _builder.Create(RecipeType.WeaponEnhancementControlSmithery2, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_con_smth2")
				.Level(14)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 3)
				.Component("ref_scordspar", 2)
				.Component("elec_flawed", 1)
				;

			// Weapon Enhancement - Control - Engineering II
			_builder.Create(RecipeType.WeaponEnhancementControlEngineering2, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_con_eng2")
				.Level(13)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 2)
				.Component("ref_scordspar", 1)
				.Component("elec_flawed", 1)
				;

			// Weapon Enhancement - Control - Fabrication II
			_builder.Create(RecipeType.WeaponEnhancementControlFabrication2, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_con_fab2")
				.Level(12)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 2)
				.Component("ref_scordspar", 1)
				.Component("elec_flawed", 1)
				;

			// Weapon Enhancement - Control - Agriculture II
			_builder.Create(RecipeType.WeaponEnhancementControlAgriculture2, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_con_agr2")
				.Level(11)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 1)
				.Component("ref_scordspar", 1)
				.Component("elec_flawed", 1)
				;

			// Weapon Enhancement - Craftsmanship - Smithery II
			_builder.Create(RecipeType.WeaponEnhancementCraftsmanshipSmithery2, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_cft_smth2")
				.Level(13)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 2)
				.Component("ref_scordspar", 1)
				.Component("elec_flawed", 1)
				;

			// Weapon Enhancement - Craftsmanship - Engineering II
			_builder.Create(RecipeType.WeaponEnhancementCraftsmanshipEngineering2, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_cft_eng2")
				.Level(14)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 3)
				.Component("ref_scordspar", 2)
				.Component("elec_flawed", 1)
				;

			// Weapon Enhancement - Craftsmanship - Fabrication II
			_builder.Create(RecipeType.WeaponEnhancementCraftsmanshipFabrication2, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_cft_fab2")
				.Level(11)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 1)
				.Component("ref_scordspar", 1)
				.Component("elec_flawed", 1)
				;

			// Weapon Enhancement - Craftsmanship - Agriculture II
			_builder.Create(RecipeType.WeaponEnhancementCraftsmanshipAgriculture2, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_cft_agr2")
				.Level(12)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 2)
				.Component("ref_scordspar", 1)
				.Component("elec_flawed", 1)
				;
			
			// Weapon Enhancement - DMG - Poison I
			_builder.Create(RecipeType.WeaponEnhancementDMGPoison1, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_dmg_psn1")
				.Level(17)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 4)
				.Component("ref_scordspar", 2)
				.Component("elec_flawed", 2)
				;

			// Weapon Enhancement - DMG - Fire I
			_builder.Create(RecipeType.WeaponEnhancementDMGFire1, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_dmg_fir1")
				.Level(16)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 4)
				.Component("ref_scordspar", 2)
				.Component("elec_flawed", 2)
				;

			// Weapon Enhancement - DMG - Ice I
			_builder.Create(RecipeType.WeaponEnhancementDMGIce1, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_dmg_ice1")
				.Level(15)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 3)
				.Component("ref_scordspar", 2)
				.Component("elec_flawed", 2)
				;

			// Weapon Enhancement - DMG - Electrical I
			_builder.Create(RecipeType.WeaponEnhancementElectrical1, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_dmg_elec1")
				.Level(16)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 4)
				.Component("ref_scordspar", 2)
				.Component("elec_flawed", 2)
				;

			// Weapon Enhancement - Evasion II
			_builder.Create(RecipeType.WeaponEnhancementEvasion2, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_eva2")
				.Level(18)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 5)
				.Component("ref_scordspar", 3)
				.Component("elec_flawed", 2)
				;

			// Weapon Enhancement - FP II
			_builder.Create(RecipeType.WeaponEnhancementFP2, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_fp2")
				.Level(18)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 5)
				.Component("ref_scordspar", 3)
				.Component("elec_flawed", 2)
				;

			// Weapon Enhancement - HP II
			_builder.Create(RecipeType.WeaponEnhancementHP2, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_hp2")
				.Level(19)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 5)
				.Component("ref_scordspar", 3)
				.Component("elec_flawed", 2)
				;

			// Weapon Enhancement - STM II
			_builder.Create(RecipeType.WeaponEnhancementSTM2, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_stm2")
				.Level(17)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 4)
				.Component("ref_scordspar", 2)
				.Component("elec_flawed", 2)
				;

            // Weapon Enhancement - Accuracy II
            _builder.Create(RecipeType.WeaponEnhancementAccuracy2, SkillType.Engineering)
                .Category(RecipeCategoryType.WeaponEnhancement)
                .Resref("wen_acc2")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 2)
                .Component("ref_currian", 5)
                .Component("ref_scordspar", 3)
                .Component("elec_flawed", 2)
                ;

            // Structure Enhancement - Structure Bonus II
            _builder.Create(RecipeType.StructureEnhancementStructureBonus2, SkillType.Engineering)
				.Category(RecipeCategoryType.StructureEnhancement)
				.Resref("sten_sb2")
				.Level(19)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 5)
				.Component("ref_scordspar", 3)
				.Component("elec_flawed", 2)
				;

			// Starship Enhancement - Accuracy II
			_builder.Create(RecipeType.StarshipEnhancementAccuracy2, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_acc2")
				.Level(12)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 2)
				.Component("ref_scordspar", 1)
				.Component("elec_flawed", 1)
				;

			// Starship Enhancement - Armor II
			_builder.Create(RecipeType.StarshipEnhancementArmor2, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_armor2")
				.Level(13)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 2)
				.Component("ref_scordspar", 1)
				.Component("elec_flawed", 1)
				;

			// Starship Enhancement - Capacitor II
			_builder.Create(RecipeType.StarshipEnhancementCapacitor2, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_cap2")
				.Level(11)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 1)
				.Component("ref_scordspar", 1)
				.Component("elec_flawed", 1)
				;

			// Starship Enhancement - EM Damage II
			_builder.Create(RecipeType.StarshipEnhancementEMDamage2, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_emdmg2")
				.Level(15)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 3)
				.Component("ref_scordspar", 2)
				.Component("elec_flawed", 1)
				;

			// Starship Enhancement - EM Defense II
			_builder.Create(RecipeType.StarshipEnhancementEMDefense2, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_emdef2")
				.Level(14)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 3)
				.Component("ref_scordspar", 2)
				.Component("elec_flawed", 1)
				;

			// Starship Enhancement - Evasion II
			_builder.Create(RecipeType.StarshipEnhancementEvasion2, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_eva2")
				.Level(16)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 4)
				.Component("ref_scordspar", 2)
				.Component("elec_flawed", 2)
				;

			// Starship Enhancement - Explosive Damage II
			_builder.Create(RecipeType.StarshipEnhancementExplosiveDamage2, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_expdmg2")
				.Level(18)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 5)
				.Component("ref_scordspar", 3)
				.Component("elec_flawed", 2)
				;

			// Starship Enhancement - Explosive Defense II
			_builder.Create(RecipeType.StarshipEnhancementDefense2, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_expdef2")
				.Level(17)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 4)
				.Component("ref_scordspar", 2)
				.Component("elec_flawed", 2)
				;

			// Starship Enhancement - Shield II
			_builder.Create(RecipeType.StarshipEnhancementShield2, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_shield2")
				.Level(16)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 4)
				.Component("ref_scordspar", 2)
				.Component("elec_flawed", 2)
				;

			// Starship Enhancement - Shield Recharge Rate II
			_builder.Create(RecipeType.StarshipEnhancementShieldRechargeRate2, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_shrech2")
				.Level(19)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 5)
				.Component("ref_scordspar", 3)
				.Component("elec_flawed", 2)
				;

			// Starship Enhancement - Thermal Damage II
			_builder.Create(RecipeType.StarshipEnhancementThermalDamage2, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_thermdmg2")
				.Level(17)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 4)
				.Component("ref_scordspar", 2)
				.Component("elec_flawed", 2)
				;

			// Starship Enhancement - Thermal Defense II
			_builder.Create(RecipeType.StarshipEnhancementThermalDefense2, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_thermdef2")
				.Level(18)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 5)
				.Component("ref_scordspar", 3)
				.Component("elec_flawed", 2)
				;

			// Module Enhancement - Module Bonus II
			_builder.Create(RecipeType.ModuleEnhancementModuleBonus2, SkillType.Engineering)
				.Category(RecipeCategoryType.ModuleEnhancement)
				.Resref("men_mod2")
				.Level(19)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 2)
				.Component("ref_currian", 5)
				.Component("ref_scordspar", 3)
				.Component("elec_flawed", 2)
				;


		}

		private void Tier3()
        {
			// Armor Enhancement - Control - Smithery III
			_builder.Create(RecipeType.ArmorEnhancementControlSmithery3, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_con_smth3")
				.Level(21)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 1)
				.Component("ref_plagionite", 1)
				.Component("elec_good", 1)
				;

			// Armor Enhancement - Control - Engineering III
			_builder.Create(RecipeType.ArmorEnhancementControlEngineering3, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_con_eng3")
				.Level(22)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 2)
				.Component("ref_plagionite", 1)
				.Component("elec_good", 1)
				;

			// Armor Enhancement - Control - Fabrication III
			_builder.Create(RecipeType.ArmorEnhancementControlFabrication3, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_con_fab3")
				.Level(23)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 2)
				.Component("ref_plagionite", 1)
				.Component("elec_good", 1)
				;

			// Armor Enhancement - Control - Agriculture III
			_builder.Create(RecipeType.ArmorEnhancementControlAgriculture3, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_con_agr3")
				.Level(24)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 3)
				.Component("ref_plagionite", 2)
				.Component("elec_good", 1)
				;

			// Armor Enhancement - Craftsmanship - Smithery III
			_builder.Create(RecipeType.ArmorEnhancementCraftsmanshipSmithery3, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_cft_smth3")
				.Level(21)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 1)
				.Component("ref_plagionite", 1)
				.Component("elec_good", 1)
				;

			// Armor Enhancement - Craftsmanship - Engineering III
			_builder.Create(RecipeType.ArmorEnhancementCraftsmanshipEngineering3, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_cft_eng3")
				.Level(22)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 2)
				.Component("ref_plagionite", 1)
				.Component("elec_good", 1)
				;

			// Armor Enhancement - Craftsmanship - Fabrication III
			_builder.Create(RecipeType.ArmorEnhancementCraftsmanshipFabrication3, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_cft_fab3")
				.Level(23)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 2)
				.Component("ref_plagionite", 1)
				.Component("elec_good", 1)
				;

			// Armor Enhancement - Craftsmanship - Agriculture III
			_builder.Create(RecipeType.ArmorEnhancementCraftsmanshipAgriculture3, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_cft_agr3")
				.Level(24)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 3)
				.Component("ref_plagionite", 2)
				.Component("elec_good", 1)
				;

			// Armor Enhancement - Defense - Physical III
			_builder.Create(RecipeType.ArmorEnhancementDefensePhysical3, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_phy3")
				.Level(25)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 3)
				.Component("ref_plagionite", 2)
				.Component("elec_good", 2)
				;

			// Armor Enhancement - Defense - Force III
			_builder.Create(RecipeType.ArmorEnhancementDefenseForce3, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_for3")
				.Level(26)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 4)
				.Component("ref_plagionite", 2)
				.Component("elec_good", 2)
				;

			// Armor Enhancement - Defense - Poison III
			_builder.Create(RecipeType.ArmorEnhancementDefensePoison3, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_psn3")
				.Level(27)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 4)
				.Component("ref_plagionite", 2)
				.Component("elec_good", 2)
				;

			// Armor Enhancement - Defense - Fire III
			_builder.Create(RecipeType.ArmorEnhancementDefenseFire3, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_fir3")
				.Level(28)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 5)
				.Component("ref_plagionite", 3)
				.Component("elec_good", 2)
				;

			// Armor Enhancement - Defense - Ice III
			_builder.Create(RecipeType.ArmorEnhancementDefenseIce3, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_ice3")
				.Level(29)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 5)
				.Component("ref_plagionite", 3)
				.Component("elec_good", 2)
				;

			// Armor Enhancement - Defense - Electrical III
			_builder.Create(RecipeType.ArmorEnhancementDefenseElectrical3, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_elec3")
				.Level(25)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 3)
				.Component("ref_plagionite", 2)
				.Component("elec_good", 2)
				;

			// Armor Enhancement - Evasion III
			_builder.Create(RecipeType.ArmorEnhancementEvasion3, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_eva3")
				.Level(28)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 5)
				.Component("ref_plagionite", 3)
				.Component("elec_good", 2)
				;

            // Armor Enhancement - Attack III
            _builder.Create(RecipeType.ArmorEnhancementAttack3, SkillType.Engineering)
                .Category(RecipeCategoryType.ArmorEnhancement)
                .Resref("aen_atk3")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 3)
                .Component("ref_idailia", 5)
                .Component("ref_plagionite", 3)
                .Component("elec_good", 2)
                ;

            // Armor Enhancement - Force Attack III
            _builder.Create(RecipeType.ArmorEnhancementForceAttack3, SkillType.Engineering)
                .Category(RecipeCategoryType.ArmorEnhancement)
                .Resref("aen_frcatk3")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 3)
                .Component("ref_idailia", 5)
                .Component("ref_plagionite", 3)
                .Component("elec_good", 2)
                ;

            // Armor Enhancement - FP III
            _builder.Create(RecipeType.ArmorEnhancementFP3, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_fp3")
				.Level(25)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 3)
				.Component("ref_plagionite", 2)
				.Component("elec_good", 2)
				;

			// Armor Enhancement - HP III
			_builder.Create(RecipeType.ArmorEnhancementHP3, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_hp3")
				.Level(26)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 4)
				.Component("ref_plagionite", 2)
				.Component("elec_good", 2)
				;

			// Armor Enhancement - Recast Reduction III
			_builder.Create(RecipeType.ArmorEnhancementRecastReduction3, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_recast3")
				.Level(29)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 5)
				.Component("ref_plagionite", 3)
				.Component("elec_good", 2)
				;

			// Armor Enhancement - STM III
			_builder.Create(RecipeType.ArmorEnhancementSTM3, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_stm3")
				.Level(27)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 4)
				.Component("ref_plagionite", 2)
				.Component("elec_good", 2)
				;

			// Weapon Enhancement - Attack Bonus III
			_builder.Create(RecipeType.WeaponEnhancementAttackBonus3, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_atk3")
				.Level(28)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 5)
				.Component("ref_plagionite", 3)
				.Component("elec_good", 2)
				;

            // Weapon Enhancement - Force Attack III
            _builder.Create(RecipeType.WeaponEnhancementForceAttack3, SkillType.Engineering)
                .Category(RecipeCategoryType.WeaponEnhancement)
                .Resref("wen_frcatk3")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 3)
                .Component("ref_idailia", 5)
                .Component("ref_plagionite", 3)
                .Component("elec_good", 2)
                ;

            // Weapon Enhancement - Control - Smithery III
            _builder.Create(RecipeType.WeaponEnhancementControlSmithery3, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_con_smth3")
				.Level(24)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 3)
				.Component("ref_plagionite", 2)
				.Component("elec_good", 1)
				;

			// Weapon Enhancement - Control - Engineering III
			_builder.Create(RecipeType.WeaponEnhancementControlEngineering3, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_con_eng3")
				.Level(23)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 2)
				.Component("ref_plagionite", 1)
				.Component("elec_good", 1)
				;

			// Weapon Enhancement - Control - Fabrication III
			_builder.Create(RecipeType.WeaponEnhancementControlFabrication3, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_con_fab3")
				.Level(22)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 2)
				.Component("ref_plagionite", 1)
				.Component("elec_good", 1)
				;

			// Weapon Enhancement - Control - Agriculture III
			_builder.Create(RecipeType.WeaponEnhancementControlAgriculture3, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_con_agr3")
				.Level(21)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 1)
				.Component("ref_plagionite", 1)
				.Component("elec_good", 1)
				;

			// Weapon Enhancement - Craftsmanship - Smithery III
			_builder.Create(RecipeType.WeaponEnhancementCraftsmanshipSmithery3, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_cft_smth3")
				.Level(23)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 2)
				.Component("ref_plagionite", 1)
				.Component("elec_good", 1)
				;

			// Weapon Enhancement - Craftsmanship - Engineering III
			_builder.Create(RecipeType.WeaponEnhancementCraftsmanshipEngineering3, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_cft_eng3")
				.Level(24)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 3)
				.Component("ref_plagionite", 2)
				.Component("elec_good", 1)
				;

			// Weapon Enhancement - Craftsmanship - Fabrication III
			_builder.Create(RecipeType.WeaponEnhancementCraftsmanshipFabrication3, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_cft_fab3")
				.Level(21)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 1)
				.Component("ref_plagionite", 1)
				.Component("elec_good", 1)
				;

			// Weapon Enhancement - Craftsmanship - Agriculture III
			_builder.Create(RecipeType.WeaponEnhancementCraftsmanshipAgriculture3, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_cft_agr3")
				.Level(22)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 2)
				.Component("ref_plagionite", 1)
				.Component("elec_good", 1)
				;

			// Weapon Enhancement - DMG - Physical II
			_builder.Create(RecipeType.WeaponEnhancementDMGPhysical2, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_dmg_phy2")
				.Level(29)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 5)
				.Component("ref_plagionite", 3)
				.Component("elec_good", 2)
				;

			// Weapon Enhancement - DMG - Force II
			_builder.Create(RecipeType.WeaponEnhancementDMGForce2, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_dmg_for2")
				.Level(28)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 5)
				.Component("ref_plagionite", 3)
				.Component("elec_good", 2)
				;
			
			// Weapon Enhancement - Evasion III
			_builder.Create(RecipeType.WeaponEnhancementEvasion3, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_eva3")
				.Level(28)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 5)
				.Component("ref_plagionite", 3)
				.Component("elec_good", 2)
				;

			// Weapon Enhancement - FP III
			_builder.Create(RecipeType.WeaponEnhancementFP3, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_fp3")
				.Level(28)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 5)
				.Component("ref_plagionite", 3)
				.Component("elec_good", 2)
				;

			// Weapon Enhancement - HP III
			_builder.Create(RecipeType.WeaponEnhancementHP3, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_hp3")
				.Level(29)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 5)
				.Component("ref_plagionite", 3)
				.Component("elec_good", 2)
				;

			// Weapon Enhancement - STM III
			_builder.Create(RecipeType.WeaponEnhancementSTM3, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_stm3")
				.Level(27)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 4)
				.Component("ref_plagionite", 2)
				.Component("elec_good", 2)
				;

            // Weapon Enhancement - Accuracy III
            _builder.Create(RecipeType.WeaponEnhancementAccuracy3, SkillType.Engineering)
                .Category(RecipeCategoryType.WeaponEnhancement)
                .Resref("wen_acc3")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 3)
                .Component("ref_idailia", 5)
                .Component("ref_plagionite", 3)
                .Component("elec_good", 2)
                ;

            // Structure Enhancement - Structure Bonus III
            _builder.Create(RecipeType.StructureEnhancementStructureBonus3, SkillType.Engineering)
				.Category(RecipeCategoryType.StructureEnhancement)
				.Resref("sten_sb3")
				.Level(29)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 5)
				.Component("ref_plagionite", 3)
				.Component("elec_good", 2)
				;

			// Starship Enhancement - Accuracy III
			_builder.Create(RecipeType.StarshipEnhancementAccuracy3, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_acc3")
				.Level(22)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 2)
				.Component("ref_plagionite", 1)
				.Component("elec_good", 1)
				;

			// Starship Enhancement - Armor III
			_builder.Create(RecipeType.StarshipEnhancementArmor3, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_armor3")
				.Level(23)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 2)
				.Component("ref_plagionite", 1)
				.Component("elec_good", 1)
				;

			// Starship Enhancement - Capacitor III
			_builder.Create(RecipeType.StarshipEnhancementCapacitor3, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_cap3")
				.Level(21)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 1)
				.Component("ref_plagionite", 1)
				.Component("elec_good", 1)
				;

			// Starship Enhancement - EM Damage III
			_builder.Create(RecipeType.StarshipEnhancementEMDamage3, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_emdmg3")
				.Level(25)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 3)
				.Component("ref_plagionite", 2)
				.Component("elec_good", 1)
				;

			// Starship Enhancement - EM Defense III
			_builder.Create(RecipeType.StarshipEnhancementEMDefense3, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_emdef3")
				.Level(24)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 3)
				.Component("ref_plagionite", 2)
				.Component("elec_good", 1)
				;

			// Starship Enhancement - Evasion III
			_builder.Create(RecipeType.StarshipEnhancementEvasion3, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_eva3")
				.Level(26)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 4)
				.Component("ref_plagionite", 2)
				.Component("elec_good", 2)
				;

			// Starship Enhancement - Explosive Damage III
			_builder.Create(RecipeType.StarshipEnhancementExplosiveDamage3, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_expdmg3")
				.Level(28)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 5)
				.Component("ref_plagionite", 3)
				.Component("elec_good", 2)
				;

			// Starship Enhancement - Explosive Defense III
			_builder.Create(RecipeType.StarshipEnhancementDefense3, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_expdef3")
				.Level(27)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 4)
				.Component("ref_plagionite", 2)
				.Component("elec_good", 2)
				;

			// Starship Enhancement - Shield III
			_builder.Create(RecipeType.StarshipEnhancementShield3, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_shield3")
				.Level(26)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 4)
				.Component("ref_plagionite", 2)
				.Component("elec_good", 2)
				;

			// Starship Enhancement - Shield Recharge Rate III
			_builder.Create(RecipeType.StarshipEnhancementShieldRechargeRate3, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_shrech3")
				.Level(29)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 5)
				.Component("ref_plagionite", 3)
				.Component("elec_good", 2)
				;

			// Starship Enhancement - Thermal Damage III
			_builder.Create(RecipeType.StarshipEnhancementThermalDamage3, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_thermdmg3")
				.Level(27)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 4)
				.Component("ref_plagionite", 2)
				.Component("elec_good", 2)
				;

			// Starship Enhancement - Thermal Defense III
			_builder.Create(RecipeType.StarshipEnhancementThermalDefense3, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_thermdef3")
				.Level(28)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 5)
				.Component("ref_plagionite", 3)
				.Component("elec_good", 2)
				;

			// Module Enhancement - Module Bonus III
			_builder.Create(RecipeType.ModuleEnhancementModuleBonus3, SkillType.Engineering)
				.Category(RecipeCategoryType.ModuleEnhancement)
				.Resref("men_mod3")
				.Level(29)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 3)
				.Component("ref_idailia", 5)
				.Component("ref_plagionite", 3)
				.Component("elec_good", 2)
				;


		}

		private void Tier4()
        {
			// Armor Enhancement - Control - Smithery IV
			_builder.Create(RecipeType.ArmorEnhancementControlSmithery4, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_con_smth4")
				.Level(31)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 1)
				.Component("ref_keromber", 1)
				.Component("elec_imperfect", 1)
				;

			// Armor Enhancement - Control - Engineering IV
			_builder.Create(RecipeType.ArmorEnhancementControlEngineering4, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_con_eng4")
				.Level(32)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 2)
				.Component("ref_keromber", 1)
				.Component("elec_imperfect", 1)
				;

			// Armor Enhancement - Control - Fabrication IV
			_builder.Create(RecipeType.ArmorEnhancementControlFabrication4, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_con_fab4")
				.Level(33)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 2)
				.Component("ref_keromber", 1)
				.Component("elec_imperfect", 1)
				;

			// Armor Enhancement - Control - Agriculture IV
			_builder.Create(RecipeType.ArmorEnhancementControlAgriculture4, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_con_agr4")
				.Level(34)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 3)
				.Component("ref_keromber", 2)
				.Component("elec_imperfect", 1)
				;

			// Armor Enhancement - Craftsmanship - Smithery IV
			_builder.Create(RecipeType.ArmorEnhancementCraftsmanshipSmithery4, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_cft_smth4")
				.Level(31)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 1)
				.Component("ref_keromber", 1)
				.Component("elec_imperfect", 1)
				;

			// Armor Enhancement - Craftsmanship - Engineering IV
			_builder.Create(RecipeType.ArmorEnhancementCraftsmanshipEngineering4, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_cft_eng4")
				.Level(32)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 2)
				.Component("ref_keromber", 1)
				.Component("elec_imperfect", 1)
				;

			// Armor Enhancement - Craftsmanship - Fabrication IV
			_builder.Create(RecipeType.ArmorEnhancementCraftsmanshipFabrication4, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_cft_fab4")
				.Level(33)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 2)
				.Component("ref_keromber", 1)
				.Component("elec_imperfect", 1)
				;

			// Armor Enhancement - Craftsmanship - Agriculture IV
			_builder.Create(RecipeType.ArmorEnhancementCraftsmanshipAgriculture4, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_cft_agr4")
				.Level(34)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 3)
				.Component("ref_keromber", 2)
				.Component("elec_imperfect", 1)
				;

			// Armor Enhancement - Defense - Physical IV
			_builder.Create(RecipeType.ArmorEnhancementDefensePhysical4, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_phy4")
				.Level(35)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 3)
				.Component("ref_keromber", 2)
				.Component("elec_imperfect", 2)
				;

			// Armor Enhancement - Defense - Force IV
			_builder.Create(RecipeType.ArmorEnhancementDefenseForce4, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_for4")
				.Level(36)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 4)
				.Component("ref_keromber", 2)
				.Component("elec_imperfect", 2)
				;

			// Armor Enhancement - Defense - Poison IV
			_builder.Create(RecipeType.ArmorEnhancementDefensePoison4, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_psn4")
				.Level(37)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 4)
				.Component("ref_keromber", 2)
				.Component("elec_imperfect", 2)
				;

			// Armor Enhancement - Defense - Fire IV
			_builder.Create(RecipeType.ArmorEnhancementDefenseFire4, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_fir4")
				.Level(38)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 5)
				.Component("ref_keromber", 3)
				.Component("elec_imperfect", 2)
				;

			// Armor Enhancement - Defense - Ice IV
			_builder.Create(RecipeType.ArmorEnhancementDefenseIce4, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_ice4")
				.Level(39)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 5)
				.Component("ref_keromber", 3)
				.Component("elec_imperfect", 2)
				;

			// Armor Enhancement - Defense - Electrical IV
			_builder.Create(RecipeType.ArmorEnhancementDefenseElectrical4, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_elec4")
				.Level(35)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 3)
				.Component("ref_keromber", 2)
				.Component("elec_imperfect", 2)
				;

			// Armor Enhancement - Evasion IV
			_builder.Create(RecipeType.ArmorEnhancementEvasion4, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_eva4")
				.Level(38)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 5)
				.Component("ref_keromber", 3)
				.Component("elec_imperfect", 2)
				;

            // Armor Enhancement - Attack IV
            _builder.Create(RecipeType.ArmorEnhancementAttack4, SkillType.Engineering)
                .Category(RecipeCategoryType.ArmorEnhancement)
                .Resref("aen_atk4")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 4)
                .Component("ref_barinium", 5)
                .Component("ref_keromber", 3)
                .Component("elec_imperfect", 2)
                ;

            // Armor Enhancement - Force Attack IV
            _builder.Create(RecipeType.ArmorEnhancementForceAttack4, SkillType.Engineering)
                .Category(RecipeCategoryType.ArmorEnhancement)
                .Resref("aen_frcatk4")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 4)
                .Component("ref_barinium", 5)
                .Component("ref_keromber", 3)
                .Component("elec_imperfect", 2)
                ;

            // Armor Enhancement - FP IV
            _builder.Create(RecipeType.ArmorEnhancementFP4, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_fp4")
				.Level(35)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 3)
				.Component("ref_keromber", 2)
				.Component("elec_imperfect", 2)
				;

			// Armor Enhancement - HP IV
			_builder.Create(RecipeType.ArmorEnhancementHP4, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_hp4")
				.Level(36)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 4)
				.Component("ref_keromber", 2)
				.Component("elec_imperfect", 2)
				;

			// Armor Enhancement - Recast Reduction IV
			_builder.Create(RecipeType.ArmorEnhancementRecastReduction4, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_recast4")
				.Level(39)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 5)
				.Component("ref_keromber", 3)
				.Component("elec_imperfect", 2)
				;

			// Armor Enhancement - STM IV
			_builder.Create(RecipeType.ArmorEnhancementSTM4, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_stm4")
				.Level(37)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 4)
				.Component("ref_keromber", 2)
				.Component("elec_imperfect", 2)
				;

			// Weapon Enhancement - Attack Bonus IV
			_builder.Create(RecipeType.WeaponEnhancementAttackBonus4, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_atk4")
				.Level(38)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 5)
				.Component("ref_keromber", 3)
				.Component("elec_imperfect", 2)
				;

            // Weapon Enhancement - Force Attack IV
            _builder.Create(RecipeType.WeaponEnhancementForceAttack4, SkillType.Engineering)
                .Category(RecipeCategoryType.WeaponEnhancement)
                .Resref("wen_frcatk4")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 4)
                .Component("ref_barinium", 5)
                .Component("ref_keromber", 3)
                .Component("elec_imperfect", 2)
                ;

            // Weapon Enhancement - Control - Smithery IV
            _builder.Create(RecipeType.WeaponEnhancementControlSmithery4, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_con_smth4")
				.Level(34)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 3)
				.Component("ref_keromber", 2)
				.Component("elec_imperfect", 1)
				;

			// Weapon Enhancement - Control - Engineering IV
			_builder.Create(RecipeType.WeaponEnhancementControlEngineering4, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_con_eng4")
				.Level(33)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 2)
				.Component("ref_keromber", 1)
				.Component("elec_imperfect", 1)
				;

			// Weapon Enhancement - Control - Fabrication IV
			_builder.Create(RecipeType.WeaponEnhancementControlFabrication4, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_con_fab4")
				.Level(32)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 2)
				.Component("ref_keromber", 1)
				.Component("elec_imperfect", 1)
				;

			// Weapon Enhancement - Control - Agriculture IV
			_builder.Create(RecipeType.WeaponEnhancementControlAgriculture4, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_con_agr4")
				.Level(31)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 1)
				.Component("ref_keromber", 1)
				.Component("elec_imperfect", 1)
				;

			// Weapon Enhancement - Craftsmanship - Smithery IV
			_builder.Create(RecipeType.WeaponEnhancementCraftsmanshipSmithery4, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_cft_smth4")
				.Level(33)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 2)
				.Component("ref_keromber", 1)
				.Component("elec_imperfect", 1)
				;

			// Weapon Enhancement - Craftsmanship - Engineering IV
			_builder.Create(RecipeType.WeaponEnhancementCraftsmanshipEngineering4, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_cft_eng4")
				.Level(34)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 3)
				.Component("ref_keromber", 2)
				.Component("elec_imperfect", 1)
				;

			// Weapon Enhancement - Craftsmanship - Fabrication IV
			_builder.Create(RecipeType.WeaponEnhancementCraftsmanshipFabrication4, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_cft_fab4")
				.Level(31)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 1)
				.Component("ref_keromber", 1)
				.Component("elec_imperfect", 1)
				;

			// Weapon Enhancement - Craftsmanship - Agriculture IV
			_builder.Create(RecipeType.WeaponEnhancementCraftsmanshipAgriculture4, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_cft_agr4")
				.Level(32)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 2)
				.Component("ref_keromber", 1)
				.Component("elec_imperfect", 1)
				;
			
			// Weapon Enhancement - DMG - Poison II
			_builder.Create(RecipeType.WeaponEnhancementDMGPoison2, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_dmg_psn2")
				.Level(37)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 4)
				.Component("ref_keromber", 2)
				.Component("elec_imperfect", 2)
				;

			// Weapon Enhancement - DMG - Fire II
			_builder.Create(RecipeType.WeaponEnhancementDMGFire2, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_dmg_fir2")
				.Level(36)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 4)
				.Component("ref_keromber", 2)
				.Component("elec_imperfect", 2)
				;

			// Weapon Enhancement - DMG - Ice II
			_builder.Create(RecipeType.WeaponEnhancementDMGIce2, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_dmg_ice2")
				.Level(35)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 3)
				.Component("ref_keromber", 2)
				.Component("elec_imperfect", 2)
				;

			// Weapon Enhancement - DMG - Electrical II
			_builder.Create(RecipeType.WeaponEnhancementElectrical2, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_dmg_elec2")
				.Level(36)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 4)
				.Component("ref_keromber", 2)
				.Component("elec_imperfect", 2)
				;

			// Weapon Enhancement - Evasion IV
			_builder.Create(RecipeType.WeaponEnhancementEvasion4, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_eva4")
				.Level(38)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 5)
				.Component("ref_keromber", 3)
				.Component("elec_imperfect", 2)
				;

			// Weapon Enhancement - FP IV
			_builder.Create(RecipeType.WeaponEnhancementFP4, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_fp4")
				.Level(38)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 5)
				.Component("ref_keromber", 3)
				.Component("elec_imperfect", 2)
				;

			// Weapon Enhancement - HP IV
			_builder.Create(RecipeType.WeaponEnhancementHP4, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_hp4")
				.Level(39)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 5)
				.Component("ref_keromber", 3)
				.Component("elec_imperfect", 2)
				;

			// Weapon Enhancement - STM IV
			_builder.Create(RecipeType.WeaponEnhancementSTM4, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_stm4")
				.Level(37)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 4)
				.Component("ref_keromber", 2)
				.Component("elec_imperfect", 2)
				;

            // Weapon Enhancement - Accuracy IV
            _builder.Create(RecipeType.WeaponEnhancementAccuracy4, SkillType.Engineering)
                .Category(RecipeCategoryType.WeaponEnhancement)
                .Resref("wen_acc4")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 4)
                .Component("ref_barinium", 5)
                .Component("ref_keromber", 3)
                .Component("elec_imperfect", 2)
                ;

            // Structure Enhancement - Structure Bonus IV
            _builder.Create(RecipeType.StructureEnhancementStructureBonus4, SkillType.Engineering)
				.Category(RecipeCategoryType.StructureEnhancement)
				.Resref("sten_sb4")
				.Level(39)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 5)
				.Component("ref_keromber", 3)
				.Component("elec_imperfect", 2)
				;

			// Starship Enhancement - Accuracy IV
			_builder.Create(RecipeType.StarshipEnhancementAccuracy4, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_acc4")
				.Level(32)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 2)
				.Component("ref_keromber", 1)
				.Component("elec_imperfect", 1)
				;

			// Starship Enhancement - Armor IV
			_builder.Create(RecipeType.StarshipEnhancementArmor4, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_armor4")
				.Level(33)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 2)
				.Component("ref_keromber", 1)
				.Component("elec_imperfect", 1)
				;

			// Starship Enhancement - Capacitor IV
			_builder.Create(RecipeType.StarshipEnhancementCapacitor4, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_cap4")
				.Level(31)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 1)
				.Component("ref_keromber", 1)
				.Component("elec_imperfect", 1)
				;

			// Starship Enhancement - EM Damage IV
			_builder.Create(RecipeType.StarshipEnhancementEMDamage4, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_emdmg4")
				.Level(35)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 3)
				.Component("ref_keromber", 2)
				.Component("elec_imperfect", 1)
				;

			// Starship Enhancement - EM Defense IV
			_builder.Create(RecipeType.StarshipEnhancementEMDefense4, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_emdef4")
				.Level(34)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 3)
				.Component("ref_keromber", 2)
				.Component("elec_imperfect", 1)
				;

			// Starship Enhancement - Evasion IV
			_builder.Create(RecipeType.StarshipEnhancementEvasion4, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_eva4")
				.Level(36)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 4)
				.Component("ref_keromber", 2)
				.Component("elec_imperfect", 2)
				;

			// Starship Enhancement - Explosive Damage IV
			_builder.Create(RecipeType.StarshipEnhancementExplosiveDamage4, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_expdmg4")
				.Level(38)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 5)
				.Component("ref_keromber", 3)
				.Component("elec_imperfect", 2)
				;

			// Starship Enhancement - Explosive Defense IV
			_builder.Create(RecipeType.StarshipEnhancementDefense4, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_expdef4")
				.Level(37)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 4)
				.Component("ref_keromber", 2)
				.Component("elec_imperfect", 2)
				;

			// Starship Enhancement - Shield IV
			_builder.Create(RecipeType.StarshipEnhancementShield4, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_shield4")
				.Level(36)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 4)
				.Component("ref_keromber", 2)
				.Component("elec_imperfect", 2)
				;

			// Starship Enhancement - Shield Recharge Rate IV
			_builder.Create(RecipeType.StarshipEnhancementShieldRechargeRate4, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_shrech4")
				.Level(39)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 5)
				.Component("ref_keromber", 3)
				.Component("elec_imperfect", 2)
				;

			// Starship Enhancement - Thermal Damage IV
			_builder.Create(RecipeType.StarshipEnhancementThermalDamage4, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_thermdmg4")
				.Level(37)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 4)
				.Component("ref_keromber", 2)
				.Component("elec_imperfect", 2)
				;

			// Starship Enhancement - Thermal Defense IV
			_builder.Create(RecipeType.StarshipEnhancementThermalDefense4, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_thermdef4")
				.Level(38)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 5)
				.Component("ref_keromber", 3)
				.Component("elec_imperfect", 2)
				;

			// Module Enhancement - Module Bonus IV
			_builder.Create(RecipeType.ModuleEnhancementModuleBonus4, SkillType.Engineering)
				.Category(RecipeCategoryType.ModuleEnhancement)
				.Resref("men_mod4")
				.Level(39)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 4)
				.Component("ref_barinium", 5)
				.Component("ref_keromber", 3)
				.Component("elec_imperfect", 2)
				;


		}

		private void Tier5()
        {
			// Armor Enhancement - Control - Smithery V
			_builder.Create(RecipeType.ArmorEnhancementControlSmithery5, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_con_smth5")
				.Level(41)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 1)
				.Component("ref_jasioclase", 1)
				.Component("elec_high", 1)
				;

			// Armor Enhancement - Control - Engineering V
			_builder.Create(RecipeType.ArmorEnhancementControlEngineering5, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_con_eng5")
				.Level(42)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 2)
				.Component("ref_jasioclase", 1)
				.Component("elec_high", 1)
				;

			// Armor Enhancement - Control - Fabrication V
			_builder.Create(RecipeType.ArmorEnhancementControlFabrication5, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_con_fab5")
				.Level(43)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 2)
				.Component("ref_jasioclase", 1)
				.Component("elec_high", 1)
				;

			// Armor Enhancement - Control - Agriculture V
			_builder.Create(RecipeType.ArmorEnhancementControlAgriculture5, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_con_agr5")
				.Level(44)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 3)
				.Component("ref_jasioclase", 2)
				.Component("elec_high", 1)
				;

			// Armor Enhancement - Craftsmanship - Smithery V
			_builder.Create(RecipeType.ArmorEnhancementCraftsmanshipSmithery5, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_cft_smth5")
				.Level(41)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 1)
				.Component("ref_jasioclase", 1)
				.Component("elec_high", 1)
				;

			// Armor Enhancement - Craftsmanship - Engineering V
			_builder.Create(RecipeType.ArmorEnhancementCraftsmanshipEngineering5, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_cft_eng5")
				.Level(42)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 2)
				.Component("ref_jasioclase", 1)
				.Component("elec_high", 1)
				;

			// Armor Enhancement - Craftsmanship - Fabrication V
			_builder.Create(RecipeType.ArmorEnhancementCraftsmanshipFabrication5, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_cft_fab5")
				.Level(43)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 2)
				.Component("ref_jasioclase", 1)
				.Component("elec_high", 1)
				;

			// Armor Enhancement - Craftsmanship - Agriculture V
			_builder.Create(RecipeType.ArmorEnhancementCraftsmanshipAgriculture5, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_cft_agr5")
				.Level(44)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 3)
				.Component("ref_jasioclase", 2)
				.Component("elec_high", 1)
				;

			// Armor Enhancement - Defense - Physical V
			_builder.Create(RecipeType.ArmorEnhancementDefensePhysical5, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_phy5")
				.Level(45)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 3)
				.Component("ref_jasioclase", 2)
				.Component("elec_high", 2)
				;

			// Armor Enhancement - Defense - Force V
			_builder.Create(RecipeType.ArmorEnhancementDefenseForce5, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_for5")
				.Level(46)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 4)
				.Component("ref_jasioclase", 2)
				.Component("elec_high", 2)
				;

			// Armor Enhancement - Defense - Poison V
			_builder.Create(RecipeType.ArmorEnhancementDefensePoison5, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_psn5")
				.Level(47)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 4)
				.Component("ref_jasioclase", 2)
				.Component("elec_high", 2)
				;

			// Armor Enhancement - Defense - Fire V
			_builder.Create(RecipeType.ArmorEnhancementDefenseFire5, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_fir5")
				.Level(48)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 5)
				.Component("ref_jasioclase", 3)
				.Component("elec_high", 2)
				;

			// Armor Enhancement - Defense - Ice V
			_builder.Create(RecipeType.ArmorEnhancementDefenseIce5, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_ice5")
				.Level(49)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 5)
				.Component("ref_jasioclase", 3)
				.Component("elec_high", 2)
				;

			// Armor Enhancement - Defense - Electrical V
			_builder.Create(RecipeType.ArmorEnhancementDefenseElectrical5, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_def_elec5")
				.Level(45)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 3)
				.Component("ref_jasioclase", 2)
				.Component("elec_high", 2)
				;

			// Armor Enhancement - Evasion V
			_builder.Create(RecipeType.ArmorEnhancementEvasion5, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_eva5")
				.Level(48)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 5)
				.Component("ref_jasioclase", 3)
				.Component("elec_high", 2)
				;

            // Armor Enhancement - Attack V
            _builder.Create(RecipeType.ArmorEnhancementAttack5, SkillType.Engineering)
                .Category(RecipeCategoryType.ArmorEnhancement)
                .Resref("aen_atk5")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 5)
                .Component("ref_gostian", 5)
                .Component("ref_jasioclase", 3)
                .Component("elec_high", 2)
                ;

            // Armor Enhancement - Force Attack V
            _builder.Create(RecipeType.ArmorEnhancementForceAttack5, SkillType.Engineering)
                .Category(RecipeCategoryType.ArmorEnhancement)
                .Resref("aen_frcatk5")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 5)
                .Component("ref_gostian", 5)
                .Component("ref_jasioclase", 3)
                .Component("elec_high", 2)
                ;

            // Armor Enhancement - FP V
            _builder.Create(RecipeType.ArmorEnhancementFP5, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_fp5")
				.Level(45)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 3)
				.Component("ref_jasioclase", 2)
				.Component("elec_high", 2)
				;

			// Armor Enhancement - HP V
			_builder.Create(RecipeType.ArmorEnhancementHP5, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_hp5")
				.Level(46)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 4)
				.Component("ref_jasioclase", 2)
				.Component("elec_high", 2)
				;

			// Armor Enhancement - Recast Reduction V
			_builder.Create(RecipeType.ArmorEnhancementRecastReduction5, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_recast5")
				.Level(49)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 5)
				.Component("ref_jasioclase", 3)
				.Component("elec_high", 2)
				;

			// Armor Enhancement - STM V
			_builder.Create(RecipeType.ArmorEnhancementSTM5, SkillType.Engineering)
				.Category(RecipeCategoryType.ArmorEnhancement)
				.Resref("aen_stm5")
				.Level(47)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 4)
				.Component("ref_jasioclase", 2)
				.Component("elec_high", 2)
				;

			// Weapon Enhancement - Attack Bonus V
			_builder.Create(RecipeType.WeaponEnhancementAttackBonus5, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_atk5")
				.Level(48)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 5)
				.Component("ref_jasioclase", 3)
				.Component("elec_high", 2)
				;

            // Weapon Enhancement - Force Attack V
            _builder.Create(RecipeType.WeaponEnhancementForceAttack5, SkillType.Engineering)
                .Category(RecipeCategoryType.WeaponEnhancement)
                .Resref("wen_frcatk5")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 5)
                .Component("ref_gostian", 5)
                .Component("ref_jasioclase", 3)
                .Component("elec_high", 2)
                ;

            // Weapon Enhancement - Control - Smithery V
            _builder.Create(RecipeType.WeaponEnhancementControlSmithery5, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_con_smth5")
				.Level(44)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 3)
				.Component("ref_jasioclase", 2)
				.Component("elec_high", 1)
				;

			// Weapon Enhancement - Control - Engineering V
			_builder.Create(RecipeType.WeaponEnhancementControlEngineering5, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_con_eng5")
				.Level(43)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 2)
				.Component("ref_jasioclase", 1)
				.Component("elec_high", 1)
				;

			// Weapon Enhancement - Control - Fabrication V
			_builder.Create(RecipeType.WeaponEnhancementControlFabrication5, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_con_fab5")
				.Level(42)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 2)
				.Component("ref_jasioclase", 1)
				.Component("elec_high", 1)
				;

			// Weapon Enhancement - Control - Agriculture V
			_builder.Create(RecipeType.WeaponEnhancementControlAgriculture5, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_con_agr5")
				.Level(41)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 1)
				.Component("ref_jasioclase", 1)
				.Component("elec_high", 1)
				;

			// Weapon Enhancement - Craftsmanship - Smithery V
			_builder.Create(RecipeType.WeaponEnhancementCraftsmanshipSmithery5, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_cft_smth5")
				.Level(43)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 2)
				.Component("ref_jasioclase", 1)
				.Component("elec_high", 1)
				;

			// Weapon Enhancement - Craftsmanship - Engineering V
			_builder.Create(RecipeType.WeaponEnhancementCraftsmanshipEngineering5, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_cft_eng5")
				.Level(44)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 3)
				.Component("ref_jasioclase", 2)
				.Component("elec_high", 1)
				;

			// Weapon Enhancement - Craftsmanship - Fabrication V
			_builder.Create(RecipeType.WeaponEnhancementCraftsmanshipFabrication5, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_cft_fab5")
				.Level(41)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 1)
				.Component("ref_jasioclase", 1)
				.Component("elec_high", 1)
				;

			// Weapon Enhancement - Craftsmanship - Agriculture V
			_builder.Create(RecipeType.WeaponEnhancementCraftsmanshipAgriculture5, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_cft_agr5")
				.Level(42)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 2)
				.Component("ref_jasioclase", 1)
				.Component("elec_high", 1)
				;

			// Weapon Enhancement - DMG - Physical III
			_builder.Create(RecipeType.WeaponEnhancementDMGPhysical3, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_dmg_phy3")
				.Level(49)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 5)
				.Component("ref_jasioclase", 3)
				.Component("elec_high", 2)
				;

			// Weapon Enhancement - DMG - Force III
			_builder.Create(RecipeType.WeaponEnhancementDMGForce3, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_dmg_for3")
				.Level(48)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 5)
				.Component("ref_jasioclase", 3)
				.Component("elec_high", 2)
				;

            // Weapon Enhancement - Evasion V
			_builder.Create(RecipeType.WeaponEnhancementEvasion5, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_eva5")
				.Level(48)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 5)
				.Component("ref_jasioclase", 3)
				.Component("elec_high", 2)
				;

			// Weapon Enhancement - FP V
			_builder.Create(RecipeType.WeaponEnhancementFP5, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_fp5")
				.Level(48)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 5)
				.Component("ref_jasioclase", 3)
				.Component("elec_high", 2)
				;

			// Weapon Enhancement - HP V
			_builder.Create(RecipeType.WeaponEnhancementHP5, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_hp5")
				.Level(49)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 5)
				.Component("ref_jasioclase", 3)
				.Component("elec_high", 2)
				;

			// Weapon Enhancement - STM V
			_builder.Create(RecipeType.WeaponEnhancementSTM5, SkillType.Engineering)
				.Category(RecipeCategoryType.WeaponEnhancement)
				.Resref("wen_stm5")
				.Level(47)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 4)
				.Component("ref_jasioclase", 2)
				.Component("elec_high", 2)
				;

            // Weapon Enhancement - Accuracy V
            _builder.Create(RecipeType.WeaponEnhancementAccuracy5, SkillType.Engineering)
                .Category(RecipeCategoryType.WeaponEnhancement)
                .Resref("wen_acc5")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 5)
                .Component("ref_gostian", 5)
                .Component("ref_jasioclase", 3)
                .Component("elec_high", 2)
                ;

            // Structure Enhancement - Structure Bonus V
            _builder.Create(RecipeType.StructureEnhancementStructureBonus5, SkillType.Engineering)
				.Category(RecipeCategoryType.StructureEnhancement)
				.Resref("sten_sb5")
				.Level(49)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 5)
				.Component("ref_jasioclase", 3)
				.Component("elec_high", 2)
				;

			// Starship Enhancement - Accuracy V
			_builder.Create(RecipeType.StarshipEnhancementAccuracy5, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_acc5")
				.Level(42)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 2)
				.Component("ref_jasioclase", 1)
				.Component("elec_high", 1)
				;

			// Starship Enhancement - Armor V
			_builder.Create(RecipeType.StarshipEnhancementArmor5, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_armor5")
				.Level(43)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 2)
				.Component("ref_jasioclase", 1)
				.Component("elec_high", 1)
				;

			// Starship Enhancement - Capacitor V
			_builder.Create(RecipeType.StarshipEnhancementCapacitor5, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_cap5")
				.Level(41)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 1)
				.Component("ref_jasioclase", 1)
				.Component("elec_high", 1)
				;

			// Starship Enhancement - EM Damage V
			_builder.Create(RecipeType.StarshipEnhancementEMDamage5, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_emdmg5")
				.Level(45)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 3)
				.Component("ref_jasioclase", 2)
				.Component("elec_high", 1)
				;

			// Starship Enhancement - EM Defense V
			_builder.Create(RecipeType.StarshipEnhancementEMDefense5, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_emdef5")
				.Level(44)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 3)
				.Component("ref_jasioclase", 2)
				.Component("elec_high", 1)
				;

			// Starship Enhancement - Evasion V
			_builder.Create(RecipeType.StarshipEnhancementEvasion5, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_eva5")
				.Level(46)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 4)
				.Component("ref_jasioclase", 2)
				.Component("elec_high", 2)
				;

			// Starship Enhancement - Explosive Damage V
			_builder.Create(RecipeType.StarshipEnhancementExplosiveDamage5, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_expdmg5")
				.Level(48)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 5)
				.Component("ref_jasioclase", 3)
				.Component("elec_high", 2)
				;

			// Starship Enhancement - Explosive Defense V
			_builder.Create(RecipeType.StarshipEnhancementDefense5, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_expdef5")
				.Level(47)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 4)
				.Component("ref_jasioclase", 2)
				.Component("elec_high", 2)
				;

			// Starship Enhancement - Shield V
			_builder.Create(RecipeType.StarshipEnhancementShield5, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_shield5")
				.Level(46)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 4)
				.Component("ref_jasioclase", 2)
				.Component("elec_high", 2)
				;

			// Starship Enhancement - Shield Recharge Rate V
			_builder.Create(RecipeType.StarshipEnhancementShieldRechargeRate5, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_shrech5")
				.Level(49)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 5)
				.Component("ref_jasioclase", 3)
				.Component("elec_high", 2)
				;

			// Starship Enhancement - Thermal Damage V
			_builder.Create(RecipeType.StarshipEnhancementThermalDamage5, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_thermdmg5")
				.Level(47)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 4)
				.Component("ref_jasioclase", 2)
				.Component("elec_high", 2)
				;

			// Starship Enhancement - Thermal Defense V
			_builder.Create(RecipeType.StarshipEnhancementThermalDefense5, SkillType.Engineering)
				.Category(RecipeCategoryType.StarshipEnhancement)
				.Resref("sen_thermdef5")
				.Level(48)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 5)
				.Component("ref_jasioclase", 3)
				.Component("elec_high", 2)
				;

			// Module Enhancement - Module Bonus V
			_builder.Create(RecipeType.ModuleEnhancementModuleBonus5, SkillType.Engineering)
				.Category(RecipeCategoryType.ModuleEnhancement)
				.Resref("men_mod5")
				.Level(49)
				.Quantity(1)
				.RequirementPerk(PerkType.EnhancementBlueprints, 5)
				.Component("ref_gostian", 5)
				.Component("ref_jasioclase", 3)
				.Component("elec_high", 2)
				;


		}

	}
}
