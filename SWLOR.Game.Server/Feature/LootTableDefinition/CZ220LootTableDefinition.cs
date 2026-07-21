using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class CZ220LootTableDefinition: ILootTableDefinition
    {
        private readonly LootTableBuilder _builder = new();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            Credits();
            JunkPile();
            SuppliesCache();
            Mynock();
            Droid();
            DroidRares();
            ColicoidExperiment();
            ColicoidExperimentRares();

            CapstoneCZ220DungeonRares();
            CapstoneCZ220RareElites();
            return _builder.Build();
        }

        // Unique loot for the named rare elite droids that spawn in the Breaker Yard.
        // Each drops a small pool of reusable industrial-salvage gear.
        private void CapstoneCZ220RareElites()
        {
            // Rare pool: unique gear plus a blueprint that unlocks a salvage crafting recipe.
            _builder.Create("CZ220_BULWARK_RARES")
                .IsRare()
                .AddItem("yard_plate", 1, 1, true)
                .AddItem("servo_frame", 1, 1, true)
                .AddItem("bp_reactorpl", 1, 1, true);
            _builder.Create("CZ220_SLAGBORN_RARES")
                .IsRare()
                .AddItem("slag_crusher", 1, 1, true)
                .AddItem("reclaim_gaunt", 1, 1, true)
                .AddItem("bp_pistongaunt", 1, 1, true);
            _builder.Create("CZ220_DEMOLISHER_RARES")
                .IsRare()
                .AddItem("ord_rig", 1, 1, true)
                .AddItem("blast_harness", 1, 1, true)
                .AddItem("bp_siegeoptic", 1, 1, true);

            // Guaranteed encounter-specific salvage component that the recipes require.
            _builder.Create("CZ220_BULWARK_COMPONENT")
                .AddItem("reactor_core", 1, 1);
            _builder.Create("CZ220_SLAGBORN_COMPONENT")
                .AddItem("crusher_piston", 1, 1);
            _builder.Create("CZ220_DEMOLISHER_COMPONENT")
                .AddItem("targeting_lens", 1, 1);
        }

        private void CapstoneCZ220DungeonRares()
        {
            _builder.Create("CAPSTONE_ADAMGUARD_RARES")
                .IsRare()
                .AddItem("adamguard_l1", 1, 1, true)
                .AddItem("adamguard_l2", 1, 1, true)
                .AddItem("adamguard_l3", 1, 1, true)
                .AddItem("adamguard_l4", 1, 1, true)
                .AddItem("adamguard_l5", 1, 1, true)
                .AddItem("adamguard_l6", 1, 1, true)
                .AddItem("adamguard_l7", 1, 1, true)
                .AddItem("adamguard_l8", 1, 1, true);
            _builder.Create("CAPSTONE_ADAMGUARD_WD_RARES")
                .IsRare()
                .AddItem("adamguard_w1", 1, 1, true)
                .AddItem("adamguard_w2", 1, 1, true)
                .AddItem("adamguard_w3", 1, 1, true)
                .AddItem("adamguard_w4", 1, 1, true)
                .AddItem("adamguard_w5", 1, 1, true);

            _builder.Create("CAPSTONE_SCRAPLOCK_RARES")
                .IsRare()
                .AddItem("scraplock_l1", 1, 1, true)
                .AddItem("scraplock_l2", 1, 1, true)
                .AddItem("scraplock_l3", 1, 1, true)
                .AddItem("scraplock_l4", 1, 1, true)
                .AddItem("scraplock_l5", 1, 1, true)
                .AddItem("scraplock_l6", 1, 1, true)
                .AddItem("scraplock_l7", 1, 1, true)
                .AddItem("scraplock_l8", 1, 1, true);
            _builder.Create("CAPSTONE_SCRAPLOCK_WD_RARES")
                .IsRare()
                .AddItem("scraplock_w1", 1, 1, true)
                .AddItem("scraplock_w2", 1, 1, true)
                .AddItem("scraplock_w3", 1, 1, true)
                .AddItem("scraplock_w4", 1, 1, true)
                .AddItem("scraplock_w5", 1, 1, true);

            _builder.Create("CAPSTONE_WORLDBRK_RARES")
                .IsRare()
                .AddItem("worldbrk_l1", 1, 1, true)
                .AddItem("worldbrk_l2", 1, 1, true)
                .AddItem("worldbrk_l3", 1, 1, true)
                .AddItem("worldbrk_l4", 1, 1, true)
                .AddItem("worldbrk_l5", 1, 1, true)
                .AddItem("worldbrk_l6", 1, 1, true)
                .AddItem("worldbrk_l7", 1, 1, true)
                .AddItem("worldbrk_l8", 1, 1, true);
            _builder.Create("CAPSTONE_WORLDBRK_WD_RARES")
                .IsRare()
                .AddItem("worldbrk_w1", 1, 1, true)
                .AddItem("worldbrk_w2", 1, 1, true)
                .AddItem("worldbrk_w3", 1, 1, true)
                .AddItem("worldbrk_w4", 1, 1, true)
                .AddItem("worldbrk_w5", 1, 1, true);
        }

        private void Credits()
        {
            _builder.Create("CZ220_CREDITS")
                .AddGold(10, 10);
        }

        private void JunkPile()
        {
            _builder.Create("CZ220_LOOT_JUNK_PILES")
                .AddItem("scrap_metal", 50)
                .AddGold(5, 5);
        }

        private void SuppliesCache()
        {
            _builder.Create("CZ220_LOOT_SUPPLIES_CACHE")
                .AddItem("scrap_metal", 10)
                .AddItem("elec_ruined", 5)
                .AddItem("lth_ruined", 5)
                .AddItem("fiberp_ruined", 50)
                .AddItem("wood", 50)
                .AddItem("v_pebble", 10)
                .AddGold(10, 15);
        }

        private void Mynock()
        {
            _builder.Create("CZ220_LOOT_MYNOCK")
                .AddItem("mynock_meat", 50)
                .AddItem("mynock_tooth", 20)
                .AddItem("lth_ruined", 5);

            _builder.Create("CZ220_LOOT_MYNOCK_WINGS")
                .AddItem("mynock_wing", 10);
        }

        private void Droid()
        {
            _builder.Create("CZ220_LOOT_DROID")
                .AddItem("elec_ruined", 50)
                .AddItem("scrap_metal", 10)
                .AddGold(10, 20);
        }

        private void DroidRares()
        {
            _builder.Create("CZ220_LOOT_DROID_RARES")
                .IsRare()
                .AddItem("map_22", 50, 1, true)
                .AddItem("lockbox_t1", 4, 1, true);
        }

        private void ColicoidExperiment()
        {
            _builder.Create("CZ220_LOOT_COLICOID")
                .AddItem("colicoid_cap_b", 1, 20)
                .AddItem("colicoid_cap_g", 1, 20)
                .AddItem("colicoid_cap_y", 1, 3)
                .AddItem("colicoid_cap_r", 1, 20)

                .AddItem("colicoid_leg_a", 1, 20)
                .AddItem("colicoid_leg_c", 1, 20)
                .AddItem("colicoid_leg_w", 1, 20)
                .AddItem("colicoid_leg_e", 1, 20)
                .AddItem("colicoid_leg_f", 1, 20)
                ;
        }

        private void ColicoidExperimentRares()
        {
            _builder.Create("CZ220_LOOT_COLICOID_RARES")
                .IsRare()
                .AddItem("bag_dirty", 1, 1, true)
                .AddItem("map_22", 3, 1, true);
        }

    }
}
