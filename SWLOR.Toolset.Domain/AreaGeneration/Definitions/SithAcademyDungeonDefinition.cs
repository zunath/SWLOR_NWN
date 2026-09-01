#nullable disable
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;

namespace SWLOR.Toolset.Domain.AreaGeneration.Definitions
{
    /// <summary>
    /// Sith Academy dungeon theme on tic01 (sw_t_castle1): a ruined academy annex on Korriban, its
    /// halls still patrolled by the temple's rank and file with a Sith Temple Master guarding the
    /// vault beyond. Tiers 1-3 reuse existing Bible-balanced Korriban Sith Temple/Crypt creatures and
    /// loot tables (KorribanSpawnDefinition/KorribanLootTableDefinition) with a different subset and
    /// tiering than MineCaveDungeonDefinition/AlienRuinDungeonDefinition already use, so this third
    /// Korriban-flavored theme still adds new ambient variety rather than a pure repeat; the tier 3
    /// boss (Sith Temple Master) is a previously-unused Korriban fortress creature. No new NPC
    /// balance work is required for the content loop. Lighting is inherited from
    /// BaseGameTilesetProfiles.CastleInterior's own registered TileLighting (0,0,8,8) — an
    /// uncalibrated placeholder per that profile's own doc comment, not newly sampled here.
    /// CastleInterior is not listed in TunnelVocabularyCheckTests.ExpectedUnsupported, but this theme
    /// uses Halls (reference: crypt/temple interiors) rather than Complex, matching its academy/temple
    /// framing.
    /// </summary>
    public class SithAcademyDungeonDefinition : IDungeonListDefinition
    {
        public const string ThemeKey = "sithacademy";

        private readonly DungeonDefinitionBuilder _builder = new();

        public Dictionary<string, DungeonDetail> BuildDungeons()
        {
            _builder.Create(ThemeKey, "Sith Academy")
                .TilesetProfile(BaseGameTilesetProfiles.CastleInterior)
                .LayoutProfile(StandardLayoutProfiles.Halls)
                .SizeRange(8, 32)
                .ExitPlaceable("zep_portal001", "Academy Gate")
                .ExitDoor("_mdrn_dt_stneint")
                .TreasurePlaceable("korrduntemple", "Sith Archive Vault")

                // Decoration: the bulk of the visual dressing now lives on the CastleInterior tileset
                // profile (mined via the sithacademy keyword match — see BaseGameTilesetProfiles.
                // CastleInterior); only a couple of theme accents (a Sith banner, an academy pillar)
                // are curated here.
                .DecorationDensity(0.25)
                .Decoration("swp_banner0001", 1, DecorationContext.DoorwayFlank)
                .Decoration("swd2_pilr005", 1, DecorationContext.RoomCenter)

                // Tier 1 — academy vermin and rogue initiates (CR ~7-10 ambient). Boss: Possessed
                // Apprentice (CR 15).
                .Tier(1)
                .AddCreature("pelko", 30)
                .AddCreature("korriinitiate", 40)
                .AddCreature("shyrack", 30)
                .CreaturesPerRoom(1, 2)
                .Boss("s_app")
                .Treasure("KORRIBAN_SITH_APPRENTICE", 2)
                .LevelNote("Ambient CR ~7-10 (Pelko Bug Swarm/Rogue Initiate/Shyrack); boss CR 15 (Possessed Apprentice).")

                // Tier 2 — academy annex guardians (CR ~15-20 ambient). Boss: Tuk'ata (CR 34).
                .Tier(2)
                .AddCreature("s_app_m", 30)
                .AddCreature("korr_wraid", 30)
                .AddCreature("sithsnake", 40)
                .CreaturesPerRoom(2, 3)
                .Boss("tukata")
                .Treasure("KORRIBAN_TUKATA", 3)
                .LevelNote("Ambient CR ~15-20 (Possessed Apprentice/Korriban Wraid/Moraband Serpent); boss CR 34 (Tuk'ata).")

                // Tier 3 — the vault's inner guard (CR ~20-34 ambient). Boss: Sith Temple Master
                // (CR 227).
                .Tier(3)
                .AddCreature("sithsnake", 30)
                .AddCreature("tukata", 40)
                .CreaturesPerRoom(2, 4)
                .Boss("vkorrdun4boss")
                .Treasure("CAPSTONE_KORRIBAN_SITH_CRYPT_DEPTHS_BOSS_LOOT", 4)
                .LevelNote("Ambient CR ~20-34 (Moraband Serpent/Tuk'ata); boss CR 227 (Sith Temple Master).");

            return _builder.Build();
        }
    }
}
