using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class ViscaraLootTableDefinition: ILootTableDefinition
    {
        private readonly LootTableBuilder _builder = new();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            KathHound();
            MandalorianLeader();
            MandalorianRanger();
            MandalorianWarrior();
            MandalorianHunter();
            MandalorianScout();
            Outlaw();
            Gimpassa();
            Kinrath();
            Cairnmog();
            VellenFleshleader();
            VellenFlesheater();
            Raivor();
            Warocas();
            Nashtah();
            CrystalSpider();
            SewersDepthsEntry();
            SewersDepthsButcher();
            SewersDepthsCircle();

            MandalorianCrate();
            CoxxionCrate();

            return _builder.Build();
        }

        private void KathHound()
        {
            _builder.Create("VISCARA_KATH_HOUND")
                .AddItem("k_hound_fur", 25)
                .AddItem("k_hound_tooth", 25)
                .AddItem("lth_ruined", 15)
                .AddItem("kath_meat_1", 15);

            _builder.Create("VISCARA_KATH_HOUND_RARES")
                .IsRare()
                .AddItem("kath_blood", 2, 1, true)
                .AddItem("k_hound_claw", 1, 1, true);

            _builder.Create("VISCARA_OLD_SCAR_RARES")
                .IsRare()
                .AddItem("recipe_osvest", 1, 1, true)
                .AddItem("recipe_oswrap", 1, 1, true)
                .AddItem("recipe_ostread", 1, 1, true)
                .AddItem("recipe_ossash", 1, 1, true)
                .AddItem("recipe_osmantle", 1, 1, true)
                .AddItem("recipe_oscollar", 1, 1, true)
                .AddItem("recipe_osband", 1, 1, true)
                .AddItem("recipe_osguard", 1, 1, true)
                .AddItem("recipe_osvisor", 1, 1, true)
                .AddItem("recipe_oscharm", 1, 1, true)
                .AddItem("recipe_ostrophy", 1, 1, true)
                .AddItem("recipe_oshide", 1, 1, true);

            _builder.Create("VISCARA_OLD_SCAR_TROPHY")
                .AddItem("oldscar_troph", 1);
        }

        private void MandalorianLeader()
        {
            _builder.Create("VISCARA_MANDALORIAN_LEADER")
                .AddItem("m_plexiplate", 20)
                .AddItem("damaryllia", 10)
                .AddItem("jade", 20)
                .AddItem("agate", 20)
                .AddItem("mando_twinblade", 5)
                .AddItem("mando_shield", 5)
                .AddItem("mando_cloak", 5)
                .AddItem("mando_belt", 5)
                .AddItem("mando_ring", 5)
                .AddItem("mando_necklace", 5)
                .AddItem("mando_armor", 5)
                .AddItem("mando_helmet", 5)
                .AddItem("mando_bracer", 5)
                .AddItem("mando_leggings", 5);

            _builder.Create("VISCARA_MANDALORIAN_LEADER_TAGS")
                .AddItem("man_tags", 50)
                .AddItem("m_polearm_parts", 10)
                .AddItem("m_ls_parts", 10);

            _builder.Create("VISCARA_MANDALORIAN_LEADER_RARES")
                .IsRare()
                .AddItem("map_048", 10)
                .AddItem("m_ls_parts", 20);
        }

        private void MandalorianWarrior()
        {
            _builder.Create("VISCARA_MANDALORIAN_WARRIOR")
                .AddItem("elec_flawed", 20)
                .AddItem("herb_m", 20)
                .AddItem("med_supplies", 3, 3)
                .AddItem("stim_pack", 3, 3)
                .AddItem("mando_blade", 5)
                .AddItem("mando_knife", 5)
                .AddItem("mando_gswd", 5)
                .AddItem("mando_spear", 5)
                .AddItem("mando_katar", 5)
                .AddItem("mando_staff", 5)
                .AddItem("mando_sabstaff", 5)
                .AddItem("mando_eblade", 5)
                .AddItem("mando_twinblade", 5)
                .AddItem("bubble_choc", 8)
                .AddGold(30, 10);

            _builder.Create("VISCARA_MANDALORIAN_WARRIOR_TAGS")
                .AddItem("man_tags", 50)
                .AddItem("m_lvibro_parts", 10)
                .AddItem("m_vibro_parts", 10)
                .AddItem("m_polearm_parts", 10);

            _builder.Create("VISCARA_MANDALORIAN_WARRIOR_RARES")
                .IsRare()
                .AddItem("m_plexiplate", 20, 1, true)
                .AddItem("map_048", 10, 1, true)
                .AddItem("jade", 5, 1, true)
                .AddItem("agate", 5, 1, true);
        }

        private void MandalorianRanger()
        {
            _builder.Create("VISCARA_MANDALORIAN_RANGER")
                .AddItem("elec_flawed", 20)
                .AddItem("herb_m", 20)
                .AddItem("med_supplies", 3, 3)
                .AddItem("stim_pack", 3, 3)
                .AddItem("mando_shuriken", 5)
                .AddItem("mando_pistol", 5)
                .AddItem("mando_rifle", 5)
                .AddItem("mando_knife", 5)
                .AddItem("b_flour", 8)
                .AddItem("sweet_butter", 2)
                .AddGold(30, 10);

            _builder.Create("VISCARA_MANDALORIAN_RANGER_TAGS")
                .AddItem("man_tags", 50)
                .AddItem("m_blast_parts", 15)
                .AddItem("m_vibro_parts", 5);

            _builder.Create("VISCARA_MANDALORIAN_RANGER_RARES")
                .IsRare()
                .AddItem("m_plexiplate", 20, 1, true)
                .AddItem("map_048", 10, 1, true)
                .AddItem("jade", 5, 1, true)
                .AddItem("agate", 5, 1, true);
        }

        private void MandalorianHunter()
        {
            _builder.Create("VISCARA_MANDALORIAN_HUNTER")
                .AddItem("elec_flawed", 20)
                .AddItem("herb_m", 20)
                .AddItem("med_supplies", 3, 3)
                .AddItem("stim_pack", 3, 3)
                .AddItem("mando_shuriken", 5)
                .AddItem("mando_pistol", 5)
                .AddItem("mando_rifle", 5)
                .AddItem("lth_ruined", 5)
                .AddItem("lth_flawed", 5)
                .AddItem("gimp_shell", 1, 1)
                .AddItem("gimp_tooth", 1)
                .AddItem("gimp_blood", 1)
                .AddItem("gimp_meat", 1)
                .AddGold(30, 10);

            _builder.Create("VISCARA_MANDALORIAN_HUNTER_TAGS")
                .AddItem("man_tags", 50)
                .AddItem("m_blast_parts", 15)
                .AddItem("m_vibro_parts", 5);

            _builder.Create("VISCARA_MANDALORIAN_HUNTER_RARES")
                .IsRare()
                .AddItem("m_plexiplate", 20, 1, true)
                .AddItem("map_053", 10, 1, true)
                .AddItem("map_048", 5, 1, true)
                .AddItem("jade", 5, 1, true)
                .AddItem("agate", 5, 1, true);
        }

        private void MandalorianScout()
        {
            _builder.Create("VISCARA_MANDALORIAN_SCOUT")
                .AddItem("elec_flawed", 20)
                .AddItem("herb_m", 20)
                .AddItem("fiberp_flawed", 15)
                .AddItem("mando_knife", 5)
                .AddItem("med_supplies", 3, 3)
                .AddItem("stim_pack", 3, 3)
                .AddItem("kinrath_limb", 5)
                .AddItem("kinrath_meat", 5)
                .AddGold(30, 10);

            _builder.Create("VISCARA_MANDALORIAN_SCOUT_TAGS")
                .AddItem("man_tags", 50)
                .AddItem("m_lvibro_parts", 10)
                .AddItem("m_vibro_parts", 10)
                .AddItem("m_polearm_parts", 10);

            _builder.Create("VISCARA_MANDALORIAN_SCOUT_RARES")
                .IsRare()
                .AddItem("m_plexiplate", 10, 1, true)
                .AddItem("map_053", 10, 1, true)
                .AddItem("map_048", 2, 1, true);
        }

        private void MandalorianCrate()
        {
            _builder.Create("VISCARA_MANDALORIAN_CRATE")
                .AddItem("herb_m", 30)
                .AddItem("elec_flawed", 20)
                .AddItem("med_supplies", 3, 3)
                .AddItem("stim_pack", 3, 3)
                .AddItem("jade", 1, 1, true)
                .AddItem("agate", 1, 1, true)
                .AddItem("m_plexiplate", 10, 1, true)
                .AddItem("m_ls_parts", 10)
                .AddItem("m_lvibro_parts", 10)
                .AddItem("m_vibro_parts", 10)
                .AddItem("m_polearm_parts", 10)
                .AddItem("m_blast_parts", 10)
                .AddItem("v_honey", 5)
                .AddItem("sweet_butter", 10)
                .AddItem("b_flour", 10)
                .AddGold(30, 10);

        }
        private void Outlaw()
        {
            _builder.Create("VISCARA_OUTLAW")
                .AddItem("elec_ruined", 20)
                .AddItem("elec_flawed", 5)
                .AddItem("med_supplies", 3, 3)
                .AddItem("stim_pack", 3, 3)
                .AddItem("outlaw_cloak", 1)
                .AddItem("outlaw_belt", 1)
                .AddItem("outlaw_ring", 1)
                .AddItem("outlaw_necklace", 1)
                .AddItem("outlaw_tunic", 1)
                .AddItem("outlaw_cap", 1)
                .AddItem("outlaw_gloves", 1)
                .AddItem("outlaw_boots", 1)
                .AddItem("v_flour", 5)
                .AddGold(20, 10);

            _builder.Create("VISCARA_OUTLAW_RARES")
                .IsRare()
                .AddItem("map_053", 20, 1, true);
        }

        private void Gimpassa()
        {
            _builder.Create("VISCARA_GIMPASSA")
                .AddItem("lth_ruined", 5)
                .AddItem("gimp_tooth", 10)
                .AddItem("lth_flawed", 20)
                .AddItem("gimp_meat", 10);

            _builder.Create("VISCARA_GIMPASSA_RARES")
                .IsRare()
                .AddItem("gimp_blood", 2, 1, true)
                .AddItem("gimp_shell", 1, 1, true);


        }

        private void Kinrath()
        {
            _builder.Create("VISCARA_KINRATH")
                .AddItem("kinrath_meat", 10)
                .AddItem("lth_ruined", 10)
                .AddItem("lth_flawed", 5)
                .AddItem("kinrath_limb", 1);

            _builder.Create("VISCARA_KINRATH_RARES")
                .IsRare()
                .AddItem("kinrath_limb", 1, 1, true)
                .AddItem("kinrath_silk", 2, 1, true);
        }

        private void Cairnmog()
        {
            _builder.Create("VISCARA_CAIRNMOG")
                .AddItem("cairnmog_meat", 10)
                .AddItem("cairnmog_spine", 10)
                .AddItem("lth_ruined", 5)
                .AddItem("lth_flawed", 10);

            _builder.Create("VISCARA_CAIRNMOG_RARES")
                .IsRare()
                .AddItem("cairnmog_blood", 2, 1, true)
                .AddItem("cairnmog_tooth", 4, 1, true)
                .AddItem("map_049", 1, 1, true);
        }

        private void VellenFleshleader()
        {
            _builder.Create("VISCARA_VELLEN_FLESHLEADER")
                .AddItem("babonsch", 5)
                .AddItem("cox_metal", 5)
                .AddItem("lth_flawed", 5)
                .AddItem("elec_flawed", 5)
                .AddItem("flesh_cloak", 5)
                .AddItem("flesh_belt", 5)
                .AddItem("flesh_ring", 5)
                .AddItem("flesh_necklace", 5)
                .AddItem("flesh_tunic", 5)
                .AddItem("flesh_cap", 5)
                .AddItem("flesh_gloves", 5)
                .AddItem("flesh_boots", 5);

            _builder.Create("VISCARA_VELLEN_FLESHLEADER_RARES")
                .IsRare()
                .AddItem("babonsch", 5, 1, true)
                .AddItem("map_041", 4, 1, true)
                .AddItem("map_045", 1, 1, true);
        }

        private void VellenFlesheater()
        {
            _builder.Create("VISCARA_VELLEN_FLESHEATER")
                .AddItem("lth_flawed", 15)
                .AddItem("lth_ruined", 5)
                .AddItem("fiberp_ruined", 15)
                .AddItem("elec_flawed", 5)
                .AddItem("bubble_choc", 10)
                .AddItem("sweet_butter", 8)
                .AddItem("b_flour", 10)
                .AddGold(30, 20);


            _builder.Create("VISCARA_VELLEN_FLESHEATER_RARES")
                .IsRare()
                .AddItem("map_041", 4, 1, true)
                .AddItem("map_045", 1, 1, true)
                .AddItem("babonsch", 10, 1, true)
                .AddItem("cox_metal", 20, 1, true);
        }

        private void CoxxionCrate()
        {
            _builder.Create("VISCARA_COXXIAN_CRATE")
                .AddItem("cox_metal", 5)
                .AddItem("ref_veldite", 5)
                .AddItem("ref_scordspar", 20)
                .AddItem("fiberp_flawed", 15)
                .AddItem("lth_flawed", 15)
                .AddItem("elec_flawed", 5)
                .AddItem("v_honey", 10)
                .AddItem("sweet_butter", 2)
                .AddItem("coonlank_blue", 1, 1, true)
                .AddItem("coonlank_green", 1, 1, true)
                .AddItem("coonlank_red", 1, 1, true)
                .AddItem("coonlank_yellow", 1, 1, true)
                .AddGold(30, 20);
        }

        private void Raivor()
        {
            _builder.Create("VISCARA_RAIVOR")
                .AddItem("raivor_meat", 10)
                .AddItem("raivor_claw", 10)
                .AddItem("raivor_tail_bone", 10);


            _builder.Create("VISCARA_RAIVOR_RARES")
                .IsRare()
                .AddItem("raivor_scale", 2, 1, true)
                .AddItem("raivor_blood", 4, 1, true)
                .AddItem("map_042", 1, 1, true);
        }

        private void Warocas()
        {
            _builder.Create("VISCARA_WAROCAS")
                .AddItem("warocas_beak", 10)
                .AddItem("waro_feathers", 15)
                .AddItem("lth_ruined", 20)
                .AddItem("warocas_meat", 20)
                .AddItem("waro_leg", 10, 1);

            _builder.Create("VISCARA_WAROCAS_RARES")
                .IsRare()
                .AddItem("waro_leg", 1, 1, true);
        }

        private void Nashtah()
        {
            _builder.Create("VISCARA_NASHTAH")
                .AddItem("lth_ruined", 5)
                .AddItem("lth_flawed", 10)
                .AddItem("nashtah_meat", 30)
                .AddItem("nash_scale", 10)
                .AddItem("nashtah_foot", 10);


            _builder.Create("VISCARA_NASHTAH_RARES")
                .IsRare()
                .AddItem("nash_tail", 3, 1, true)
                .AddItem("map_049", 1, 1, true);
        }

        private void CrystalSpider()
        {
            _builder.Create("VISCARA_CRYSTAL_SPIDER")
                .AddItem("p_crystal_blue", 10)
                .AddItem("p_crystal_red", 10)
                .AddItem("p_crystal_green", 10)
                .AddItem("p_crystal_yellow", 10);

            _builder.Create("VISCARA_CRYSTAL_SPIDER_RARES")
                .IsRare()
                .AddItem("agate", 3, 1, true)
                .AddItem("map_039", 1, 1, true);
        }

        private void SewersDepthsEntry()
        {
            _builder.Create("VISCARA_SEWERS_DEPTHS_SCAVENGER")
                .AddItem("lth_flawed", 20)
                .AddItem("elec_flawed", 20)
                .AddItem("stim_pack", 10, 2)
                .AddGold(100, 10);

            _builder.Create("VISCARA_SEWERS_DEPTHS_SCAVENGER_RARES")
                .IsRare()
                .AddItem("redline_vblade", 1, 1, true)
                .AddItem("redvein_pistol", 1, 1, true)
                .AddItem("sump_vknife", 1, 1, true)
                .AddItem("gutter_staff", 1, 1, true)
                .AddItem("redvein_wraps", 1, 1, true)
                .AddItem("stolen_belt", 1, 1, true)
                .AddItem("codex_mantle", 1, 1, true)
                .AddItem("rustred_band", 1, 1, true)
                .AddItem("scav_visor", 1, 1, true)
                .AddItem("stalk_boots", 1, 1, true)
                .AddItem("redvein_charm", 1, 1, true);

            _builder.Create("VISCARA_SEWERS_DEPTHS_PULSE_DROID")
                .AddItem("elec_good", 20)
                .AddItem("med_supplies", 10, 2)
                .AddGold(100, 10);

            _builder.Create("VISCARA_SEWERS_DEPTHS_PULSE_DROID_RARES")
                .IsRare()
                .AddItem("pulse_calrifle", 1, 1, true)
                .AddItem("servo_pistol", 1, 1, true)
                .AddItem("cad_rifle", 1, 1, true)
                .AddItem("pulse_conduct", 1, 1, true)
                .AddItem("time_bracer", 1, 1, true)
                .AddItem("metro_ring", 1, 1, true)
                .AddItem("servosync_belt", 1, 1, true)
                .AddItem("calib_lens", 1, 1, true)
                .AddItem("pulse_cape", 1, 1, true)
                .AddItem("frame_boots", 1, 1, true)
                .AddItem("spark_gloves", 1, 1, true);
        }

        private void SewersDepthsButcher()
        {
            _builder.Create("VISCARA_SEWERS_DEPTHS_BUTCHER")
                .AddItem("elec_good", 20)
                .AddItem("stim_pack", 20, 3)
                .AddItem("med_supplies", 20, 3)
                .AddGold(150, 10);

            _builder.Create("VISCARA_SEWERS_DEPTHS_BUTCHER_RARES")
                .IsRare()
                .AddItem("butch_cleaver", 1, 1, true)
                .AddItem("butch_injector", 1, 1, true)
                .AddItem("stim_splitter", 1, 1, true)
                .AddItem("black_cleaver", 1, 1, true)
                .AddItem("adren_harness", 1, 1, true)
                .AddItem("clot_mask", 1, 1, true)
                .AddItem("lab_bracer", 1, 1, true)
                .AddItem("inject_belt", 1, 1, true)
                .AddItem("suture_gloves", 1, 1, true)
                .AddItem("adren_pendant", 1, 1, true)
                .AddItem("blackmkt_boots", 1, 1, true);
        }

        private void SewersDepthsCircle()
        {
            _builder.Create("VISCARA_SEWERS_DEPTHS_DUELIST")
                .AddItem("elec_good", 20)
                .AddItem("stim_pack", 15, 2)
                .AddItem("med_supplies", 15, 2)
                .AddGold(120, 10);

            _builder.Create("VISCARA_SEWERS_DEPTHS_DUELIST_RARES")
                .IsRare()
                .AddItem("duel_splitter", 1, 1, true)
                .AddItem("charm_katar", 1, 1, true)
                .AddItem("redcircle_star", 1, 1, true)
                .AddItem("duel_fang", 1, 1, true)
                .AddItem("circle_twin", 1, 1, true)
                .AddItem("binding_sash", 1, 1, true)
                .AddItem("split_boots", 1, 1, true)
                .AddItem("restr_band", 1, 1, true)
                .AddItem("circle_mantle", 1, 1, true)
                .AddItem("duel_grip", 1, 1, true)
                .AddItem("broken_charm", 1, 1, true);

            _builder.Create("VISCARA_SEWERS_DEPTHS_KING")
                .AddItem("elec_good", 20)
                .AddItem("stim_pack", 20, 3)
                .AddItem("med_supplies", 20, 3)
                .AddGold(200, 10);
        }
    }
}
