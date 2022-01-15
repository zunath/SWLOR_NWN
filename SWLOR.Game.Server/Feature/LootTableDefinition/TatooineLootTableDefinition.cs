﻿using System.Collections.Generic;
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
            SandWorm();
            TuskenRaider();
            TuskenCrate();

            return _builder.Build();
        }

        private void Womprat()
        {
            _builder.Create("TATOOINE_WOMPRAT")
                .AddItem("lth_imperfect", 15)
                .AddItem("womprathide", 5)
                .AddItem("womprattooth", 5)
                .AddItem("wompratclaw", 5)
                .AddItem("wompratmeat", 5);
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
                .AddItem("beetle_chitin", 1);
        }

        private void SandDemon()
        {
            _builder.Create("TATOOINE_SAND_DEMON")
                .AddItem("fiberp_high", 20)
                .AddItem("herb_t", 20)
                .AddItem("lth_high", 15)
                .AddItem("sanddemonclaw", 5)
                .AddItem("sanddemonhide", 5)
                .AddItem("ruby", 3, 1, true);
        }

        private void SandWorm()
        {
            _builder.Create("TATOOINE_SAND_WORM")
                .AddItem("fiberp_high", 20)
                .AddItem("lth_high", 20)
                .AddItem("sandwormtooth", 5, 1, true)
                .AddItem("emerald", 1, 1, true)
                .AddItem("ruby", 2, 1, true);
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

                .AddItem("ruby", 2, 1, true);

            _builder.Create("TATOOINE_TUSKEN_RAIDER_RARES")
                .AddItem("map_038", 20, 1, true)
                .AddItem("map_036", 1, 1, true);
        }

        private void TuskenCrate()
        {
            _builder.Create("TATOOINE_TUSKEN_CRATE")
                .AddItem("elec_imperfect", 20)
                .AddItem("fiberp_imperfect", 15)
                .AddItem("ruby", 2, 1, true)
                .AddGold(100,30);

        }
    }
}
