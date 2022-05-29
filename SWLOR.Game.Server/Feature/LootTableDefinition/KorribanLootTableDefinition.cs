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

            SithCryptCrates();

            return _builder.Build();
        }

        private void Hssiss()
        {
            _builder.Create("KORRIBAN_HSSISS")
                .AddItem("lth_good", 20);
        }

        private void MorabandSerpent()
        {
            _builder.Create("KORRIBAN_MORABAND_SERPENT")
                .AddItem("lth_good", 20);
        }

        private void Shyrack()
        {
            _builder.Create("KORRIBAN_SHYRACK")
                .AddItem("lth_good", 20);
        }

        private void Terentatek()
        {
            _builder.Create("KORRIBAN_TERENTATEK")
                .AddItem("lth_good", 20);
        }

        private void Tukata()
        {
            _builder.Create("KORRIBAN_TUKATA")
                .AddItem("lth_good", 20);
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
                .AddGold(24, 15)
                .AddItem("jade", 1, 1, true);

            _builder.Create("KORRIBAN_SITH_CRATE_2")
                .AddItem("elec_flawed", 20)
                .AddItem("ref_scordspar", 5)
                .AddItem("fine_wood", 5)
                .AddItem("fiberp_flawed", 5)
                .AddItem("distilled_water", 10)
                .AddItem("b_flour", 10)
                .AddItem("sweet_butter", 5)
                .AddItem("v_apple", 3)
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
                .AddGold(40, 10)
                .AddItem("agate", 1, 1, true)
                .AddItem("map_56", 1, 1, true);
        }
    }
}
