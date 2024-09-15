﻿using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    public class StarshipRecipes: IRecipeListDefinition
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
            // Striker
            _builder.Create(RecipeType.Striker, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_striker")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1);

            // Condor
            _builder.Create(RecipeType.Condor, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_condor")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3);
        }

        private void Tier2()
        {
            // Hound
            _builder.Create(RecipeType.Hound, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_hound")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1);

            // Panther
            _builder.Create(RecipeType.Panther, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_panther")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3);
        }

        private void Tier3()
        {
            // Saber
            _builder.Create(RecipeType.Saber, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_saber")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1);

            // Falchion
            _builder.Create(RecipeType.Falchion, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_falchion")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3);
        }

        private void Tier4()
        {
            // Mule
            _builder.Create(RecipeType.Mule, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_mule")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1);

            // Merchant
            _builder.Create(RecipeType.Merchant, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_merchant")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3);
        }

        private void Tier5()
        {
            // Throne
            _builder.Create(RecipeType.Throne, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_throne")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1);

            // Consular
            _builder.Create(RecipeType.Consular, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_consular")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3);

            // Cutlass
            _builder.Create(RecipeType.Cutlass, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_cutla")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3);

            // Basilisk
            _builder.Create(RecipeType.BasiliskWarDroid, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_basi")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3);

            // Aurek Strikefighter
            _builder.Create(RecipeType.AurekStrikefighter, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_aurek")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3);

            // Sith Fighter
            _builder.Create(RecipeType.SithFighter, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_sfight")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3);

            // Republic Thranta
            _builder.Create(RecipeType.CorvetteRepThranta, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("capdeed_rthran")
                .Level(53)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("capc_corhull", 1)
                .Component("capc_powsys", 1)
                .Component("capc_eng", 1);

            // Sith Thranta
            _builder.Create(RecipeType.CorvetteSithThranta, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("capdeed_sthran")
                .Level(53)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("capc_corhull", 1)
                .Component("capc_powsys", 1)
                .Component("capc_eng", 1);

            // Neutral Thranta
            _builder.Create(RecipeType.CorvetteNeutThranta, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("capdeed_nthran")
                .Level(53)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("capc_corhull", 1)
                .Component("capc_powsys", 1)
                .Component("capc_eng", 1);

            // Terminus Corsair
            _builder.Create(RecipeType.CorvetteTerminus, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("capdeed_corsa")
                .Level(53)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("capc_corhull", 1)
                .Component("capc_powsys", 1)
                .Component("capc_eng", 1);

            // Hutt Corvette
            _builder.Create(RecipeType.CorvetteHutt, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("capdeed_huttco")
                .Level(53)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("capc_corhull", 1)
                .Component("capc_powsys", 1)
                .Component("capc_eng", 1);

            // CZC Armored Transport
            _builder.Create(RecipeType.CorvetteArmoredTransport, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("capdeed_hvycor")
                .Level(53)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("capc_corhull", 1)
                .Component("capc_powsys", 1)
                .Component("capc_eng", 1);

            // Chiss Trireme
            _builder.Create(RecipeType.CorvetteChissTrireme, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("capdeed_chisst")
                .Level(53)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("capc_corhull", 1)
                .Component("capc_powsys", 1)
                .Component("capc_eng", 1);

            // Corellian Gunboat
            _builder.Create(RecipeType.CorvetteCorellian, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("capdeed_cgunb")
                .Level(53)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("capc_corhull", 1)
                .Component("capc_powsys", 1)
                .Component("capc_eng", 1);

            // JehaveyFrigate
            _builder.Create(RecipeType.CorvetteJehaveyFrigate, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("capdeed_jfrigate")
                .Level(53)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("capc_corhull", 1)
                .Component("capc_powsys", 1)
                .Component("capc_eng", 1);
        }
    }
}
