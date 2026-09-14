#nullable disable
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;

namespace SWLOR.Toolset.Domain.AreaGeneration.Definitions
{
    /// <summary>
    /// Sci-Fi Base dungeon theme on zsf01 (sw_t_scifibase): an abandoned/hostile facility overrun
    /// by malfunctioning droids and rogue security forces. Tiers 1-3 reuse existing Bible-balanced
    /// CZ-220 (Module/are/czs220_maintlvl.are.json), Nar Shaddaa, and Republic security creatures
    /// and loot tables so no new NPC balance work is required for the content loop. Tile lighting
    /// (4, 0, 2, 2) matches czs220_maintlvl.
    /// </summary>
    public class SciFiBaseDungeonDefinition : IDungeonListDefinition
    {
        public const string ThemeKey = "scifibase";

        private readonly DungeonDefinitionBuilder _builder = new();

        public Dictionary<string, DungeonDetail> BuildDungeons()
        {
            _builder.Create(ThemeKey, "Sci-Fi Base")
                .TilesetProfile(StandardTilesetProfiles.Facility)
                .LayoutProfile(StandardLayoutProfiles.Complex)
                .SizeRange(8, 32)
                .ExitPlaceable("_mdrn_placedoord", "Maintenance Hatch")
                .ExitDoor("_mdrn_dt_slid001")
                .TreasurePlaceable("cz220_cache", "Supply Cache")

                // Decoration: the bulk of the visual dressing now lives on the Facility tileset
                // profile (its own zsf01 evidence — see StandardTilesetProfiles.Facility); only a
                // couple of theme accents (an engineering console, a hazard marker) are curated here.
                .DecorationDensity(0.17)
                .Decoration("_mdrn_pl_engctr1", 1, DecorationContext.RoomCenter)
                .Decoration("_mdrn_pl_fac13xe", 1, DecorationContext.DoorwayFlank)

                // Tier 1 — malfunctioning junkyard droids and lab escapees (CR ~1-4 ambient).
                // Boss: Republic Trooper (CR 10), a security patrol that never left the facility.
                .Tier(1)
                .AddCreature("malsecdroid", 35)
                .AddCreature("malspiderdroid", 35)
                .AddCreature("colicoidexp", 30)
                .CreaturesPerRoom(1, 2)
                .Boss("republictrooperf")
                .Treasure("CZ220_LOOT_SUPPLIES_CACHE", 2)
                .LevelNote("Ambient CR ~1-4 (Malfunctioning Patrol/Probe Droid, Colicoid Experiment); boss CR 10 (Republic Trooper).")

                // Tier 2 — rogue droids and Republic security details gone dark (CR ~7-38 ambient).
                // Boss: Adamantine Guard Adept (CR 50), first rank of the CZ-220 Breaker Yard line.
                .Tier(2)
                .AddCreature("nar_rogue_droid", 30)
                .AddCreature("vrepnpctroop1", 70)
                .CreaturesPerRoom(2, 3)
                .Boss("cp_adamguard_ad")
                .Treasure("CAPSTONE_CZ220_BREAKER_YARD_LESSON_LOOT", 3)
                .LevelNote("Ambient CR ~7-38 (Rogue Droid/Republic Trooper/Republic Soldier); boss CR 50 (Adamantine Guard Adept).")

                // Tier 3 — CZ-220 Breaker Yard security elites (CR ~50-55 ambient).
                // Boss: Worldbreaker Master (CR 60), the facility's top-rank guard unit.
                .Tier(3)
                .AddCreature("cp_scraplock_sp", 30)
                .AddCreature("cp_worldbrk_ic", 40)
                .AddCreature("cp_adamguard_wd", 30)
                .CreaturesPerRoom(2, 4)
                .Boss("cp_worldbrk_ms")
                .Treasure("CAPSTONE_CZ220_BREAKER_YARD_BOSS_LOOT", 4)
                .LevelNote("Ambient CR ~50-55 (Scrapheap Lockdown Specialist/Worldbreaker Inner Circle/Adamantine Guard Warden); boss CR 60 (Worldbreaker Master).");

            return _builder.Build();
        }
    }
}
