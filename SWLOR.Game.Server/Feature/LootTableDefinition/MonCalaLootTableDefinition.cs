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
            EcoTerroristLeader();

            return _builder.Build();
        }

        private void Aradile()
        {
            _builder.Create("MONCALA_ARADILE")
                .AddItem("lth_flawed", 20)
                .AddItem("aradile_meat", 10)
                .AddItem("aradile_skin2", 2, 1, true);
        }

        private void Viper()
        {
            _builder.Create("MONCALA_VIPER")
                .AddItem("lth_flawed", 20)
                .AddItem("lth_ruined", 10)
                .AddItem("viper_bile", 5);
        }

        private void AmphiHydrus()
        {
            _builder.Create("MONCALA_AMPHIHYDRUS")
                .AddItem("amphi_brain", 30)
                .AddItem("amphi_brain2", 30)
                .AddItem("elec_flawed", 10)
                .AddItem("fiberp_flawed", 10)
                .AddGold(20, 20);

            _builder.Create("MONCALA_AMPHIHYDRUS_RARES")
                .AddItem("map_028", 4, 1, true)
                .AddItem("map_029", 4, 1, true)
                .AddItem("agate", 1, 1, true);
        }

        private void EcoTerrorist()
        {
            _builder.Create("MONCALA_ECOTERRORIST")
                .AddItem("elec_flawed", 15)
                .AddItem("fiberp_flawed", 15)
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
                .AddGold(40, 20);

            _builder.Create("MONCALA_ECOTERRORIST_RARES")
                .AddItem("map_027", 3, 1, true)
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
                .AddItem("seeker_boots", 5)
                .AddGold(80, 10);

            _builder.Create("MONCALA_ECOTERRORIST_LEADER_RARES")
                .AddItem("map_027", 1, 1, true)
                .AddItem("rruchi", 1, 1, true);
        }
    }
}
