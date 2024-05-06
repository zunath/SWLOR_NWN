using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    public class CapitalConstructionRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            CapitalShipComponents();

            return _builder.Build();
        }

        private void CapitalShipComponents()
        {
            // Capital Class Hull: Corvette
            _builder.Create(RecipeType.CorvetteHull, SkillType.Engineering)
                .Category(RecipeCategoryType.CapitalConstruction)
                .Resref("capc_corhull")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .Component("capc_struc", 10)
                .Component("capc_shgen", 2);

            // Capital Class Structural Components
            _builder.Create(RecipeType.CapitalConstruction, SkillType.Engineering)
                .Category(RecipeCategoryType.CapitalConstruction)
                .Resref("capc_struc")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .Component("zinsiam", 5)
                .Component("raw_arkoxit", 10)
                .Component("ref_arda", 5)
                .Component("ref_gostian", 25)
                .Component("ref_jasioclase", 25);

            // Capital Class Power Relay
            _builder.Create(RecipeType.CapitalPowerRelay, SkillType.Engineering)
                .Category(RecipeCategoryType.CapitalConstruction)
                .Resref("capc_power")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .Component("elec_high", 5)
                .Component("ref_gostian", 5)
                .Component("zinsiam", 2);

            // Capital Class Power System: Corvette
            _builder.Create(RecipeType.CorvettePowerSystem, SkillType.Engineering)
                .Category(RecipeCategoryType.CapitalConstruction)
                .Resref("capc_powsys")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .Component("capc_rebay", 1)
                .Component("capc_react", 2)
                .Component("capc_power", 20);

            // Capital Class Reactor Bay: Corvette
            _builder.Create(RecipeType.CorvetteReactorBay, SkillType.Engineering)
                .Category(RecipeCategoryType.CapitalConstruction)
                .Resref("capc_rebay")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .Component("ref_jasioclase", 75)
                .Component("ref_gostian", 75)
                .Component("elec_high", 80)
                .Component("ref_arda", 20);

            // Capital Class Reactor
            _builder.Create(RecipeType.CapitalReactor, SkillType.Engineering)
                .Category(RecipeCategoryType.CapitalConstruction)
                .Resref("capc_react")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .Component("ref_gostian", 80)
                .Component("ref_jasioclase", 80)
                .Component("zinsiam", 20)
                .Component("elec_high", 50);

            // Capital Class Engines: Corvette
            _builder.Create(RecipeType.CorvetteEngines, SkillType.Engineering)
                .Category(RecipeCategoryType.CapitalConstruction)
                .Resref("capc_eng")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .Component("capc_thrust", 10);

            // Capital Class Thrusters
            _builder.Create(RecipeType.CapitalThruster, SkillType.Engineering)
                .Category(RecipeCategoryType.CapitalConstruction)
                .Resref("capc_thrust")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .Component("elec_high", 25)
                .Component("ref_gostian", 50)
                .Component("zinsiam", 5);

            // Capital Class System Dedicated Computer
            _builder.Create(RecipeType.CapitalModuleComputer, SkillType.Engineering)
                .Category(RecipeCategoryType.CapitalConstruction)
                .Resref("capc_modcomp")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .Component("ref_arda", 1)
                .Component("elec_high", 25);

            // Capital Class Tech Components
            _builder.Create(RecipeType.CapitalTechComponents, SkillType.Engineering)
                .Category(RecipeCategoryType.CapitalConstruction)
                .Resref("capc_techcom")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .Component("ref_arda", 3)
                .Component("thor_crys", 1);

            // Capital Class Shield Generator
            _builder.Create(RecipeType.CapitalShieldGenerator, SkillType.Engineering)
                .Category(RecipeCategoryType.CapitalConstruction)
                .Resref("capc_shgen")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .Component("zinsiam", 5)
                .Component("elec_high", 50)
                .Component("ref_gostian", 50)
                .Component("ref_jasioclase", 25)
                .Component("thor_crys", 10);
        }
    }
}