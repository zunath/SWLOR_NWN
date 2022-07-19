using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class ViscaraLootTableDefinition: ILootTableDefinition
    {
        private readonly LootTableBuilder _builder = new LootTableBuilder();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            KathHound();
            MandalorianLeader();
            MandalorianRanger();
            MandalorianWarrior();
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

            MandalorianCrate();
            CoxxionCrate();

            return _builder.Build();
        }

        private void KathHound()
        {
            _builder.Create("VISCARA_KATH_HOUND")
                .AddItem("k_hound_fur", 20)
                .AddItem("k_hound_tooth", 20)
                .AddItem("lth_ruined", 20)
                .AddItem("kath_meat_1", 15)
                .AddItem("kath_blood", 5);

            _builder.Create("VISCARA_KATH_HOUND_RARES")
                .AddItem("k_hound_claw", 1, 1, true);
        }

        private void MandalorianLeader()
        {
            _builder.Create("VISCARA_MANDALORIAN_LEADER")
                .AddItem("m_plexiplate", 20)
                .AddItem("damaryllia", 10)
                .AddItem("jade", 20)
                .AddItem("agate", 20)
                .AddItem("mando_shield", 5)
                .AddItem("mando_cloak", 5)
                .AddItem("mando_belt", 5)
                .AddItem("mando_ring", 5)
                .AddItem("mando_necklace", 5)
                .AddItem("mando_armor", 5)
                .AddItem("mando_helmet", 5)
                .AddItem("mando_bracer", 5)
                .AddItem("mando_leggings", 5);

            _builder.Create("VISCARA_MANDALORIAN_LEADER_RARES")
                .AddItem("map_048", 20, 1, true);
        }

        private void MandalorianWarrior()
        {
            _builder.Create("VISCARA_MANDALORIAN_WARRIOR")
                .AddItem("elec_flawed", 20)
                .AddItem("fiberp_flawed", 20)
                .AddItem("herb_m", 20)
                .AddItem("man_tags", 10)
                .AddItem("med_supplies", 3, 3)
                .AddItem("stim_pack", 3, 3)
                .AddItem("mando_blade", 5)
                .AddItem("mando_knife", 5)
                .AddItem("mando_gswd", 5)
                .AddItem("mando_spear", 5)
                .AddItem("mando_katar", 5)
                .AddItem("mando_staff", 5)
                .AddItem("mando_twinblade", 5)
                .AddItem("bubble_choc", 8)
                .AddGold(30, 10);

            _builder.Create("VISCARA_MANDALORIAN_WARRIOR_RARES")
                .AddItem("map_048", 20, 1, true)
                .AddItem("jade", 4, 1, true)
                .AddItem("agate", 4, 1, true)
                .AddItem("m_plexiplate", 4, 1, true);
        }

        private void MandalorianRanger()
        {
            _builder.Create("VISCARA_MANDALORIAN_RANGER")
                .AddItem("elec_flawed", 20)
                .AddItem("fiberp_flawed", 20)
                .AddItem("herb_m", 20)
                .AddItem("man_tags", 10)
                .AddItem("med_supplies", 3, 3)
                .AddItem("stim_pack", 3, 3)
                .AddItem("mando_shuriken", 5)
                .AddItem("mando_pistol", 5)
                .AddItem("mando_rifle", 5)
                .AddItem("b_flour", 8)
                .AddItem("sweet_butter", 2, 1, true)
                .AddGold(30, 10);

            _builder.Create("VISCARA_MANDALORIAN_RANGER_RARES")
                .AddItem("map_048", 20, 1, true)
                .AddItem("jade", 4, 1, true)
                .AddItem("agate", 4, 1, true)
                .AddItem("m_plexiplate", 4, 1, true);
        }

        private void MandalorianCrate()
        {
            _builder.Create("VISCARA_MANDALORIAN_CRATE")
                .AddItem("herb_m", 30)
                .AddItem("elec_flawed", 20)
                .AddItem("fiberp_flawed", 20)
                .AddItem("man_tags", 5)
                .AddItem("med_supplies", 3, 3)
                .AddItem("stim_pack", 3, 3)
                .AddItem("jade", 1, 1, true)
                .AddItem("agate", 1, 1, true)
                .AddItem("m_plexiplate", 1, 1, true)
                .AddItem("v_honey", 5)
                .AddItem("sweet_butter", 2, 1, true)
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
                .AddItem("map_053", 20, 1, true);
        }

        private void Gimpassa()
        {
            _builder.Create("VISCARA_GIMPASSA")
                .AddItem("lth_ruined", 5)
                .AddItem("gimp_shell", 2, 1, true)
                .AddItem("gimp_tooth", 8)
                .AddItem("lth_flawed", 20)
                .AddItem("gimp_blood", 5)
                .AddItem("gimp_meat", 10);
        }

        private void Kinrath()
        {
            _builder.Create("VISCARA_KINRATH")
                .AddItem("kinrath_limb", 5)
                .AddItem("kinrath_meat", 10)
                .AddItem("kinrath_silk", 20)
                .AddItem("lth_ruined", 10)
                .AddItem("lth_flawed", 5);
        }

        private void Cairnmog()
        {
            _builder.Create("VISCARA_CAIRNMOG")
                .AddItem("cairnmog_meat", 7)
                .AddItem("cairnmog_spine", 9)
                .AddItem("cairnmog_blood", 2, 1, true)
                .AddItem("lth_ruined", 10)
                .AddItem("lth_flawed", 20)
                .AddItem("cairnmog_tooth", 4)

                .AddItem("map_049", 1, 1, true);
        }

        private void VellenFleshleader()
        {
            _builder.Create("VISCARA_VELLEN_FLESHLEADER")
                .AddItem("babonsch", 5, 1, true)
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
                .AddItem("map_041", 20, 1, true)
                .AddItem("map_045", 5, 1, true);
        }

        private void VellenFlesheater()
        {
            _builder.Create("VISCARA_VELLEN_FLESHEATER")
                .AddItem("cox_metal", 5)
                .AddItem("lth_flawed", 15)
                .AddItem("lth_ruined", 5)
                .AddItem("fiberp_ruined", 15)
                .AddItem("elec_flawed", 5)
                .AddItem("bubble_choc", 10)
                .AddItem("sweet_butter", 8)
                .AddItem("b_flour", 10)
                .AddGold(30, 20);


            _builder.Create("VISCARA_VELLEN_FLESHEATER_RARES")
                .AddItem("map_041", 20, 1, true)
                .AddItem("map_045", 5, 1, true)
                .AddItem("babonsch", 5, 1, true);
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
                .AddItem("raivor_tail_bone", 10)
                .AddItem("raivor_scale", 5)
                .AddItem("raivor_blood", 8)
                
                .AddItem("map_042", 1, 1, true);
        }

        private void Warocas()
        {
            _builder.Create("VISCARA_WAROCAS")
                .AddItem("warocas_beak", 20)
                .AddItem("waro_feathers", 15)
                .AddItem("lth_ruined", 20)
                .AddItem("warocas_meat", 15)
                .AddItem("waro_leg", 2, 1, true);

            _builder.Create("VISCARA_WAROCAS_RARES")
                .AddItem("waro_leg", 1, 1, true);
        }

        private void Nashtah()
        {
            _builder.Create("VISCARA_NASHTAH")
                .AddItem("lth_ruined", 20)
                .AddItem("lth_flawed", 10)
                .AddItem("nashtah_meat", 30)
                .AddItem("nash_scale", 10)
                .AddItem("nash_tail", 2)
                .AddItem("nashtah_foot", 8)

                .AddItem("map_049", 1, 1, true);
        }

        private void CrystalSpider()
        {
            _builder.Create("VISCARA_CRYSTAL_SPIDER")
                .AddItem("p_crystal_blue", 10)
                .AddItem("p_crystal_red", 10)
                .AddItem("p_crystal_green", 10)
                .AddItem("p_crystal_yellow", 10)
                .AddItem("agate", 1, 1, true);

            _builder.Create("VISCARA_CRYSTAL_SPIDER_RARES")
                .AddItem("map_039", 20, 1, true);
        }
    }
}
