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
            _builder.Create("KORRIBAN_SITH_CRATE_1");

            _builder.Create("KORRIBAN_SITH_CRATE_2");

            _builder.Create("KORRIBAN_SITH_CRATE_3");
        }
    }
}
