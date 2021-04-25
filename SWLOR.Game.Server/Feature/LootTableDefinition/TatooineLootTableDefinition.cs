using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class TatooineLootTableDefinition: ILootTableDefinition
    {
        private readonly LootTableBuilder _builder = new LootTableBuilder();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            Womprat();
            Sandswimmer();
            SandBeetle();
            SandDemon();

            return _builder.Build();
        }

        private void Womprat()
        {
            _builder.Create("TATOOINE_WOMPRAT");
        }

        private void Sandswimmer()
        {
            _builder.Create("TATOOINE_SANDSWIMMER");
        }

        private void SandBeetle()
        {
            _builder.Create("TATOOINE_SAND_BEETLE");
        }

        private void SandDemon()
        {
            _builder.Create("TATOOINE_SAND_DEMON");
        }
    }
}
