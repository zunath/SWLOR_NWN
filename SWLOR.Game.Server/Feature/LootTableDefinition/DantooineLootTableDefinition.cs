using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class DantooineLootTableDefinition : ILootTableDefinition
    {
        private readonly LootTableBuilder _builder = new();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            DantooineHay();
            MedicalCache();
            QueenKinrath();
            ThuneLeader();
            DantariHunter();
            DantariShaman();
            HiveKinrath();
            Gizka();
            PlainsThune();
            VoritorLizard();
            BolBoss();
            Iriaz();
            DantooineHerb();

            CapstoneDantooineDungeonRares();
            DanenclaveRareElites();
            DanmedRareElites();
            return _builder.Build();
        }

        private void DanmedRareElites()
        {
            _builder.Create("DANMED_TRIAGE_RARES").IsRare()
                .AddItem("triagewardenda", 1, 1, true).AddItem("triagewardendb", 1, 1, true).AddItem("bp_triagewarden", 1, 1, true);
            _builder.Create("DANMED_CHEM_RARES").IsRare()
                .AddItem("chemslingerda", 1, 1, true).AddItem("chemslingerdb", 1, 1, true).AddItem("bp_chemslinger", 1, 1, true);
            _builder.Create("DANMED_CONDUIT_RARES").IsRare()
                .AddItem("conduitmatrnda", 1, 1, true).AddItem("conduitmatrndb", 1, 1, true).AddItem("bp_conduitmatrn", 1, 1, true);
            _builder.Create("DANMED_TRIAGE_COMP").AddItem("triagewardencm", 1, 1);
            _builder.Create("DANMED_CHEM_COMP").AddItem("chemslingercm", 1, 1);
            _builder.Create("DANMED_CONDUIT_COMP").AddItem("conduitmatrncm", 1, 1);
        }

        private void DanenclaveRareElites()
        {
            _builder.Create("DANENCLAVE_SABRAE_RARES").IsRare()
                .AddItem("sabraetrialda", 1, 1, true).AddItem("sabraetrialdb", 1, 1, true).AddItem("bp_sabraetrial", 1, 1, true);
            _builder.Create("DANENCLAVE_SENTINEL_RARES").IsRare()
                .AddItem("enclavesentlda", 1, 1, true).AddItem("enclavesentldb", 1, 1, true).AddItem("bp_enclavesentl", 1, 1, true);
            _builder.Create("DANENCLAVE_CYCLONE_RARES").IsRare()
                .AddItem("cycloneadptda", 1, 1, true).AddItem("cycloneadptdb", 1, 1, true).AddItem("bp_cycloneadpt", 1, 1, true);
            _builder.Create("DANENCLAVE_SABRAE_COMP").AddItem("sabraetrialcm", 1, 1);
            _builder.Create("DANENCLAVE_SENTINEL_COMP").AddItem("enclavesentlcm", 1, 1);
            _builder.Create("DANENCLAVE_CYCLONE_COMP").AddItem("cycloneadptcm", 1, 1);
        }

        private void CapstoneDantooineDungeonRares()
        {
            _builder.Create("CAPSTONE_SABSTORM_RARES")
                .IsRare()
                .AddItem("sabstorm_l1", 1, 1, true)
                .AddItem("sabstorm_l2", 1, 1, true)
                .AddItem("sabstorm_l3", 1, 1, true)
                .AddItem("sabstorm_l4", 1, 1, true)
                .AddItem("sabstorm_l5", 1, 1, true)
                .AddItem("sabstorm_l6", 1, 1, true)
                .AddItem("sabstorm_l7", 1, 1, true)
                .AddItem("sabstorm_l8", 1, 1, true);
            _builder.Create("CAPSTONE_SABSTORM_WD_RARES")
                .IsRare()
                .AddItem("sabstorm_w1", 1, 1, true)
                .AddItem("sabstorm_w2", 1, 1, true)
                .AddItem("sabstorm_w3", 1, 1, true)
                .AddItem("sabstorm_w4", 1, 1, true)
                .AddItem("sabstorm_w5", 1, 1, true);

            _builder.Create("CAPSTONE_GUARDMST_RARES")
                .IsRare()
                .AddItem("guardmst_l1", 1, 1, true)
                .AddItem("guardmst_l2", 1, 1, true)
                .AddItem("guardmst_l3", 1, 1, true)
                .AddItem("guardmst_l4", 1, 1, true)
                .AddItem("guardmst_l5", 1, 1, true)
                .AddItem("guardmst_l6", 1, 1, true)
                .AddItem("guardmst_l7", 1, 1, true)
                .AddItem("guardmst_l8", 1, 1, true);
            _builder.Create("CAPSTONE_GUARDMST_WD_RARES")
                .IsRare()
                .AddItem("guardmst_w1", 1, 1, true)
                .AddItem("guardmst_w2", 1, 1, true)
                .AddItem("guardmst_w3", 1, 1, true)
                .AddItem("guardmst_w4", 1, 1, true)
                .AddItem("guardmst_w5", 1, 1, true);

            _builder.Create("CAPSTONE_SABCYCL_RARES")
                .IsRare()
                .AddItem("sabcycl_l1", 1, 1, true)
                .AddItem("sabcycl_l2", 1, 1, true)
                .AddItem("sabcycl_l3", 1, 1, true)
                .AddItem("sabcycl_l4", 1, 1, true)
                .AddItem("sabcycl_l5", 1, 1, true)
                .AddItem("sabcycl_l6", 1, 1, true)
                .AddItem("sabcycl_l7", 1, 1, true)
                .AddItem("sabcycl_l8", 1, 1, true);
            _builder.Create("CAPSTONE_SABCYCL_WD_RARES")
                .IsRare()
                .AddItem("sabcycl_w1", 1, 1, true)
                .AddItem("sabcycl_w2", 1, 1, true)
                .AddItem("sabcycl_w3", 1, 1, true)
                .AddItem("sabcycl_w4", 1, 1, true)
                .AddItem("sabcycl_w5", 1, 1, true);

            _builder.Create("CAPSTONE_EMCOCKTAIL_RARES")
                .IsRare()
                .AddItem("emcocktail_l1", 1, 1, true)
                .AddItem("emcocktail_l2", 1, 1, true)
                .AddItem("emcocktail_l3", 1, 1, true)
                .AddItem("emcocktail_l4", 1, 1, true)
                .AddItem("emcocktail_l5", 1, 1, true)
                .AddItem("emcocktail_l6", 1, 1, true)
                .AddItem("emcocktail_l7", 1, 1, true)
                .AddItem("emcocktail_l8", 1, 1, true);
            _builder.Create("CAPSTONE_EMCOCKTAIL_WD_RARES")
                .IsRare()
                .AddItem("emcocktail_w1", 1, 1, true)
                .AddItem("emcocktail_w2", 1, 1, true)
                .AddItem("emcocktail_w3", 1, 1, true)
                .AddItem("emcocktail_w4", 1, 1, true)
                .AddItem("emcocktail_w5", 1, 1, true);

            _builder.Create("CAPSTONE_HOLDLINE_RARES")
                .IsRare()
                .AddItem("holdline_l1", 1, 1, true)
                .AddItem("holdline_l2", 1, 1, true)
                .AddItem("holdline_l3", 1, 1, true)
                .AddItem("holdline_l4", 1, 1, true)
                .AddItem("holdline_l5", 1, 1, true)
                .AddItem("holdline_l6", 1, 1, true)
                .AddItem("holdline_l7", 1, 1, true)
                .AddItem("holdline_l8", 1, 1, true);
            _builder.Create("CAPSTONE_HOLDLINE_WD_RARES")
                .IsRare()
                .AddItem("holdline_w1", 1, 1, true)
                .AddItem("holdline_w2", 1, 1, true)
                .AddItem("holdline_w3", 1, 1, true)
                .AddItem("holdline_w4", 1, 1, true)
                .AddItem("holdline_w5", 1, 1, true);

            _builder.Create("CAPSTONE_INFCONDUIT_RARES")
                .IsRare()
                .AddItem("infconduit_l1", 1, 1, true)
                .AddItem("infconduit_l2", 1, 1, true)
                .AddItem("infconduit_l3", 1, 1, true)
                .AddItem("infconduit_l4", 1, 1, true)
                .AddItem("infconduit_l5", 1, 1, true)
                .AddItem("infconduit_l6", 1, 1, true)
                .AddItem("infconduit_l7", 1, 1, true)
                .AddItem("infconduit_l8", 1, 1, true);
            _builder.Create("CAPSTONE_INFCONDUIT_WD_RARES")
                .IsRare()
                .AddItem("infconduit_w1", 1, 1, true)
                .AddItem("infconduit_w2", 1, 1, true)
                .AddItem("infconduit_w3", 1, 1, true)
                .AddItem("infconduit_w4", 1, 1, true)
                .AddItem("infconduit_w5", 1, 1, true);
        }

        private void QueenKinrath()
        {
            _builder.Create("DANTOOINE_KINRATH_QUEEN")
                .AddItem("fiberp_high", 20)
                .AddItem("yotbean", 100);

            _builder.Create("DANTOOINE_KINRATH_QUEEN_RARES")
                .IsRare()
                .AddItem("emerald", 1, 1, true)
                .AddItem("map_72", 2, 1, true)
                .AddItem("map_76", 2, 1, true)
                .AddItem("map_77", 2, 1, true)
                .AddItem("map_78", 2, 1, true)
                .AddItem("dan_kin_boots", 20, 1, true);

            _builder.Create("DANTOOINE_KINRATH_QUEEN_GEMS")
                    .AddItem("emerald", 100, 1, true);

            _builder.Create("DANTOOINE_KINRATH_QUEEN_RECIPES")
                    .AddItem("recipe_dancarrot", 20)
                .AddItem("recipe_krafters", 20);

        }

        private void ThuneLeader()
        {
            _builder.Create("DANTOOINE_THUNE_LEADER")
                .AddItem("lth_high", 1)
                .AddItem("thune_meat", 10)
                .AddItem("thune_blood", 100);

            _builder.Create("DANTOOINE_THUNE_LEADER_RARES")
                .IsRare()
                .AddItem("ruby", 20, 1, true)
                .AddItem("emerald", 80, 1, true);
        }

        private void DantariHunter()
        {
            _builder.Create("DANTOOINE_DANTARI_HUNTER")
                .AddItem("bantha_milk", 70)
                .AddItem("carrot", 10)
                .AddItem("cultured_butter", 5);

            _builder.Create("DANTOOINE_DANTARI_HUNTER_GEAR")
                .AddItem("dantari_iknife", 20);

            _builder.Create("DANTOOINE_DANTARI_HUNTER_GEAR_RARES")
                .IsRare()
                .AddItem("dan_heavy_armor", 1)
                .AddItem("dan_hyper_gloves", 1)
                .AddItem("dan_h_cloak", 1)
                .AddItem("dan_shuriken", 1);

            _builder.Create("DANTOOINE_DANTARI_HUNTER_RARES")
                .IsRare()
                .AddItem("bantha_milk", 99, 1, true)
                .AddItem("map_70", 2, 1, true)
                .AddItem("map_71", 2, 1, true)
                .AddItem("map_73", 2, 1, true)
                .AddItem("emerald", 1, 1, true);
        }

        private void DantariShaman()
        {
            _builder.Create("DANTOOINE_DANTARI_SHAMAN")
                .AddItem("fiberp_imperfect", 5)
                .AddItem("fiberp_high", 10)
                .AddItem("lth_imperfect", 5)
                .AddItem("lth_high", 10)
                .AddItem("bantha_milk", 1);

            _builder.Create("DANTOOINE_DANTARI_SHAMAN_GEAR_RARES")
                .IsRare()
                .AddItem("dan_h_cloak", 10)
                .AddItem("dan_shuriken", 10)
                .AddItem("dan_spear", 10);

            _builder.Create("DANTOOINE_DANTARI_SHAMAN_RARES")
                .IsRare()
                .AddItem("map_70", 2, 1, true)
                .AddItem("map_71", 2, 1, true)
                .AddItem("map_73", 2, 1, true)
                .AddItem("map_74", 2, 1, true)
                .AddItem("map_75", 2, 1, true)
                .AddItem("bread_flour", 2, 1, true);
        }

        private void HiveKinrath()
        {
            _builder.Create("DANTOOINE_HIVE_KINRATH")
                .AddItem("fiberp_imperfect", 10)
                .AddItem("fiberp_high", 5)
                .AddItem("lth_imperfect", 10)
                .AddItem("lth_high", 5)
                .AddItem("bread_flour", 5);

            _builder.Create("DANTOOINE_HIVE_KINRATH_RARES")
                .IsRare()
                .AddItem("ruby", 99, 1, true)
                .AddItem("map_72", 2, 1, true)
                .AddItem("emerald", 1, 1, true)
                .AddItem("lockbox_t5", 2, 1, true);
        }

        private void Gizka()
        {
            _builder.Create("DANTOOINE_GIZKA")
                .AddItem("yotbean", 5)
                .AddItem("lth_high", 10)
                .AddItem("wild_innards", 10);

            _builder.Create("DANTOOINE_GIZKA_RARES")
                .IsRare()
                .AddItem("ruby", 99, 1, true);
        }

        private void PlainsThune()
        {
            _builder.Create("DANTOOINE_PLAINS_THUNE")
                .AddItem("thune_meat", 10)
                .AddItem("thune_blood", 2);

            _builder.Create("DANTOOINE_PLAINS_THUNE_RARES")
                .IsRare()
                .AddItem("emerald", 1, 1, true);
        }

        private void VoritorLizard()
        {
            _builder.Create("DANTOOINE_VORITOR_LIZARD")
                .AddItem("wild_leg", 5)
                .AddItem("tomato", 20)
                .AddItem("yotbean", 10);

            _builder.Create("DANTOOINE_VORITOR_LIZARD_RARES")
                .IsRare()
                .AddItem("ruby", 99, 1, true);

        }

        private void BolBoss()
        {
            _builder.Create("DANTOOINE_BOL_BOSS")
                .AddItem("lth_high", 20)
                .AddItem("carrot", 20)
                .AddItem("bol_leather", 40)
                .AddItem("milk", 100);

            _builder.Create("DANTOOINE_BOL_BOSS_RARES")
                .IsRare()
                .AddItem("ruby", 99, 1, true)
                .AddItem("emerald", 1, 1, true);

            _builder.Create("DANTOOINE_BOL_BOSS_RECIPES")
                .AddItem("recipe_bolrifle", 10)
                .AddItem("recipe_danflap", 10)
                .AddItem("recipe_ocsushi", 10)
                .AddItem("recipe_iksushi", 10)
                .AddItem("recipe_wisushi", 10)
                .AddItem("recipe_tesushi", 10)
                .AddItem("recipe_dosushi", 10);
        }

        private void Iriaz()
        {
            _builder.Create("DANTOOINE_IRIAZ")
                .AddItem("wild_meat", 10)
                .AddItem("yotbean", 50)
                .AddItem("wild_leg", 2);

            _builder.Create("DANTOOINE_IRIAZ_RARES")
                .IsRare()
                .AddItem("ruby", 99, 1, true)
                .AddItem("emerald", 1, 1, true);
        }
        private void MedicalCache()
        {
            _builder.Create("DANTOOINE_JUNKPILES")
                .AddItem("kolto_injection", 20)
                .AddItem("medisyringes", 20)
                .AddGold(10, 15);
        }

        private void DantooineHay()
        {
            _builder.Create("DANTOOINE_HAY")
                .AddItem("haybundle", 50);
        }

        private void DantooineHerb()
        {
            _builder.Create("DANTOOINE_HERB")
                .AddItem("dant_starwort", 50);

        }
    }
}
