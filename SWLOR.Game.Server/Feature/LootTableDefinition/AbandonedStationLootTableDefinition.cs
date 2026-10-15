using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class AbandonedStationLootTableDefinition: ILootTableDefinition
    {
        private readonly LootTableBuilder _builder = new();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            ZombieRancor();
            StationHorrors();
            NamedRares();

            return _builder.Build();
        }

        private void ZombieRancor()
        {
            _builder.Create("ABANDONED_STATION_ZOMBIE_RANCOR");
        }

        private void StationHorrors()
        {
            // Main Level, levels 20-24: salvage off the reanimated crew.
            _builder.Create("STATION_MAIN")
                .AddItem("elec_flawed", 15)
                .AddItem("elec_good", 10)
                .AddItem("fiberp_flawed", 10)
                .AddItem("fiberp_good", 5)
                .AddGold(45, 20);

            // Restricted Level and Director's Chambers, levels 25-30.
            _builder.Create("STATION_RESTRICTED")
                .AddItem("elec_good", 15)
                .AddItem("fiberp_good", 10)
                .AddItem("lth_good", 10)
                .AddItem("elec_flawed", 5)
                .AddGold(60, 20);

            _builder.Create("STATION_RARES")
                .IsRare()
                .AddItem("citrine", 70, 1, true)
                .AddItem("lockbox_t3", 30, 1, true)
                .AddItem("map_277", 2, 1, true);

            _builder.Create("STATION_DIRECTOR")
                .AddItem("lockbox_t3", 1);

            _builder.Create("STATION_RANCOR")
                .AddItem("lockbox_t3", 1);
        }

        private void NamedRares()
        {
            // Named rare elites: unique gear plus a blueprint, and a guaranteed component that the blueprint's recipe requires.
            _builder.Create("STATION_CHIEF_RARES")
                .IsRare()
                .AddItem("riotvisor_helm", 1, 1, true)
                .AddItem("lockdown_bracer", 1, 1, true)
                .AddItem("bp_breachbelt", 1, 1, true);
            _builder.Create("STATION_SUBZERO_RARES")
                .IsRare()
                .AddItem("quarantine_mask", 1, 1, true)
                .AddItem("isolation_shroud", 1, 1, true)
                .AddItem("bp_serumneck", 1, 1, true);

            _builder.Create("STATION_CHIEF_COMP")
                .AddItem("override_spike", 1, 1);
            _builder.Create("STATION_SUBZERO_COMP")
                .AddItem("plague_culture", 1, 1);
        }
    }
}
