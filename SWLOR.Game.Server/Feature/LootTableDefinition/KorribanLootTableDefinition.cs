using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class KorribanLootTableDefinition: ILootTableDefinition
    {
        private readonly LootTableBuilder _builder = new();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            Hssiss();
            MorabandSerpent();
            Shyrack();
            Terentatek();
            Tukata();
            SithApprenticeGhost();

            SithCryptCrates();

            return _builder.Build();
        }

        private void Hssiss()
        {
            _builder.Create("KORRIBAN_HSSISS")
                .AddItem("lth_ruined", 25)
                .AddItem("lth_flawed", 15)
                .AddItem("hssiss_meat", 10)
                .AddItem("hssiss_tail", 10)
                .AddItem("hssiss_innards", 5)
                .AddItem("hssiss_skin2", 2, 1, true);
        }

        private void MorabandSerpent()
        {
            _builder.Create("KORRIBAN_MORABAND_SERPENT")
                .AddItem("lth_ruined", 10)
                .AddItem("lth_flawed", 5)
                .AddItem("mserp_meat", 10)
                .AddItem("mserp_bile", 5)
                .AddItem("mserp_guts", 5);
        }

        private void Shyrack()
        {
            _builder.Create("KORRIBAN_SHYRACK")
                .AddItem("lth_flawed", 20)
                .AddItem("shyrack_wing", 10)
                .AddItem("shyrack_meat", 50)
                .AddItem("shyrack_tooth", 20);
        }

        private void Terentatek()
        {
            _builder.Create("KORRIBAN_TERENTATEK")
                .AddItem("lth_ruined", 20)
                .AddItem("lth_flawed", 20)
                .AddItem("lth_good", 5)
                .AddItem("teren_claw", 10)
                .AddItem("teren_tusk", 5)
                .AddItem("teren_tooth", 5)
                .AddItem("teren_spine", 1, 1, true);
        }

        private void Tukata()
        {
            _builder.Create("KORRIBAN_TUKATA")
                .AddItem("lth_ruined", 10)
                .AddItem("lth_flawed", 2)
                .AddItem("tukata_fur", 20)
                .AddItem("tukata_tooth", 20)
                .AddItem("tukata_meat", 10)
                .AddItem("tukata_blood", 5);
        }

        private void SithApprenticeGhost()
        {
            _builder.Create("KORRIBAN_SITH_APPRENTICE")
                .AddItem("lth_ruined", 20)
                .AddItem("lth_flawed", 10)
                .AddItem("lth_good", 5)
                .AddItem("sith_longsword", 5)
                .AddItem("sith_knife", 5)
                .AddItem("sith_gswd", 5)
                .AddItem("sith_spear", 5)
                .AddItem("sith_katar", 5)
                .AddItem("sith_staff", 5)
                .AddItem("sith_pistol", 5)
                .AddItem("sith_twinblade", 5)
                .AddItem("sith_rifle", 5)
                .AddItem("sith_shuriken", 5)
                .AddItem("sith_katar", 5)

                .AddItem("abdamaryllia", 2, 1, true)
                .AddItem("map_56", 5, 1, true)
                .AddGold(32, 5);
        }

        private void SithCryptCrates()
        {
            _builder.Create("KORRIBAN_SITH_CRATE_1")
                .AddItem("elec_flawed", 20)
                .AddItem("ref_scordspar", 5)
                .AddItem("fine_wood", 5)
                .AddItem("fiberp_flawed", 5)
                .AddItem("distilled_water", 10)
                .AddItem("b_flour", 2)
                .AddItem("sweet_butter", 5)
                .AddItem("v_apple", 3)
                .AddItem("choco_cookies", 4)
                .AddItem("med_supplies", 3)
                .AddItem("jade", 1, 1, true)

                .AddGold(24, 15);

            _builder.Create("KORRIBAN_SITH_CRATE_2")
                .AddItem("elec_flawed", 20)
                .AddItem("ref_scordspar", 5)
                .AddItem("fine_wood", 5)
                .AddItem("fiberp_flawed", 5)
                .AddItem("distilled_water", 10)
                .AddItem("b_flour", 10)
                .AddItem("sweet_butter", 5)
                .AddItem("v_apple", 3)
                .AddItem("med_supplies", 3)
                .AddItem("cairn_sandwich", 3)

                .AddGold(22, 15);

            _builder.Create("KORRIBAN_SITH_CRATE_3")
                .AddItem("elec_flawed", 20)
                .AddItem("ref_scordspar", 12)
                .AddItem("fine_wood", 12)
                .AddItem("fiberp_flawed", 12)
                .AddItem("b_flour", 5)
                .AddItem("sweet_butter", 5)
                .AddItem("v_apple", 3)
                .AddItem("green_curry", 5)
                .AddItem("med_supplies", 3)
                .AddItem("agate", 1, 1, true)
                .AddItem("map_56", 1, 1, true)

                .AddGold(40, 10);
        }
    }
}
