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

            return _builder.Build();
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
                    .AddItem("recipe_krafterk", 20)
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
                    .AddItem("recipe_dllg005c", 10);

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
                .AddItem("culture_butter", 5);

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
                .AddItem("emerald", 1, 1, true);
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
