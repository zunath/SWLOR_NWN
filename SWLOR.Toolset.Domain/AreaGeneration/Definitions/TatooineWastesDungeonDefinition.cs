#nullable disable
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;

namespace SWLOR.Toolset.Domain.AreaGeneration.Definitions
{
    /// <summary>
    /// Tatooine Wastes dungeon theme on ttd01 (sw_t_tatooine): sun-blasted canyon clearings picked
    /// over by Jawa scavengers and Tusken raiding bands. Tiers 1-3 reuse existing Bible-balanced
    /// Tatooine creatures and loot tables (TatooineSpawnDefinition/TatooineLootTableDefinition) so no
    /// new NPC balance work is required for the content loop. Lighting is inherited from
    /// BaseGameTilesetProfiles.Desert's own registered TileLighting (0,0,8,8) — an uncalibrated
    /// placeholder per that profile's own doc comment, not newly sampled here. Desert's
    /// SolidTerrainOverride("Cliff")/PrimaryOpenTerrain("Desert") composition has no verified Tunnel
    /// vocabulary (TunnelVocabularyCheckTests.ExpectedUnsupported), so this theme uses Halls
    /// (OpenLane rooms-and-corridors) rather than Complex.
    /// </summary>
    public class TatooineWastesDungeonDefinition : IDungeonListDefinition
    {
        public const string ThemeKey = "tatooinewastes";

        private readonly DungeonDefinitionBuilder _builder = new();

        public Dictionary<string, DungeonDetail> BuildDungeons()
        {
            _builder.Create(ThemeKey, "Tatooine Wastes")
                .TilesetProfile(BaseGameTilesetProfiles.Desert)
                .LayoutProfile(StandardLayoutProfiles.Halls)
                .SizeRange(8, 32)
                .ExitPlaceable("_mdrn_placedoord", "Canyon Passage")
                .ExitDoor("_mdrn_dt_rough")
                .TreasurePlaceable("structure_rubble", "Sand-Buried Cache")

                // Decoration: the bulk of the visual dressing now lives on the Desert tileset profile
                // (its own ttd01 evidence — see BaseGameTilesetProfiles.Desert); only a couple of
                // theme accents (a rough-hewn stool, a canyon archway) are curated here.
                .DecorationDensity(0.15)
                .Decoration("swd_stool01", 1, DecorationContext.RoomCenter)
                .Decoration("zep_arch003", 1, DecorationContext.DoorwayFlank)

                // Tier 1 — Jawa scavenger bands raiding a fresh wreck (CR ~2-5 ambient).
                // Boss: Tusken Raider scout (CR 17).
                .Tier(1)
                .AddCreature("ext_jawa002", 40)
                .AddCreature("ext_jawa003", 30)
                .AddCreature("ext_jawa004", 30)
                .CreaturesPerRoom(1, 2)
                .Boss("ext_tusken_tr003")
                .Treasure("TATOOINE_TUSKEN_RAIDER", 2)
                .LevelNote("Ambient CR ~2-5 (Jawa Raider/Aggressive Jawa/Jawa Ninja); boss CR 17 (Tusken Raider scout).")

                // Tier 2 — Tusken raiding party holding the canyon mouth (CR ~21-25 ambient).
                // Boss: Tusken Elite (CR 49).
                .Tier(2)
                .AddCreature("tusken_melee", 40)
                .AddCreature("sandswimmer", 30)
                .AddCreature("sandbeetle", 30)
                .CreaturesPerRoom(2, 3)
                .Boss("tusken_elite1")
                .Treasure("TATOOINE_TUSKEN_ELITE", 3)
                .LevelNote("Ambient CR ~21-25 (Tusken Raider/Sandswimmer/Sand Beetle); boss CR 49 (Tusken Elite).")

                // Tier 3 — hardened Tusken elites and a bounty hunter laying low (CR ~53-142
                // ambient). Boss: Sand Worm (CR 220).
                .Tier(3)
                .AddCreature("tusken_elite2", 40)
                .AddCreature("vtattbountyhunt", 20)
                .CreaturesPerRoom(2, 4)
                .Boss("sandworm")
                .Treasure("TATOOINE_SAND_WORM", 4)
                .LevelNote("Ambient CR ~53-142 (Tusken Elite/Peerless Bounty Hunter); boss CR 220 (Sand Worm).");

            return _builder.Build();
        }
    }
}
