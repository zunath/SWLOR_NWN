using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class MonCalaLootTableDefinition: ILootTableDefinition
    {
        private readonly LootTableBuilder _builder = new LootTableBuilder();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            Aradile();
            Viper();
            AmphiHydrus();
            EcoTerrorist();

            return _builder.Build();
        }

        private void Aradile()
        {
            _builder.Create("MONCALA_ARADILE");
        }

        private void Viper()
        {
            _builder.Create("MONCALA_VIPER");
        }

        private void AmphiHydrus()
        {
            _builder.Create("MONCALA_AMPHIHYDRUS");
        }

        private void EcoTerrorist()
        {
            _builder.Create("MONCALA_ECOTERRORIST");
        }
    }
}
