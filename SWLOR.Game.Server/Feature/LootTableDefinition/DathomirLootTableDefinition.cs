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

            CapstoneDathomirDungeonRares();
            DathomirGrottoRareElites();
            DathtarnRareElites();
            return _builder.Build();
        }

        private void DathtarnRareElites()
        {
            _builder.Create("DATHTARN_APEX_RARES").IsRare()
                .AddItem("tarnapexmawda", 1, 1, true).AddItem("tarnapexmawdb", 1, 1, true).AddItem("bp_tarnapexmaw", 1, 1, true);
            _builder.Create("DATHTARN_QUILL_RARES").IsRare()
                .AddItem("quillstalkerda", 1, 1, true).AddItem("quillstalkerdb", 1, 1, true).AddItem("bp_quillstalker", 1, 1, true);
            _builder.Create("DATHTARN_RHYDEL_RARES").IsRare()
                .AddItem("rhydelalphada", 1, 1, true).AddItem("rhydelalphadb", 1, 1, true).AddItem("bp_rhydelalpha", 1, 1, true);
            _builder.Create("DATHTARN_APEX_COMP").AddItem("tarnapexmawcm", 1, 1);
            _builder.Create("DATHTARN_QUILL_COMP").AddItem("quillstalkercm", 1, 1);
            _builder.Create("DATHTARN_RHYDEL_COMP").AddItem("rhydelalphacm", 1, 1);
        }

        private void DathomirGrottoRareElites()
        {
            _builder.Create("DATHGROTTO_ALPHA_RARES").IsRare()
                .AddItem("bone_guard", 1, 1, true).AddItem("thornhide", 1, 1, true).AddItem("bp_fanggaunt", 1, 1, true);
            _builder.Create("DATHGROTTO_SPINE_RARES").IsRare()
                .AddItem("quill_bracer", 1, 1, true).AddItem("beasthide", 1, 1, true).AddItem("bp_ridgeplate", 1, 1, true);
            _builder.Create("DATHGROTTO_STALKER_RARES").IsRare()
                .AddItem("totem_charm", 1, 1, true).AddItem("spirit_hide", 1, 1, true).AddItem("bp_ritecrown", 1, 1, true);
            _builder.Create("DATHGROTTO_ALPHA_COMP").AddItem("alpha_fang", 1, 1);
            _builder.Create("DATHGROTTO_SPINE_COMP").AddItem("spine_quill", 1, 1);
            _builder.Create("DATHGROTTO_STALKER_COMP").AddItem("spirit_totem", 1, 1);
        }

        private void CapstoneDathomirDungeonRares()
        {
            _builder.Create("CAPSTONE_PRIMOVER_RARES")
                .IsRare()
                .AddItem("primover_l1", 1, 1, true)
                .AddItem("primover_l2", 1, 1, true)
                .AddItem("primover_l3", 1, 1, true)
                .AddItem("primover_l4", 1, 1, true)
                .AddItem("primover_l5", 1, 1, true)
                .AddItem("primover_l6", 1, 1, true)
                .AddItem("primover_l7", 1, 1, true)
                .AddItem("primover_l8", 1, 1, true);
            _builder.Create("CAPSTONE_PRIMOVER_WD_RARES")
                .IsRare()
                .AddItem("primover_w1", 1, 1, true)
                .AddItem("primover_w2", 1, 1, true)
                .AddItem("primover_w3", 1, 1, true)
                .AddItem("primover_w4", 1, 1, true)
                .AddItem("primover_w5", 1, 1, true);

            _builder.Create("CAPSTONE_UNTINST_RARES")
                .IsRare()
                .AddItem("untinst_l1", 1, 1, true)
                .AddItem("untinst_l2", 1, 1, true)
                .AddItem("untinst_l3", 1, 1, true)
                .AddItem("untinst_l4", 1, 1, true)
                .AddItem("untinst_l5", 1, 1, true)
                .AddItem("untinst_l6", 1, 1, true)
                .AddItem("untinst_l7", 1, 1, true)
                .AddItem("untinst_l8", 1, 1, true);
            _builder.Create("CAPSTONE_UNTINST_WD_RARES")
                .IsRare()
                .AddItem("untinst_w1", 1, 1, true)
                .AddItem("untinst_w2", 1, 1, true)
                .AddItem("untinst_w3", 1, 1, true)
                .AddItem("untinst_w4", 1, 1, true)
                .AddItem("untinst_w5", 1, 1, true);

            _builder.Create("CAPSTONE_FORCEBEAST_RARES")
                .IsRare()
                .AddItem("forcebeast_l1", 1, 1, true)
                .AddItem("forcebeast_l2", 1, 1, true)
                .AddItem("forcebeast_l3", 1, 1, true)
                .AddItem("forcebeast_l4", 1, 1, true)
                .AddItem("forcebeast_l5", 1, 1, true)
                .AddItem("forcebeast_l6", 1, 1, true)
                .AddItem("forcebeast_l7", 1, 1, true)
                .AddItem("forcebeast_l8", 1, 1, true);
            _builder.Create("CAPSTONE_FORCEBEAST_WD_RARES")
                .IsRare()
                .AddItem("forcebeast_w1", 1, 1, true)
                .AddItem("forcebeast_w2", 1, 1, true)
                .AddItem("forcebeast_w3", 1, 1, true)
                .AddItem("forcebeast_w4", 1, 1, true)
                .AddItem("forcebeast_w5", 1, 1, true);

            _builder.Create("CAPSTONE_APEXBITE_RARES")
                .IsRare()
                .AddItem("apexbite_l1", 1, 1, true)
                .AddItem("apexbite_l2", 1, 1, true)
                .AddItem("apexbite_l3", 1, 1, true)
                .AddItem("apexbite_l4", 1, 1, true)
                .AddItem("apexbite_l5", 1, 1, true)
                .AddItem("apexbite_l6", 1, 1, true)
                .AddItem("apexbite_l7", 1, 1, true)
                .AddItem("apexbite_l8", 1, 1, true);
            _builder.Create("CAPSTONE_APEXBITE_WD_RARES")
                .IsRare()
                .AddItem("apexbite_w1", 1, 1, true)
                .AddItem("apexbite_w2", 1, 1, true)
                .AddItem("apexbite_w3", 1, 1, true)
                .AddItem("apexbite_w4", 1, 1, true)
                .AddItem("apexbite_w5", 1, 1, true);

            _builder.Create("CAPSTONE_UNBRBEAST_RARES")
                .IsRare()
                .AddItem("unbrbeast_l1", 1, 1, true)
                .AddItem("unbrbeast_l2", 1, 1, true)
                .AddItem("unbrbeast_l3", 1, 1, true)
                .AddItem("unbrbeast_l4", 1, 1, true)
                .AddItem("unbrbeast_l5", 1, 1, true)
                .AddItem("unbrbeast_l6", 1, 1, true)
                .AddItem("unbrbeast_l7", 1, 1, true)
                .AddItem("unbrbeast_l8", 1, 1, true);
            _builder.Create("CAPSTONE_UNBRBEAST_WD_RARES")
                .IsRare()
                .AddItem("unbrbeast_w1", 1, 1, true)
                .AddItem("unbrbeast_w2", 1, 1, true)
                .AddItem("unbrbeast_w3", 1, 1, true)
                .AddItem("unbrbeast_w4", 1, 1, true)
                .AddItem("unbrbeast_w5", 1, 1, true);

            _builder.Create("CAPSTONE_ALPHARHY_RARES")
                .IsRare()
                .AddItem("alpharhy_l1", 1, 1, true)
                .AddItem("alpharhy_l2", 1, 1, true)
                .AddItem("alpharhy_l3", 1, 1, true)
                .AddItem("alpharhy_l4", 1, 1, true)
                .AddItem("alpharhy_l5", 1, 1, true)
                .AddItem("alpharhy_l6", 1, 1, true)
                .AddItem("alpharhy_l7", 1, 1, true)
                .AddItem("alpharhy_l8", 1, 1, true);
            _builder.Create("CAPSTONE_ALPHARHY_WD_RARES")
                .IsRare()
                .AddItem("alpharhy_w1", 1, 1, true)
                .AddItem("alpharhy_w2", 1, 1, true)
                .AddItem("alpharhy_w3", 1, 1, true)
                .AddItem("alpharhy_w4", 1, 1, true)
                .AddItem("alpharhy_w5", 1, 1, true);
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
                .AddItem("fnote_2001", 2, 1, true)
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
                .AddItem("recipe_chlsupg", 10)
                .AddItem("recipe_chssupg", 10)
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
                .AddItem("fnote_2031", 2, 1, true)
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
                .AddItem("emerald", 1, 1, true)
                .AddItem("lockbox_t5", 2, 1, true);
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

            _builder.Create("DATHOMIR_GAPING_SPIDER_RARES")
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
