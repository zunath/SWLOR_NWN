using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class RingRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Tier1();
            Tier1A();
            Tier2();
            Tier2A();
            Tier3();
            Tier3A();
            Tier4();
            Tier4A();
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
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 1)
                .Component("jade", 1)
                .ForgeParameters(rangeCenter: 49, rangeWidth: 39, coolingRate: 3, startingThermal: 57, shapeShare: 38);

            // Spiritmaster Ring
            _builder.Create(RecipeType.SpiritmasterRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("sm_ring")
                .Level(1)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 1)
                .Component("jade", 1)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 34, coolingRate: 3, startingThermal: 53, shapeShare: 38);

            // Combat Ring
            _builder.Create(RecipeType.CombatRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("com_ring")
                .Level(1)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 1)
                .Component("jade", 1)
                .ForgeParameters(rangeCenter: 47, rangeWidth: 33, coolingRate: 3, startingThermal: 52, shapeShare: 38);

            // Advent Ring
            _builder.Create(RecipeType.AdventRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("advent_ring")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3)
                .ForgeParameters(rangeCenter: 45, rangeWidth: 42, coolingRate: 4, startingThermal: 52, shapeShare: 38);

            // Amateur Ring
            _builder.Create(RecipeType.AmateurRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("engi_ring_1")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 34, coolingRate: 4, startingThermal: 53, shapeShare: 38);

            // Cloth Ring
            _builder.Create(RecipeType.ClothRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("fabr_ring_1")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3)
                .ForgeParameters(rangeCenter: 47, rangeWidth: 37, coolingRate: 4, startingThermal: 53, shapeShare: 38);

            // Chef Ring
            _builder.Create(RecipeType.ChefRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("chef_ring_1")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3)
                .ForgeParameters(rangeCenter: 51, rangeWidth: 33, coolingRate: 3, startingThermal: 56, shapeShare: 38);
        }

        private void Tier2()
        {
            // Titan Ring
            _builder.Create(RecipeType.TitanRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("tit_ring")
                .Level(11)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 1)
                .Component("agate", 1)
                .ForgeParameters(rangeCenter: 51, rangeWidth: 50, coolingRate: 5, startingThermal: 65, shapeShare: 39);

            // Vivid Ring
            _builder.Create(RecipeType.VividRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("viv_ring")
                .Level(11)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 1)
                .Component("agate", 1)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 48, coolingRate: 2, startingThermal: 61, shapeShare: 37);

            // Valor Ring
            _builder.Create(RecipeType.ValorRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("val_ring")
                .Level(11)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 1)
                .Component("agate", 1)
                .ForgeParameters(rangeCenter: 45, rangeWidth: 40, coolingRate: 2, startingThermal: 55, shapeShare: 39);

            // Frontier Ring
            _builder.Create(RecipeType.FrontierRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("frontier_ring")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3)
                .ForgeParameters(rangeCenter: 51, rangeWidth: 33, coolingRate: 1, startingThermal: 57, shapeShare: 37);

            // Worker Ring
            _builder.Create(RecipeType.WorkerRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("engi_ring_2")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3)
                .ForgeParameters(rangeCenter: 47, rangeWidth: 31, coolingRate: 4, startingThermal: 49, shapeShare: 37);

            // Linen Ring
            _builder.Create(RecipeType.LinenRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("fabr_ring_2")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3)
                .ForgeParameters(rangeCenter: 47, rangeWidth: 31, coolingRate: 7, startingThermal: 55, shapeShare: 39);

            // Velveteen Ring
            _builder.Create(RecipeType.VelveteenRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("chef_ring_2")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3)
                .ForgeParameters(rangeCenter: 46, rangeWidth: 39, coolingRate: 3, startingThermal: 56, shapeShare: 38);
        }

        private void Tier3()
        {
            // Quark Ring
            _builder.Create(RecipeType.QuarkRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("qk_ring")
                .Level(21)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 1)
                .Component("citrine", 1)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 29, coolingRate: 2, startingThermal: 54, shapeShare: 38);

            // Reginal Ring
            _builder.Create(RecipeType.ReginalRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("reg_ring")
                .Level(21)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 1)
                .Component("citrine", 1)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 33, coolingRate: 4, startingThermal: 44, shapeShare: 38);

            // Forza Ring
            _builder.Create(RecipeType.ForzaRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("for_ring")
                .Level(21)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 1)
                .Component("citrine", 1)
                .ForgeParameters(rangeCenter: 49, rangeWidth: 33, coolingRate: 6, startingThermal: 43, shapeShare: 38);

            // Majestic Ring
            _builder.Create(RecipeType.MajesticRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("majestic_ring")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 21, coolingRate: 8, startingThermal: 42, shapeShare: 38);

            // Mechanic Ring
            _builder.Create(RecipeType.MechanicRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("engi_ring_3")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3)
                .ForgeParameters(rangeCenter: 47, rangeWidth: 26, coolingRate: 4, startingThermal: 46, shapeShare: 38);

            // Designer Ring
            _builder.Create(RecipeType.DesignerRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("fabr_ring_3")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 25, coolingRate: 5, startingThermal: 56, shapeShare: 38);

            // Silk Ring
            _builder.Create(RecipeType.SilkRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("chef_ring_3")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 26, coolingRate: 9, startingThermal: 56, shapeShare: 38);
        }

        private void Tier4()
        {
            // Argos Ring
            _builder.Create(RecipeType.ArgosRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("ar_ring")
                .Level(31)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 1)
                .Component("ruby", 1)
                .ForgeParameters(rangeCenter: 47, rangeWidth: 27, coolingRate: 8, startingThermal: 54, shapeShare: 38);

            // Grenada Ring
            _builder.Create(RecipeType.GrenadaRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("gr_ring")
                .Level(31)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 1)
                .Component("ruby", 1)
                .ForgeParameters(rangeCenter: 45, rangeWidth: 20, coolingRate: 9, startingThermal: 43, shapeShare: 38);

            // Survival Ring
            _builder.Create(RecipeType.SurvivalRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("sur_ring")
                .Level(31)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 1)
                .Component("ruby", 1)
                .ForgeParameters(rangeCenter: 51, rangeWidth: 26, coolingRate: 9, startingThermal: 53, shapeShare: 38);

            // Dream Ring
            _builder.Create(RecipeType.DreamRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("majestic_ring001")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3)
                .ForgeParameters(rangeCenter: 47, rangeWidth: 23, coolingRate: 9, startingThermal: 49, shapeShare: 38);

            // Devotion Ring
            _builder.Create(RecipeType.DevotionRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("engi_ring_4")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3)
                .ForgeParameters(rangeCenter: 44, rangeWidth: 22, coolingRate: 9, startingThermal: 47, shapeShare: 38);

            // Oasis Ring
            _builder.Create(RecipeType.OasisRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("fabr_ring_4")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3)
                .ForgeParameters(rangeCenter: 49, rangeWidth: 25, coolingRate: 9, startingThermal: 52, shapeShare: 38);

            // Vintage Ring
            _builder.Create(RecipeType.VintageRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("chef_ring_4")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3)
                .ForgeParameters(rangeCenter: 45, rangeWidth: 23, coolingRate: 9, startingThermal: 49, shapeShare: 38);
        }

        private void Tier5()
        {
            // Eclipse Ring
            _builder.Create(RecipeType.EclipseRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("ec_ring")
                .Level(41)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 1)
                .Component("emerald", 1)
                .ForgeParameters(rangeCenter: 51, rangeWidth: 23, coolingRate: 8, startingThermal: 51, shapeShare: 38);

            // Transcendent Ring
            _builder.Create(RecipeType.TranscendentRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("tran_ring")
                .Level(41)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 1)
                .Component("emerald", 1)
                .ForgeParameters(rangeCenter: 51, rangeWidth: 24, coolingRate: 8, startingThermal: 50, shapeShare: 38);

            // Supreme Ring
            _builder.Create(RecipeType.SupremeRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("sup_ring")
                .Level(41)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 1)
                .Component("emerald", 1)
                .ForgeParameters(rangeCenter: 45, rangeWidth: 23, coolingRate: 8, startingThermal: 43, shapeShare: 38);

            // Eternal Ring
            _builder.Create(RecipeType.EternalRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("eterenal_ring")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3)
                .ForgeParameters(rangeCenter: 47, rangeWidth: 24, coolingRate: 9, startingThermal: 55, shapeShare: 38);

            // Skysteel Ring
            _builder.Create(RecipeType.SkysteelRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("engi_ring_5")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3)
                .ForgeParameters(rangeCenter: 44, rangeWidth: 24, coolingRate: 8, startingThermal: 51, shapeShare: 38);

            // Rose Ring
            _builder.Create(RecipeType.RoseRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("fabr_ring_5")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3)
                .ForgeParameters(rangeCenter: 52, rangeWidth: 24, coolingRate: 8, startingThermal: 58, shapeShare: 38);

            // Moonflame Ring
            _builder.Create(RecipeType.MoonflameRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("chef_ring_5")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3)
                .ForgeParameters(rangeCenter: 51, rangeWidth: 24, coolingRate: 8, startingThermal: 57, shapeShare: 38);

            // Chaos Ring
            _builder.Create(RecipeType.ChaosRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("ch_ring")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("emerald", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 52, rangeWidth: 24, coolingRate: 8, startingThermal: 58, shapeShare: 38);

            // Arkanian Dragon Ward Ring
            _builder.Create(RecipeType.ArkanianDragonWardRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("ark_ward_ring")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("ark_drg_scales", 5)
                .Component("chiro_shard", 2)
                .Component("ark_dragon_troph", 1)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 45, rangeWidth: 24, coolingRate: 8, startingThermal: 50, shapeShare: 38);

            // Arkanian Dragon Force Ring
            _builder.Create(RecipeType.ArkanianDragonForceRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("ark_force_ring")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("ark_drg_scales", 5)
                .Component("chiro_shard", 2)
                .Component("ark_dragon_troph", 1)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 44, rangeWidth: 24, coolingRate: 8, startingThermal: 51, shapeShare: 38);

            // Magus Ring
            _builder.Create(RecipeType.MagusRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("mag_ring")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("emerald", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 51, rangeWidth: 24, coolingRate: 8, startingThermal: 57, shapeShare: 38);

            // Immortal Ring
            _builder.Create(RecipeType.ImmortalRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("imm_ring")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("emerald", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 50, rangeWidth: 24, coolingRate: 9, startingThermal: 55, shapeShare: 38);
        }

        private void Tier1A()
        {
            // Warden Ring
            _builder.Create(RecipeType.WardenRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("fld_bul_ring")
                .Level(6)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 2)
                .Component("jade", 2)
                .Component("lth_ruined", 1)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 32, coolingRate: 3, startingThermal: 51, shapeShare: 38);

            // Mystic Ring
            _builder.Create(RecipeType.MysticRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("fld_chn_ring")
                .Level(6)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 2)
                .Component("jade", 2)
                .Component("fiberp_ruined", 1)
                .ForgeParameters(rangeCenter: 47, rangeWidth: 34, coolingRate: 3, startingThermal: 51, shapeShare: 38);

            // Vanguard Ring
            _builder.Create(RecipeType.VanguardRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("fld_skm_ring")
                .Level(6)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 2)
                .Component("jade", 2)
                .Component("elec_ruined", 1)
                .ForgeParameters(rangeCenter: 46, rangeWidth: 39, coolingRate: 3, startingThermal: 52, shapeShare: 38);
        }

        private void Tier2A()
        {
            // Bastion Ring
            _builder.Create(RecipeType.BastionRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("vet_bul_ring")
                .Level(16)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 2)
                .Component("agate", 2)
                .Component("lth_flawed", 1)
                .ForgeParameters(rangeCenter: 42, rangeWidth: 34, coolingRate: 5, startingThermal: 51, shapeShare: 36);

            // Oracle Ring
            _builder.Create(RecipeType.OracleRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("vet_chn_ring")
                .Level(16)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 2)
                .Component("agate", 2)
                .Component("fiberp_flawed", 1)
                .ForgeParameters(rangeCenter: 50, rangeWidth: 52, coolingRate: 2, startingThermal: 64, shapeShare: 39);

            // Onslaught Ring
            _builder.Create(RecipeType.OnslaughtRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("vet_skm_ring")
                .Level(16)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 2)
                .Component("agate", 2)
                .Component("elec_flawed", 1)
                .ForgeParameters(rangeCenter: 44, rangeWidth: 40, coolingRate: 4, startingThermal: 55, shapeShare: 37);
        }

        private void Tier3A()
        {
            // Sentinel Ring
            _builder.Create(RecipeType.SentinelRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("prm_bul_ring")
                .Level(26)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 2)
                .Component("citrine", 2)
                .Component("lth_good", 1)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 21, coolingRate: 9, startingThermal: 58, shapeShare: 38);

            // Arcanist Ring
            _builder.Create(RecipeType.ArcanistRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("prm_chn_ring")
                .Level(26)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 2)
                .Component("citrine", 2)
                .Component("fiberp_good", 1)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 20, coolingRate: 2, startingThermal: 49, shapeShare: 38);

            // Maverick Ring
            _builder.Create(RecipeType.MaverickRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("prm_skm_ring")
                .Level(26)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 2)
                .Component("citrine", 2)
                .Component("elec_good", 1)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 25, coolingRate: 6, startingThermal: 35, shapeShare: 38);
        }

        private void Tier4A()
        {
            // Aegis Ring
            _builder.Create(RecipeType.AegisRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("asc_bul_ring")
                .Level(36)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 2)
                .Component("ruby", 2)
                .Component("lth_imperfect", 1)
                .ForgeParameters(rangeCenter: 51, rangeWidth: 20, coolingRate: 9, startingThermal: 53, shapeShare: 38);

            // Luminary Ring
            _builder.Create(RecipeType.LuminaryRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("asc_chn_ring")
                .Level(36)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 2)
                .Component("ruby", 2)
                .Component("fiberp_imperfect", 1)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 26, coolingRate: 9, startingThermal: 51, shapeShare: 38);

            // Apex Ring
            _builder.Create(RecipeType.ApexRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("asc_skm_ring")
                .Level(36)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 2)
                .Component("ruby", 2)
                .Component("elec_imperfect", 1)
                .ForgeParameters(rangeCenter: 49, rangeWidth: 24, coolingRate: 9, startingThermal: 52, shapeShare: 38);
        }

}
}
