using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    public class CapitalModuleRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Tier5();

            return _builder.Build();
        }

        private void Tier5()
        {
            // Advanced Thrusters
            _builder.Create(RecipeType.AdvancedThrusters, SkillType.Engineering)
                .Category(RecipeCategoryType.CapitalShipModule)
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
                .Category(RecipeCategoryType.CapitalShipModule)
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
                .Category(RecipeCategoryType.CapitalShipModule)
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
                .Category(RecipeCategoryType.CapitalShipModule)
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
                .Category(RecipeCategoryType.CapitalShipModule)
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
                .Category(RecipeCategoryType.CapitalShipModule)
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
                .Category(RecipeCategoryType.CapitalShipModule)
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
                .Category(RecipeCategoryType.CapitalShipModule)
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
                .Category(RecipeCategoryType.CapitalShipModule)
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
                .Category(RecipeCategoryType.CapitalShipModule)
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
                .Category(RecipeCategoryType.CapitalShipModule)
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
                .Category(RecipeCategoryType.CapitalShipModule)
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
                .Category(RecipeCategoryType.CapitalShipModule)
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
                .Category(RecipeCategoryType.CapitalShipModule)
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
                .Category(RecipeCategoryType.CapitalShipModule)
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
                .Category(RecipeCategoryType.CapitalShipModule)
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
                .Category(RecipeCategoryType.CapitalShipModule)
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
                .Category(RecipeCategoryType.CapitalShipModule)
                .Resref("cap_minlas")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("capc_techcom", 1)
                .Component("capc_modcomp", 1)
                .Component("capc_power", 3);

            // Weapons Computer
            _builder.Create(RecipeType.WeaponsComputer, SkillType.Engineering)
                .Category(RecipeCategoryType.CapitalShipModule)
                .Resref("cap_wcomp1")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("capc_techcom", 1)
                .Component("capc_modcomp", 1)
                .Component("capc_power", 3);

            // Capital E-War Module
            _builder.Create(RecipeType.CapitalEWar, SkillType.Engineering)
                .Category(RecipeCategoryType.CapitalShipModule)
                .Resref("cap_ewar")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("capc_techcom", 1)
                .Component("capc_modcomp", 1)
                .Component("capc_power", 3);

            // Capital E-War Module
            _builder.Create(RecipeType.CapitalPowerDiverter, SkillType.Engineering)
                .Category(RecipeCategoryType.CapitalShipModule)
                .Resref("cap_pwdiv")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Module, 2)
                .Component("capc_techcom", 1)
                .Component("capc_modcomp", 1)
                .Component("capc_power", 3);

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
        }
    }
}