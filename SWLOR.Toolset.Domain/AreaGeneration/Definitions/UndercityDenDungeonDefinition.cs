#nullable disable
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;

namespace SWLOR.Toolset.Domain.AreaGeneration.Definitions
{
    /// <summary>
    /// Undercity Den dungeon theme on tin01 (sw_t_cityint): a Nar Shaddaa tenement block turned
    /// underworld fight club. Tiers 1-3 reuse existing Bible-balanced Nar Shaddaa street-crime
    /// creatures and loot tables (NarShaddaaSpawnDefinition/NarShaddaaLootTableDefinition, plus the
    /// existing Smuggler's Moon Fight Club Backrooms capstone roster/loot for tiers 2-3) so no new
    /// NPC balance work is required for the content loop — distinct from SewerDungeonDefinition's
    /// Nar Shaddaa/Viscara sewer-tunnel roster (nar_scavenger/looter_1-2/nar_hiddenblade/
    /// nar_troublemaker/nar_sniper/nar_slavercaptn/womprat/gizka/bf_scavenger/bf_kess), avoiding
    /// overlap between the two Nar Shaddaa-flavored themes. Lighting is inherited from
    /// BaseGameTilesetProfiles.CityInterior's own registered TileLighting (0,0,8,8) — an
    /// uncalibrated placeholder per that profile's own doc comment, not newly sampled here.
    /// CityInterior is not listed in TunnelVocabularyCheckTests.ExpectedUnsupported, so this theme
    /// uses Complex.
    /// </summary>
    public class UndercityDenDungeonDefinition : IDungeonListDefinition
    {
        public const string ThemeKey = "undercity";

        private readonly DungeonDefinitionBuilder _builder = new();

        public Dictionary<string, DungeonDetail> BuildDungeons()
        {
            _builder.Create(ThemeKey, "Undercity Den")
                .TilesetProfile(BaseGameTilesetProfiles.CityInterior)
                .LayoutProfile(StandardLayoutProfiles.Complex)
                .SizeRange(8, 32)
                .ExitPlaceable("_mdrn_placedoord", "Tenement Door")
                .ExitDoor("_mdrn_dt_bars")
                .TreasurePlaceable("corpse", "Stashed Contraband")

                // Decoration: the bulk of the visual dressing now lives on the CityInterior tileset
                // profile (its own tin01 evidence — see BaseGameTilesetProfiles.CityInterior); only a
                // couple of theme accents (a market kiosk, a scavenged fence panel) are curated here.
                .DecorationDensity(0.35)
                .Decoration("swd2_kiosk004", 1, DecorationContext.RoomCenter)
                .Decoration("swd2_fence004", 1, DecorationContext.DoorwayFlank)
                .Decoration("_mdrn_pl_rubb031", 1, DecorationContext.DoorwayFlank)

                // Tier 1 — street toughs squatting in the tenement (CR ~2 ambient). Boss: Command
                // Droid (CR 7).
                .Tier(1)
                .AddCreature("nar_thief", 40)
                .AddCreature("nar_serpent", 30)
                .AddCreature("nar_arenafight", 30)
                .CreaturesPerRoom(1, 2)
                .Boss("nar_cmd_droid")
                .Treasure("NARSHADDAA_COMMAND_DROID", 2)
                .LevelNote("Ambient CR ~2 (Nar Shaddaa Thief/Black Serpent/Arena Fighter); boss CR 7 (Command Droid).")

                // Tier 2 — gang enforcers guarding the backroom stairs (CR ~8 ambient). Boss:
                // Crippling Defense Adept (CR 50), the fight club's opening-round champion.
                .Tier(2)
                .AddCreature("nar_redblade", 40)
                .AddCreature("nar_pirate", 40)
                .CreaturesPerRoom(2, 3)
                .Boss("cp_cripdef_ad")
                .Treasure("CAPSTONE_SMUGGLERS_MOON_FIGHT_CLUB_BACKROOMS_LESSON_LOOT", 3)
                .LevelNote("Ambient CR ~8 (Red Blade/Nar Shaddaa Pirate); boss CR 50 (Crippling Defense Adept).")

                // Tier 3 — the fight club's ranked circle (CR ~50-52 ambient). Boss: Crippling
                // Defense Master (CR 60), the backroom champion.
                .Tier(3)
                .AddCreature("cp_cripdef_sp", 40)
                .AddCreature("cp_cripdef_ic", 30)
                .CreaturesPerRoom(2, 4)
                .Boss("cp_cripdef_ms")
                .Treasure("CAPSTONE_SMUGGLERS_MOON_FIGHT_CLUB_BACKROOMS_BOSS_LOOT", 4)
                .LevelNote("Ambient CR ~50-52 (Crippling Defense Specialist/Inner Circle); boss CR 60 (Crippling Defense Master).");

            return _builder.Build();
        }
    }
}
