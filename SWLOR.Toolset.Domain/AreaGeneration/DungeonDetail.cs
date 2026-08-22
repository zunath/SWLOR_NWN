#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>
    /// A content package plus its default composition: per-tier creatures/boss/treasure, exit and
    /// treasure placeables, and the default tileset/layout profile keys. Any tileset or layout
    /// profile can be substituted at request time — nothing here is tileset-bound.
    /// <see cref="Authoring.DefinitionCatalog"/> discovers definitions via reflection over
    /// <see cref="Definitions.IDungeonListDefinition"/>.
    /// </summary>
    public class DungeonDetail
    {
        public string ThemeKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Default tileset profile key; overridable per request.</summary>
        public string TilesetProfileKey { get; set; } = string.Empty;
        /// <summary>Default layout profile key; overridable per request.</summary>
        public string LayoutProfileKey { get; set; } = string.Empty;

        public int MinSize { get; set; } = 8;
        public int MaxSize { get; set; } = 32;

        /// <summary>
        /// Exit placeable spawned in the Entrance room. Must be a useable, non-static blueprint
        /// with a real (non-blank) appearance row — several "door"/"portal" blueprints in the
        /// module are invisible objects or have blank appearance rows; verify in placeables.2da.
        /// </summary>
        public string ExitPlaceableResref { get; set; } = "_mdrn_placedoord";
        public string ExitDisplayName { get; set; } = "Exit";

        /// <summary>
        /// Door blueprint spawned for Door-style transitions (doorway tiles embedded in room walls).
        /// Type=0 SET slots keep this blueprint's generic door appearance. Positive slot types select
        /// the matching tileset-specific doortypes.2da row while retaining the blueprint's behavior.
        /// </summary>
        public string ExitDoorResref { get; set; } = "_mdrn_dt_wood";

        /// <summary>Treasure container spawned in the Boss room. Must have HasInventory=1 and a real appearance.</summary>
        public string TreasurePlaceableResref { get; set; } = "structure_rubble";
        public string TreasureDisplayName { get; set; } = "Treasure Cache";

        /// <summary>
        /// Weighted "set dressing" placeable palette curated from hand-built reference areas of this
        /// theme's family, grouped by <see cref="DecorationContext"/>.
        /// Empty = no decoration pass for this theme (DungeonDecorationPlanner.Plan returns nothing).
        /// </summary>
        public List<DungeonDecorationEntry> Decorations { get; set; } = new();

        /// <summary>
        /// Name of the composed tileset's decoration profile this theme requests (see
        /// <see cref="DungeonTilesetProfile.DecorationProfiles"/>) -- e.g. a ruin-flavored theme may
        /// request a city tileset's "ruined" destruction palette. Empty (the default) = the
        /// tileset's standard palette. <see cref="Authoring.LayoutKnobOverrides.DecorationProfile"/>
        /// wins over this declaration; a name the composed tileset never declared falls back to the
        /// standard palette.
        /// </summary>
        public string DecorationProfile { get; set; } = string.Empty;

        /// <summary>
        /// Name of the composed tileset's NAMED atmosphere this theme requests (see
        /// <see cref="DungeonTilesetProfile.AtmosphereProfiles"/>), mirroring
        /// <see cref="DecorationProfile"/> exactly: empty (the default) = the tileset's standard
        /// <see cref="DungeonTilesetProfile.Atmosphere"/>; a per-request override wins over this
        /// declaration; an undeclared name falls back to the standard atmosphere.
        /// </summary>
        public string AtmosphereProfile { get; set; } = string.Empty;

        /// <summary>
        /// Target decorative placeables PER TOTAL AREA TILE (layout.Width * layout.Height) at 100%
        /// request density — evidence-derived per theme from the decorative-placeable density of its
        /// hand-built reference areas. DungeonDecorationPlanner.Plan converts this into a
        /// per-eligible-tile placement probability sized so the EXPECTED realized count converges on
        /// DecorationBaseDensity * totalTiles, not a literal per-eligible-tile coin-flip chance (the
        /// eligible pool — room perimeter cells only — is much smaller than the total area). Scaled by
        /// <see cref="Authoring.LayoutKnobOverrides.DecorationDensityPercent"/> (0-200, default 100).
        /// </summary>
        public double DecorationBaseDensity { get; set; } = 0.2;

        public Dictionary<int, DungeonTierDetail> Tiers { get; set; } = new();
    }
}
