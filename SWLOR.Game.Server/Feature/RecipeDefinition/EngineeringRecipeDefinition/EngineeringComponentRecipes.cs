using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    public class EngineeringComponentRecipes: IRecipeListDefinition
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
            // Control Unit Alpha
            _builder.Create(RecipeType.ControlUnitAlpha, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("c_unit1")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("aluminum", 5)
                .Component("elec_ruined", 2)
                .Component("jade", 4)
                .Component("quadrenium", 4);

            // Logic Unit Alpha
            _builder.Create(RecipeType.LogicUnitAlpha, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("l_unit1")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("aluminum", 5)
                .Component("elec_ruined", 2)
                .Component("jade", 4)
                .Component("quadrenium", 4);

            // Caching Unit Alpha
            _builder.Create(RecipeType.CachingUnitAlpha, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("ca_unit1")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("aluminum", 5)
                .Component("elec_ruined", 2)
                .Component("jade", 4)
                .Component("quadrenium", 4);

            // Droid Brain Alpha
            _builder.Create(RecipeType.DroidBrainAlpha, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("d_brain1")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("aluminum", 5)
                .Component("elec_ruined", 2)
                .Component("jade", 4)
                .Component("quadrenium", 4);

            // Droid Sensor Alpha
            _builder.Create(RecipeType.DroidSensorAlpha, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("d_sensor1")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("aluminum", 5)
                .Component("elec_ruined", 2)
                .Component("jade", 4)
                .Component("quadrenium", 4);

            // Manipulator Arm Alpha
            _builder.Create(RecipeType.ManipulatorArmAlpha, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("manip_arm1")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("aluminum", 5)
                .Component("elec_ruined", 2)
                .Component("jade", 4)
                .Component("quadrenium", 4);

            // Discharge Unit Alpha
            _builder.Create(RecipeType.DischargeUnitAlpha, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("discharge_unit1")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("aluminum", 5)
                .Component("elec_ruined", 2)
                .Component("jade", 4)
                .Component("quadrenium", 4);

            // Droid Motive System Alpha
            _builder.Create(RecipeType.DroidMotiveSystemAlpha, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("dmotive_sys1")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("aluminum", 5)
                .Component("elec_ruined", 2)
                .Component("jade", 4)
                .Component("quadrenium", 4);

            // Diagnostic Circuit Alpha
            _builder.Create(RecipeType.DiagnosticCircuitAlpha, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("diag_circuit1")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("aluminum", 5)
                .Component("elec_ruined", 2)
                .Component("jade", 4)
                .Component("quadrenium", 4);

            // Droid Power Supply Alpha
            _builder.Create(RecipeType.DroidPowerSupplyAlpha, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("dp_supply1")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("aluminum", 5)
                .Component("elec_ruined", 2)
                .Component("jade", 4)
                .Component("quadrenium", 4);

            // Droid Chassis Alpha
            _builder.Create(RecipeType.DroidChassisAlpha, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("d_chassis1")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("aluminum", 5)
                .Component("elec_ruined", 2)
                .Component("jade", 4)
                .Component("quadrenium", 4);

        }

        private void Tier2()
        {
            // Control Unit Beta
            _builder.Create(RecipeType.ControlUnitBeta, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("c_unit2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("steel", 5)
                .Component("elec_flawed", 2)
                .Component("agate", 4)
                .Component("vintrium", 4);

            // Logic Unit Beta
            _builder.Create(RecipeType.LogicUnitBeta, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("l_unit2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("steel", 5)
                .Component("elec_flawed", 2)
                .Component("agate", 4)
                .Component("vintrium", 4);

            // Caching Unit Beta
            _builder.Create(RecipeType.CachingUnitBeta, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("ca_unit2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("steel", 5)
                .Component("elec_flawed", 2)
                .Component("agate", 4)
                .Component("vintrium", 4);

            // Droid Brain Beta
            _builder.Create(RecipeType.DroidBrainBeta, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("d_brain2")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("steel", 5)
                .Component("elec_flawed", 2)
                .Component("agate", 4)
                .Component("vintrium", 4);

            // Droid Sensor Beta
            _builder.Create(RecipeType.DroidSensorBeta, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("d_sensor2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("steel", 5)
                .Component("elec_flawed", 2)
                .Component("agate", 4)
                .Component("vintrium", 4);

            // Manipulator Arm Beta
            _builder.Create(RecipeType.ManipulatorArmBeta, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("manip_arm2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("steel", 5)
                .Component("elec_flawed", 2)
                .Component("agate", 4)
                .Component("vintrium", 4);

            // Discharge Unit Beta
            _builder.Create(RecipeType.DischargeUnitBeta, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("discharge_unit2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("steel", 5)
                .Component("elec_flawed", 2)
                .Component("agate", 4)
                .Component("vintrium", 4);

            // Droid Motive System Beta
            _builder.Create(RecipeType.DroidMotiveSystemBeta, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("dmotive_sys2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("steel", 5)
                .Component("elec_flawed", 2)
                .Component("agate", 4)
                .Component("vintrium", 4);

            // Diagnostic Circuit Beta
            _builder.Create(RecipeType.DiagnosticCircuitBeta, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("diag_circuit2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("steel", 5)
                .Component("elec_flawed", 2)
                .Component("agate", 4)
                .Component("vintrium", 4);

            // Droid Power Supply Beta
            _builder.Create(RecipeType.DroidPowerSupplyBeta, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("dp_supply2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("steel", 5)
                .Component("elec_flawed", 2)
                .Component("agate", 4)
                .Component("vintrium", 4);

            // Droid Chassis Beta
            _builder.Create(RecipeType.DroidChassisBeta, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("d_chassis2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("steel", 5)
                .Component("elec_flawed", 2)
                .Component("agate", 4)
                .Component("vintrium", 4);
        }

        private void Tier3()
        {
            // Control Unit Gamma
            _builder.Create(RecipeType.ControlUnitGamma, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("c_unit3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("obsidian", 5)
                .Component("elec_good", 2)
                .Component("citrine", 4)
                .Component("ionite", 4);

            // Logic Unit Gamma
            _builder.Create(RecipeType.LogicUnitGamma, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("l_unit3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("obsidian", 5)
                .Component("elec_good", 2)
                .Component("citrine", 4)
                .Component("ionite", 4);

            // Caching Unit Gamma
            _builder.Create(RecipeType.CachingUnitGamma, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("ca_unit3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("obsidian", 5)
                .Component("elec_good", 2)
                .Component("citrine", 4)
                .Component("ionite", 4);

            // Droid Brain Gamma
            _builder.Create(RecipeType.DroidBrainGamma, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("d_brain3")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("obsidian", 5)
                .Component("elec_good", 2)
                .Component("citrine", 4)
                .Component("ionite", 4);

            // Droid Sensor Gamma
            _builder.Create(RecipeType.DroidSensorGamma, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("d_sensor3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("obsidian", 5)
                .Component("elec_good", 2)
                .Component("citrine", 4)
                .Component("ionite", 4);

            // Manipulator Arm Gamma
            _builder.Create(RecipeType.ManipulatorArmGamma, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("manip_arm3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("obsidian", 5)
                .Component("elec_good", 2)
                .Component("citrine", 4)
                .Component("ionite", 4);

            // Discharge Unit Gamma
            _builder.Create(RecipeType.DischargeUnitGamma, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("discharge_unit3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("obsidian", 5)
                .Component("elec_good", 2)
                .Component("citrine", 4)
                .Component("ionite", 4);

            // Droid Motive System Gamma
            _builder.Create(RecipeType.DroidMotiveSystemGamma, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("dmotive_sys3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("obsidian", 5)
                .Component("elec_good", 2)
                .Component("citrine", 4)
                .Component("ionite", 4);

            // Diagnostic Circuit Gamma
            _builder.Create(RecipeType.DiagnosticCircuitGamma, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("diag_circuit3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("obsidian", 5)
                .Component("elec_good", 2)
                .Component("citrine", 4)
                .Component("ionite", 4);

            // Droid Power Supply Gamma
            _builder.Create(RecipeType.DroidPowerSupplyGamma, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("dp_supply3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("obsidian", 5)
                .Component("elec_good", 2)
                .Component("citrine", 4)
                .Component("ionite", 4);

            // Droid Chassis Gamma
            _builder.Create(RecipeType.DroidChassisGamma, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("d_chassis3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .Component("obsidian", 5)
                .Component("elec_good", 2)
                .Component("citrine", 4)
                .Component("ionite", 4);
        }

        private void Tier4()
        {
            // Control Unit Delta
            _builder.Create(RecipeType.ControlUnitDelta, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("c_unit4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("crystal", 5)
                .Component("elec_imperfect", 2)
                .Component("ruby", 4)
                .Component("katrium", 4);

            // Logic Unit Delta
            _builder.Create(RecipeType.LogicUnitDelta, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("l_unit4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("crystal", 5)
                .Component("elec_imperfect", 2)
                .Component("ruby", 4)
                .Component("katrium", 4);

            // Caching Unit Delta
            _builder.Create(RecipeType.CachingUnitDelta, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("ca_unit4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("crystal", 5)
                .Component("elec_imperfect", 2)
                .Component("ruby", 4)
                .Component("katrium", 4);

            // Droid Brain Delta
            _builder.Create(RecipeType.DroidBrainDelta, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("d_brain4")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("crystal", 5)
                .Component("elec_imperfect", 2)
                .Component("ruby", 4)
                .Component("katrium", 4);

            // Droid Sensor Delta
            _builder.Create(RecipeType.DroidSensorDelta, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("d_sensor4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("crystal", 5)
                .Component("elec_imperfect", 2)
                .Component("ruby", 4)
                .Component("katrium", 4);

            // Manipulator Arm Delta
            _builder.Create(RecipeType.ManipulatorArmDelta, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("manip_arm4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("crystal", 5)
                .Component("elec_imperfect", 2)
                .Component("ruby", 4)
                .Component("katrium", 4);

            // Discharge Unit Delta
            _builder.Create(RecipeType.DischargeUnitDelta, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("discharge_unit4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("crystal", 5)
                .Component("elec_imperfect", 2)
                .Component("ruby", 4)
                .Component("katrium", 4);

            // Droid Motive System Delta
            _builder.Create(RecipeType.DroidMotiveSystemDelta, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("dmotive_sys4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("crystal", 5)
                .Component("elec_imperfect", 2)
                .Component("ruby", 4)
                .Component("katrium", 4);

            // Diagnostic Circuit Delta
            _builder.Create(RecipeType.DiagnosticCircuitDelta, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("diag_circuit4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("crystal", 5)
                .Component("elec_imperfect", 2)
                .Component("ruby", 4)
                .Component("katrium", 4);

            // Droid Power Supply Delta
            _builder.Create(RecipeType.DroidPowerSupplyDelta, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("dp_supply4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("crystal", 5)
                .Component("elec_imperfect", 2)
                .Component("ruby", 4)
                .Component("katrium", 4);

            // Droid Chassis Delta
            _builder.Create(RecipeType.DroidChassisDelta, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("d_chassis4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .Component("crystal", 5)
                .Component("elec_imperfect", 2)
                .Component("ruby", 4)
                .Component("katrium", 4);
        }

        private void Tier5()
        {
            // Control Unit Epsilon
            _builder.Create(RecipeType.ControlUnitEpsilon, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("c_unit5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("diamond", 5)
                .Component("elec_high", 2)
                .Component("emerald", 4)
                .Component("zinsiam", 4);

            // Logic Unit Epsilon
            _builder.Create(RecipeType.LogicUnitEpsilon, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("l_unit5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("diamond", 5)
                .Component("elec_high", 2)
                .Component("emerald", 4)
                .Component("zinsiam", 4);

            // Caching Unit Epsilon
            _builder.Create(RecipeType.CachingUnitEpsilon, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("ca_unit5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("diamond", 5)
                .Component("elec_high", 2)
                .Component("emerald", 4)
                .Component("zinsiam", 4);

            // Droid Brain Epsilon
            _builder.Create(RecipeType.DroidBrainEpsilon, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("d_brain5")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("diamond", 5)
                .Component("elec_high", 2)
                .Component("emerald", 4)
                .Component("zinsiam", 4);

            // Droid Sensor Epsilon
            _builder.Create(RecipeType.DroidSensorEpsilon, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("d_sensor5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("diamond", 5)
                .Component("elec_high", 2)
                .Component("emerald", 4)
                .Component("zinsiam", 4);

            // Manipulator Arm Epsilon
            _builder.Create(RecipeType.ManipulatorArmEpsilon, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("manip_arm5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("diamond", 5)
                .Component("elec_high", 2)
                .Component("emerald", 4)
                .Component("zinsiam", 4);

            // Discharge Unit Epsilon
            _builder.Create(RecipeType.DischargeUnitEpsilon, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("discharge_unit5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("diamond", 5)
                .Component("elec_high", 2)
                .Component("emerald", 4)
                .Component("zinsiam", 4);

            // Droid Motive System Epsilon
            _builder.Create(RecipeType.DroidMotiveSystemEpsilon, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("dmotive_sys5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("diamond", 5)
                .Component("elec_high", 2)
                .Component("emerald", 4)
                .Component("zinsiam", 4);

            // Diagnostic Circuit Epsilon
            _builder.Create(RecipeType.DiagnosticCircuitEpsilon, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("diag_circuit5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("diamond", 5)
                .Component("elec_high", 2)
                .Component("emerald", 4)
                .Component("zinsiam", 4);

            // Droid Power Supply Epsilon
            _builder.Create(RecipeType.DroidPowerSupplyEpsilon, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("dp_supply5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("diamond", 5)
                .Component("elec_high", 2)
                .Component("emerald", 4)
                .Component("zinsiam", 4);

            // Droid Chassis Epsilon
            _builder.Create(RecipeType.DroidChassisEpsilon, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidComponent)
                .Resref("d_chassis5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .Component("diamond", 5)
                .Component("elec_high", 2)
                .Component("emerald", 4)
                .Component("zinsiam", 4);
        }
    }
}
