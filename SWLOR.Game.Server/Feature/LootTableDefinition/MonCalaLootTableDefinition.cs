using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class MonCalaLootTableDefinition: ILootTableDefinition
    {
        private readonly LootTableBuilder _builder = new();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            Aradile();
            Viper();
            AmphiHydrus();
            EcoTerrorist();
            EcoTerroristLeader();
            Microtench();
            Octotench();
            Scorchellus();

            return _builder.Build();
        }

        private void Aradile()
        {
            _builder.Create("MONCALA_ARADILE")
                .AddItem("lth_good", 20)
                .AddItem("aradile_meat", 10)
                .AddItem("aradile_tail", 10)
                .AddItem("aradile_innards", 5);

            _builder.Create("MONCALA_ARADILE_RARES")
                .AddItem("aradile_skin2", 1, 1, true);
        }

        private void Viper()
        {
            _builder.Create("MONCALA_VIPER")
                .AddItem("lth_flawed", 5)
                .AddItem("lth_good", 20)
                .AddItem("viper_meat", 10)
                .AddItem("viper_bile", 5)
                .AddItem("viper_guts", 5);
        }

        private void AmphiHydrus()
        {
            _builder.Create("MONCALA_AMPHIHYDRUS")
                .AddItem("amphi_brain", 30)
                .AddItem("amphi_brain2", 30)
                .AddItem("amphi_blood", 15)
                .AddItem("elec_good", 10)
                .AddItem("fiberp_good", 10)
                .AddItem("c_butter", 5)
                .AddGold(20, 20);

            _builder.Create("MONCALA_AMPHIHYDRUS_RARES")
                .IsRare()
                .AddItem("map_028", 2, 1, true)
                .AddItem("map_029", 2, 1, true)
                .AddItem("agate", 1, 1, true);
        }

        private void EcoTerrorist()
        {
            _builder.Create("MONCALA_ECOTERRORIST")
                .AddItem("elec_good", 15)
                .AddItem("fiberp_good", 15)
                .AddItem("herb_c", 15)
                .AddItem("poach_longsword", 5)
                .AddItem("poach_knife", 5)
                .AddItem("poach_gswd", 5)
                .AddItem("poach_spear", 5)
                .AddItem("poach_katar", 5)
                .AddItem("poach_staff", 5)
                .AddItem("poach_pistol", 5)
                .AddItem("poach_twinblade", 5)
                .AddItem("poach_rifle", 5)
                .AddItem("poach_shuriken", 5)
                .AddItem("poach_katar", 5)
                .AddItem("c_butter", 1)
                .AddGold(40, 20);

            _builder.Create("MONCALA_ECOTERRORIST_GEAR")
                .IsRare()
                .AddItem("poach_shield", 5)
                .AddItem("poach_cloak", 5)
                .AddItem("poach_belt", 5)
                .AddItem("poach_ring", 5)
                .AddItem("poach_necklace", 5)
                .AddItem("poach_armor", 5)
                .AddItem("poach_helmet", 5)
                .AddItem("poach_bracer", 5)
                .AddItem("poach_leggings", 5)
                .AddItem("hunter_cloak", 5)
                .AddItem("hunter_belt", 5)
                .AddItem("hunter_ring", 5)
                .AddItem("hunter_necklace", 5)
                .AddItem("hunter_tunic", 5)
                .AddItem("hunter_cap", 5)
                .AddItem("hunter_gloves", 5)
                .AddItem("hunter_boots", 5)
                .AddItem("seeker_cloak", 5)
                .AddItem("seeker_belt", 5)
                .AddItem("seeker_ring", 5)
                .AddItem("seeker_necklace", 5)
                .AddItem("seeker_tunic", 5)
                .AddItem("seeker_cap", 5)
                .AddItem("seeker_gloves", 5)
                .AddItem("seeker_boots", 5);

            _builder.Create("MONCALA_ECOTERRORIST_RARES")
                .IsRare()
                .AddItem("map_027", 3, 1, true)
                .AddItem("map_59", 1, 1, true)
                .AddItem("map_60", 1, 1, true);
        }

        private void EcoTerroristLeader()
        {
            _builder.Create("MONCALA_ECOTERRORIST_LEADER")
                .AddItem("poach_shield", 5)
                .AddItem("poach_cloak", 5)
                .AddItem("poach_belt", 5)
                .AddItem("poach_ring", 5)
                .AddItem("poach_necklace", 5)
                .AddItem("poach_armor", 5)
                .AddItem("poach_helmet", 5)
                .AddItem("poach_bracer", 5)
                .AddItem("poach_leggings", 5)
                .AddGold(80, 10);

            _builder.Create("MONCALA_ECOTERRORIST_LEADER_HUNT")
                .AddItem("hunter_cloak", 5)
                .AddItem("hunter_belt", 5)
                .AddItem("hunter_ring", 5)
                .AddItem("hunter_necklace", 5)
                .AddItem("hunter_tunic", 5)
                .AddItem("hunter_cap", 5)
                .AddItem("hunter_gloves", 5)
                .AddItem("hunter_boots", 5);

            _builder.Create("MONCALA_ECOTERRORIST_LEADER_SEEK")
                .AddItem("seeker_cloak", 5)
                .AddItem("seeker_belt", 5)
                .AddItem("seeker_ring", 5)
                .AddItem("seeker_necklace", 5)
                .AddItem("seeker_tunic", 5)
                .AddItem("seeker_cap", 5)
                .AddItem("seeker_gloves", 5)
                .AddItem("seeker_boots", 5);

            _builder.Create("MONCALA_ECOTERRORIST_LEADER_RARES")
                .IsRare()
                .AddItem("map_027", 1, 1, true)
                .AddItem("citrine", 1, 1, true)
                .AddItem("rruchi", 1, 1, true)
                .AddItem("map_59", 1, 1, true)
                .AddItem("map_60", 1, 1, true);
        }

        private void Microtench()
        {
            _builder.Create("MONCALA_MICROTENCH")
                .AddItem("mtench_tentacle", 20)
                .AddItem("mtench_mantle", 10);

            _builder.Create("MONCALA_MICROTENCH_RARES")
                .IsRare()
                .AddItem("mtench_ink", 3, 1, true)
                .AddItem("mtench_poison", 1, 1, true);
        }

        private void Octotench()
        {
            _builder.Create("MONCALA_OCTOTENCH")
                .AddItem("mtench_tentacle", 25)
                .AddItem("mtench_mantle", 20)
                .AddItem("mtench_ink", 3)
                .AddItem("mtench_sac", 3)
                .AddItem("mtench_poison", 3);

            _builder.Create("MONCALA_OCTOTENCH_RARES")
                .IsRare()
                .AddItem("mtench_ink", 3, 1, true)
                .AddItem("mtench_sac", 3, 1, true)
                .AddItem("mtench_poison", 3, 1, true)
                .AddItem("map_58", 1, 1, true);
        }

        private void Scorchellus()
        {
            _builder.Create("MONCALA_SCORCHELLUS")
                .AddItem("scorch_tail", 20)
                .AddItem("scorch_leg", 10)
                .AddItem("scorch_chitin", 15)
                .AddItem("scorch_antennae", 5);

            _builder.Create("MONCALA_SCORCHELLUS_RARES")
                .AddItem("map_58", 1, 1, true);
        }
    }
}
