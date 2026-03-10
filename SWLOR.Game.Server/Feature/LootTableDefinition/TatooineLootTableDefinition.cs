using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class TatooineLootTableDefinition : ILootTableDefinition
    {
        private readonly LootTableBuilder _builder = new();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            Womprat();
            Sandswimmer();
            SandBeetle();
            SandDemon();
            SandWorm();
            TuskenRaider();
            TuskenElite();
            TuskenCrate();
            AncientWorm();

            return _builder.Build();
        }

        private void Womprat()
        {
            _builder.Create("TATOOINE_WOMPRAT")
                .AddItem("lth_imperfect", 15)
                .AddItem("womprathide", 5)
                .AddItem("womprattooth", 5)
                .AddItem("wompratclaw", 5)
                .AddItem("wompratmeat", 5)
                .AddItem("womp_innards", 5);
        }

        private void Sandswimmer()
        {
            _builder.Create("TATOOINE_SANDSWIMMER")
                .AddItem("lth_imperfect", 15)
                .AddItem("sandswimmerfin", 5)
                .AddItem("sandswimmerh", 5)
                .AddItem("sandswimmerleg", 5);
        }

        private void SandBeetle()
        {
            _builder.Create("TATOOINE_SAND_BEETLE")
                .AddItem("shat_beetle_chit", 20)
                .AddItem("beetle_chitin", 1)
                .AddItem("wild_leg", 1);
        }

        private void SandDemon()
        {
            _builder.Create("TATOOINE_SAND_DEMON")
                .AddItem("fiberp_high", 20)
                .AddItem("herb_t", 20)
                .AddItem("lth_high", 15)
                .AddItem("sanddemonclaw", 5)
                .AddItem("sanddemonhide", 5)
                .AddItem("sand_demon_leg", 10)
                .AddItem("sanddemon_meat", 5);

            _builder.Create("TATOOINE_SAND_DEMON_RARES")
                .IsRare()
                .AddItem("ruby", 1, 1, true);
        }

        private void SandWorm()
        {
            _builder.Create("TATOOINE_SAND_WORM")
                .AddItem("fiberp_high", 20)
                .AddItem("lth_high", 20)
                .AddItem("wild_meat", 10)
                .AddItem("wild_innards", 10)
                .AddItem("sandwormtooth", 5);

            _builder.Create("TATOOINE_SAND_WORM_RARES")
                .IsRare()
                .AddItem("emerald", 1, 1, true);

        }

        private void TuskenRaider()
        {
            _builder.Create("TATOOINE_TUSKEN_RAIDER")
                .AddItem("elec_imperfect", 20)
                .AddItem("fiberp_imperfect", 15)

                .AddItem("raider_longsword", 5)
                .AddItem("raider_knife", 5)
                .AddItem("raider_gswd", 5)
                .AddItem("raider_spear", 5)
                .AddItem("raider_katar", 5)
                .AddItem("raider_staff", 5)
                .AddItem("raider_pistol", 5)
                .AddItem("raider_shuriken", 5)
                .AddItem("raider_twinblade", 5)
                .AddItem("raider_rifle", 5)

                .AddItem("tusken_meat", 10)
                .AddItem("tusken_bones", 5)
                .AddItem("tusken_blood", 10)
                .AddItem("r_flour", 5);

            _builder.Create("TATOOINE_TUSKEN_RAIDER_RARES")
                .IsRare()
                .AddItem("bread_flour", 2, 1, true)
                .AddItem("ruby", 3, 1, true)
                .AddItem("map_038", 1, 1, true)
                .AddItem("map_036", 1, 1, true);
        }

        private void TuskenElite()
        {
            _builder.Create("TATOOINE_TUSKEN_ELITE")
                .AddItem("elec_high", 12)
                .AddItem("fiberp_high", 7)

                .AddItem("raider_longsword", 3)
                .AddItem("raider_knife", 3)
                .AddItem("raider_gswd", 3)
                .AddItem("raider_spear", 3)
                .AddItem("raider_katar", 3)
                .AddItem("raider_staff", 3)
                .AddItem("raider_pistol", 3)
                .AddItem("raider_shuriken", 3)
                .AddItem("raider_twinblade", 3)
                .AddItem("raider_rifle", 3)
                .AddItem("tusken_meat", 7)
                .AddItem("tusken_bones", 3)
                .AddItem("tusken_blood", 5)
                .AddItem("r_flour", 3);

            _builder.Create("TATOOINE_TUSKEN_ELITE_RARES")
                .IsRare()
                .AddItem("bread_flour", 4, 1, true)
                .AddItem("ruby", 8, 1, true)
                .AddItem("hyphae_wood", 4, 1, true)
                .AddItem("map_038", 1, 1, true)
                .AddItem("map_036", 1, 1, true);
        }

        private void TuskenCrate()
        {
            _builder.Create("TATOOINE_TUSKEN_CRATE")
                .AddItem("elec_imperfect", 20)
                .AddItem("fiberp_imperfect", 15)
                .AddItem("plant_butter", 10)
                .AddItem("r_flour", 10)
                .AddItem("cultured_butter", 1, 1, true)
                .AddItem("dried_bonito", 1, 1, true)
                .AddItem("ruby", 2, 1, true)
                .AddItem("p_flour", 1, 1, true)
                .AddGold(100, 30);

        }

        private void AncientWorm()
        {
            _builder.Create("TATOOINE_ANCIENT_WORM")
                .AddItem("fiberp_high", 20)
                .AddItem("lth_high", 20)
                .AddItem("wild_meat", 10)
                .AddItem("wild_innards", 10)
                .AddItem("sandwormtooth", 5)
                .AddItem("chiro_shard", 1);

            _builder.Create("TATOOINE_ANCIENT_WORM_GEMS")
                .AddItem("emerald", 100, 1, true)
                .AddItem("chiro_shard", 50, 1, true);

            _builder.Create("TATOOINE_ANCIENT_WORM_RARES")
                .IsRare()
                .AddItem("emerald", 1, 1, true)
                .AddItem("chiro_shard", 1, 1, true)
                .AddGold(1000, 5);
        }
    }
}
