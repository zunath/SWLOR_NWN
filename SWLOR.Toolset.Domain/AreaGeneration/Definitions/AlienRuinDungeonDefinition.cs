#nullable disable
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;

namespace SWLOR.Toolset.Domain.AreaGeneration.Definitions
{
    /// <summary>
    /// Alien Ruin dungeon theme on vmr01 (sw_t_alienruin): ancient Sith ruins guarded by temple
    /// guardians and dark side cultists. Tiers 1-3 reuse existing Bible-balanced Korriban Sith
    /// Crypt/Temple and Korriban Fortress dungeon-raid creatures and loot tables (Module/are/
    /// korr_crypt_zil.are.json) so no new NPC balance work is required for the content loop. Tile
    /// lighting (31, 27, 10, 12) is the most common combination sampled across korr_crypt_zil's
    /// Tile_List (114 of 256 tiles, 44.5%; the given 0/0/2/2 guess accounted for only 6 tiles).
    /// </summary>
    public class AlienRuinDungeonDefinition : IDungeonListDefinition
    {
        public const string ThemeKey = "alienruin";

        private readonly DungeonDefinitionBuilder _builder = new();

        public Dictionary<string, DungeonDetail> BuildDungeons()
        {
            _builder.Create(ThemeKey, "Alien Ruin")
                .TilesetProfile(StandardTilesetProfiles.AncientRuin)
                .LayoutProfile(StandardLayoutProfiles.Halls)
                .SizeRange(8, 32)
                .ExitPlaceable("zep_portal001", "Ancient Portal")
                .ExitDoor("_mdrn_dt_stneint")
                .TreasurePlaceable("korrduntemple", "Ancient Reliquary")

                // Decoration: the bulk of the visual dressing now lives on the AncientRuin tileset
                // profile (its own vmr01 evidence — see StandardTilesetProfiles.AncientRuin); only a
                // couple of genuinely Sith-flavored accents are curated here.
                .DecorationDensity(0.3)
                .Decoration("zep_altarevil1", 1, DecorationContext.RoomCenter)
                .Decoration("zep_monument001", 1, DecorationContext.DoorwayFlank)

                // Tier 1 — dark side cultists haunting the outer crypt (CR ~10-15 ambient).
                // Boss: Sith Temple Guard (CR 83).
                .Tier(1)
                .AddCreature("korriinitiate", 40)
                .AddCreature("s_app", 30)
                .AddCreature("s_app_m", 30)
                .CreaturesPerRoom(1, 2)
                .Boss("vkorrdun1sword")
                .Treasure("KORRIBAN_SITH_APPRENTICE", 2)
                .LevelNote("Ambient CR ~10-15 (Rogue Initiate/Possessed Apprentice); boss CR 83 (Sith Temple Guard).")

                // Tier 2 — Sith fortress marauders and sorceresses (CR ~86-95 ambient).
                // Boss: Temple Guardian (CR 129).
                .Tier(2)
                .AddCreature("vkorrdun1rifle", 30)
                .AddCreature("vkorrdunmarauder", 40)
                .AddCreature("vkorrdunsorc", 30)
                .CreaturesPerRoom(2, 3)
                .Boss("vkorrdungate")
                .Treasure("KORRIBAN_FORTRESS_GEAR", 3)
                .LevelNote("Ambient CR ~86-95 (Sith Temple Guard/Sith Marauder/Sith Sorceress); boss CR 129 (Temple Guardian).")

                // Tier 3 — Imperial war machines and Sith inquisitors deep in the ruin
                // (CR ~114-196 ambient). Boss: Temple Council Guardian (CR 312).
                .Tier(3)
                .AddCreature("vkorrdundroidhvy", 30)
                .AddCreature("vkorrduninquis", 40)
                .AddCreature("vkorrdunwarform", 30)
                .CreaturesPerRoom(2, 4)
                .Boss("vkorrduncouncilg")
                .Treasure("KORRIBAN_FORTRESS_RESOURCES", 4)
                .LevelNote("Ambient CR ~114-196 (Imperial Heavy Industry Drone/Sith Inquisitor/Imperial Prototype Warform); boss CR 312 (Temple Council Guardian).");

            return _builder.Build();
        }
    }
}
