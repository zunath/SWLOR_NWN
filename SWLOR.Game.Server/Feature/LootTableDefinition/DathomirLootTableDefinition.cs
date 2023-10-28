using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class DathomirLootTableDefinition: ILootTableDefinition
    {
        private readonly LootTableBuilder _builder = new();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            Chirodactyl();
            DragonTurtle();
            KwiGuardian();
            KwiShaman();
            KwiTribal();
            Purbole();
            ShearMite();
            Sprantal();
            Squellbug();
            Ssurian();
            JungleBug();
            GapingSpider();

            return _builder.Build();
        }

        private void Chirodactyl()
        {
            _builder.Create("DATHOMIR_CHIRODACTYL")
                .AddItem("fiberp_high", 20)
                .AddItem("lth_high", 20)
                .AddItem("wild_meat", 10)
                .AddItem("wild_innards", 10)
                .AddItem("chiro_shard", 1);

            _builder.Create("DATHOMIR_CHIRODACTYL_RARES")
                .IsRare()
                .AddItem("emerald", 1, 1, true)
                .AddItem("chiro_shard", 1, 1, true);

            _builder.Create("DATHOMIR_CHIRODACTYL_GEMS")
                .AddItem("emerald", 100, 1, true)
                .AddItem("chiro_shard", 50, 1, true);

            _builder.Create("DATHOMIR_CHIRODACTYL_RECIPES")
                .AddItem("recipe_chigswd", 10)
                .AddItem("recipe_chispear", 10)
                .AddItem("recipe_chiknife", 10)
                .AddItem("recipe_chipistol", 10)
                .AddItem("recipe_chistaff", 10)
                .AddItem("recipe_chilngswd", 10)
                .AddItem("recipe_chikatar", 10)
                .AddItem("recipe_chishuri", 10)
                .AddItem("recipe_chirifle", 10)
                .AddItem("recipe_chitwinbl", 10)
                .AddItem("recipe_chielec", 10)
                .AddItem("recipe_saberupg1", 10)
                .AddItem("recipe_staffupg1", 10)
                .AddItem("recipe_chitelec", 10)
                .AddItem("recipe_chshield", 10)
                .AddItem("recipe_chcloak", 10)
                .AddItem("recipe_chbelt", 10)
                .AddItem("recipe_chring", 10)
                .AddItem("recipe_chneck", 10)
                .AddItem("recipe_chbreast", 10)
                .AddItem("recipe_chhelm", 10)
                .AddItem("recipe_chbracer", 10)
                .AddItem("recipe_chlegg", 10)
                .AddItem("recipe_mgcloak", 10)
                .AddItem("recipe_mgbelt", 10)
                .AddItem("recipe_mgring", 10)
                .AddItem("recipe_mgneck", 10)
                .AddItem("recipe_mgtunic", 10)
                .AddItem("recipe_mgcap", 10)
                .AddItem("recipe_mggloves", 10)
                .AddItem("recipe_mgboots", 10)
                .AddItem("recipe_imcloak", 10)
                .AddItem("recipe_imbelt", 10)
                .AddItem("recipe_imring", 10)
                .AddItem("recipe_imneck", 10)
                .AddItem("recipe_imtunic", 10)
                .AddItem("recipe_imcap", 10)
                .AddItem("recipe_imgloves", 10)
                .AddItem("recipe_imboots", 10)
                .AddItem("recipe_dhcl005c", 10)
                .AddItem("recipe_dhbe005c", 10)
                .AddItem("recipe_dhrg005c", 10)
                .AddItem("recipe_dhnk005c", 10)
                .AddItem("recipe_dhar005c", 10)
                .AddItem("recipe_dhhl005c", 10)
                .AddItem("recipe_dhbr005c", 10)
                .AddItem("recipe_dhlg005c", 10)
                .AddItem("recipe_dlcl005c", 10)
                .AddItem("recipe_dlbe005c", 10)
                .AddItem("recipe_dlrg005c", 10)
                .AddItem("recipe_dlnk005c", 10)
                .AddItem("recipe_dlar005c", 10)
                .AddItem("recipe_dlhl005c", 10)
                .AddItem("recipe_dlbr005c", 10)
                .AddItem("recipe_dllg005c", 10)
                .AddItem("recipe_brsushi", 10)
                .AddItem("recipe_ocsushi", 10)
                .AddItem("recipe_iksushi", 10)
                .AddItem("recipe_wisushi", 10)
                .AddItem("recipe_tesushi", 10)
                .AddItem("recipe_dosushi", 10);
        }

        private void DragonTurtle()
        {
            _builder.Create("DATHOMIR_DRAGON_TURTLE")
                .AddItem("fiberp_high", 1)
                .AddItem("lth_high", 1)
                .AddItem("wild_meat", 10)
                .AddItem("wild_innards", 10);

            _builder.Create("DATHOMIR_DRAGON_TURTLE_RARES")
                .IsRare()
                .AddItem("ruby", 20, 1, true)
                .AddItem("emerald", 80, 1, true)
                .AddItem("red_shell_shard", 1, 1, true)
                .AddItem("whit_shell_shard", 1, 1, true)
                .AddItem("grn_shell_shard", 1, 1, true)
                .AddItem("yell_shell_shard", 1, 1, true);
        }

        private void KwiGuardian()
        {
            _builder.Create("DATHOMIR_KWI_GUARDIAN")
                .AddItem("fiberp_imperfect", 5)
                .AddItem("fiberp_high", 5)
                .AddItem("lth_imperfect", 5)
                .AddItem("lth_high", 5);

            _builder.Create("DATHOMIR_KWI_GUARDIAN_GEAR")
                .AddItem("kwi_knife", 20)
                .AddItem("kwi_greatsword", 20)
                .AddItem("kwi_longsword", 20)
                .AddItem("kwi_electroblade", 20)
                .AddItem("kwi_katar", 20)
                .AddItem("kwi_staff", 20)
                .AddItem("kwi_twinblade", 20)
                .AddItem("kwi_twinelec", 20)
                .AddItem("kwi_spear", 20)
                .AddItem("kwi_pistol", 20)
                .AddItem("kwi_rifle", 20)
                .AddItem("kwi_shield", 10)
                .AddItem("kwi_h_cloak", 10)
                .AddItem("dhcl005k", 10)
                .AddItem("dlcl005k", 10)
                .AddItem("kwi_hyper_cloak", 10)
                .AddItem("kwi_light_cloak", 10)
                .AddItem("dhbe005k", 10)
                .AddItem("dlbe005k", 10)
                .AddItem("kwi_heavy_belt", 10)
                .AddItem("kwi_hyper_belt", 10)
                .AddItem("kwi_light_belt", 10)
                .AddItem("dhrg005k", 10)
                .AddItem("dlrg005k", 10)
                .AddItem("kwi_heavy_ring", 10)
                .AddItem("kwi_hyper_ring", 10)
                .AddItem("kwi_light_ring", 10)
                .AddItem("dhnk005k", 10)
                .AddItem("dlnk005k", 10)
                .AddItem("kwi_heavy_neck", 10)
                .AddItem("kwi_hyper_neck", 10)
                .AddItem("kwi_light_neck", 10)
                .AddItem("dhbr005k", 10)
                .AddItem("kwi_heavy_bracer", 10)
                .AddItem("dlbr005k", 10)
                .AddItem("kwi_hyper_gloves", 10)
                .AddItem("kwi_light_gloves", 10)
                .AddItem("dhlg005k", 10)
                .AddItem("dllg005k", 10)
                .AddItem("kwi_heavy_leg", 10)
                .AddItem("kwi_hyper_boots", 10)
                .AddItem("kwi_light_boots", 10);

            _builder.Create("DATHOMIR_KWI_GUARDIAN_GEAR_RARES")
                .AddItem("kwi_heavy_armor", 1)
                .AddItem("kwi_hyper_tunic", 1)
                .AddItem("kwi_light_tunic", 1)
                .AddItem("dhar005k", 10)
                .AddItem("dlar005k", 10)
                .AddItem("dhhl005k", 10)
                .AddItem("dlhl005k", 10)
                .AddItem("kwi_heavy_helm", 10)
                .AddItem("kwi_hyper_cap", 10)
                .AddItem("kwi_light_cap", 10);

            _builder.Create("DATHOMIR_KWI_GUARDIAN_RARES")
                .IsRare()
                .AddItem("ruby", 99, 1, true)
                .AddItem("map_61", 2, 1, true)
                .AddItem("map_62", 2, 1, true)
                .AddItem("map_63", 2, 1, true)
                .AddItem("map_64", 2, 1, true)
                .AddItem("map_65", 2, 1, true)
                .AddItem("map_66", 2, 1, true)
                .AddItem("map_67", 2, 1, true)
                .AddItem("map_68", 2, 1, true)
                .AddItem("map_69", 2, 1, true)
                .AddItem("emerald", 1, 1, true);
        }

        private void KwiShaman()
        {
            _builder.Create("DATHOMIR_KWI_SHAMAN")
                .AddItem("fiberp_imperfect", 5)
                .AddItem("fiberp_high", 10)
                .AddItem("lth_imperfect", 5)
                .AddItem("lth_high", 10)
                .AddItem("bread_flour", 5);

            _builder.Create("DATHOMIR_KWI_SHAMAN_GEAR")
                .AddItem("kwi_knife", 20)
                .AddItem("kwi_greatsword", 20)
                .AddItem("kwi_longsword", 20)
                .AddItem("kwi_electroblade", 20)
                .AddItem("kwi_katar", 20)
                .AddItem("kwi_staff", 20)
                .AddItem("kwi_twinblade", 20)
                .AddItem("kwi_twinelec", 20)
                .AddItem("kwi_spear", 20)
                .AddItem("kwi_pistol", 20)
                .AddItem("kwi_rifle", 20)
                .AddItem("kwi_shield", 10)
                .AddItem("kwi_h_cloak", 10)
                .AddItem("dhcl005k", 10)
                .AddItem("dlcl005k", 10)
                .AddItem("kwi_hyper_cloak", 10)
                .AddItem("kwi_light_cloak", 10)
                .AddItem("dhbe005k", 10)
                .AddItem("dlbe005k", 10)
                .AddItem("kwi_heavy_belt", 10)
                .AddItem("kwi_hyper_belt", 10)
                .AddItem("kwi_light_belt", 10)
                .AddItem("dhrg005k", 10)
                .AddItem("dlrg005k", 10)
                .AddItem("kwi_heavy_ring", 10)
                .AddItem("kwi_hyper_ring", 10)
                .AddItem("kwi_light_ring", 10)
                .AddItem("dhnk005k", 10)
                .AddItem("dlnk005k", 10)
                .AddItem("kwi_heavy_neck", 10)
                .AddItem("kwi_hyper_neck", 10)
                .AddItem("kwi_light_neck", 10)
                .AddItem("dhbr005k", 10)
                .AddItem("kwi_heavy_bracer", 10)
                .AddItem("dlbr005k", 10)
                .AddItem("kwi_hyper_gloves", 10)
                .AddItem("kwi_light_gloves", 10)
                .AddItem("dhlg005k", 10)
                .AddItem("dllg005k", 10)
                .AddItem("kwi_heavy_leg", 10)
                .AddItem("kwi_hyper_boots", 10)
                .AddItem("kwi_light_boots", 10);

            _builder.Create("DATHOMIR_KWI_SHAMAN_GEAR_RARES")
                .AddItem("kwi_heavy_armor", 1)
                .AddItem("kwi_hyper_tunic", 1)
                .AddItem("kwi_light_tunic", 1)
                .AddItem("dhar005k", 10)
                .AddItem("dlar005k", 10)
                .AddItem("dhhl005k", 10)
                .AddItem("dlhl005k", 10)
                .AddItem("kwi_heavy_helm", 10)
                .AddItem("kwi_hyper_cap", 10)
                .AddItem("kwi_light_cap", 10);

            _builder.Create("DATHOMIR_KWI_SHAMAN_RARES")
                .IsRare()
                .AddItem("ruby", 99, 1, true)
                .AddItem("map_61", 2, 1, true)
                .AddItem("map_62", 2, 1, true)
                .AddItem("map_63", 2, 1, true)
                .AddItem("map_64", 2, 1, true)
                .AddItem("map_65", 2, 1, true)
                .AddItem("map_66", 2, 1, true)
                .AddItem("map_67", 2, 1, true)
                .AddItem("map_68", 2, 1, true)
                .AddItem("map_69", 2, 1, true)
                .AddItem("emerald", 1, 1, true);
        }

        private void KwiTribal()
        {
            _builder.Create("DATHOMIR_KWI_TRIBAL")
                .AddItem("fiberp_imperfect", 10)
                .AddItem("fiberp_high", 5)
                .AddItem("lth_imperfect", 10)
                .AddItem("lth_high", 5)
                .AddItem("bread_flour", 5);

            _builder.Create("DATHOMIR_KWI_TRIBAL_GEAR")
                .AddItem("kwi_knife", 20)
                .AddItem("kwi_greatsword", 20)
                .AddItem("kwi_longsword", 20)
                .AddItem("kwi_electroblade", 20)
                .AddItem("kwi_katar", 20)
                .AddItem("kwi_staff", 20)
                .AddItem("kwi_twinblade", 20)
                .AddItem("kwi_twinelec", 20)
                .AddItem("kwi_spear", 20)
                .AddItem("kwi_pistol", 20)
                .AddItem("kwi_rifle", 20)
                .AddItem("kwi_shield", 10)
                .AddItem("kwi_h_cloak", 10)
                .AddItem("dhcl005k", 10)
                .AddItem("dlcl005k", 10)
                .AddItem("kwi_hyper_cloak", 10)
                .AddItem("kwi_light_cloak", 10)
                .AddItem("dhbe005k", 10)
                .AddItem("dlbe005k", 10)
                .AddItem("kwi_heavy_belt", 10)
                .AddItem("kwi_hyper_belt", 10)
                .AddItem("kwi_light_belt", 10)
                .AddItem("dhrg005k", 10)
                .AddItem("dlrg005k", 10)
                .AddItem("kwi_heavy_ring", 10)
                .AddItem("kwi_hyper_ring", 10)
                .AddItem("kwi_light_ring", 10)
                .AddItem("dhnk005k", 10)
                .AddItem("dlnk005k", 10)
                .AddItem("kwi_heavy_neck", 10)
                .AddItem("kwi_hyper_neck", 10)
                .AddItem("kwi_light_neck", 10)
                .AddItem("dhbr005k", 10)
                .AddItem("kwi_heavy_bracer", 10)
                .AddItem("dlbr005k", 10)
                .AddItem("kwi_hyper_gloves", 10)
                .AddItem("kwi_light_gloves", 10)
                .AddItem("dhlg005k", 10)
                .AddItem("dllg005k", 10)
                .AddItem("kwi_heavy_leg", 10)
                .AddItem("kwi_hyper_boots", 10)
                .AddItem("kwi_light_boots", 10);

            _builder.Create("DATHOMIR_KWI_TRIBAL_GEAR_RARES")
                .AddItem("kwi_heavy_armor", 1)
                .AddItem("kwi_hyper_tunic", 1)
                .AddItem("kwi_light_tunic", 1)
                .AddItem("dhar005k", 10)
                .AddItem("dlar005k", 10)
                .AddItem("dhhl005k", 10)
                .AddItem("dlhl005k", 10)
                .AddItem("kwi_heavy_helm", 10)
                .AddItem("kwi_hyper_cap", 10)
                .AddItem("kwi_light_cap", 10);

            _builder.Create("DATHOMIR_KWI_TRIBAL_RARES")
                .IsRare()
                .AddItem("ruby", 99, 1, true)
                .AddItem("map_61", 2, 1, true)
                .AddItem("map_62", 2, 1, true)
                .AddItem("map_63", 2, 1, true)
                .AddItem("map_64", 2, 1, true)
                .AddItem("map_65", 2, 1, true)
                .AddItem("map_66", 2, 1, true)
                .AddItem("map_67", 2, 1, true)
                .AddItem("map_68", 2, 1, true)
                .AddItem("map_69", 2, 1, true)
                .AddItem("emerald", 1, 1, true);
        }

        private void Purbole()
        {
            _builder.Create("DATHOMIR_PURBOLE")
                .AddItem("lth_imperfect", 5)
                .AddItem("lth_high", 10)
                .AddItem("wild_innards", 10);

            _builder.Create("DATHOMIR_PURBOLE_RARES")
                .IsRare()
                .AddItem("ruby", 99, 1, true)
                .AddItem("map_61", 2, 1, true)
                .AddItem("map_62", 2, 1, true)
                .AddItem("map_63", 2, 1, true)
                .AddItem("map_64", 2, 1, true)
                .AddItem("map_65", 2, 1, true)
                .AddItem("map_66", 2, 1, true)
                .AddItem("map_67", 2, 1, true)
                .AddItem("map_68", 2, 1, true)
                .AddItem("map_69", 2, 1, true)
                .AddItem("emerald", 1, 1, true);
        }

        private void ShearMite()
        {
            _builder.Create("DATHOMIR_SHEAR_MITE")
                .AddItem("wild_innards", 10)
                .AddItem("wild_blood", 2);

            _builder.Create("DATHOMIR_SHEAR_MITE_RARES")
                .IsRare()
                .AddItem("ruby", 99, 1, true)
                .AddItem("map_61", 2, 1, true)
                .AddItem("map_62", 2, 1, true)
                .AddItem("map_63", 2, 1, true)
                .AddItem("map_64", 2, 1, true)
                .AddItem("map_65", 2, 1, true)
                .AddItem("map_66", 2, 1, true)
                .AddItem("map_67", 2, 1, true)
                .AddItem("map_68", 2, 1, true)
                .AddItem("map_69", 2, 1, true)
                .AddItem("emerald", 1, 1, true);
        }

        private void Sprantal()
        {
            _builder.Create("DATHOMIR_SPRANTAL")
                .AddItem("lth_high", 5)
                .AddItem("lth_imperfect", 10)
                .AddItem("fiberp_high", 5)
                .AddItem("fiberp_imperfect", 10);

            _builder.Create("DATHOMIR_SPRANTAL_RARES")
                .IsRare()
                .AddItem("ruby", 99, 1, true)
                .AddItem("map_61", 2, 1, true)
                .AddItem("map_62", 2, 1, true)
                .AddItem("map_63", 2, 1, true)
                .AddItem("map_64", 2, 1, true)
                .AddItem("map_65", 2, 1, true)
                .AddItem("map_66", 2, 1, true)
                .AddItem("map_67", 2, 1, true)
                .AddItem("map_68", 2, 1, true)
                .AddItem("map_69", 2, 1, true)
                .AddItem("emerald", 1, 1, true);
        }

        private void Squellbug()
        {
            _builder.Create("DATHOMIR_SQUELLBUG")
                .AddItem("wild_innards", 10)
                .AddItem("wild_leg", 5)
                .AddItem("tomato", 1)
                .AddItem("cultured_butter", 1);

            _builder.Create("DATHOMIR_SQUELLBUG_RARES")
                .IsRare()
                .AddItem("ruby", 99, 1, true)
                .AddItem("map_61", 2, 1, true)
                .AddItem("map_62", 2, 1, true)
                .AddItem("map_63", 2, 1, true)
                .AddItem("map_64", 2, 1, true)
                .AddItem("map_65", 2, 1, true)
                .AddItem("map_66", 2, 1, true)
                .AddItem("map_67", 2, 1, true)
                .AddItem("map_68", 2, 1, true)
                .AddItem("map_69", 2, 1, true)
                .AddItem("emerald", 1, 1, true);
        }

        private void Ssurian()
        {
            _builder.Create("DATHOMIR_SSURIAN")
                .AddItem("lth_high", 10)
                .AddItem("lth_imperfect", 5)
                .AddItem("fiberp_high", 10)
                .AddItem("fiberp_imperfect", 5);

            _builder.Create("DATHOMIR_SSURIAN_RARES")
                .IsRare()
                .AddItem("ruby", 99, 1, true)
                .AddItem("map_61", 2, 1, true)
                .AddItem("map_62", 2, 1, true)
                .AddItem("map_63", 2, 1, true)
                .AddItem("map_64", 2, 1, true)
                .AddItem("map_65", 2, 1, true)
                .AddItem("map_66", 2, 1, true)
                .AddItem("map_67", 2, 1, true)
                .AddItem("map_68", 2, 1, true)
                .AddItem("map_69", 2, 1, true)
                .AddItem("emerald", 1, 1, true);
        }

        private void JungleBug()
        {
            _builder.Create("DATHOMIR_JUNGLE_BUG")
                .AddItem("wild_meat", 10)
                .AddItem("herb_x", 5)
                .AddItem("wild_leg", 2);

            _builder.Create("DATHOMIR_JUNGLE_BUG_RARES")
                .IsRare()
                .AddItem("ruby", 99, 1, true)
                .AddItem("map_61", 2, 1, true)
                .AddItem("map_62", 2, 1, true)
                .AddItem("map_63", 2, 1, true)
                .AddItem("map_64", 2, 1, true)
                .AddItem("map_65", 2, 1, true)
                .AddItem("map_66", 2, 1, true)
                .AddItem("map_67", 2, 1, true)
                .AddItem("map_68", 2, 1, true)
                .AddItem("map_69", 2, 1, true)
                .AddItem("emerald", 1, 1, true);


        }

        private void GapingSpider()
        {
            _builder.Create("DATHOMIR_GAPING_SPIDER")
                .AddItem("lth_high", 10)
                .AddItem("lth_imperfect", 5)
                .AddItem("spider_guts", 10)
                .AddItem("spider_leg", 5);

            _builder.Create("DATHOMIR_GAPING_SPIDER")
                .IsRare()
                .AddItem("spider_thread", 99, 1, true)
                .AddItem("map_61", 2, 1, true)
                .AddItem("map_62", 2, 1, true)
                .AddItem("map_63", 2, 1, true)
                .AddItem("map_64", 2, 1, true)
                .AddItem("map_65", 2, 1, true)
                .AddItem("map_66", 2, 1, true)
                .AddItem("map_67", 2, 1, true)
                .AddItem("map_68", 2, 1, true)
                .AddItem("map_69", 2, 1, true)
                .AddItem("emerald", 1, 1, true);
        }

    }
}
