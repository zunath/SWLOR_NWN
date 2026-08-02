using System.Collections.Generic;
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

            // Hound
            _builder.Create(RecipeType.Hound, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_hound")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1);

            // Panther
            _builder.Create(RecipeType.Panther, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_panther")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1);

            // Saber
            _builder.Create(RecipeType.Saber, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_saber")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3);

            // Falchion
            _builder.Create(RecipeType.Falchion, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_falchion")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1);

            // Mule
            _builder.Create(RecipeType.Mule, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_mule")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3);

            // Merchant
            _builder.Create(RecipeType.Merchant, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_merchant")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1);

            // Throne
            _builder.Create(RecipeType.Throne, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_throne")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1);

            // Consular
            _builder.Create(RecipeType.Consular, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_consular")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3);

            // Cutlass
            _builder.Create(RecipeType.Cutlass, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_cutla")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1);

            // Neutral Striker I
            _builder.Create(RecipeType.NeutralStrikerTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_nstrike")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Armored Transport I
            _builder.Create(RecipeType.ArmtransTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_armtrans")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Bretonia Freighter I
            _builder.Create(RecipeType.BretoniaFrtTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_bretfrt")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Civilian Elite Fighter I
            _builder.Create(RecipeType.CivEliteFtrTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_civelftr")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Civilian Fighter I
            _builder.Create(RecipeType.CivFtrTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_civftr")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Corsair Mk2 I
            _builder.Create(RecipeType.CorsairMk2Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_corsmk2")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Corsair I
            _builder.Create(RecipeType.CorsairTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_corsair")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // S-100 Stinger Starfighter I
            _builder.Create(RecipeType.S100StingerTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_s100stg")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Hutt Bomber I
            _builder.Create(RecipeType.HuttBomberTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_huttbomb")
                .Level(5)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Hutt Fighter I
            _builder.Create(RecipeType.HuttFtrTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_huttftr")
                .Level(5)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Hutt Gunship I
            _builder.Create(RecipeType.HuttGunTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_huttgun")
                .Level(10)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Invader I
            _builder.Create(RecipeType.InvaderTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_invader")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Hunter I
            _builder.Create(RecipeType.HunterTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_hunter")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Jedi Transport I
            _builder.Create(RecipeType.JediTransTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_jeditrn")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Kusari Mk2 I
            _builder.Create(RecipeType.KusariMk2Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_kusarmk2")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Kusari I
            _builder.Create(RecipeType.KusariTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_kusari")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Kusari Freighter I
            _builder.Create(RecipeType.KusariFrtTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_kusarfrt")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Liberty Mk2 I
            _builder.Create(RecipeType.LibertyMk2Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_libmk2")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Liberty I
            _builder.Create(RecipeType.LibertyTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_liberty")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Mandalorian Brute Patrol Ship I
            _builder.Create(RecipeType.MandoBruteTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_mdobrute")
                .Level(5)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Davaab-type Starfighter I
            _builder.Create(RecipeType.DavaabTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_davaab")
                .Level(10)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Teroch-type Gunship I
            _builder.Create(RecipeType.TerochTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_teroch")
                .Level(5)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Neutral Barracuda I
            _builder.Create(RecipeType.BarracudaTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_barracud")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Civilian BW Fighter I
            _builder.Create(RecipeType.CivBwFtrTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_civbwftr")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Civilian Condor I
            _builder.Create(RecipeType.CivCondorTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_civcondr")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Civilian Freighter I
            _builder.Create(RecipeType.CivFrtTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_civfrt")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // ST-07 Assault Ship I
            _builder.Create(RecipeType.St07AssaultTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_st07aslt")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Neutral Quartermaster Transport I
            _builder.Create(RecipeType.QmTransTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_qmtrans")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Starflier I
            _builder.Create(RecipeType.StarflierTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_starflir")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // YV-929 Hauler I
            _builder.Create(RecipeType.Yv929Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_yv929")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Onderon Ruping Bomber I
            _builder.Create(RecipeType.OnderonBombTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_ondrbomb")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Onderon Type81a Fighter I
            _builder.Create(RecipeType.OnderonFtrTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_ondrftr")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Order Fighter I
            _builder.Create(RecipeType.OrderFtrTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_orderftr")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Phoebos I
            _builder.Create(RecipeType.PhoebosTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_phoebos")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Pirate Fighter I
            _builder.Create(RecipeType.PirateFtrTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_pirftr")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Pirate Freighter I
            _builder.Create(RecipeType.PirateFrtTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_pirfrt")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Assault Transport I
            _builder.Create(RecipeType.AsltTransTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_aslttrn")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Military Bomber MK 1 I
            _builder.Create(RecipeType.MilBomb1Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milbomb1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Military Bomber MK 2 I
            _builder.Create(RecipeType.MilBomb2Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milbomb2")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Military Bomber MK 3 I
            _builder.Create(RecipeType.MilBomb3Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milbomb3")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // S-250 Chela Starfighter I
            _builder.Create(RecipeType.ChelaTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_chela")
                .Level(10)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Military Gunship, Large I
            _builder.Create(RecipeType.MilGunLgTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milgunlg")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Military Gunship MK 1 I
            _builder.Create(RecipeType.MilGun1Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milgun1")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Military Gunship MK 2 I
            _builder.Create(RecipeType.MilGun2Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milgun2")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Military Gunship MK 3 I
            _builder.Create(RecipeType.MilGun3Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milgun3")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Infiltrator MK 1 I
            _builder.Create(RecipeType.InfMk1Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_infmk1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Infiltrator MK 2 I
            _builder.Create(RecipeType.InfMk2Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_infmk2")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Infiltrator MK 3 I
            _builder.Create(RecipeType.InfMk3Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_infmk3")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Twin Infiltrator I
            _builder.Create(RecipeType.TwinInfTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_twininf")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Advanced Scout MK 1 I
            _builder.Create(RecipeType.AdvScout1Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advsc1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Advanced Scout MK 2 I
            _builder.Create(RecipeType.AdvScout2Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advsc2")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Star Saber XC-01 I
            _builder.Create(RecipeType.StarSaberTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_starsabr")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Advanced Striker MK 1 I
            _builder.Create(RecipeType.AdvStrk1Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Advanced Striker MK 2 I
            _builder.Create(RecipeType.AdvStrk2Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr2")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Advanced Striker MK 3 I
            _builder.Create(RecipeType.AdvStrk3Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr3")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Rheinland Mk 2 I
            _builder.Create(RecipeType.RheinMk2Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_rheinmk2")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Rheinland I
            _builder.Create(RecipeType.RheinTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_rhein")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Rheinland Freighter I
            _builder.Create(RecipeType.RheinFrtTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_rheinfrt")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Advanced Bomber I
            _builder.Create(RecipeType.AdvBomb1Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advbomb1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Twin Bomber I
            _builder.Create(RecipeType.TwinBombTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_twinbomb")
                .Level(10)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Advanced Bomber MK 2 I
            _builder.Create(RecipeType.AdvBomb2Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advbomb2")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Advanced Bomber MK 3 I
            _builder.Create(RecipeType.AdvBomb3Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advbomb3")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Advanced Gunboat I
            _builder.Create(RecipeType.AdvGunboatTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advgunbt")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Advanced Gunship MK 1 I
            _builder.Create(RecipeType.AdvGun1Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advgun1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Advanced Gunship MK 2 I
            _builder.Create(RecipeType.AdvGun2Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advgun2")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Advanced Gunship MK 3 I
            _builder.Create(RecipeType.AdvGun3Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advgun3")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Sith Infiltrator MK 1 I
            _builder.Create(RecipeType.SinfMk1Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_sinfmk1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Sith Infiltrator MK 2 I
            _builder.Create(RecipeType.SinfMk2Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_sinfmk2")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Sith Infiltrator MK 3 I
            _builder.Create(RecipeType.SinfMk3Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_sinfmk3")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Advanced Scout MK 1 Escort I
            _builder.Create(RecipeType.AdvScout1ETier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advsc1e")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Advanced Scout MK 2 Escort I
            _builder.Create(RecipeType.AdvScout2ETier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advsc2e")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Advanced Scout MK 3 Escort I
            _builder.Create(RecipeType.AdvScout3ETier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advsc3e")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Advanced Striker Mk 1 Escort I
            _builder.Create(RecipeType.AdvStrk1ETier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr1e")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Advanced Striker Mk 2 Escort I
            _builder.Create(RecipeType.AdvStrk2ETier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr2e")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Advanced Striker Mk 3 Escort I
            _builder.Create(RecipeType.AdvStrk3ETier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr3e")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // KT-400 Light Freighter I
            _builder.Create(RecipeType.Kt400Tier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_kt400")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Trandoshan Transport I
            _builder.Create(RecipeType.TrandoTransTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_trandtrn")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // XS Freighter I
            _builder.Create(RecipeType.XsFrtTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_xsfrt")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Y8 Miner Ship I
            _builder.Create(RecipeType.Y8MinerTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_y8miner")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 2)
                .Component("aluminum", 1)
                .Component("fiberp_ruined", 1)
                .Component("elec_ruined", 1)
                ;

            // Zoomer Fighter I
            _builder.Create(RecipeType.ZoomerTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_zoomer")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;

            // Legion Fighter I
            _builder.Create(RecipeType.LegionFtrTier1, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_legionf")
                .Level(10)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_tilarium", 5)
                .Component("aluminum", 3)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                ;


        }

        private void Tier2()
        {
            // Striker II
            _builder.Create(RecipeType.StrikerTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_striker_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Condor II
            _builder.Create(RecipeType.CondorTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_condor_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Hound II
            _builder.Create(RecipeType.HoundTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_hound_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Panther II
            _builder.Create(RecipeType.PantherTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_panther_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Saber II
            _builder.Create(RecipeType.SaberTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_saber_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Falchion II
            _builder.Create(RecipeType.FalchionTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_falchion_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Mule II
            _builder.Create(RecipeType.MuleTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_mule_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Merchant II
            _builder.Create(RecipeType.MerchantTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_merchant_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Throne II
            _builder.Create(RecipeType.ThroneTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_throne_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Consular II
            _builder.Create(RecipeType.ConsularTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_consular_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Cutlass Starfighter II
            _builder.Create(RecipeType.CutlassTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_cutla_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Light Freighter II
            _builder.Create(RecipeType.LtfreighterTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_ltfrt_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Light Escort II
            _builder.Create(RecipeType.LtescortTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_ltesc_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Neutral Striker II
            _builder.Create(RecipeType.NeutralStrikerTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_nstrike_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Armored Transport II
            _builder.Create(RecipeType.ArmtransTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_armtrans_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Bretonia Freighter II
            _builder.Create(RecipeType.BretoniaFrtTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_bretfrt_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Civilian Elite Fighter II
            _builder.Create(RecipeType.CivEliteFtrTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_civelftr_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Civilian Fighter II
            _builder.Create(RecipeType.CivFtrTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_civftr_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Corsair Mk2 II
            _builder.Create(RecipeType.CorsairMk2Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_corsmk2_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Corsair II
            _builder.Create(RecipeType.CorsairTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_corsair_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // S-100 Stinger Starfighter II
            _builder.Create(RecipeType.S100StingerTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_s100stg_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Hutt Bomber II
            _builder.Create(RecipeType.HuttBomberTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_huttbomb_2")
                .Level(15)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Hutt Fighter II
            _builder.Create(RecipeType.HuttFtrTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_huttftr_2")
                .Level(15)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Hutt Gunship II
            _builder.Create(RecipeType.HuttGunTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_huttgun_2")
                .Level(20)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Invader II
            _builder.Create(RecipeType.InvaderTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_invader_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Hunter II
            _builder.Create(RecipeType.HunterTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_hunter_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Jedi Transport II
            _builder.Create(RecipeType.JediTransTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_jeditrn_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Kusari Mk2 II
            _builder.Create(RecipeType.KusariMk2Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_kusarmk2_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Kusari II
            _builder.Create(RecipeType.KusariTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_kusari_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Kusari Freighter II
            _builder.Create(RecipeType.KusariFrtTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_kusarfrt_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Liberty Mk2 II
            _builder.Create(RecipeType.LibertyMk2Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_libmk2_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Liberty II
            _builder.Create(RecipeType.LibertyTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_liberty_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Mandalorian Brute Patrol Ship II
            _builder.Create(RecipeType.MandoBruteTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_mdobrute_2")
                .Level(15)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Davaab-type Starfighter II
            _builder.Create(RecipeType.DavaabTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_davaab_2")
                .Level(20)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Teroch-type Gunship II
            _builder.Create(RecipeType.TerochTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_teroch_2")
                .Level(15)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Neutral Barracuda II
            _builder.Create(RecipeType.BarracudaTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_barracud_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Civilian BW Fighter II
            _builder.Create(RecipeType.CivBwFtrTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_civbwftr_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Civilian Condor II
            _builder.Create(RecipeType.CivCondorTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_civcondr_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Civilian Freighter II
            _builder.Create(RecipeType.CivFrtTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_civfrt_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // ST-07 Assault Ship II
            _builder.Create(RecipeType.St07AssaultTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_st07aslt_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Neutral Quartermaster Transport II
            _builder.Create(RecipeType.QmTransTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_qmtrans_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Starflier II
            _builder.Create(RecipeType.StarflierTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_starflir_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // YV-929 Hauler II
            _builder.Create(RecipeType.Yv929Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_yv929_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Onderon Ruping Bomber II
            _builder.Create(RecipeType.OnderonBombTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_ondrbomb_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Onderon Type81a Fighter II
            _builder.Create(RecipeType.OnderonFtrTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_ondrftr_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Order Fighter II
            _builder.Create(RecipeType.OrderFtrTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_orderftr_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Phoebos II
            _builder.Create(RecipeType.PhoebosTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_phoebos_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Pirate Fighter II
            _builder.Create(RecipeType.PirateFtrTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_pirftr_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Pirate Freighter II
            _builder.Create(RecipeType.PirateFrtTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_pirfrt_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Assault Transport II
            _builder.Create(RecipeType.AsltTransTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_aslttrn_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Military Bomber MK 1 II
            _builder.Create(RecipeType.MilBomb1Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milbomb1_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Military Bomber MK 2 II
            _builder.Create(RecipeType.MilBomb2Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milbomb2_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Military Bomber MK 3 II
            _builder.Create(RecipeType.MilBomb3Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milbomb3_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // S-250 Chela Starfighter II
            _builder.Create(RecipeType.ChelaTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_chela_2")
                .Level(20)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Military Gunship, Large II
            _builder.Create(RecipeType.MilGunLgTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milgunlg_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Military Gunship MK 1 II
            _builder.Create(RecipeType.MilGun1Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milgun1_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Military Gunship MK 2 II
            _builder.Create(RecipeType.MilGun2Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milgun2_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Military Gunship MK 3 II
            _builder.Create(RecipeType.MilGun3Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milgun3_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Infiltrator MK 1 II
            _builder.Create(RecipeType.InfMk1Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_infmk1_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Infiltrator MK 2 II
            _builder.Create(RecipeType.InfMk2Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_infmk2_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Infiltrator MK 3 II
            _builder.Create(RecipeType.InfMk3Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_infmk3_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Twin Infiltrator II
            _builder.Create(RecipeType.TwinInfTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_twininf_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Advanced Scout MK 1 II
            _builder.Create(RecipeType.AdvScout1Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advsc1_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Advanced Scout MK 2 II
            _builder.Create(RecipeType.AdvScout2Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advsc2_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Star Saber XC-01 II
            _builder.Create(RecipeType.StarSaberTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_starsabr_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Advanced Striker MK 1 II
            _builder.Create(RecipeType.AdvStrk1Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr1_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Advanced Striker MK 2 II
            _builder.Create(RecipeType.AdvStrk2Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr2_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Advanced Striker MK 3 II
            _builder.Create(RecipeType.AdvStrk3Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr3_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Rheinland Mk 2 II
            _builder.Create(RecipeType.RheinMk2Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_rheinmk2_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Rheinland II
            _builder.Create(RecipeType.RheinTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_rhein_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Rheinland Freighter II
            _builder.Create(RecipeType.RheinFrtTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_rheinfrt_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Advanced Bomber II
            _builder.Create(RecipeType.AdvBomb1Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advbomb1_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Twin Bomber II
            _builder.Create(RecipeType.TwinBombTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_twinbomb_2")
                .Level(20)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Advanced Bomber MK 2 II
            _builder.Create(RecipeType.AdvBomb2Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advbomb2_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Advanced Bomber MK 3 II
            _builder.Create(RecipeType.AdvBomb3Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advbomb3_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Advanced Gunboat II
            _builder.Create(RecipeType.AdvGunboatTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advgunbt_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Advanced Gunship MK 1 II
            _builder.Create(RecipeType.AdvGun1Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advgun1_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Advanced Gunship MK 2 II
            _builder.Create(RecipeType.AdvGun2Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advgun2_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Advanced Gunship MK 3 II
            _builder.Create(RecipeType.AdvGun3Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advgun3_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Sith Infiltrator MK 1 II
            _builder.Create(RecipeType.SinfMk1Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_sinfmk1_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Sith Infiltrator MK 2 II
            _builder.Create(RecipeType.SinfMk2Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_sinfmk2_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Sith Infiltrator MK 3 II
            _builder.Create(RecipeType.SinfMk3Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_sinfmk3_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Advanced Scout MK 1 Escort II
            _builder.Create(RecipeType.AdvScout1ETier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advsc1e_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Advanced Scout MK 2 Escort II
            _builder.Create(RecipeType.AdvScout2ETier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advsc2e_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Advanced Scout MK 3 Escort II
            _builder.Create(RecipeType.AdvScout3ETier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advsc3e_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Advanced Striker Mk 1 Escort II
            _builder.Create(RecipeType.AdvStrk1ETier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr1e_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Advanced Striker Mk 2 Escort II
            _builder.Create(RecipeType.AdvStrk2ETier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr2e_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Advanced Striker Mk 3 Escort II
            _builder.Create(RecipeType.AdvStrk3ETier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr3e_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // KT-400 Light Freighter II
            _builder.Create(RecipeType.Kt400Tier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_kt400_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Trandoshan Transport II
            _builder.Create(RecipeType.TrandoTransTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_trandtrn_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // XS Freighter II
            _builder.Create(RecipeType.XsFrtTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_xsfrt_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Y8 Miner Ship II
            _builder.Create(RecipeType.Y8MinerTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_y8miner_2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 2)
                .Component("steel", 1)
                .Component("fiberp_flawed", 1)
                .Component("elec_flawed", 1)
                ;

            // Zoomer Fighter II
            _builder.Create(RecipeType.ZoomerTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_zoomer_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;

            // Legion Fighter II
            _builder.Create(RecipeType.LegionFtrTier2, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_legionf_2")
                .Level(20)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Starship, 1)
                .Component("ref_currian", 5)
                .Component("steel", 3)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                ;


        }

        private void Tier3()
        {
            // Striker III
            _builder.Create(RecipeType.StrikerTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_striker_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Condor III
            _builder.Create(RecipeType.CondorTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_condor_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Hound III
            _builder.Create(RecipeType.HoundTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_hound_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Panther III
            _builder.Create(RecipeType.PantherTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_panther_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Saber III
            _builder.Create(RecipeType.SaberTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_saber_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Falchion III
            _builder.Create(RecipeType.FalchionTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_falchion_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Mule III
            _builder.Create(RecipeType.MuleTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_mule_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Merchant III
            _builder.Create(RecipeType.MerchantTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_merchant_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Throne III
            _builder.Create(RecipeType.ThroneTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_throne_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Consular III
            _builder.Create(RecipeType.ConsularTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_consular_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Cutlass Starfighter III
            _builder.Create(RecipeType.CutlassTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_cutla_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Light Freighter III
            _builder.Create(RecipeType.LtfreighterTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_ltfrt_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Light Escort III
            _builder.Create(RecipeType.LtescortTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_ltesc_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Neutral Striker III
            _builder.Create(RecipeType.NeutralStrikerTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_nstrike_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Armored Transport III
            _builder.Create(RecipeType.ArmtransTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_armtrans_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Bretonia Freighter III
            _builder.Create(RecipeType.BretoniaFrtTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_bretfrt_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Civilian Elite Fighter III
            _builder.Create(RecipeType.CivEliteFtrTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_civelftr_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Civilian Fighter III
            _builder.Create(RecipeType.CivFtrTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_civftr_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Corsair Mk2 III
            _builder.Create(RecipeType.CorsairMk2Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_corsmk2_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Corsair III
            _builder.Create(RecipeType.CorsairTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_corsair_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // S-100 Stinger Starfighter III
            _builder.Create(RecipeType.S100StingerTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_s100stg_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Hutt Bomber III
            _builder.Create(RecipeType.HuttBomberTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_huttbomb_3")
                .Level(25)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Hutt Fighter III
            _builder.Create(RecipeType.HuttFtrTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_huttftr_3")
                .Level(25)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Hutt Gunship III
            _builder.Create(RecipeType.HuttGunTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_huttgun_3")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Invader III
            _builder.Create(RecipeType.InvaderTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_invader_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Hunter III
            _builder.Create(RecipeType.HunterTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_hunter_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Jedi Transport III
            _builder.Create(RecipeType.JediTransTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_jeditrn_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Kusari Mk2 III
            _builder.Create(RecipeType.KusariMk2Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_kusarmk2_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Kusari III
            _builder.Create(RecipeType.KusariTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_kusari_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Kusari Freighter III
            _builder.Create(RecipeType.KusariFrtTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_kusarfrt_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Liberty Mk2 III
            _builder.Create(RecipeType.LibertyMk2Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_libmk2_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Liberty III
            _builder.Create(RecipeType.LibertyTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_liberty_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Mandalorian Brute Patrol Ship III
            _builder.Create(RecipeType.MandoBruteTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_mdobrute_3")
                .Level(25)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Davaab-type Starfighter III
            _builder.Create(RecipeType.DavaabTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_davaab_3")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Teroch-type Gunship III
            _builder.Create(RecipeType.TerochTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_teroch_3")
                .Level(25)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Neutral Barracuda III
            _builder.Create(RecipeType.BarracudaTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_barracud_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Civilian BW Fighter III
            _builder.Create(RecipeType.CivBwFtrTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_civbwftr_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Civilian Condor III
            _builder.Create(RecipeType.CivCondorTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_civcondr_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Civilian Freighter III
            _builder.Create(RecipeType.CivFrtTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_civfrt_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // ST-07 Assault Ship III
            _builder.Create(RecipeType.St07AssaultTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_st07aslt_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Neutral Quartermaster Transport III
            _builder.Create(RecipeType.QmTransTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_qmtrans_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Starflier III
            _builder.Create(RecipeType.StarflierTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_starflir_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // YV-929 Hauler III
            _builder.Create(RecipeType.Yv929Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_yv929_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Onderon Ruping Bomber III
            _builder.Create(RecipeType.OnderonBombTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_ondrbomb_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Onderon Type81a Fighter III
            _builder.Create(RecipeType.OnderonFtrTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_ondrftr_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Order Fighter III
            _builder.Create(RecipeType.OrderFtrTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_orderftr_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Phoebos III
            _builder.Create(RecipeType.PhoebosTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_phoebos_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Pirate Fighter III
            _builder.Create(RecipeType.PirateFtrTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_pirftr_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Pirate Freighter III
            _builder.Create(RecipeType.PirateFrtTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_pirfrt_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Assault Transport III
            _builder.Create(RecipeType.AsltTransTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_aslttrn_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Military Bomber MK 1 III
            _builder.Create(RecipeType.MilBomb1Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milbomb1_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Military Bomber MK 2 III
            _builder.Create(RecipeType.MilBomb2Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milbomb2_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Military Bomber MK 3 III
            _builder.Create(RecipeType.MilBomb3Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milbomb3_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // S-250 Chela Starfighter III
            _builder.Create(RecipeType.ChelaTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_chela_3")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Military Gunship, Large III
            _builder.Create(RecipeType.MilGunLgTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milgunlg_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Military Gunship MK 1 III
            _builder.Create(RecipeType.MilGun1Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milgun1_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Military Gunship MK 2 III
            _builder.Create(RecipeType.MilGun2Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milgun2_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Military Gunship MK 3 III
            _builder.Create(RecipeType.MilGun3Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milgun3_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Infiltrator MK 1 III
            _builder.Create(RecipeType.InfMk1Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_infmk1_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Infiltrator MK 2 III
            _builder.Create(RecipeType.InfMk2Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_infmk2_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Infiltrator MK 3 III
            _builder.Create(RecipeType.InfMk3Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_infmk3_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Twin Infiltrator III
            _builder.Create(RecipeType.TwinInfTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_twininf_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Advanced Scout MK 1 III
            _builder.Create(RecipeType.AdvScout1Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advsc1_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Advanced Scout MK 2 III
            _builder.Create(RecipeType.AdvScout2Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advsc2_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Star Saber XC-01 III
            _builder.Create(RecipeType.StarSaberTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_starsabr_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Advanced Striker MK 1 III
            _builder.Create(RecipeType.AdvStrk1Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr1_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Advanced Striker MK 2 III
            _builder.Create(RecipeType.AdvStrk2Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr2_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Advanced Striker MK 3 III
            _builder.Create(RecipeType.AdvStrk3Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr3_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Rheinland Mk 2 III
            _builder.Create(RecipeType.RheinMk2Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_rheinmk2_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Rheinland III
            _builder.Create(RecipeType.RheinTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_rhein_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Rheinland Freighter III
            _builder.Create(RecipeType.RheinFrtTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_rheinfrt_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Advanced Bomber III
            _builder.Create(RecipeType.AdvBomb1Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advbomb1_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Twin Bomber III
            _builder.Create(RecipeType.TwinBombTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_twinbomb_3")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Advanced Bomber MK 2 III
            _builder.Create(RecipeType.AdvBomb2Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advbomb2_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Advanced Bomber MK 3 III
            _builder.Create(RecipeType.AdvBomb3Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advbomb3_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Advanced Gunboat III
            _builder.Create(RecipeType.AdvGunboatTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advgunbt_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Advanced Gunship MK 1 III
            _builder.Create(RecipeType.AdvGun1Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advgun1_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Advanced Gunship MK 2 III
            _builder.Create(RecipeType.AdvGun2Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advgun2_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Advanced Gunship MK 3 III
            _builder.Create(RecipeType.AdvGun3Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advgun3_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Sith Infiltrator MK 1 III
            _builder.Create(RecipeType.SinfMk1Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_sinfmk1_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Sith Infiltrator MK 2 III
            _builder.Create(RecipeType.SinfMk2Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_sinfmk2_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Sith Infiltrator MK 3 III
            _builder.Create(RecipeType.SinfMk3Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_sinfmk3_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Advanced Scout MK 1 Escort III
            _builder.Create(RecipeType.AdvScout1ETier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advsc1e_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Advanced Scout MK 2 Escort III
            _builder.Create(RecipeType.AdvScout2ETier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advsc2e_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Advanced Scout MK 3 Escort III
            _builder.Create(RecipeType.AdvScout3ETier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advsc3e_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Advanced Striker Mk 1 Escort III
            _builder.Create(RecipeType.AdvStrk1ETier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr1e_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Advanced Striker Mk 2 Escort III
            _builder.Create(RecipeType.AdvStrk2ETier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr2e_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Advanced Striker Mk 3 Escort III
            _builder.Create(RecipeType.AdvStrk3ETier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr3e_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // KT-400 Light Freighter III
            _builder.Create(RecipeType.Kt400Tier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_kt400_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Trandoshan Transport III
            _builder.Create(RecipeType.TrandoTransTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_trandtrn_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // XS Freighter III
            _builder.Create(RecipeType.XsFrtTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_xsfrt_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Y8 Miner Ship III
            _builder.Create(RecipeType.Y8MinerTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_y8miner_3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 2)
                .Component("obsidian", 1)
                .Component("fiberp_good", 1)
                .Component("elec_good", 1)
                ;

            // Zoomer Fighter III
            _builder.Create(RecipeType.ZoomerTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_zoomer_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;

            // Legion Fighter III
            _builder.Create(RecipeType.LegionFtrTier3, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_legionf_3")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_idailia", 5)
                .Component("obsidian", 3)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                ;


        }

        private void Tier4()
        {
            // Striker IV
            _builder.Create(RecipeType.StrikerTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_striker_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Condor IV
            _builder.Create(RecipeType.CondorTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_condor_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Hound IV
            _builder.Create(RecipeType.HoundTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_hound_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Panther IV
            _builder.Create(RecipeType.PantherTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_panther_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Saber IV
            _builder.Create(RecipeType.SaberTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_saber_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Falchion IV
            _builder.Create(RecipeType.FalchionTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_falchion_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Mule IV
            _builder.Create(RecipeType.MuleTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_mule_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Merchant IV
            _builder.Create(RecipeType.MerchantTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_merchant_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Throne IV
            _builder.Create(RecipeType.ThroneTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_throne_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Consular IV
            _builder.Create(RecipeType.ConsularTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_consular_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Cutlass Starfighter IV
            _builder.Create(RecipeType.CutlassTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_cutla_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Light Freighter IV
            _builder.Create(RecipeType.LtfreighterTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_ltfrt_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Light Escort IV
            _builder.Create(RecipeType.LtescortTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_ltesc_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Neutral Striker IV
            _builder.Create(RecipeType.NeutralStrikerTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_nstrike_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Armored Transport IV
            _builder.Create(RecipeType.ArmtransTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_armtrans_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Bretonia Freighter IV
            _builder.Create(RecipeType.BretoniaFrtTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_bretfrt_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Civilian Elite Fighter IV
            _builder.Create(RecipeType.CivEliteFtrTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_civelftr_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Civilian Fighter IV
            _builder.Create(RecipeType.CivFtrTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_civftr_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Corsair Mk2 IV
            _builder.Create(RecipeType.CorsairMk2Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_corsmk2_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Corsair IV
            _builder.Create(RecipeType.CorsairTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_corsair_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // S-100 Stinger Starfighter IV
            _builder.Create(RecipeType.S100StingerTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_s100stg_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Hutt Bomber IV
            _builder.Create(RecipeType.HuttBomberTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_huttbomb_4")
                .Level(35)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Hutt Fighter IV
            _builder.Create(RecipeType.HuttFtrTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_huttftr_4")
                .Level(35)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Hutt Gunship IV
            _builder.Create(RecipeType.HuttGunTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_huttgun_4")
                .Level(40)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Invader IV
            _builder.Create(RecipeType.InvaderTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_invader_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Hunter IV
            _builder.Create(RecipeType.HunterTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_hunter_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Jedi Transport IV
            _builder.Create(RecipeType.JediTransTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_jeditrn_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Kusari Mk2 IV
            _builder.Create(RecipeType.KusariMk2Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_kusarmk2_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Kusari IV
            _builder.Create(RecipeType.KusariTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_kusari_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Kusari Freighter IV
            _builder.Create(RecipeType.KusariFrtTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_kusarfrt_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Liberty Mk2 IV
            _builder.Create(RecipeType.LibertyMk2Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_libmk2_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Liberty IV
            _builder.Create(RecipeType.LibertyTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_liberty_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Mandalorian Brute Patrol Ship IV
            _builder.Create(RecipeType.MandoBruteTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_mdobrute_4")
                .Level(35)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Davaab-type Starfighter IV
            _builder.Create(RecipeType.DavaabTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_davaab_4")
                .Level(40)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Teroch-type Gunship IV
            _builder.Create(RecipeType.TerochTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_teroch_4")
                .Level(35)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Neutral Barracuda IV
            _builder.Create(RecipeType.BarracudaTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_barracud_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Civilian BW Fighter IV
            _builder.Create(RecipeType.CivBwFtrTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_civbwftr_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Civilian Condor IV
            _builder.Create(RecipeType.CivCondorTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_civcondr_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Civilian Freighter IV
            _builder.Create(RecipeType.CivFrtTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_civfrt_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // ST-07 Assault Ship IV
            _builder.Create(RecipeType.St07AssaultTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_st07aslt_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Neutral Quartermaster Transport IV
            _builder.Create(RecipeType.QmTransTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_qmtrans_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Starflier IV
            _builder.Create(RecipeType.StarflierTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_starflir_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // YV-929 Hauler IV
            _builder.Create(RecipeType.Yv929Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_yv929_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Onderon Ruping Bomber IV
            _builder.Create(RecipeType.OnderonBombTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_ondrbomb_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Onderon Type81a Fighter IV
            _builder.Create(RecipeType.OnderonFtrTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_ondrftr_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Order Fighter IV
            _builder.Create(RecipeType.OrderFtrTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_orderftr_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Phoebos IV
            _builder.Create(RecipeType.PhoebosTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_phoebos_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Pirate Fighter IV
            _builder.Create(RecipeType.PirateFtrTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_pirftr_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Pirate Freighter IV
            _builder.Create(RecipeType.PirateFrtTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_pirfrt_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Assault Transport IV
            _builder.Create(RecipeType.AsltTransTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_aslttrn_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Military Bomber MK 1 IV
            _builder.Create(RecipeType.MilBomb1Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milbomb1_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Military Bomber MK 2 IV
            _builder.Create(RecipeType.MilBomb2Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milbomb2_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Military Bomber MK 3 IV
            _builder.Create(RecipeType.MilBomb3Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milbomb3_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // S-250 Chela Starfighter IV
            _builder.Create(RecipeType.ChelaTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_chela_4")
                .Level(40)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Military Gunship, Large IV
            _builder.Create(RecipeType.MilGunLgTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milgunlg_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Military Gunship MK 1 IV
            _builder.Create(RecipeType.MilGun1Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milgun1_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Military Gunship MK 2 IV
            _builder.Create(RecipeType.MilGun2Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milgun2_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Military Gunship MK 3 IV
            _builder.Create(RecipeType.MilGun3Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milgun3_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Infiltrator MK 1 IV
            _builder.Create(RecipeType.InfMk1Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_infmk1_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Infiltrator MK 2 IV
            _builder.Create(RecipeType.InfMk2Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_infmk2_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Infiltrator MK 3 IV
            _builder.Create(RecipeType.InfMk3Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_infmk3_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Twin Infiltrator IV
            _builder.Create(RecipeType.TwinInfTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_twininf_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Advanced Scout MK 1 IV
            _builder.Create(RecipeType.AdvScout1Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advsc1_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Advanced Scout MK 2 IV
            _builder.Create(RecipeType.AdvScout2Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advsc2_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Star Saber XC-01 IV
            _builder.Create(RecipeType.StarSaberTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_starsabr_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Advanced Striker MK 1 IV
            _builder.Create(RecipeType.AdvStrk1Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr1_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Advanced Striker MK 2 IV
            _builder.Create(RecipeType.AdvStrk2Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr2_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Advanced Striker MK 3 IV
            _builder.Create(RecipeType.AdvStrk3Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr3_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Rheinland Mk 2 IV
            _builder.Create(RecipeType.RheinMk2Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_rheinmk2_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Rheinland IV
            _builder.Create(RecipeType.RheinTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_rhein_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Rheinland Freighter IV
            _builder.Create(RecipeType.RheinFrtTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_rheinfrt_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Advanced Bomber IV
            _builder.Create(RecipeType.AdvBomb1Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advbomb1_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Twin Bomber IV
            _builder.Create(RecipeType.TwinBombTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_twinbomb_4")
                .Level(40)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Advanced Bomber MK 2 IV
            _builder.Create(RecipeType.AdvBomb2Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advbomb2_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Advanced Bomber MK 3 IV
            _builder.Create(RecipeType.AdvBomb3Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advbomb3_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Advanced Gunboat IV
            _builder.Create(RecipeType.AdvGunboatTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advgunbt_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Advanced Gunship MK 1 IV
            _builder.Create(RecipeType.AdvGun1Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advgun1_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Advanced Gunship MK 2 IV
            _builder.Create(RecipeType.AdvGun2Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advgun2_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Advanced Gunship MK 3 IV
            _builder.Create(RecipeType.AdvGun3Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advgun3_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Sith Infiltrator MK 1 IV
            _builder.Create(RecipeType.SinfMk1Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_sinfmk1_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Sith Infiltrator MK 2 IV
            _builder.Create(RecipeType.SinfMk2Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_sinfmk2_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Sith Infiltrator MK 3 IV
            _builder.Create(RecipeType.SinfMk3Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_sinfmk3_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Advanced Scout MK 1 Escort IV
            _builder.Create(RecipeType.AdvScout1ETier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advsc1e_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Advanced Scout MK 2 Escort IV
            _builder.Create(RecipeType.AdvScout2ETier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advsc2e_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Advanced Scout MK 3 Escort IV
            _builder.Create(RecipeType.AdvScout3ETier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advsc3e_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Advanced Striker Mk 1 Escort IV
            _builder.Create(RecipeType.AdvStrk1ETier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr1e_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Advanced Striker Mk 2 Escort IV
            _builder.Create(RecipeType.AdvStrk2ETier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr2e_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Advanced Striker Mk 3 Escort IV
            _builder.Create(RecipeType.AdvStrk3ETier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr3e_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // KT-400 Light Freighter IV
            _builder.Create(RecipeType.Kt400Tier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_kt400_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Trandoshan Transport IV
            _builder.Create(RecipeType.TrandoTransTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_trandtrn_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // XS Freighter IV
            _builder.Create(RecipeType.XsFrtTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_xsfrt_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Y8 Miner Ship IV
            _builder.Create(RecipeType.Y8MinerTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_y8miner_4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 2)
                .Component("crystal", 1)
                .Component("fiberp_imperfect", 1)
                .Component("elec_imperfect", 1)
                ;

            // Zoomer Fighter IV
            _builder.Create(RecipeType.ZoomerTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_zoomer_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;

            // Legion Fighter IV
            _builder.Create(RecipeType.LegionFtrTier4, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_legionf_4")
                .Level(40)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_barinium", 5)
                .Component("crystal", 3)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                ;


        }

        private void Tier5()
        {
            // Striker V
            _builder.Create(RecipeType.StrikerTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_striker_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Condor V
            _builder.Create(RecipeType.CondorTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_condor_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Hound V
            _builder.Create(RecipeType.HoundTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_hound_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Panther V
            _builder.Create(RecipeType.PantherTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_panther_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Saber V
            _builder.Create(RecipeType.SaberTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_saber_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Falchion V
            _builder.Create(RecipeType.FalchionTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_falchion_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Mule V
            _builder.Create(RecipeType.MuleTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_mule_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Merchant V
            _builder.Create(RecipeType.MerchantTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_merchant_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Throne V
            _builder.Create(RecipeType.ThroneTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_throne_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Consular V
            _builder.Create(RecipeType.ConsularTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_consular_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Cutlass Starfighter V
            _builder.Create(RecipeType.CutlassTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_cutla_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Light Freighter V
            _builder.Create(RecipeType.LtfreighterTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_ltfrt_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Light Escort V
            _builder.Create(RecipeType.LtescortTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_ltesc_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Neutral Striker V
            _builder.Create(RecipeType.NeutralStrikerTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_nstrike_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Armored Transport V
            _builder.Create(RecipeType.ArmtransTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_armtrans_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Bretonia Freighter V
            _builder.Create(RecipeType.BretoniaFrtTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_bretfrt_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Civilian Elite Fighter V
            _builder.Create(RecipeType.CivEliteFtrTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_civelftr_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Civilian Fighter V
            _builder.Create(RecipeType.CivFtrTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_civftr_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Corsair Mk2 V
            _builder.Create(RecipeType.CorsairMk2Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_corsmk2_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Corsair V
            _builder.Create(RecipeType.CorsairTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_corsair_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // S-100 Stinger Starfighter V
            _builder.Create(RecipeType.S100StingerTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_s100stg_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Hutt Bomber V
            _builder.Create(RecipeType.HuttBomberTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_huttbomb_5")
                .Level(45)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Hutt Fighter V
            _builder.Create(RecipeType.HuttFtrTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_huttftr_5")
                .Level(45)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Hutt Gunship V
            _builder.Create(RecipeType.HuttGunTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_huttgun_5")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Invader V
            _builder.Create(RecipeType.InvaderTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_invader_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Hunter V
            _builder.Create(RecipeType.HunterTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_hunter_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Jedi Transport V
            _builder.Create(RecipeType.JediTransTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_jeditrn_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Kusari Mk2 V
            _builder.Create(RecipeType.KusariMk2Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_kusarmk2_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Kusari V
            _builder.Create(RecipeType.KusariTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_kusari_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Kusari Freighter V
            _builder.Create(RecipeType.KusariFrtTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_kusarfrt_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Liberty Mk2 V
            _builder.Create(RecipeType.LibertyMk2Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_libmk2_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Liberty V
            _builder.Create(RecipeType.LibertyTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_liberty_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Mandalorian Brute Patrol Ship V
            _builder.Create(RecipeType.MandoBruteTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_mdobrute_5")
                .Level(45)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Davaab-type Starfighter V
            _builder.Create(RecipeType.DavaabTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_davaab_5")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Teroch-type Gunship V
            _builder.Create(RecipeType.TerochTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_teroch_5")
                .Level(45)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Neutral Barracuda V
            _builder.Create(RecipeType.BarracudaTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_barracud_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Civilian BW Fighter V
            _builder.Create(RecipeType.CivBwFtrTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_civbwftr_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Civilian Condor V
            _builder.Create(RecipeType.CivCondorTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_civcondr_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Civilian Freighter V
            _builder.Create(RecipeType.CivFrtTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_civfrt_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // ST-07 Assault Ship V
            _builder.Create(RecipeType.St07AssaultTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_st07aslt_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Neutral Quartermaster Transport V
            _builder.Create(RecipeType.QmTransTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_qmtrans_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Starflier V
            _builder.Create(RecipeType.StarflierTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_starflir_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // YV-929 Hauler V
            _builder.Create(RecipeType.Yv929Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_yv929_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Onderon Ruping Bomber V
            _builder.Create(RecipeType.OnderonBombTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_ondrbomb_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Onderon Type81a Fighter V
            _builder.Create(RecipeType.OnderonFtrTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_ondrftr_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Order Fighter V
            _builder.Create(RecipeType.OrderFtrTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_orderftr_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Phoebos V
            _builder.Create(RecipeType.PhoebosTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_phoebos_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Pirate Fighter V
            _builder.Create(RecipeType.PirateFtrTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_pirftr_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Pirate Freighter V
            _builder.Create(RecipeType.PirateFrtTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_pirfrt_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Assault Transport V
            _builder.Create(RecipeType.AsltTransTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_aslttrn_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Military Bomber MK 1 V
            _builder.Create(RecipeType.MilBomb1Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milbomb1_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Military Bomber MK 2 V
            _builder.Create(RecipeType.MilBomb2Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milbomb2_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Military Bomber MK 3 V
            _builder.Create(RecipeType.MilBomb3Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milbomb3_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // S-250 Chela Starfighter V
            _builder.Create(RecipeType.ChelaTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_chela_5")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Military Gunship, Large V
            _builder.Create(RecipeType.MilGunLgTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milgunlg_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Military Gunship MK 1 V
            _builder.Create(RecipeType.MilGun1Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milgun1_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Military Gunship MK 2 V
            _builder.Create(RecipeType.MilGun2Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milgun2_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Military Gunship MK 3 V
            _builder.Create(RecipeType.MilGun3Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_milgun3_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Infiltrator MK 1 V
            _builder.Create(RecipeType.InfMk1Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_infmk1_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Infiltrator MK 2 V
            _builder.Create(RecipeType.InfMk2Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_infmk2_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Infiltrator MK 3 V
            _builder.Create(RecipeType.InfMk3Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_infmk3_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Twin Infiltrator V
            _builder.Create(RecipeType.TwinInfTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_twininf_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Advanced Scout MK 1 V
            _builder.Create(RecipeType.AdvScout1Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advsc1_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Advanced Scout MK 2 V
            _builder.Create(RecipeType.AdvScout2Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advsc2_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Star Saber XC-01 V
            _builder.Create(RecipeType.StarSaberTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_starsabr_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Advanced Striker MK 1 V
            _builder.Create(RecipeType.AdvStrk1Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr1_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Advanced Striker MK 2 V
            _builder.Create(RecipeType.AdvStrk2Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr2_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Advanced Striker MK 3 V
            _builder.Create(RecipeType.AdvStrk3Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr3_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Rheinland Mk 2 V
            _builder.Create(RecipeType.RheinMk2Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_rheinmk2_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Rheinland V
            _builder.Create(RecipeType.RheinTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_rhein_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Rheinland Freighter V
            _builder.Create(RecipeType.RheinFrtTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_rheinfrt_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Advanced Bomber V
            _builder.Create(RecipeType.AdvBomb1Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advbomb1_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Twin Bomber V
            _builder.Create(RecipeType.TwinBombTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_twinbomb_5")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Advanced Bomber MK 2 V
            _builder.Create(RecipeType.AdvBomb2Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advbomb2_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Advanced Bomber MK 3 V
            _builder.Create(RecipeType.AdvBomb3Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advbomb3_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Advanced Gunboat V
            _builder.Create(RecipeType.AdvGunboatTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advgunbt_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Advanced Gunship MK 1 V
            _builder.Create(RecipeType.AdvGun1Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advgun1_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Advanced Gunship MK 2 V
            _builder.Create(RecipeType.AdvGun2Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advgun2_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Advanced Gunship MK 3 V
            _builder.Create(RecipeType.AdvGun3Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advgun3_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Sith Infiltrator MK 1 V
            _builder.Create(RecipeType.SinfMk1Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_sinfmk1_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Sith Infiltrator MK 2 V
            _builder.Create(RecipeType.SinfMk2Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_sinfmk2_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Sith Infiltrator MK 3 V
            _builder.Create(RecipeType.SinfMk3Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_sinfmk3_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Advanced Scout MK 1 Escort V
            _builder.Create(RecipeType.AdvScout1ETier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advsc1e_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Advanced Scout MK 2 Escort V
            _builder.Create(RecipeType.AdvScout2ETier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advsc2e_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Advanced Scout MK 3 Escort V
            _builder.Create(RecipeType.AdvScout3ETier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advsc3e_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Advanced Striker Mk 1 Escort V
            _builder.Create(RecipeType.AdvStrk1ETier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr1e_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Advanced Striker Mk 2 Escort V
            _builder.Create(RecipeType.AdvStrk2ETier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr2e_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Advanced Striker Mk 3 Escort V
            _builder.Create(RecipeType.AdvStrk3ETier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_advstr3e_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // KT-400 Light Freighter V
            _builder.Create(RecipeType.Kt400Tier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_kt400_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Trandoshan Transport V
            _builder.Create(RecipeType.TrandoTransTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_trandtrn_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // XS Freighter V
            _builder.Create(RecipeType.XsFrtTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_xsfrt_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Y8 Miner Ship V
            _builder.Create(RecipeType.Y8MinerTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_y8miner_5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 2)
                .Component("diamond", 1)
                .Component("fiberp_high", 1)
                .Component("elec_high", 1)
                ;

            // Zoomer Fighter V
            _builder.Create(RecipeType.ZoomerTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_zoomer_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;

            // Legion Fighter V
            _builder.Create(RecipeType.LegionFtrTier5, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("sdeed_legionf_5")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .RequirementPerk(PerkType.StarshipBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Starship, 2)
                .Component("ref_gostian", 5)
                .Component("diamond", 3)
                .Component("fiberp_high", 3)
                .Component("elec_high", 3)
                ;


            // Basilisk War Droid
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

            // Crusader Corvette
            _builder.Create(RecipeType.CorvetteCrusader, SkillType.Engineering)
                .Category(RecipeCategoryType.Starship)
                .Resref("capdeed_cruscor")
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