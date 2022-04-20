using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class SpaceLootTableDefinition: ILootTableDefinition
    {
        private readonly LootTableBuilder _builder = new();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            Tier1();
            Tier2();
            Tier3();
            Tier4();
            Tier5();

            return _builder.Build();
        }

        private void Tier1()
        {
            _builder.Create("SPACE_SYBIL")
                .AddItem("elec_ruined", 20)
                .AddItem("ref_tilarium", 3);

            _builder.Create("SPACE_TERROR")
                .AddItem("elec_ruined", 20)
                .AddItem("elec_flawed", 2)
                .AddItem("aluminum", 1, 1, true);
        }

        private void Tier2()
        {
            _builder.Create("SPACE_NIGHTMARE")
                .AddItem("elec_flawed", 20)
                .AddItem("ref_currian", 3);

            _builder.Create("SPACE_FERRON")
                .AddItem("elec_flawed", 20)
                .AddItem("elec_good", 2)
                .AddItem("steel", 1, 1, true);
        }

        private void Tier3()
        {
            _builder.Create("SPACE_STORM")
                .AddItem("elec_good", 20)
                .AddItem("ref_idailia", 3);

            _builder.Create("SPACE_RANGER")
                .AddItem("elec_good", 20)
                .AddItem("elec_imperfect", 2)
                .AddItem("obsidian", 1, 1, true);
        }

        private void Tier4()
        {
            _builder.Create("SPACE_HAMMER")
                .AddItem("elec_imperfect", 20)
                .AddItem("ref_barinium", 3)
                .AddItem("obsidian", 1, 1, true);

            _builder.Create("SPACE_DRAKE")
                .AddItem("elec_imperfect", 20)
                .AddItem("elec_high", 2)
                .AddItem("crystal", 1, 1, true);
        }

        private void Tier5()
        {
            _builder.Create("SPACE_BOREALIS")
                .AddItem("elec_high", 20)
                .AddItem("ref_gostian", 3);

            _builder.Create("SPACE_ELEYNA")
                .AddItem("elec_high", 20)
                .AddItem("ref_jasioclase", 2)
                .AddItem("diamond", 1, 1, true);
        }
    }
}
