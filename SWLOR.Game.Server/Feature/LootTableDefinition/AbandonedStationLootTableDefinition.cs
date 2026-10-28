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
            StationGear();
            BossUniques();
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
                .AddItem("quar_seal", 6)
                .AddGold(45, 20);

            // Restricted Level and Director's Chambers, levels 25-30.
            _builder.Create("STATION_RESTRICTED")
                .AddItem("elec_good", 15)
                .AddItem("fiberp_good", 10)
                .AddItem("lth_good", 10)
                .AddItem("elec_flawed", 5)
                .AddItem("quar_seal", 8)
                .AddGold(60, 20);

            _builder.Create("STATION_RARES")
                .IsRare()
                .AddItem("citrine", 70, 1, true)
                .AddItem("lockbox_t3", 30, 1, true)
                .AddItem("map_277", 20, 1, true);

            _builder.Create("STATION_DIRECTOR")
                .AddItem("lockbox_t3", 1);

            _builder.Create("STATION_RANCOR")
                .AddItem("lockbox_t3", 1);
        }

        private void StationGear()
        {
            // Level-25 gear worn by the crew: security drops Containment armor and weapons,
            // dock crews drop Gantry light armor, researchers drop Biolab Force armor.
            _builder.Create("STATION_SECURITY_GEAR")
                .AddItem("cnt_helmet", 1)
                .AddItem("cnt_armor", 1)
                .AddItem("cnt_bracer", 1)
                .AddItem("cnt_leggings", 1)
                .AddItem("cnt_shield", 1)
                .AddItem("cnt_longsword", 1)
                .AddItem("cnt_knife", 1)
                .AddItem("cnt_gswd", 1)
                .AddItem("cnt_spear", 1)
                .AddItem("cnt_twinblade", 1)
                .AddItem("cnt_katar", 1)
                .AddItem("cnt_pglove", 1)
                .AddItem("cnt_staff", 1)
                .AddItem("cnt_pistol", 1)
                .AddItem("cnt_twinpistol", 1)
                .AddItem("cnt_rifle", 1)
                .AddItem("cnt_shuriken", 1);

            _builder.Create("STATION_CREW_GEAR")
                .AddItem("gantry_cap", 1)
                .AddItem("gantry_tunic", 1)
                .AddItem("gantry_gloves", 1)
                .AddItem("gantry_boots", 1);

            _builder.Create("STATION_RESEARCH_GEAR")
                .AddItem("biolab_cap", 1)
                .AddItem("biolab_tunic", 1)
                .AddItem("biolab_gloves", 1)
                .AddItem("biolab_boots", 1);

            // Colicoids, test subjects and the rancor carry whatever their victims wore.
            _builder.Create("STATION_GEAR")
                .AddItem("cnt_helmet", 1)
                .AddItem("cnt_armor", 1)
                .AddItem("cnt_bracer", 1)
                .AddItem("cnt_leggings", 1)
                .AddItem("cnt_shield", 1)
                .AddItem("cnt_longsword", 1)
                .AddItem("cnt_knife", 1)
                .AddItem("cnt_gswd", 1)
                .AddItem("cnt_spear", 1)
                .AddItem("cnt_twinblade", 1)
                .AddItem("cnt_katar", 1)
                .AddItem("cnt_pglove", 1)
                .AddItem("cnt_staff", 1)
                .AddItem("cnt_pistol", 1)
                .AddItem("cnt_twinpistol", 1)
                .AddItem("cnt_rifle", 1)
                .AddItem("cnt_shuriken", 1)
                .AddItem("gantry_cap", 1)
                .AddItem("gantry_tunic", 1)
                .AddItem("gantry_gloves", 1)
                .AddItem("gantry_boots", 1)
                .AddItem("biolab_cap", 1)
                .AddItem("biolab_tunic", 1)
                .AddItem("biolab_gloves", 1)
                .AddItem("biolab_boots", 1);
        }

        private void BossUniques()
        {
            _builder.Create("STATION_DIRECTOR_RARES")
                .IsRare()
                .AddItem("director_coat", 1, 1, true)
                .AddItem("director_signet", 1, 1, true);

            _builder.Create("STATION_RANCOR_RARES")
                .IsRare()
                .AddItem("rancorbone_gswd", 1, 1, true)
                .AddItem("rancorhide_cloak", 1, 1, true);

            // Blueprints for the crew lines' crafted accessories; each recipe also needs a Quarantine Seal from the floors.
            _builder.Create("STATION_DIRECTOR_RECIPES")
                .AddItem("bp_cntcloak", 1)
                .AddItem("bp_cntbelt", 1)
                .AddItem("bp_cntring", 1)
                .AddItem("bp_cntneck", 1)
                .AddItem("bp_gantcloak", 1)
                .AddItem("bp_gantbelt", 1)
                .AddItem("bp_gantring", 1)
                .AddItem("bp_gantneck", 1)
                .AddItem("bp_biocloak", 1)
                .AddItem("bp_biobelt", 1)
                .AddItem("bp_bioring", 1)
                .AddItem("bp_bioneck", 1);
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
