using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    public class ModuleRecipes: IRecipeListDefinition
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
			// Basic Capacitor Booster
			_builder.Create(RecipeType.BasicCapacitorBooster, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("cap_boost_b")
				.Level(5)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_tilarium", 3)
				.Component("elec_ruined", 2);

			// Basic Combat Laser
			_builder.Create(RecipeType.BasicCombatLaser, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("com_laser_b")
				.Level(2)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_tilarium", 2)
				.Component("elec_ruined", 1);

            // Basic Beam Cannon
            _builder.Create(RecipeType.BasicBeamCannon, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("beamcannon1")
                .Level(2)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Module, 1)
                .Component("ref_tilarium", 2)
                .Component("elec_ruined", 1);

            // Basic EM Amplifier
            _builder.Create(RecipeType.BasicEMAmplifier, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("em_amp_b")
				.Level(7)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_tilarium", 4)
				.Component("elec_ruined", 2);

			// Basic Thermal Amplifier
			_builder.Create(RecipeType.BasicThermalAmplifier, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("therm_amp_b")
				.Level(6)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_tilarium", 4)
				.Component("elec_ruined", 2);

			// Basic Explosive Amplifier
			_builder.Create(RecipeType.BasicExplosiveAmplifier, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("exp_amp_b")
				.Level(6)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_tilarium", 4)
				.Component("elec_ruined", 2);

			// Basic Evasion Booster
			_builder.Create(RecipeType.BasicEvasionBooster, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("eva_boost_b")
				.Level(8)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_tilarium", 5)
				.Component("elec_ruined", 3);

			// Basic Hull Booster
			_builder.Create(RecipeType.BasicHullBooster, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("hull_boost_b")
				.Level(4)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_tilarium", 3)
				.Component("elec_ruined", 2);

			// Basic Hull Repairer
			_builder.Create(RecipeType.BasicHullRepairer, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("hull_rep_b")
				.Level(3)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_tilarium", 2)
				.Component("elec_ruined", 1);

			// Basic Ion Cannon
			_builder.Create(RecipeType.BasicIonCannon, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("ion_cann_b")
				.Level(2)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_tilarium", 2)
				.Component("elec_ruined", 1);

			// Basic Mining Laser
			_builder.Create(RecipeType.BasicMiningLaser, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("min_laser_b")
				.Level(1)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_tilarium", 1)
				.Component("elec_ruined", 1);

			// Basic Missile Launcher
			_builder.Create(RecipeType.BasicMissileLauncher, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("msl_launch_b")
				.Level(4)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_tilarium", 3)
				.Component("elec_ruined", 2);

			// Basic Shield Booster
			_builder.Create(RecipeType.BasicShieldBooster, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("shld_boost_b")
				.Level(3)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_tilarium", 2)
				.Component("elec_ruined", 1);

			// Basic Shield Repairer
			_builder.Create(RecipeType.BasicShieldRepairer, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("shld_rep_b")
				.Level(8)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_tilarium", 5)
				.Component("elec_ruined", 3);

			// Basic Targeting System
			_builder.Create(RecipeType.BasicTargetingSystem, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("tgt_sys_b")
				.Level(9)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_tilarium", 5)
				.Component("elec_ruined", 3);

			// Basic Hypermatter Injector
			_builder.Create(RecipeType.FuelInjector1, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("cap_inject1")
				.Level(9)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_tilarium", 5)
				.Component("elec_ruined", 3);

			// Basic Mirrored Plating
			_builder.Create(RecipeType.BasicThermalArmor, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("las_armor_1")
				.Level(5)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_tilarium", 5)
				.Component("elec_ruined", 1);

            // Basic Supplemental Ion Shielding
            _builder.Create(RecipeType.BasicIonArmor, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("ion_armor_1")
				.Level(5)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_tilarium", 5)
				.Component("elec_ruined", 1);

            // Basic Reactive Armor
            _builder.Create(RecipeType.BasicExplosiveArmor, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("exp_armor_1")
				.Level(5)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_tilarium", 5)
				.Component("elec_ruined", 1);

            // Basic Durasteel Plating
            _builder.Create(RecipeType.BasicHeavyArmor, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("hvy_armor_1")
				.Level(5)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 1)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_tilarium", 5)
				.Component("elec_ruined", 1);

            // Basic Fighter Config
            _builder.Create(RecipeType.BasicFighterConfig, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("config_fig1")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Module, 1)
                .Component("ref_tilarium", 3)
                .Component("elec_ruined", 2);

            // Basic Bomber Config
            _builder.Create(RecipeType.BasicBomberConfig, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("config_bmb1")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Module, 1)
                .Component("ref_tilarium", 3)
                .Component("elec_ruined", 2);

            // Basic Interceptor Config
            _builder.Create(RecipeType.BasicInterceptorConfig, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("config_int1")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Module, 1)
                .Component("ref_tilarium", 3)
                .Component("elec_ruined", 2);

            // Basic Logistics Config
            _builder.Create(RecipeType.BasicLogisticsConfig, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("config_ind1")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Module, 1)
                .Component("ref_tilarium", 3)
                .Component("elec_ruined", 2);
        }

		private void Tier2()
        {

			//  Capacitor Booster I
			_builder.Create(RecipeType.CapacitorBooster1, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("cap_boost_1")
				.Level(15)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_currian", 3)
				.Component("elec_flawed", 2);

			//  Combat Laser I
			_builder.Create(RecipeType.CombatLaser1, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("com_laser_1")
				.Level(12)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_currian", 2)
				.Component("elec_flawed", 1);

            // Beam Cannon I
            _builder.Create(RecipeType.BeamCannon1, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("beamcannon2")
                .Level(12)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("ref_currian", 2)
                .Component("elec_flawed", 1);

            //  EM Amplifier I
            _builder.Create(RecipeType.EMAmplifier1, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("em_amp_1")
				.Level(17)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_currian", 4)
				.Component("elec_flawed", 2);

			//  Thermal Amplifier I
			_builder.Create(RecipeType.ThermalAmplifier1, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("therm_amp_1")
				.Level(16)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_currian", 4)
				.Component("elec_flawed", 2);

			//  Explosive Amplifier I
			_builder.Create(RecipeType.ExplosiveAmplifier1, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("exp_amp_1")
				.Level(16)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_currian", 4)
				.Component("elec_flawed", 2);

			//  Evasion Booster I
			_builder.Create(RecipeType.EvasionBooster1, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("eva_boost_1")
				.Level(18)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_currian", 5)
				.Component("elec_flawed", 3);

			//  Hull Booster I
			_builder.Create(RecipeType.HullBooster1, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("hull_boost_1")
				.Level(14)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_currian", 3)
				.Component("elec_flawed", 2);

			//  Hull Repairer I
			_builder.Create(RecipeType.HullRepairer1, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("hull_rep_1")
				.Level(13)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_currian", 2)
				.Component("elec_flawed", 1);

			//  Ion Cannon I
			_builder.Create(RecipeType.IonCannon1, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("ion_cann_1")
				.Level(12)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_currian", 2)
				.Component("elec_flawed", 1);

			//  Mining Laser I
			_builder.Create(RecipeType.MiningLaser1, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("min_laser_1")
				.Level(11)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_currian", 1)
				.Component("elec_flawed", 1);

			//  Missile Launcher I
			_builder.Create(RecipeType.MissileLauncher1, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("msl_launch_1")
				.Level(14)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_currian", 3)
				.Component("elec_flawed", 2);

			//  Shield Booster I
			_builder.Create(RecipeType.ShieldBooster1, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("shld_boost_1")
				.Level(13)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_currian", 2)
				.Component("elec_flawed", 1);

			//  Shield Repairer I
			_builder.Create(RecipeType.ShieldRepairer1, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("shld_rep_1")
				.Level(18)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_currian", 5)
				.Component("elec_flawed", 3);

			//  Targeting System I
			_builder.Create(RecipeType.TargetingSystem1, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("tgt_sys_1")
				.Level(19)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_currian", 5)
				.Component("elec_flawed", 3);
			
			// Hypermatter Injector I
			_builder.Create(RecipeType.FuelInjector2, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("cap_inject2")
				.Level(19)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Module, 1)
				.Component("ref_currian", 5)
				.Component("elec_flawed", 3);
			
			// Mirror Plating I
			_builder.Create(RecipeType.ThermalArmor1, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("las_armor_2")
				.Level(15)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_currian", 5)
				.Component("elec_flawed", 1);
			
			// Supplemental Ion Shielding I
			_builder.Create(RecipeType.IonArmor1, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("ion_armor_2")
				.Level(15)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_currian", 5)
				.Component("elec_flawed", 1);

			// Reactive Armor I
			_builder.Create(RecipeType.ExplosiveArmor1, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("exp_armor_2")
				.Level(15)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_currian", 5)
				.Component("elec_flawed", 1);

			// Durasteel Plating I
			_builder.Create(RecipeType.HeavyArmor1, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("hvy_armor_2")
				.Level(15)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 2)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_currian", 5)
				.Component("elec_flawed", 1);

            // Fighter Config I
            _builder.Create(RecipeType.FighterConfig1, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("config_fig2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("ref_currian", 3)
                .Component("elec_flawed", 2);

            // Bomber Config I
            _builder.Create(RecipeType.BomberConfig1, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("config_bmb2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("ref_currian", 3)
                .Component("elec_flawed", 2);

            // Interceptor Config I
            _builder.Create(RecipeType.InterceptorConfig1, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("config_int2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("ref_currian", 3)
                .Component("elec_flawed", 2);

            // Logistics Config I
            _builder.Create(RecipeType.LogisticsConfig1, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("config_ind2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("ref_currian", 3)
                .Component("elec_flawed", 2);
        }

		private void Tier3()
        {

			//  Capacitor Booster II
			_builder.Create(RecipeType.CapacitorBooster2, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("cap_boost_2")
				.Level(25)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_idailia", 3)
				.Component("elec_good", 2);

			//  Combat Laser II
			_builder.Create(RecipeType.CombatLaser2, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("com_laser_2")
				.Level(22)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_idailia", 2)
				.Component("elec_good", 1);

            // Beam Cannon II
            _builder.Create(RecipeType.BeamCannon2, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("beamcannon3")
                .Level(22)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("ref_idailia", 2)
                .Component("elec_good", 1);

            //  EM Amplifier II
            _builder.Create(RecipeType.EMAmplifier2, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("em_amp_2")
				.Level(27)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_idailia", 4)
				.Component("elec_good", 2);

			//  Thermal Amplifier II
			_builder.Create(RecipeType.ThermalAmplifier2, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("therm_amp_2")
				.Level(26)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_idailia", 4)
				.Component("elec_good", 2);

			//  Explosive Amplifier II
			_builder.Create(RecipeType.ExplosiveAmplifier2, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("exp_amp_2")
				.Level(26)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_idailia", 4)
				.Component("elec_good", 2);

			//  Evasion Booster II
			_builder.Create(RecipeType.EvasionBooster2, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("eva_boost_2")
				.Level(28)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_idailia", 5)
				.Component("elec_good", 3);

			//  Hull Booster II
			_builder.Create(RecipeType.HullBooster2, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("hull_boost_2")
				.Level(24)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_idailia", 3)
				.Component("elec_good", 2);

			//  Hull Repairer II
			_builder.Create(RecipeType.HullRepairer2, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("hull_rep_2")
				.Level(23)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_idailia", 2)
				.Component("elec_good", 1);

			//  Ion Cannon II
			_builder.Create(RecipeType.IonCannon2, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("ion_cann_2")
				.Level(22)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_idailia", 2)
				.Component("elec_good", 1);

			//  Mining Laser II
			_builder.Create(RecipeType.MiningLaser2, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("min_laser_2")
				.Level(21)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_idailia", 1)
				.Component("elec_good", 1);

			//  Missile Launcher II
			_builder.Create(RecipeType.MissileLauncher2, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("msl_launch_2")
				.Level(24)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_idailia", 3)
				.Component("elec_good", 2);

			//  Shield Booster II
			_builder.Create(RecipeType.ShieldBooster2, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("shld_boost_2")
				.Level(23)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_idailia", 2)
				.Component("elec_good", 1);

			//  Shield Repairer II
			_builder.Create(RecipeType.ShieldRepairer2, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("shld_rep_2")
				.Level(28)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_idailia", 5)
				.Component("elec_good", 3);

			//  Targeting System II
			_builder.Create(RecipeType.TargetingSystem2, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("tgt_sys_2")
				.Level(29)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_idailia", 5)
				.Component("elec_good", 3);

			// Hypermatter Injector II
			_builder.Create(RecipeType.FuelInjector2, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("cap_inject3")
				.Level(29)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_idailia", 5)
				.Component("elec_good", 3);

			// Mirrored Plating II
			_builder.Create(RecipeType.ThermalArmor2, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("las_armor_3")
				.Level(25)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_idailia", 5)
				.Component("elec_good", 1);

			// Supplemental Ion Shielding II
			_builder.Create(RecipeType.IonArmor2, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("ion_armor_3")
				.Level(25)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_idailia", 5)
				.Component("elec_good", 1);

			// Reactive Armor II
			_builder.Create(RecipeType.ExplosiveArmor2, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("exp_armor_3")
				.Level(25)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_idailia", 5)
				.Component("elec_good", 1);

			// Durasteel Plating II
			_builder.Create(RecipeType.HeavyArmor2, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("hvy_armor_3")
				.Level(25)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 3)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_idailia", 5)
				.Component("elec_good", 1);

            // Fighter Config II
            _builder.Create(RecipeType.FighterConfig2, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("config_fig3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("ref_idailia", 3)
                .Component("elec_good", 2);

            // Bomber Config II
            _builder.Create(RecipeType.BomberConfig2, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("config_bmb3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("ref_idailia", 3)
                .Component("elec_good", 2);

            // Interceptor Config II
            _builder.Create(RecipeType.InterceptorConfig2, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("config_int3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("ref_idailia", 3)
                .Component("elec_good", 2);

            // Logistics Config II
            _builder.Create(RecipeType.LogisticsConfig2, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("config_ind3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("ref_idailia", 3)
                .Component("elec_good", 2);
        }

		private void Tier4()
        {

			//  Capacitor Booster III
			_builder.Create(RecipeType.CapacitorBooster3, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("cap_boost_3")
				.Level(35)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_barinium", 3)
				.Component("elec_imperfect", 2);

			//  Combat Laser III
			_builder.Create(RecipeType.CombatLaser3, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("com_laser_3")
				.Level(32)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_barinium", 2)
				.Component("elec_imperfect", 1);

            //  Beam Cannon III
            _builder.Create(RecipeType.BeamCannon3, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("beamcannon4")
                .Level(32)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("ref_barinium", 2)
                .Component("elec_imperfect", 1);

            //  EM Amplifier III
            _builder.Create(RecipeType.EMAmplifier3, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("em_amp_3")
				.Level(37)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_barinium", 4)
				.Component("elec_imperfect", 2);

			//  Thermal Amplifier III
			_builder.Create(RecipeType.ThermalAmplifier3, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("therm_amp_3")
				.Level(36)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_barinium", 4)
				.Component("elec_imperfect", 2);

			//  Explosive Amplifier III
			_builder.Create(RecipeType.ExplosiveAmplifier3, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("exp_amp_3")
				.Level(36)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_barinium", 4)
				.Component("elec_imperfect", 2);

			//  Evasion Booster III
			_builder.Create(RecipeType.EvasionBooster3, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("eva_boost_3")
				.Level(38)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_barinium", 5)
				.Component("elec_imperfect", 3);

			//  Hull Booster III
			_builder.Create(RecipeType.HullBooster3, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("hull_boost_3")
				.Level(34)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_barinium", 3)
				.Component("elec_imperfect", 2);

			//  Hull Repairer III
			_builder.Create(RecipeType.HullRepairer3, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("hull_rep_3")
				.Level(33)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_barinium", 2)
				.Component("elec_imperfect", 1);

			//  Ion Cannon III
			_builder.Create(RecipeType.IonCannon3, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("ion_cann_3")
				.Level(32)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_barinium", 2)
				.Component("elec_imperfect", 1);

			//  Mining Laser III
			_builder.Create(RecipeType.MiningLaser3, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("min_laser_3")
				.Level(31)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_barinium", 1)
				.Component("elec_imperfect", 1);

			//  Missile Launcher III
			_builder.Create(RecipeType.MissileLauncher3, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("msl_launch_3")
				.Level(34)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_barinium", 3)
				.Component("elec_imperfect", 2);

			//  Shield Booster III
			_builder.Create(RecipeType.ShieldBooster3, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("shld_boost_3")
				.Level(33)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_barinium", 2)
				.Component("elec_imperfect", 1);

			//  Shield Repairer III
			_builder.Create(RecipeType.ShieldRepairer3, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("shld_rep_3")
				.Level(38)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_barinium", 5)
				.Component("elec_imperfect", 3);

			//  Targeting System III
			_builder.Create(RecipeType.TargetingSystem3, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("tgt_sys_3")
				.Level(39)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_barinium", 5)
				.Component("elec_imperfect", 3);

			// Hypermatter Injector III
			_builder.Create(RecipeType.FuelInjector3, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("cap_inject4")
				.Level(39)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_barinium", 5)
				.Component("elec_imperfect", 3);

			// Mirrored Plating III
			_builder.Create(RecipeType.ThermalArmor3, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("las_armor_4")
				.Level(35)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_barinium", 5)
				.Component("elec_imperfect", 1);

			// Supplemental Ion Shielding III
			_builder.Create(RecipeType.IonArmor3, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("ion_armor_4")
				.Level(35)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_barinium", 5)
				.Component("elec_imperfect", 1);

			// Reactive Armor III
			_builder.Create(RecipeType.ExplosiveArmor3, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("exp_armor_4")
				.Level(35)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_barinium", 5)
				.Component("elec_imperfect", 1);

			// Durasteel Plating III
			_builder.Create(RecipeType.HeavyArmor3, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("hvy_armor_4")
				.Level(35)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 4)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_barinium", 5)
				.Component("elec_imperfect", 1);

            // Fighter Config III
            _builder.Create(RecipeType.FighterConfig3, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("config_fig4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("ref_barinium", 3)
                .Component("elec_imperfect", 2);

            // Bomber Config III
            _builder.Create(RecipeType.BomberConfig3, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("config_bmb4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("ref_barinium", 3)
                .Component("elec_imperfect", 2);

            // Interceptor Config III
            _builder.Create(RecipeType.InterceptorConfig3, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("config_int4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("ref_barinium", 3)
                .Component("elec_imperfect", 2);

            // Logistics Config III
            _builder.Create(RecipeType.LogisticsConfig3, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("config_ind4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("ref_barinium", 3)
                .Component("elec_imperfect", 2);
        }

		private void Tier5()
        {

			//  Capacitor Booster IV
			_builder.Create(RecipeType.CapacitorBooster4, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("cap_boost_4")
				.Level(45)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_gostian", 3)
				.Component("elec_high", 2);

			//  Combat Laser IV
			_builder.Create(RecipeType.CombatLaser4, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("com_laser_4")
				.Level(42)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_gostian", 2)
				.Component("elec_high", 1);

            //  Beam Cannon IV
            _builder.Create(RecipeType.BeamCannon4, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("beamcannon5")
                .Level(42)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("ref_gostian", 2)
                .Component("elec_high", 1);

            //  EM Amplifier IV
            _builder.Create(RecipeType.EMAmplifier4, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("em_amp_4")
				.Level(47)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_gostian", 4)
				.Component("elec_high", 2);

			//  Thermal Amplifier IV
			_builder.Create(RecipeType.ThermalAmplifier4, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("therm_amp_4")
				.Level(46)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_gostian", 4)
				.Component("elec_high", 2);

			//  Explosive Amplifier IV
			_builder.Create(RecipeType.ExplosiveAmplifier4, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("exp_amp_4")
				.Level(46)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_gostian", 4)
				.Component("elec_high", 2);

			//  Evasion Booster IV
			_builder.Create(RecipeType.EvasionBooster4, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("eva_boost_4")
				.Level(48)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_gostian", 5)
				.Component("elec_high", 3);

			//  Hull Booster IV
			_builder.Create(RecipeType.HullBooster4, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("hull_boost_4")
				.Level(44)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_gostian", 3)
				.Component("elec_high", 2);

			//  Hull Repairer IV
			_builder.Create(RecipeType.HullRepairer4, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("hull_rep_4")
				.Level(43)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_gostian", 2)
				.Component("elec_high", 1);

			//  Ion Cannon IV
			_builder.Create(RecipeType.IonCannon4, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("ion_cann_4")
				.Level(42)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_gostian", 2)
				.Component("elec_high", 1);

			//  Mining Laser IV
			_builder.Create(RecipeType.MiningLaser4, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("min_laser_4")
				.Level(41)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_gostian", 1)
				.Component("elec_high", 1);

			//  Missile Launcher IV
			_builder.Create(RecipeType.MissileLauncher4, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("msl_launch_4")
				.Level(44)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_gostian", 3)
				.Component("elec_high", 2);

            //  Proton Bomb Launcher
            _builder.Create(RecipeType.ProtonBombLauncher, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("protbomblnch")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("ref_gostian", 3)
                .Component("elec_high", 2);

            //  Shield Booster IV
            _builder.Create(RecipeType.ShieldBooster4, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("shld_boost_4")
				.Level(43)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_gostian", 2)
				.Component("elec_high", 1);

			//  Shield Repairer IV
			_builder.Create(RecipeType.ShieldRepairer4, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("shld_rep_4")
				.Level(48)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_gostian", 5)
				.Component("elec_high", 3);

			//  Targeting System IV
			_builder.Create(RecipeType.TargetingSystem4, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("tgt_sys_4")
				.Level(49)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_gostian", 5)
				.Component("elec_high", 3);

			// Hypermatter Injector IV
			_builder.Create(RecipeType.FuelInjector4, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("cap_inject5")
				.Level(49)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_gostian", 5)
				.Component("elec_high", 3);

			// Mirrored Plating IV
			_builder.Create(RecipeType.ThermalArmor4, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("las_armor_5")
				.Level(45)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_gostian", 5)
				.Component("elec_high", 1);

			// Supplemental Ion Shielding IV
			_builder.Create(RecipeType.IonArmor4, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("ion_armor_5")
				.Level(45)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_gostian", 5)
				.Component("elec_high", 1);

			// Reactive Armor IV
			_builder.Create(RecipeType.ExplosiveArmor4, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("exp_armor_5")
				.Level(45)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_gostian", 5)
				.Component("elec_high", 1);

			// Durasteel Plating IV
			_builder.Create(RecipeType.HeavyArmor4, SkillType.Engineering)
				.Category(RecipeCategoryType.ShipModule)
				.Resref("hvy_armor_5")
				.Level(45)
				.Quantity(1)
				.RequirementPerk(PerkType.StarshipBlueprints, 5)
				.EnhancementSlots(RecipeEnhancementType.Module, 2)
				.Component("ref_gostian", 5)
				.Component("elec_high", 1);

            // Fighter Config IV
            _builder.Create(RecipeType.FighterConfig4, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("config_fig5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("ref_gostian", 3)
                .Component("elec_high", 2);

            // Bomber Config IV
            _builder.Create(RecipeType.BomberConfig4, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("config_bmb5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("ref_gostian", 3)
                .Component("elec_high", 2);

            // Interceptor Config IV
            _builder.Create(RecipeType.InterceptorConfig4, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("config_int5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("ref_gostian", 3)
                .Component("elec_high", 2);

            // Logistics Config IV
            _builder.Create(RecipeType.LogisticsConfig4, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("config_ind5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("ref_gostian", 3)
                .Component("elec_high", 2);

            // Assault Concussion Missile Launcher I
            _builder.Create(RecipeType.AssaultConcMissileLauncher, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("acm_launch_1")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("capc_techcom", 1)
                .Component("capc_modcomp", 1)
				.Component("capc_power", 3);

            // Assault Concussion Missile
            _builder.Create(RecipeType.AssaultConcMissile, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("acm_ammo")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .Component("elec_high", 3)
                .Component("ref_arda", 3)
                .Component("ref_gostian", 5);

            // Advanced Thrusters
            _builder.Create(RecipeType.AdvancedThrusters, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("cap_thrust1")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("capc_techcom", 1)
                .Component("capc_modcomp", 1)
                .Component("capc_power", 3);

            // Bulwark Shield Generator
            _builder.Create(RecipeType.BulwarkShieldGenerator, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("bulwarkgen")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("capc_techcom", 1)
                .Component("capc_modcomp", 1)
                .Component("capc_power", 3);

            // Dedicated Targeting Sensor Array
            _builder.Create(RecipeType.TargetingArray, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("cap_target1")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("capc_techcom", 1)
                .Component("capc_modcomp", 1)
                .Component("capc_power", 3);

            // Heavy Turbolaser Cannon
            _builder.Create(RecipeType.Turbolaser1, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("turbolas1")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("capc_techcom", 1)
                .Component("capc_modcomp", 1)
                .Component("capc_power", 3);

            // Dual Turbolaser Cannon
            _builder.Create(RecipeType.Turbolaser2, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("turbolas2")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("capc_techcom", 1)
                .Component("capc_modcomp", 1)
                .Component("capc_power", 3);

            // Quad Turbolaser Cannon
            _builder.Create(RecipeType.Turbolaser3, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("turbolas3")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("capc_techcom", 1)
                .Component("capc_modcomp", 1)
                .Component("capc_power", 3);

            // Laser Cannon Battery I
            _builder.Create(RecipeType.LaserCannonBattery1, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("lasbattery1")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("capc_techcom", 1)
                .Component("capc_modcomp", 1)
                .Component("capc_power", 3);

            // Quad Laser Cannons
            _builder.Create(RecipeType.QuadLaserCannon1, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("cap_quadlas1")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("capc_techcom", 1)
                .Component("capc_modcomp", 1)
                .Component("capc_power", 3);

            // Quad Laser Cannon Array
            _builder.Create(RecipeType.QuadLaserCannon2, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("cap_quadlas2")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("capc_techcom", 1)
                .Component("capc_modcomp", 1)
                .Component("capc_power", 3);

            // Quad Laser Cannon Battery
            _builder.Create(RecipeType.QuadLaserCannon3, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("cap_quadlas3")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("capc_techcom", 1)
                .Component("capc_modcomp", 1)
                .Component("capc_power", 3);

            // Redundant Shield Generator
            _builder.Create(RecipeType.RedundantShieldGenerator, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("cap_shields1")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("capc_techcom", 1)
                .Component("capc_modcomp", 1)
                .Component("capc_power", 3);

            // Reinforced Plating
            _builder.Create(RecipeType.ReinforcedPlating, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("cap_armor1")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("capc_techcom", 1)
                .Component("capc_modcomp", 1)
                .Component("capc_power", 3);

            // Repair Field Generator
            _builder.Create(RecipeType.RepairFieldGenerator, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("repairfield")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("capc_techcom", 1)
                .Component("capc_modcomp", 1)
                .Component("capc_power", 3);

            // Storm Cannon
            _builder.Create(RecipeType.StormCannon, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("storm_cann")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("capc_techcom", 1)
                .Component("capc_modcomp", 1)
                .Component("capc_power", 3);

            // Logistics Nexus Configuration
            _builder.Create(RecipeType.LogisticsNexusConfig, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("cap_indus")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("capc_techcom", 1)
                .Component("capc_modcomp", 1)
                .Component("capc_power", 3);

            // Skirmisher Configuration
            _builder.Create(RecipeType.SkirmisherConfig, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("cap_skirm")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("capc_techcom", 1)
                .Component("capc_modcomp", 1)
                .Component("capc_power", 3);

            // Warship Configuration
            _builder.Create(RecipeType.WarshipConfig1, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("cap_warship")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("capc_techcom", 1)
                .Component("capc_modcomp", 1)
                .Component("capc_power", 3);

            // Strip Miner Configuration
            _builder.Create(RecipeType.StripMiner, SkillType.Engineering)
                .Category(RecipeCategoryType.ShipModule)
                .Resref("cap_minlas")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("capc_techcom", 1)
                .Component("capc_modcomp", 1)
                .Component("capc_power", 3);
        }
	}
}
