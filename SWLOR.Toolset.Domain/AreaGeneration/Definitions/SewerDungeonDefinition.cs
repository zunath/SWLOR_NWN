#nullable disable
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;

namespace SWLOR.Toolset.Domain.AreaGeneration.Definitions
{
    /// <summary>
    /// Sewer dungeon theme on tds01 (sw_t_sewer): undercity drainage tunnels crawling with vermin
    /// and scavengers. Tiers 1-3 reuse existing Bible-balanced Nar Shaddaa and Veles Sewers Depths
    /// (Module/are/veles_sewers.are.json) creatures and loot tables so no new NPC balance work is
    /// required for the content loop. Tile lighting (0, 2, 2, 2) is the most common non-dark
    /// Main/Source light combination sampled across veles_sewers's Tile_List (39 of 256 tiles;
    /// the given 0/0/3/3 guess did not occur anywhere in that area's tile inventory).
    /// </summary>
    public class SewerDungeonDefinition : IDungeonListDefinition
    {
        public const string ThemeKey = "sewer";

        private readonly DungeonDefinitionBuilder _builder = new();

        public Dictionary<string, DungeonDetail> BuildDungeons()
        {
            _builder.Create(ThemeKey, "Sewer")
                .TilesetProfile(StandardTilesetProfiles.Sewers)
                .LayoutProfile(StandardLayoutProfiles.Warren)
                .SizeRange(8, 32)
                .ExitPlaceable("_mdrn_placedoord", "Sewer Grate")
                .ExitDoor("_mdrn_dt_bars")
                .TreasurePlaceable("corpse", "Stashed Loot")

                // Decoration: the bulk of the visual dressing now lives on the Sewers tileset profile
                // (its own tds01 evidence — see StandardTilesetProfiles.Sewers); only a couple of
                // theme accents (a glowing crystal, a stray force field) are curated here.
                .DecorationDensity(0.3)
                .Decoration("swd_cryst02", 1, DecorationContext.RoomCenter)
                .Decoration("_mdrn_pl_frcfw2", 1, DecorationContext.CorridorSide)

                // Tier 1 — scavenger droids and outlaw looters (CR ~1-5 ambient).
                // Boss: Serpent Leader (CR 11), a Black Serpent enforcer holed up in the tunnels.
                .Tier(1)
                .AddCreature("nar_scavenger", 40)
                .AddCreature("looter_1", 30)
                .AddCreature("looter_2", 30)
                .CreaturesPerRoom(1, 2)
                .Boss("nar_serp_leader")
                .Treasure("NARSHADDAA_SERPENT_LEADER", 2)
                .LevelNote("Ambient CR ~1-5 (Scavenger Droid/Outlaws); boss CR 11 (Serpent Leader).")

                // Tier 2 — undercity criminals (CR ~12-18 ambient). Boss: Slaver Captain (CR 30).
                .Tier(2)
                .AddCreature("nar_hiddenblade", 30)
                .AddCreature("nar_troublemaker", 40)
                .AddCreature("nar_sniper", 30)
                .CreaturesPerRoom(2, 3)
                .Boss("nar_slavercaptn")
                .Treasure("NARSHADDAA_SLAVER_CAPTAIN", 3)
                .LevelNote("Ambient CR ~12-18 (Hidden Blade/Troublemaker/Rooftop Sniper); boss CR 30 (Slaver Captain).")

                // Tier 3 — sewer-depths vermin and Blood Frenzy scavengers (CR ~26-50 ambient).
                // Boss: Kess Draavo (CR 60), the Veles sewers depths boss (pw_sc_velsewboss).
                .Tier(3)
                .AddCreature("womprat", 30)
                .AddCreature("gizka", 40)
                .AddCreature("bf_scavenger", 30)
                .CreaturesPerRoom(2, 4)
                .Boss("bf_kess")
                .Treasure("VISCARA_SEWERS_DEPTHS_KING", 4)
                .LevelNote("Ambient CR ~26-50 (Womprat/Gizka/Red Vein Scavenger); boss CR 60 (Kess Draavo).");

            return _builder.Build();
        }
    }
}
