#nullable disable
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;

namespace SWLOR.Toolset.Domain.AreaGeneration.Definitions
{
    /// <summary>
    /// Nightsister Coven dungeon theme on tid01 (sw_t_drowint): a Dathomir witch-cult's ritual
    /// enclave carved into the rock. tid01 is a small, single-district tileset (97 tiles, 12 groups —
    /// see BaseGameTilesetProfiles.DrowInterior's own doc comment), so this theme's roster is
    /// deliberately compact (2 ambient picks per tier) rather than the usual 3. Tiers 1-3 reuse
    /// existing Bible-balanced Dathomir grotto-cult creatures and loot tables
    /// (DathomirSpawnDefinition's GrottosBoss table / DathomirLootTableDefinition) so no new NPC
    /// balance work is required for the content loop. Lighting is inherited from
    /// BaseGameTilesetProfiles.DrowInterior's own registered TileLighting (0,0,8,8) — an uncalibrated
    /// placeholder per that profile's own doc comment, not newly sampled here. DrowInterior is not
    /// listed in TunnelVocabularyCheckTests.ExpectedUnsupported (Complex-capable), but this theme uses
    /// Labyrinth instead: tid01's small room budget suits Labyrinth's tighter MinRooms(3)/MaxRooms(4)
    /// better than Complex's MinRooms(6)/MaxRooms(9).
    /// </summary>
    public class NightsisterCovenDungeonDefinition : IDungeonListDefinition
    {
        public const string ThemeKey = "nightsistercoven";

        private readonly DungeonDefinitionBuilder _builder = new();

        public Dictionary<string, DungeonDetail> BuildDungeons()
        {
            _builder.Create(ThemeKey, "Nightsister Coven")
                .TilesetProfile(BaseGameTilesetProfiles.DrowInterior)
                .LayoutProfile(StandardLayoutProfiles.Labyrinth)
                .SizeRange(8, 32)
                .ExitPlaceable("_mdrn_placedoord", "Ritual Threshold")
                .ExitDoor("_mdrn_dt_bars")
                .TreasurePlaceable("korrduntemple", "Coven Reliquary")

                // Decoration: the bulk of the visual dressing now lives on the DrowInterior tileset
                // profile (mined via the nightsistercoven keyword match — see BaseGameTilesetProfiles.
                // DrowInterior); only a couple of theme accents (a ritual geiser, a coven archway) are
                // curated here.
                .DecorationDensity(0.2)
                .Decoration("zep_geiser002", 1, DecorationContext.RoomCenter)
                .Decoration("zep_arch002", 1, DecorationContext.DoorwayFlank)

                // Tier 1 — coven initiates and their winged familiars (CR ~53 ambient). Boss: Kwi
                // Tribal (CR 60).
                .Tier(1)
                .AddCreature("vdathchirodac", 40)
                .CreaturesPerRoom(1, 2)
                .Boss("vdathtribal")
                .Treasure("DATHOMIR_KWI_TRIBAL", 2)
                .LevelNote("Ambient CR ~53 (Chirodactyl); boss CR 60 (Kwi Tribal).")

                // Tier 2 — coven guardians (CR ~60-93 ambient). Boss: Kwi Shaman (CR 100).
                .Tier(2)
                .AddCreature("vdathtribal", 30)
                .AddCreature("vdathguard", 20)
                .CreaturesPerRoom(2, 3)
                .Boss("vdathshaman")
                .Treasure("DATHOMIR_KWI_SHAMAN", 3)
                .LevelNote("Ambient CR ~60-93 (Kwi Tribal/Kwi Guardian); boss CR 100 (Kwi Shaman).")

                // Tier 3 — the coven's inner ring (CR ~93-100 ambient). Boss: Dark Side Adept
                // (CR 136).
                .Tier(3)
                .AddCreature("vdathguard", 30)
                .AddCreature("vdathshaman", 20)
                .CreaturesPerRoom(2, 3)
                .Boss("vdathdarkadept")
                .Treasure("DATHOMIR_KWI_SHAMAN_GEAR_RARES", 4)
                .LevelNote("Ambient CR ~93-100 (Kwi Guardian/Kwi Shaman); boss CR 136 (Dark Side Adept).");

            return _builder.Build();
        }
    }
}
