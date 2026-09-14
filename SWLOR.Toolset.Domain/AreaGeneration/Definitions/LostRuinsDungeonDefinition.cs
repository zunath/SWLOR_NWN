#nullable disable
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;

namespace SWLOR.Toolset.Domain.AreaGeneration.Definitions
{
    /// <summary>
    /// Lost Ruins dungeon theme on tdr01 (sw_t_ruin): a fallen colony's exterior ruins, now home to
    /// Dantooine's grassland fauna and the Dantari tribe that has claimed the site. Tiers 1-3 reuse
    /// existing Bible-balanced Dantooine creatures and loot tables (DantooineSpawnDefinition/
    /// DantooineLootTableDefinition) so no new NPC balance work is required for the content loop —
    /// distinct from AlienRuinDungeonDefinition's Korriban Sith-ruin roster on vmr01, giving the two
    /// "ruins" themes different inhabitants. Lighting is inherited from BaseGameTilesetProfiles.
    /// Ruins's own registered TileLighting (0,0,8,8) — an uncalibrated placeholder per that profile's
    /// own doc comment, not newly sampled here. Ruins is not listed in TunnelVocabularyCheckTests.
    /// ExpectedUnsupported, but this theme uses Halls (reference: crypt/temple interiors, and — via
    /// AccentChannels — Ruins's own ChannelTerrain("Chasm")) rather than Complex, mirroring
    /// AlienRuinDungeonDefinition's own Halls pairing on vmr01.
    /// </summary>
    public class LostRuinsDungeonDefinition : IDungeonListDefinition
    {
        public const string ThemeKey = "lostruins";

        private readonly DungeonDefinitionBuilder _builder = new();

        public Dictionary<string, DungeonDetail> BuildDungeons()
        {
            _builder.Create(ThemeKey, "Lost Ruins")
                .TilesetProfile(BaseGameTilesetProfiles.Ruins)
                .LayoutProfile(StandardLayoutProfiles.Halls)
                .SizeRange(8, 32)
                .ExitPlaceable("zep_portal001", "Weathered Archway")
                .ExitDoor("_mdrn_dt_stneint")
                .TreasurePlaceable("korrduntemple", "Buried Reliquary")

                // Decoration: the bulk of the visual dressing now lives on the Ruins tileset profile
                // (its own tdr01 evidence — see BaseGameTilesetProfiles.Ruins); only a couple of theme
                // accents (a distinctive crystal spire centerpiece, a weathered archway) are curated
                // here.
                .DecorationDensity(0.25)
                .Decoration("crystalspire", 1, DecorationContext.RoomCenter)
                .Decoration("zep_arch002", 1, DecorationContext.DoorwayFlank)

                // Tier 1 — grassland vermin nesting in the rubble (CR ~39-43 ambient). Boss: Iriaz
                // (CR 55).
                .Tier(1)
                .AddCreature("pthune", 40)
                .AddCreature("gizka", 40)
                .CreaturesPerRoom(1, 2)
                .Boss("iriaz")
                .Treasure("DANTOOINE_IRIAZ", 2)
                .LevelNote("Ambient CR ~39-43 (Plains Thune/Gizka); boss CR 55 (Iriaz).")

                // Tier 2 — voritor lizards denning in the colonnade (CR ~55-72 ambient). Boss:
                // Dantari Hunter (CR 73).
                .Tier(2)
                .AddCreature("iriaz", 30)
                .AddCreature("voritorlizard", 40)
                .CreaturesPerRoom(2, 3)
                .Boss("dantarihunter")
                .Treasure("DANTOOINE_DANTARI_HUNTER", 3)
                .LevelNote("Ambient CR ~55-72 (Iriaz/Voritor Lizard); boss CR 73 (Dantari Hunter).")

                // Tier 3 — the Dantari tribe's inner circle (CR ~88-93 ambient). Boss: Dantari
                // Shaman (CR 100).
                .Tier(3)
                .AddCreature("thune", 30)
                .AddCreature("dgraul", 30)
                .CreaturesPerRoom(2, 4)
                .Boss("dantarishaman")
                .Treasure("DANTOOINE_DANTARI_SHAMAN", 4)
                .LevelNote("Ambient CR ~88-93 (Herd Leader Thune/Graul); boss CR 100 (Dantari Shaman).");

            return _builder.Build();
        }
    }
}
