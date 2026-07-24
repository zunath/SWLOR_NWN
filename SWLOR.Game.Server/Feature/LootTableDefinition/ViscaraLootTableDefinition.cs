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
            SootlineRusk();
            NaraVenn();
            Silkshade();
            Mossback();
            TarnKyric();
            VaroSkeld();
            HarrekVoss();
            Greyspine();
            MawSeerGhal();
            RedtailKor();
            ShardEye();
            Rootcoil();
            Mirevein();
            Vrix7();
            Ashwing();

            MandalorianCrate();
            CoxxionCrate();

            CapstoneViscaraDungeonRares();
            VelesRareElites();
            VisbunkerRareElites();
            return _builder.Build();
        }

        private void VisbunkerRareElites()
        {
            _builder.Create("VISBUNKER_BUNKER_RARES").IsRare()
                .AddItem("bunkerbreakda", 1, 1, true).AddItem("bunkerbreakdb", 1, 1, true).AddItem("bp_bunkerbreak", 1, 1, true);
            _builder.Create("VISBUNKER_BEACON_RARES").IsRare()
                .AddItem("beaconmarksda", 1, 1, true).AddItem("beaconmarksdb", 1, 1, true).AddItem("bp_beaconmarks", 1, 1, true);
            _builder.Create("VISBUNKER_DECURION_RARES").IsRare()
                .AddItem("decurioncmdda", 1, 1, true).AddItem("decurioncmddb", 1, 1, true).AddItem("bp_decurioncmd", 1, 1, true);
            _builder.Create("VISBUNKER_BUNKER_COMP").AddItem("bunkerbreakcm", 1, 1);
            _builder.Create("VISBUNKER_BEACON_COMP").AddItem("beaconmarkscm", 1, 1);
            _builder.Create("VISBUNKER_DECURION_COMP").AddItem("decurioncmdcm", 1, 1);
        }

        private void VelesRareElites()
        {
            _builder.Create("VELES_INVICTUS_RARES").IsRare()
                .AddItem("invictusda", 1, 1, true).AddItem("invictusdb", 1, 1, true).AddItem("bp_invictus", 1, 1, true);
            _builder.Create("VELES_RUPTOR_RARES").IsRare()
                .AddItem("ruptorvaneda", 1, 1, true).AddItem("ruptorvanedb", 1, 1, true).AddItem("bp_ruptorvane", 1, 1, true);
            _builder.Create("VELES_BLACKOUT_RARES").IsRare()
                .AddItem("blackoutwrdda", 1, 1, true).AddItem("blackoutwrddb", 1, 1, true).AddItem("bp_blackoutwrd", 1, 1, true);
            _builder.Create("VELES_INVICTUS_COMP").AddItem("invictuscm", 1, 1);
            _builder.Create("VELES_RUPTOR_COMP").AddItem("ruptorvanecm", 1, 1);
            _builder.Create("VELES_BLACKOUT_COMP").AddItem("blackoutwrdcm", 1, 1);
        }

        private void CapstoneViscaraDungeonRares()
        {
            _builder.Create("CAPSTONE_INVINC_RARES")
                .IsRare()
                .AddItem("invinc_l1", 1, 1, true)
                .AddItem("invinc_l2", 1, 1, true)
                .AddItem("invinc_l3", 1, 1, true)
                .AddItem("invinc_l4", 1, 1, true)
                .AddItem("invinc_l5", 1, 1, true)
                .AddItem("invinc_l6", 1, 1, true)
                .AddItem("invinc_l7", 1, 1, true)
                .AddItem("invinc_l8", 1, 1, true);
            _builder.Create("CAPSTONE_INVINC_WD_RARES")
                .IsRare()
                .AddItem("invinc_w1", 1, 1, true)
                .AddItem("invinc_w2", 1, 1, true)
                .AddItem("invinc_w3", 1, 1, true)
                .AddItem("invinc_w4", 1, 1, true)
                .AddItem("invinc_w5", 1, 1, true);

            _builder.Create("CAPSTONE_VITRUPT_RARES")
                .IsRare()
                .AddItem("vitrupt_l1", 1, 1, true)
                .AddItem("vitrupt_l2", 1, 1, true)
                .AddItem("vitrupt_l3", 1, 1, true)
                .AddItem("vitrupt_l4", 1, 1, true)
                .AddItem("vitrupt_l5", 1, 1, true)
                .AddItem("vitrupt_l6", 1, 1, true)
                .AddItem("vitrupt_l7", 1, 1, true)
                .AddItem("vitrupt_l8", 1, 1, true);
            _builder.Create("CAPSTONE_VITRUPT_WD_RARES")
                .IsRare()
                .AddItem("vitrupt_w1", 1, 1, true)
                .AddItem("vitrupt_w2", 1, 1, true)
                .AddItem("vitrupt_w3", 1, 1, true)
                .AddItem("vitrupt_w4", 1, 1, true)
                .AddItem("vitrupt_w5", 1, 1, true);

            _builder.Create("CAPSTONE_SYSSHUT_RARES")
                .IsRare()
                .AddItem("sysshut_l1", 1, 1, true)
                .AddItem("sysshut_l2", 1, 1, true)
                .AddItem("sysshut_l3", 1, 1, true)
                .AddItem("sysshut_l4", 1, 1, true)
                .AddItem("sysshut_l5", 1, 1, true)
                .AddItem("sysshut_l6", 1, 1, true)
                .AddItem("sysshut_l7", 1, 1, true)
                .AddItem("sysshut_l8", 1, 1, true);
            _builder.Create("CAPSTONE_SYSSHUT_WD_RARES")
                .IsRare()
                .AddItem("sysshut_w1", 1, 1, true)
                .AddItem("sysshut_w2", 1, 1, true)
                .AddItem("sysshut_w3", 1, 1, true)
                .AddItem("sysshut_w4", 1, 1, true)
                .AddItem("sysshut_w5", 1, 1, true);

            _builder.Create("CAPSTONE_KILLBEACON_RARES")
                .IsRare()
                .AddItem("killbeacon_l1", 1, 1, true)
                .AddItem("killbeacon_l2", 1, 1, true)
                .AddItem("killbeacon_l3", 1, 1, true)
                .AddItem("killbeacon_l4", 1, 1, true)
                .AddItem("killbeacon_l5", 1, 1, true)
                .AddItem("killbeacon_l6", 1, 1, true)
                .AddItem("killbeacon_l7", 1, 1, true)
                .AddItem("killbeacon_l8", 1, 1, true);
            _builder.Create("CAPSTONE_KILLBEACON_WD_RARES")
                .IsRare()
                .AddItem("killbeacon_w1", 1, 1, true)
                .AddItem("killbeacon_w2", 1, 1, true)
                .AddItem("killbeacon_w3", 1, 1, true)
                .AddItem("killbeacon_w4", 1, 1, true)
                .AddItem("killbeacon_w5", 1, 1, true);

            _builder.Create("CAPSTONE_EMBUNKER_RARES")
                .IsRare()
                .AddItem("embunker_l1", 1, 1, true)
                .AddItem("embunker_l2", 1, 1, true)
                .AddItem("embunker_l3", 1, 1, true)
                .AddItem("embunker_l4", 1, 1, true)
                .AddItem("embunker_l5", 1, 1, true)
                .AddItem("embunker_l6", 1, 1, true)
                .AddItem("embunker_l7", 1, 1, true)
                .AddItem("embunker_l8", 1, 1, true);
            _builder.Create("CAPSTONE_EMBUNKER_WD_RARES")
                .IsRare()
                .AddItem("embunker_w1", 1, 1, true)
                .AddItem("embunker_w2", 1, 1, true)
                .AddItem("embunker_w3", 1, 1, true)
                .AddItem("embunker_w4", 1, 1, true)
                .AddItem("embunker_w5", 1, 1, true);

            _builder.Create("CAPSTONE_DECCOMMAND_RARES")
                .IsRare()
                .AddItem("deccommand_l1", 1, 1, true)
                .AddItem("deccommand_l2", 1, 1, true)
                .AddItem("deccommand_l3", 1, 1, true)
                .AddItem("deccommand_l4", 1, 1, true)
                .AddItem("deccommand_l5", 1, 1, true)
                .AddItem("deccommand_l6", 1, 1, true)
                .AddItem("deccommand_l7", 1, 1, true)
                .AddItem("deccommand_l8", 1, 1, true);
            _builder.Create("CAPSTONE_DECCOMMAND_WD_RARES")
                .IsRare()
                .AddItem("deccommand_w1", 1, 1, true)
                .AddItem("deccommand_w2", 1, 1, true)
                .AddItem("deccommand_w3", 1, 1, true)
                .AddItem("deccommand_w4", 1, 1, true)
                .AddItem("deccommand_w5", 1, 1, true);
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
                .AddItem("k_hound_claw", 1, 1, true)
                .AddItem("lockbox_t1", 2, 1, true);

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
                .AddItem("map_053", 20, 1, true)
                .AddItem("lockbox_t2", 3, 1, true);
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

            _builder.Create("VISCARA_STORMPLUME_RARES")
                .IsRare()
                .AddItem("recipe_spharn", 1, 1, true)
                .AddItem("recipe_spwrap", 1, 1, true)
                .AddItem("recipe_spstrid", 1, 1, true)
                .AddItem("recipe_spsash", 1, 1, true)
                .AddItem("recipe_spmant", 1, 1, true)
                .AddItem("recipe_spgorg", 1, 1, true)
                .AddItem("recipe_spband", 1, 1, true)
                .AddItem("recipe_spguard", 1, 1, true)
                .AddItem("recipe_spvisor", 1, 1, true)
                .AddItem("recipe_spcharm", 1, 1, true)
                .AddItem("recipe_sptroph", 1, 1, true)
                .AddItem("recipe_spplume", 1, 1, true);

            _builder.Create("VISCARA_STORMPLUME_PLUME")
                .AddItem("stormpl_plume", 1);
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
                .AddItem("redvein_vblade", 1, 1, true)
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
                .AddItem("rending_cleaver", 1, 1, true)
                .AddItem("adrenal_injector", 1, 1, true)
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

        private void SootlineRusk()
        {
            _builder.Create("VISCARA_SOOTLINE_RUSK_RARES")
                .IsRare()
                .AddItem("bpstructure0331", 1, 1, true)
                .AddItem("bpstructure0332", 1, 1, true)
                .AddItem("bpstructure0333", 1, 1, true)
                .AddItem("bpstructure0334", 1, 1, true)
                .AddItem("bpstructure0335", 1, 1, true)
                .AddItem("bpstructure0336", 1, 1, true)
                .AddItem("bpstructure0337", 1, 1, true)
                .AddItem("bpstructure0338", 1, 1, true)
                .AddItem("bpstructure0339", 1, 1, true)
                .AddItem("bpstructure0340", 1, 1, true)
                .AddItem("bpsrjrcell", 1, 1, true);

            _builder.Create("VISCARA_SOOTLINE_RUSK_TOKEN")
                .AddItem("sr_token", 1);
        }

        private void NaraVenn()
        {
            _builder.Create("VISCARA_NARA_VENN_RARES")
                .IsRare()
                .AddItem("bpstructure0341", 1, 1, true)
                .AddItem("bpstructure0342", 1, 1, true)
                .AddItem("bpstructure0343", 1, 1, true)
                .AddItem("bpstructure0344", 1, 1, true)
                .AddItem("bpstructure0345", 1, 1, true)
                .AddItem("bpstructure0346", 1, 1, true)
                .AddItem("bpstructure0347", 1, 1, true)
                .AddItem("bpstructure0348", 1, 1, true)
                .AddItem("bpstructure0349", 1, 1, true)
                .AddItem("bpstructure0350", 1, 1, true)
                .AddItem("bpnvrelay", 1, 1, true);

            _builder.Create("VISCARA_NARA_VENN_PIN")
                .AddItem("nv_pin", 1);
        }

        private void Silkshade()
        {
            _builder.Create("VISCARA_SILKSHADE_RARES")
                .IsRare()
                .AddItem("bpssharness", 1, 1, true)
                .AddItem("bpsswraps", 1, 1, true)
                .AddItem("bpsstreads", 1, 1, true)
                .AddItem("bpsssash", 1, 1, true)
                .AddItem("bpssmantle", 1, 1, true)
                .AddItem("bpssgorget", 1, 1, true)
                .AddItem("bpssband", 1, 1, true)
                .AddItem("bpssguard", 1, 1, true)
                .AddItem("bpssvisor", 1, 1, true)
                .AddItem("bpsscharm", 1, 1, true)
                .AddItem("bpssskewer", 1, 1, true);

            _builder.Create("VISCARA_SILKSHADE_SILK")
                .AddItem("ss_silk", 1);
        }

        private void Mossback()
        {
            _builder.Create("VISCARA_MOSSBACK_RARES")
                .IsRare()
                .AddItem("bpmbharness", 1, 1, true)
                .AddItem("bpmbwraps", 1, 1, true)
                .AddItem("bpmbtreads", 1, 1, true)
                .AddItem("bpmbsash", 1, 1, true)
                .AddItem("bpmbmantle", 1, 1, true)
                .AddItem("bpmbgorget", 1, 1, true)
                .AddItem("bpmbband", 1, 1, true)
                .AddItem("bpmbguard", 1, 1, true)
                .AddItem("bpmbvisor", 1, 1, true)
                .AddItem("bpmbcharm", 1, 1, true)
                .AddItem("bpmbbraise", 1, 1, true);

            _builder.Create("VISCARA_MOSSBACK_SHELL")
                .AddItem("mb_shell", 1);
        }

        private void TarnKyric()
        {
            _builder.Create("VISCARA_TARN_KYRIC_RARES")
                .IsRare()
                .AddItem("bpstructure0351", 1, 1, true)
                .AddItem("bpstructure0352", 1, 1, true)
                .AddItem("bpstructure0353", 1, 1, true)
                .AddItem("bpstructure0354", 1, 1, true)
                .AddItem("bpstructure0355", 1, 1, true)
                .AddItem("bpstructure0356", 1, 1, true)
                .AddItem("bpstructure0357", 1, 1, true)
                .AddItem("bpstructure0358", 1, 1, true)
                .AddItem("bpstructure0359", 1, 1, true)
                .AddItem("bpstructure0360", 1, 1, true)
                .AddItem("bptksensor", 1, 1, true);

            _builder.Create("VISCARA_TARN_KYRIC_BADGE")
                .AddItem("tk_badge", 1);
        }

        private void VaroSkeld()
        {
            _builder.Create("VISCARA_VARO_SKELD_RARES")
                .IsRare()
                .AddItem("bpstructure0361", 1, 1, true)
                .AddItem("bpstructure0362", 1, 1, true)
                .AddItem("bpstructure0363", 1, 1, true)
                .AddItem("bpstructure0364", 1, 1, true)
                .AddItem("bpstructure0365", 1, 1, true)
                .AddItem("bpstructure0366", 1, 1, true)
                .AddItem("bpstructure0367", 1, 1, true)
                .AddItem("bpstructure0368", 1, 1, true)
                .AddItem("bpstructure0369", 1, 1, true)
                .AddItem("bpstructure0370", 1, 1, true)
                .AddItem("bpvsrelay", 1, 1, true);

            _builder.Create("VISCARA_VARO_SKELD_MASK")
                .AddItem("vs_mask", 1);
        }

        private void HarrekVoss()
        {
            _builder.Create("VISCARA_HARREK_VOSS_RARES")
                .IsRare()
                .AddItem("bpstructure0371", 1, 1, true)
                .AddItem("bpstructure0372", 1, 1, true)
                .AddItem("bpstructure0373", 1, 1, true)
                .AddItem("bpstructure0374", 1, 1, true)
                .AddItem("bpstructure0375", 1, 1, true)
                .AddItem("bpstructure0376", 1, 1, true)
                .AddItem("bpstructure0377", 1, 1, true)
                .AddItem("bpstructure0378", 1, 1, true)
                .AddItem("bpstructure0379", 1, 1, true)
                .AddItem("bpstructure0380", 1, 1, true)
                .AddItem("bphvservo", 1, 1, true);

            _builder.Create("VISCARA_HARREK_VOSS_PLATE")
                .AddItem("hv_plate", 1);
        }

        private void Greyspine()
        {
            _builder.Create("VISCARA_GREYSPINE_RARES")
                .IsRare()
                .AddItem("bpgsharness", 1, 1, true)
                .AddItem("bpgswraps", 1, 1, true)
                .AddItem("bpgstreads", 1, 1, true)
                .AddItem("bpgssash", 1, 1, true)
                .AddItem("bpgsmantle", 1, 1, true)
                .AddItem("bpgsgorget", 1, 1, true)
                .AddItem("bpgsband", 1, 1, true)
                .AddItem("bpgsguard", 1, 1, true)
                .AddItem("bpgsvisor", 1, 1, true)
                .AddItem("bpgscharm", 1, 1, true)
                .AddItem("bpgspotpie", 1, 1, true);

            _builder.Create("VISCARA_GREYSPINE_SPINE")
                .AddItem("gs_spine", 1);
        }

        private void MawSeerGhal()
        {
            _builder.Create("VISCARA_MAW_SEER_GHAL_RARES")
                .IsRare()
                .AddItem("bpstructure0381", 1, 1, true)
                .AddItem("bpstructure0382", 1, 1, true)
                .AddItem("bpstructure0383", 1, 1, true)
                .AddItem("bpstructure0384", 1, 1, true)
                .AddItem("bpstructure0385", 1, 1, true)
                .AddItem("bpstructure0386", 1, 1, true)
                .AddItem("bpstructure0387", 1, 1, true)
                .AddItem("bpstructure0388", 1, 1, true)
                .AddItem("bpstructure0389", 1, 1, true)
                .AddItem("bpstructure0390", 1, 1, true)
                .AddItem("bpmgsplice", 1, 1, true);

            _builder.Create("VISCARA_MAW_SEER_GHAL_TOTEM")
                .AddItem("mg_totem", 1);
        }

        private void RedtailKor()
        {
            _builder.Create("VISCARA_REDTAIL_KOR_RARES")
                .IsRare()
                .AddItem("bprkharness", 1, 1, true)
                .AddItem("bprkwraps", 1, 1, true)
                .AddItem("bprktreads", 1, 1, true)
                .AddItem("bprksash", 1, 1, true)
                .AddItem("bprkmantle", 1, 1, true)
                .AddItem("bprkgorget", 1, 1, true)
                .AddItem("bprkband", 1, 1, true)
                .AddItem("bprkguard", 1, 1, true)
                .AddItem("bprkvisor", 1, 1, true)
                .AddItem("bprkcharm", 1, 1, true)
                .AddItem("bprkroast", 1, 1, true);

            _builder.Create("VISCARA_REDTAIL_KOR_CLAW")
                .AddItem("rk_claw", 1);
        }

        private void ShardEye()
        {
            _builder.Create("VISCARA_SHARD_EYE_RARES")
                .IsRare()
                .AddItem("bpseharness", 1, 1, true)
                .AddItem("bpsewraps", 1, 1, true)
                .AddItem("bpsetreads", 1, 1, true)
                .AddItem("bpsesash", 1, 1, true)
                .AddItem("bpsemantle", 1, 1, true)
                .AddItem("bpsegorget", 1, 1, true)
                .AddItem("bpseband", 1, 1, true)
                .AddItem("bpseguard", 1, 1, true)
                .AddItem("bpsevisor", 1, 1, true)
                .AddItem("bpsecharm", 1, 1, true)
                .AddItem("bpseconsomme", 1, 1, true);

            _builder.Create("VISCARA_SHARD_EYE")
                .AddItem("se_eye", 1);
        }

        private void Rootcoil()
        {
            _builder.Create("VISCARA_ROOTCOIL_RARES")
                .IsRare()
                .AddItem("bprcharness", 1, 1, true)
                .AddItem("bprcwraps", 1, 1, true)
                .AddItem("bprctreads", 1, 1, true)
                .AddItem("bprcsash", 1, 1, true)
                .AddItem("bprcmantle", 1, 1, true)
                .AddItem("bprcgorget", 1, 1, true)
                .AddItem("bprcband", 1, 1, true)
                .AddItem("bprcguard", 1, 1, true)
                .AddItem("bprcvisor", 1, 1, true)
                .AddItem("bprccharm", 1, 1, true)
                .AddItem("bprcbroth", 1, 1, true);

            _builder.Create("VISCARA_ROOTCOIL_VINE")
                .AddItem("rc_vine", 1);
        }

        private void Mirevein()
        {
            _builder.Create("VISCARA_MIREVEIN_RARES")
                .IsRare()
                .AddItem("bpmvharness", 1, 1, true)
                .AddItem("bpmvwraps", 1, 1, true)
                .AddItem("bpmvtreads", 1, 1, true)
                .AddItem("bpmvsash", 1, 1, true)
                .AddItem("bpmvmantle", 1, 1, true)
                .AddItem("bpmvgorget", 1, 1, true)
                .AddItem("bpmvband", 1, 1, true)
                .AddItem("bpmvguard", 1, 1, true)
                .AddItem("bpmvvisor", 1, 1, true)
                .AddItem("bpmvcharm", 1, 1, true)
                .AddItem("bpmvtea", 1, 1, true);

            _builder.Create("VISCARA_MIREVEIN_CORE")
                .AddItem("mv_core", 1);
        }

        private void Vrix7()
        {
            _builder.Create("VISCARA_VRIX7_RARES")
                .IsRare()
                .AddItem("bpstructure0391", 1, 1, true)
                .AddItem("bpstructure0392", 1, 1, true)
                .AddItem("bpstructure0393", 1, 1, true)
                .AddItem("bpstructure0394", 1, 1, true)
                .AddItem("bpstructure0395", 1, 1, true)
                .AddItem("bpstructure0396", 1, 1, true)
                .AddItem("bpstructure0397", 1, 1, true)
                .AddItem("bpstructure0398", 1, 1, true)
                .AddItem("bpstructure0399", 1, 1, true)
                .AddItem("bpstructure0400", 1, 1, true)
                .AddItem("bpvxmatrix", 1, 1, true);

            _builder.Create("VISCARA_VRIX7_CORE")
                .AddItem("vx_core", 1);
        }

        private void Ashwing()
        {
            _builder.Create("VISCARA_ASHWING_RARES")
                .IsRare()
                .AddItem("bpstructure0401", 1, 1, true)
                .AddItem("bpstructure0402", 1, 1, true)
                .AddItem("bpstructure0403", 1, 1, true)
                .AddItem("bpstructure0404", 1, 1, true)
                .AddItem("bpstructure0405", 1, 1, true)
                .AddItem("bpstructure0406", 1, 1, true)
                .AddItem("bpstructure0407", 1, 1, true)
                .AddItem("bpstructure0408", 1, 1, true)
                .AddItem("bpstructure0409", 1, 1, true)
                .AddItem("bpstructure0410", 1, 1, true)
                .AddItem("bpaebroth", 1, 1, true);

            _builder.Create("VISCARA_ASHWING_ECHO")
                .AddItem("ae_echo", 1);
        }
    }
}
