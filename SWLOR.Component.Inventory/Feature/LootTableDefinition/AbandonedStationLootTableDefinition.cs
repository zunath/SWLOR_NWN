using SWLOR.Component.Inventory.Contracts;
using SWLOR.Component.Inventory.Model;
using SWLOR.Component.Inventory.Service;

namespace SWLOR.Component.Inventory.Feature.LootTableDefinition
{
    public class AbandonedStationLootTableDefinition: ILootTableDefinition
    {
        private readonly ILootTableBuilder _builder;

        public AbandonedStationLootTableDefinition(ILootTableBuilder lootTableBuilder)
        {
            _builder = lootTableBuilder;
        }

        public Dictionary<string, LootTable> BuildLootTables()
        {
            ZombieRancor();

            return _builder.Build();
        }

        private void ZombieRancor()
        {
            _builder.Create("ABANDONED_STATION_ZOMBIE_RANCOR");
        }
    }
}
