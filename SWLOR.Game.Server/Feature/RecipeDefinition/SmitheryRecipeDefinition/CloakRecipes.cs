using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class CloakRecipes : IRecipeListDefinition
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
            // Battlemaster Cloak
            _builder.Create(RecipeType.BattlemasterCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("bm_cloak")
                .Level(8)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 5)
                .Component("fiberp_ruined", 3);

            // Spiritmaster Cloak
            _builder.Create(RecipeType.SpiritmasterCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("sm_cloak")
                .Level(8)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 5)
                .Component("fiberp_ruined", 3);

            // Combat Cloak
            _builder.Create(RecipeType.CombatCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("com_cloak")
                .Level(8)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 5)
                .Component("fiberp_ruined", 3);

            // Advent Cloak
            _builder.Create(RecipeType.AdventCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("advent_cloak")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // Amateur Cloak
            _builder.Create(RecipeType.AmateurCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("engi_cloak_1")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // Cloth Cloak
            _builder.Create(RecipeType.ClothCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("fabr_belt_1")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // Chef Cloak
            _builder.Create(RecipeType.ChefCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("chef_cloak_1")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);
        }

        private void Tier2()
        {
            // Titan Cloak
            _builder.Create(RecipeType.TitanCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("tit_cloak")
                .Level(18)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 5)
                .Component("fiberp_flawed", 3);

            // Vivid Cloak
            _builder.Create(RecipeType.VividCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("viv_cloak")
                .Level(18)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 5)
                .Component("fiberp_flawed", 3);

            // Valor Cloak
            _builder.Create(RecipeType.ValorCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("val_cloak")
                .Level(18)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 5)
                .Component("fiberp_flawed", 3);

            // Frontier Cloak
            _builder.Create(RecipeType.FrontierCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("frontier_cloak")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // Worker Cloak
            _builder.Create(RecipeType.WorkerCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("engi_cloak_2")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // Linen Cloak
            _builder.Create(RecipeType.LinenCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("fabr_cloak_2")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // Velveteen Cloak
            _builder.Create(RecipeType.VelveteenCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("chef_cloak_2")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);
        }

        private void Tier3()
        {
            // Quark Cloak
            _builder.Create(RecipeType.QuarkCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("qk_cloak")
                .Level(28)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 5)
                .Component("fiberp_good", 3);

            // Reginal Cloak
            _builder.Create(RecipeType.ReginalCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("reg_cloak")
                .Level(28)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 5)
                .Component("fiberp_good", 3);

            // Forza Cloak
            _builder.Create(RecipeType.ForzaCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("for_cloak")
                .Level(28)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 5)
                .Component("fiberp_good", 3);

            // Majestic Cloak
            _builder.Create(RecipeType.MajesticCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("majestic_cloak")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // Mechanic Cloak
            _builder.Create(RecipeType.MechanicCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("engi_cloak_3")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // Designer Cloak
            _builder.Create(RecipeType.DesignerCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("fabr_cloak_3")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // Silk Cloak
            _builder.Create(RecipeType.SilkCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("chef_cloak_3")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

        }

        private void Tier4()
        {
            // Argos Cloak
            _builder.Create(RecipeType.ArgosCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("ar_cloak")
                .Level(38)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 5)
                .Component("fiberp_imperfect", 3);

            // Grenada Cloak
            _builder.Create(RecipeType.GrenadaCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("gre_cloak")
                .Level(38)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 5)
                .Component("fiberp_imperfect", 3);

            // Survival Cloak
            _builder.Create(RecipeType.SurvivalCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("sur_cloak")
                .Level(38)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 5)
                .Component("fiberp_imperfect", 3);

            // Dream Cloak
            _builder.Create(RecipeType.DreamCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("dream_cloak")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // Devotion Cloak
            _builder.Create(RecipeType.DevotionCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("engi_cloak_4")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // Oasis Cloak
            _builder.Create(RecipeType.OasisCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("fabr_cloak_4")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // Vintage Cloak
            _builder.Create(RecipeType.VintageCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("chef_cloak_4")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);
        }

        private void Tier5()
        {
            // Eclipse Cloak
            _builder.Create(RecipeType.EclipseCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("ec_cloak")
                .Level(48)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 5)
                .Component("fiberp_high", 3);

            // Transcendent Cloak
            _builder.Create(RecipeType.TranscendentCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("tran_cloak")
                .Level(48)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 5)
                .Component("fiberp_high", 3);

            // Supreme Cloak
            _builder.Create(RecipeType.SupremeCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("sup_cloak")
                .Level(48)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 5)
                .Component("fiberp_high", 3);

            // Eternal Cloak
            _builder.Create(RecipeType.EternalCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("eternal_cloak")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Skysteel Cloak
            _builder.Create(RecipeType.SkysteelCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("engi_cloak_5")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Rose Cloak
            _builder.Create(RecipeType.RoseCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("fabr_cloak_5")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Moonflame Cloak
            _builder.Create(RecipeType.MoonflameCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("chef_cloak_5")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Chaos Cloak
            _builder.Create(RecipeType.ChaosCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("ch_cloak")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("lth_high", 20)
                .Component("fiberp_high", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5);

            // Magus Cloak
            _builder.Create(RecipeType.MagusCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("mag_cloak")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("lth_high", 20)
                .Component("fiberp_high", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5);

            // Immortal Cloak
            _builder.Create(RecipeType.ImmortalCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("imm_cloak")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("lth_high", 20)
                .Component("fiberp_high", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5);
        }
        private void Tier1A()
        {
            // Warden Cloak
            _builder.Create(RecipeType.WardenCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("fld_bul_cloak")
                .Level(13)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 5)
                .Component("fiberp_ruined", 3);

            // Mystic Cloak
            _builder.Create(RecipeType.MysticCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("fld_chn_cloak")
                .Level(13)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 5)
                .Component("fiberp_ruined", 3);

            // Vanguard Cloak
            _builder.Create(RecipeType.VanguardCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("fld_skm_cloak")
                .Level(13)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 5)
                .Component("fiberp_ruined", 3);
        }

        private void Tier2A()
        {
            // Bastion Cloak
            _builder.Create(RecipeType.BastionCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("vet_bul_cloak")
                .Level(23)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 5)
                .Component("fiberp_flawed", 3);

            // Oracle Cloak
            _builder.Create(RecipeType.OracleCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("vet_chn_cloak")
                .Level(23)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 5)
                .Component("fiberp_flawed", 3);

            // Onslaught Cloak
            _builder.Create(RecipeType.OnslaughtCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("vet_skm_cloak")
                .Level(23)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 5)
                .Component("fiberp_flawed", 3);
        }

        private void Tier3A()
        {
            // Sentinel Cloak
            _builder.Create(RecipeType.SentinelCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("prm_bul_cloak")
                .Level(33)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 5)
                .Component("fiberp_good", 3);

            // Arcanist Cloak
            _builder.Create(RecipeType.ArcanistCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("prm_chn_cloak")
                .Level(33)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 5)
                .Component("fiberp_good", 3);

            // Maverick Cloak
            _builder.Create(RecipeType.MaverickCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("prm_skm_cloak")
                .Level(33)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 5)
                .Component("fiberp_good", 3);
        }

        private void Tier4A()
        {
            // Aegis Cloak
            _builder.Create(RecipeType.AegisCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("asc_bul_cloak")
                .Level(43)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 5)
                .Component("fiberp_imperfect", 3);

            // Luminary Cloak
            _builder.Create(RecipeType.LuminaryCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("asc_chn_cloak")
                .Level(43)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 5)
                .Component("fiberp_imperfect", 3);

            // Apex Cloak
            _builder.Create(RecipeType.ApexCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("asc_skm_cloak")
                .Level(43)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 5)
                .Component("fiberp_imperfect", 3);
        }

}
}
