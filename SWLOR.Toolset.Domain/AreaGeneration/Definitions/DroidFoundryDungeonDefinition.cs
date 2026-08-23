#nullable disable
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;

namespace SWLOR.Toolset.Domain.AreaGeneration.Definitions
{
    /// <summary>
    /// Droid Foundry dungeon theme on tsw01 (sw_t_steamwork): an industrial droid-assembly works
    /// left running unattended. Tiers 1-3 reuse existing Bible-balanced CZ-220/Viscara droid and
    /// Blood Frenzy creatures and loot tables (CZ220SpawnDefinition/ViscaraSpawnDefinition's Sewers
    /// Depths roster + CZ220LootTableDefinition/ViscaraLootTableDefinition, plus the existing Viscara
    /// Republic Engineering Bunker capstone loot for the tier 3 boss) so no new NPC balance work is
    /// required for the content loop. Tier 1's low-CR patrol droids (CR 1-4) to tier 2's Blood Frenzy
    /// elites (CR 50-52) is a single steep jump — the catalog has no CR 10-40 industrial-droid content
    /// bridging that gap — but each step is itself real, deployed content (not invented), so the gap
    /// is reported rather than smoothed with reused filler from another theme. Lighting is inherited
    /// from BaseGameTilesetProfiles.Steamworks's own registered TileLighting (0,0,8,8) — an
    /// uncalibrated placeholder per that profile's own doc comment, not newly sampled here.
    /// Steamworks is not listed in TunnelVocabularyCheckTests.ExpectedUnsupported, so this theme uses
    /// Complex.
    /// </summary>
    public class DroidFoundryDungeonDefinition : IDungeonListDefinition
    {
        public const string ThemeKey = "droidfoundry";

        private readonly DungeonDefinitionBuilder _builder = new();

        public Dictionary<string, DungeonDetail> BuildDungeons()
        {
            _builder.Create(ThemeKey, "Droid Foundry")
                .TilesetProfile(BaseGameTilesetProfiles.Steamworks)
                .LayoutProfile(StandardLayoutProfiles.Complex)
                .SizeRange(8, 32)
                .ExitPlaceable("zep_portal001", "Foundry Gate")
                .ExitDoor("_mdrn_dt_slid001")
                .TreasurePlaceable("cz220_cache", "Parts Cache")

                // Decoration: the bulk of the visual dressing now lives on the Steamworks tileset
                // profile (its own tsw01 evidence — see BaseGameTilesetProfiles.Steamworks); only a
                // couple of theme accents (a scrapped droid chassis, a foundry lamp) are curated here.
                .DecorationDensity(0.2)
                .Decoration("_mdrn_pl_droidd2", 1, DecorationContext.RoomCenter)
                .Decoration("_mdrn_pl_lampd04", 1, DecorationContext.DoorwayFlank)

                // Tier 1 — malfunctioning assembly-line units (CR ~1-4 ambient). Boss: Rogue Droid
                // (CR 7), a supervisor unit gone rogue.
                .Tier(1)
                .AddCreature("malsecdroid", 35)
                .AddCreature("malspiderdroid", 35)
                .AddCreature("colicoidexp", 30)
                .CreaturesPerRoom(1, 2)
                .Boss("nar_rogue_droid")
                .Treasure("CZ220_LOOT_DROID", 2)
                .LevelNote("Ambient CR ~1-4 (Malfunctioning Patrol Droid/Probe Droid, Colicoid Experiment); boss CR 7 (Rogue Droid).")

                // Tier 2 — foundry security drones (CR ~50 ambient). Boss: Blood Frenzy Duelist
                // (CR 52), reframed as the foundry's ranking enforcer.
                .Tier(2)
                .AddCreature("bf_scavenger", 40)
                .AddCreature("bf_pulsedroid", 40)
                .CreaturesPerRoom(2, 3)
                .Boss("bf_duelist")
                .Treasure("VISCARA_SEWERS_DEPTHS_PULSE_DROID", 3)
                .LevelNote("Ambient CR ~50 (Red Vein Scavenger/Pulse-Frame Training Droid); boss CR 52 (Blood Frenzy Duelist).")

                // Tier 3 — the foundry's ranking enforcers (CR ~50-52 ambient). Boss: Emergency
                // Bunker Master (CR 60), the foundry overseer.
                .Tier(3)
                .AddCreature("bf_duelist", 30)
                .AddCreature("vrix7", 20)
                .CreaturesPerRoom(2, 4)
                .Boss("cp_embunker_ms")
                .Treasure("CAPSTONE_VISCARA_REPUBLIC_ENGINEERING_BUNKER_BOSS_LOOT", 4)
                .LevelNote("Ambient CR ~50-52 (Blood Frenzy Duelist/Vrix-7, Pulse Butcher); boss CR 60 (Emergency Bunker Master).");

            return _builder.Build();
        }
    }
}
