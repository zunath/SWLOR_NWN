#nullable disable
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;

namespace SWLOR.Toolset.Domain.AreaGeneration.Definitions
{
    /// <summary>
    /// Mandalorian Garrison dungeon theme on twc03 (sw_t_fortint): a fortified garrison held by a
    /// Mandalorian warband. Tiers 1-3 reuse existing Bible-balanced Viscara Mandalorian raider-band
    /// creatures and loot tables (ViscaraSpawnDefinition/ViscaraLootTableDefinition) so no new NPC
    /// balance work is required for the content loop. The whole roster clusters CR 7-14 — the
    /// raider-band content this theme draws from tops out at Harrek Voss (CR 14); there is no
    /// higher-CR "garrison commander" creature in the existing catalog to promote to a bigger final
    /// boss, so the progression is intentionally flat/low rather than forced. Lighting is inherited
    /// from BaseGameTilesetProfiles.FortInterior's own registered TileLighting (0,0,8,8) — an
    /// uncalibrated placeholder per that profile's own doc comment, not newly sampled here.
    /// FortInterior is not listed in TunnelVocabularyCheckTests.ExpectedUnsupported, but this theme
    /// uses Packed (reference: facility interiors) for its wall-sharing barracks-room feel rather
    /// than Complex.
    /// </summary>
    public class MandalorianGarrisonDungeonDefinition : IDungeonListDefinition
    {
        public const string ThemeKey = "mandogarrison";

        private readonly DungeonDefinitionBuilder _builder = new();

        public Dictionary<string, DungeonDetail> BuildDungeons()
        {
            _builder.Create(ThemeKey, "Mandalorian Garrison")
                .TilesetProfile(BaseGameTilesetProfiles.FortInterior)
                .LayoutProfile(StandardLayoutProfiles.Packed)
                .SizeRange(8, 32)
                .ExitPlaceable("_mdrn_placedoord", "Barracks Door")
                .ExitDoor("_mdrn_dt_slid001")
                .TreasurePlaceable("cz220_cache", "Armory Cache")

                // Decoration: the bulk of the visual dressing now lives on the FortInterior tileset
                // profile (mined via the mandogarrison keyword match — see BaseGameTilesetProfiles.
                // FortInterior); only a couple of theme accents (a pazaak table, a mess-hall desk) are
                // curated here.
                .DecorationDensity(0.25)
                .Decoration("_mdrn_pl_pazaaks", 1, DecorationContext.DoorwayFlank)
                .Decoration("_mdrn_pl_deskgry", 1, DecorationContext.RoomCenter)
                .Decoration("_mdrn_pl_couch08", 1, DecorationContext.DoorwayFlank)

                // Tier 1 — garrison rank and file (CR ~7-8 ambient). Boss: Mandalorian Scout
                // (CR 11).
                .Tier(1)
                .AddCreature("man_ranger_1", 40)
                .AddCreature("man_warrior_1", 30)
                .AddCreature("man_warrior_2", 30)
                .CreaturesPerRoom(1, 2)
                .Boss("man_scout")
                .Treasure("VISCARA_MANDALORIAN_WARRIOR", 2)
                .LevelNote("Ambient CR ~7-8 (Mandalorian Ranger/Mandalorian Warrior); boss CR 11 (Mandalorian Scout).")

                // Tier 2 — forward scouts (CR ~8-11 ambient). Boss: Mandalorian War Hero (CR 13).
                .Tier(2)
                .AddCreature("man_ranger_2", 40)
                .AddCreature("man_scout", 30)
                .CreaturesPerRoom(2, 3)
                .Boss("man_leader")
                .Treasure("VISCARA_MANDALORIAN_RANGER", 3)
                .LevelNote("Ambient CR ~8-11 (Mandalorian Ranger/Mandalorian Scout); boss CR 13 (Mandalorian War Hero).")

                // Tier 3 — the garrison command staff (CR ~8-13 ambient). Boss: Harrek Voss,
                // Iron-Stripe (CR 14).
                .Tier(3)
                .AddCreature("man_leader", 30)
                .AddCreature("man_ranger_2", 30)
                .CreaturesPerRoom(2, 3)
                .Boss("harrek_voss")
                .Treasure("VISCARA_HARREK_VOSS_RARES", 4)
                .LevelNote("Ambient CR ~8-13 (Mandalorian War Hero/Mandalorian Ranger); boss CR 14 (Harrek Voss, Iron-Stripe).");

            return _builder.Build();
        }
    }
}
