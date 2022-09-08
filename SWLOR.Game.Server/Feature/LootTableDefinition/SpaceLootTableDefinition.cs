using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class SpaceLootTableDefinition : ILootTableDefinition
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

            _builder.Create("SPACE_COURIER")
                .AddItem("elec_ruined", 1, 2)
                .AddItem("ref_tilarium", 1, 2)
                .AddGold(200, 1)
                .AddItem("ref_veldite", 1, 5)
                .AddItem("lth_ruined", 1, 2)
                .AddItem("jade", 1, 2)
                .AddItem("aluminum", 1, 2)
                .AddItem("wood", 1, 2)
                .AddItem("fiberp_ruined", 1, 2);
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

            _builder.Create("SPACE_SHUTTLE")
                .AddItem("elec_flawed", 1, 2)
                .AddItem("ref_scordspar", 1, 2)
                .AddGold(400, 1)
                .AddItem("ref_currian", 1, 5)
                .AddItem("lth_flawed", 1, 2)
                .AddItem("agate", 1, 2)
                .AddItem("fiberp_flawed", 1, 2)
                .AddItem("fine_wood", 1, 2)
                .AddItem("steel", 1, 2);
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

            _builder.Create("SPACE_FREIGHT")
                .AddItem("elec_good", 1, 2)
                .AddItem("ref_plagionite", 1, 2)
                .AddGold(600, 1)
                .AddItem("ref_idailia", 1, 5)
                .AddItem("lth_good", 1, 2)
                .AddItem("citrine", 1, 2)
                .AddItem("fiberp_good", 1, 2)
                .AddItem("ancient_wood", 1, 2)
                .AddItem("obsidian", 1, 2);
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

            _builder.Create("SPACE_BULK")
                .AddItem("elec_imperfect", 1, 2)
                .AddItem("ref_keromber", 1, 2)
                .AddGold(800, 1)
                .AddItem("ref_barinium", 1, 5)
                .AddItem("lth_imperfect", 1, 2)
                .AddItem("crystal", 1, 2)
                .AddItem("fiberp_imperfect", 1, 2)
                .AddItem("aracia_wood", 1, 2)
                .AddItem("ruby", 1, 2);
        }

        private void Tier5()
        {
            _builder.Create("SPACE_BOREALIS")
                .AddItem("elec_high", 20)
                .AddItem("ref_gostian", 3);

            _builder.Create("SPACE_ELEYNA")
                .AddItem("elec_high", 20)
                .AddItem("ref_jasioclase", 2)
                .AddItem("diamond", 1, 1, true)
                .AddItem("hyphae_wood", 1, 1, true)
                .AddItem("p_flour", 1, 1, true);

            _builder.Create("SPACE_MERCHANT")
                .AddItem("elec_high", 1, 2)
                .AddItem("ref_gostian", 1, 5)
                .AddGold(1000, 1)
                .AddItem("ref_jasioclase", 1, 2)
                .AddItem("lth_high", 1, 2)
                .AddItem("diamond", 1, 2)
                .AddItem("fiberp_high", 1, 2)
                .AddItem("hyphae_wood", 1, 2)
                .AddItem("emerald", 1, 2);

            _builder.Create("KYBER_CAP")
                .AddItem("kyberfragment", 1, 1);
        }
    }
}
