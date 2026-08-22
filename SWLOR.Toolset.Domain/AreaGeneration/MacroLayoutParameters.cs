#nullable disable
using System;
using System.Collections.Generic;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    public class MacroLayoutParameters
    {
        public int Width { get; set; } = 16;
        public int Height { get; set; } = 16;
        /// <summary>Terrain label for solid/unwalkable space (typically TilesetModel.DefaultTerrain).</summary>
        public string SolidTerrain { get; set; } = string.Empty;
        /// <summary>Terrain label for open/walkable space (typically TilesetModel.FloorTerrain).</summary>
        public string OpenTerrain { get; set; } = string.Empty;

        /// <summary>
        /// Optional second open-terrain label for multi-terrain districts: some rooms are carved as
        /// walled districts of this terrain instead of OpenTerrain, connected via Tunnel-mode
        /// corridors (see RoomsAndCorridorsLayout, LayoutTunnelCarver). Empty = no districts (default;
        /// fully back-compat, zero extra RNG draws). v1 scope: honored only when Style is
        /// RoomsAndCorridors and CorridorMode is Tunnel — OpenLane corridors carve straight bands from
        /// room center to room center and would repaint a secondary room's interior back to
        /// OpenTerrain, so this field is silently ignored outside Tunnel mode. Callers must verify the
        /// tileset has full (secondary, solid) corner coverage AND Doorway-junction tiles for the
        /// secondary terrain before enabling (see MultiTerrainDistrictTests).
        /// </summary>
        public string SecondaryOpenTerrain { get; set; } = string.Empty;

        /// <summary>
        /// Fraction of non-entrance rooms carved from SecondaryOpenTerrain instead of OpenTerrain when
        /// districts are active (room 0 — the first room a style places — always stays OpenTerrain, so
        /// the entrance is biased toward the primary district). Ignored when SecondaryOpenTerrain is
        /// empty. Default 0.35 mirrors AccentDensity's "roughly this share" convention.
        /// </summary>
        public double SecondaryRoomFraction { get; set; } = 0.35;

        /// <summary>
        /// When true, margin cells that front an open room cell get their remaining solid corners
        /// repainted to the fronted open terrain -- a paved PLATFORM APRON one cell deep around
        /// the walkable grid, so structural frontage buildings erected on those margins stand on
        /// real platform surface (see LayoutPlatformApronPainter and FrontageSupportRule). Mined
        /// hand-built evidence: fcx01 city areas are platform-dominant (median "holes" chasm
        /// corner share ~0.17, several flagship areas 0.00) with towers standing ON the paving and
        /// the drop beyond them, while ungated generated chasm-margin layouts ran 0.72 -- deep
        /// towers had nothing to stand on. Set by DungeonComposition.BuildLayoutParameters for
        /// tilesets declaring BOTH frontage buildings and chasm terrains; default false leaves
        /// every other composition's corner plan untouched.
        /// </summary>
        public bool PlatformApron { get; set; }

        public DungeonLayoutStyle Style { get; set; } = DungeonLayoutStyle.RoomsAndCorridors;

        public int MinRooms { get; set; } = 4;
        public int MaxRooms { get; set; } = 8;

        /// <summary>
        /// When true, MacroLayoutGenerator.Generate scales MinRooms/MaxRooms up with area (above the
        /// 20x20 room-count tuning baseline) via
        /// LayoutParameterConstraints.ApplySetPieceRoomSupplyScaling, so set-piece-heavy tilesets
        /// (fcx01 city districts) keep a proportional supply of stampable rooms at larger sizes
        /// instead of the styles' flat hardcoded counts. Default false (fully back-compat: no clone,
        /// no RNG change, no derivation for any composition that never declares it) -- stamped from
        /// DungeonTilesetProfile.SetPieceRoomSupplyScaling by DungeonComposition.BuildLayoutParameters,
        /// the same "tileset declares physical need, generator applies it" shape as
        /// SetPieceRoomCornerFloor.
        /// </summary>
        public bool SetPieceRoomSupplyScaling { get; set; }

        /// <summary>
        /// When true, LayoutGroupStamper's OpenSetPiece placement may stamp a building group adjacent
        /// to an already-stamped OpenSetPiece footprint (shared edges, seam-verified) so buildings
        /// assemble into contiguous blocks walling the street network instead of standing isolated --
        /// see DungeonTilesetProfile.BuildingBlockContiguity for the full rule set and hand-built
        /// evidence. Default false (fully back-compat, zero extra RNG draws for any composition that
        /// never declares it) -- stamped from the tileset profile by
        /// DungeonComposition.BuildLayoutParameters.
        /// </summary>
        public bool BuildingBlockContiguity { get; set; }

        /// <summary>
        /// When true, LayoutRoadCarver routes every street lane and connector spur as a SHORTEST,
        /// then FEWEST-TURNS path (straight avenues with single L-corners -- the hand-built city
        /// street shape) instead of the legacy first-found breadth-first shortest path, whose
        /// expansion order produced diagonal staircase zigzags across open plazas. Path LENGTHS
        /// are identical either way (turn count is a secondary cost), so road-share bands are
        /// unaffected; only the lane geometry changes. Default false (fully back-compat: every
        /// composition that never declares it keeps its exact legacy lane geometry) -- stamped
        /// from DungeonTilesetProfile.StraightStreetRouting by
        /// DungeonComposition.BuildLayoutParameters.
        /// </summary>
        public bool StraightStreetRouting { get; set; }
        /// <summary>Room rectangle bounds in corners (RoomsAndCorridors/Warren chambers/PackedRooms leaves).</summary>
        public int MinRoomCornerSize { get; set; } = 3;
        public int MaxRoomCornerSize { get; set; } = 7;

        /// <summary>Corridor width in corners. 1 = narrow tunnels, 2 = broad halls. OpenLane mode only.</summary>
        public int CorridorWidth { get; set; } = 1;

        /// <summary>
        /// How corridors are realized. OpenLane carves open-terrain corner bands (original behavior).
        /// Tunnel keeps the corners solid and lays Corridor edge crossers along the cell path instead,
        /// resolving to wall-embedded tunnel tiles with Doorway junctions where a tunnel meets open
        /// space — the way hand-built facility interiors are assembled. Tunnel passages are always
        /// exactly one tile wide and traverse via crosser edges, so CorridorWidth and pathnode-driven
        /// minimum opening widths do not apply to them.
        /// </summary>
        public CorridorMode CorridorMode { get; set; } = CorridorMode.OpenLane;

        /// <summary>
        /// Crosser vocabulary Tunnel-mode corridors carve. See <see cref="CorridorCrosserType"/>.
        /// Ignored unless CorridorMode is Tunnel; the default Corridor value is fully back-compat.
        /// </summary>
        public CorridorCrosserType CorridorCrosserType { get; set; } = CorridorCrosserType.Corridor;

        /// <summary>
        /// Tunnel body/port crosser strings used when <see cref="CorridorCrosserType"/> is Custom
        /// (ignored otherwise). Usually stamped from DungeonTilesetProfile.TunnelBodyCrosser/
        /// TunnelPortCrosser by DungeonComposition.BuildLayoutParameters -- see that type's own doc
        /// comment for the "layout expresses intent, tileset profile supplies the vocabulary" shape
        /// this mirrors (the same pattern as AccentTerrain/ChannelTerrain vs AccentDensity/
        /// AccentChannels). LayoutTunnelCarver/TunnelVocabularyCheck read these instead of the literal
        /// "Corridor"/"Doorway" constants when CorridorCrosserType is Custom.
        /// </summary>
        public string TunnelBodyCrosser { get; set; } = string.Empty;

        /// <summary>See <see cref="TunnelBodyCrosser"/>.</summary>
        public string TunnelPortCrosser { get; set; } = string.Empty;

        /// <summary>
        /// Fraction of additional connections carved beyond the spanning tree (0 = tree only).
        /// Loops make layouts feel like real areas instead of dead-end branches.
        /// </summary>
        public double LoopFactor { get; set; } = 0.25;

        /// <summary>OrganicCave: target fraction of interior corners that end up open.</summary>
        public double OpenFillTarget { get; set; } = 0.45;
        /// <summary>OrganicCave: cellular-automata smoothing passes.</summary>
        public int SmoothingPasses { get; set; } = 4;

        /// <summary>
        /// Optional third terrain painted as patches strictly inside open space (e.g. Water pools
        /// in caves, Pit channels in sewers). Empty = none. Callers must verify the tileset covers
        /// all (open, accent) corner combinations before enabling (see TileResolver coverage).
        /// </summary>
        public string AccentTerrain { get; set; } = string.Empty;
        /// <summary>Fraction of open corners converted to accent patches (0..~0.2).</summary>
        public double AccentDensity { get; set; } = 0.0;

        /// <summary>
        /// Number of linear accent-terrain channels (one-cell-wide bands, e.g. a Water/Pit "river")
        /// carved through open space, each crossed by exactly one real Bridge edge-crosser chain.
        /// 0 = none (default; back-compat). Requires ChannelTerrain (or, when that's empty,
        /// AccentTerrain) to be set and the tileset to carry Bridge-edge vocabulary — callers must
        /// verify coverage before enabling (see LayoutAccentChannelCarver).
        /// </summary>
        public int AccentChannels { get; set; } = 0;

        /// <summary>
        /// Effective terrain LayoutAccentChannelCarver paints channel bands/banks in. Usually stamped
        /// from DungeonTilesetProfile.ChannelTerrain (falling back to AccentTerrain) by
        /// DungeonComposition.BuildLayoutParameters. Separate from AccentTerrain because a tileset can
        /// have verified channel/bank coverage against a terrain with no verified blob-patch coverage
        /// (e.g. vmr01's Chasm against Plaza).
        /// </summary>
        public string ChannelTerrain { get; set; } = string.Empty;

        /// <summary>
        /// Number of linear Fence edge-crosser lines carved through open room interiors (e.g. a
        /// fenced-off maintenance yard divider in tds01 Sewers, or a courtyard partition in vmr01).
        /// Unlike AccentChannels, a fence line never repaints corner terrain -- every corner on both
        /// sides stays this layout's own open terrain the whole time -- so it needs no TunnelLink, but
        /// that also means the shared corner-graph connectivity check can't see a Fence barrier either
        /// (both sides still read as plain open corners). LayoutFenceCarver instead runs its own
        /// cell-level tentative-commit/verify/revert check (mirroring LayoutAccentChannelCarver's own
        /// pattern) before keeping any run. 0 = none (default; back-compat). Requires the tileset to
        /// carry Fence-edge vocabulary for the current
        /// OpenTerrain -- LayoutFenceCarver probes TileResolver.HasCandidate at carve time and
        /// silently no-ops when absent (e.g. tdt01, zsf01), so this is safe to enable on any
        /// tileset/profile pairing without per-tileset configuration.
        /// </summary>
        public int FenceLines { get; set; } = 0;

        /// <summary>
        /// Number of street-style Road edge-crosser lanes LayoutRoadCarver connects between transition
        /// anchors and room centers, through open space, AFTER LayoutGroupStamper has already stamped
        /// set pieces -- so a lane naturally routes between/around stamped buildings rather than
        /// through them (see LayoutRoadCarver's own doc comment for why it runs last). Unlike
        /// AccentChannels/FenceLines, a nonzero default is set directly here rather than opted into per
        /// layout profile: a road lane is a general composition improvement (real hand-built city
        /// tilesets carve streets through every open plaza), not a narrative per-profile choice, and it
        /// is fully inert on every tileset that never declares DungeonTilesetProfile.RoadCrosser (see
        /// DungeonComposition.BuildLayoutParameters, which zeroes this out whenever the composed
        /// tileset has no verified road vocabulary) -- so this default only ever takes effect on a
        /// composition that opts in via the tileset side of the pairing. 0 disables road carving.
        /// </summary>
        public int RoadLanes { get; set; } = 6;

        /// <summary>
        /// Effective Road edge-crosser name LayoutRoadCarver carves. Stamped from
        /// DungeonTilesetProfile.RoadCrosser by DungeonComposition.BuildLayoutParameters; empty when
        /// the composed tileset never declared one (RoadLanes is zeroed alongside it in that case).
        /// </summary>
        public string RoadCrosser { get; set; } = string.Empty;

        /// <summary>Arrival points assigned to rooms (1..3). The first is the primary anchor.</summary>
        public int EntranceCount { get; set; } = 1;
        /// <summary>Outbound exit points assigned to rooms (1..3). Exit placeables spawn at each.</summary>
        public int ExitCount { get; set; } = 1;

        /// <summary>
        /// When true (default), TileDoorPlanner opportunistically substitutes a real tileset door
        /// (room-side doorway tile + solid-side terminator tile) for each transition instead of a
        /// placeable. Falls back to Placeable per-transition when the tileset has no usable flat,
        /// ungrouped door-slot tiles for that spot (e.g. zsf01, which has none at all).
        /// </summary>
        public bool DoorTransitions { get; set; } = true;

        /// <summary>
        /// Fraction of eligible open cells (see TileResolver) that receive a rare decorative feature
        /// tile (treasure mounds, pillars, hot springs, ...) instead of a normal open tile.
        /// </summary>
        public double FeatureDensity { get; set; } = 0.05;

        /// <summary>
        /// Feature group name -> relative weight, usually stamped from DungeonTilesetProfile.FeatureTiles
        /// by DungeonComposition.BuildLayoutParameters. Empty = no feature sprinkling. TileResolver
        /// re-verifies each name's structural eligibility rather than trusting this list blindly.
        /// </summary>
        public Dictionary<string, int> FeatureTiles { get; set; } = new();

        /// <summary>
        /// Set-piece group name -> max instances per area, usually stamped from
        /// DungeonTilesetProfile.SetPieces by DungeonComposition.BuildLayoutParameters. Empty = no
        /// set-piece stamping. LayoutGroupStamper re-verifies each name's structural eligibility
        /// (shape, corners, crossers) rather than trusting this list blindly.
        /// </summary>
        public Dictionary<string, int> SetPieces { get; set; } = new();

        /// <summary>
        /// Themed 1x1 "exit" group names (e.g. tdt01 Exit01-03) this tileset offers as a GroupExit
        /// substitution for Exit-kind transitions, in priority order — usually stamped from
        /// DungeonTilesetProfile.ExitGroups by DungeonComposition.BuildLayoutParameters. Empty = no
        /// group-exit substitution for this tileset. GroupExitPlanner re-verifies each name's
        /// structural eligibility (1x1, flat, crosser-free, has a door slot) rather than trusting
        /// this list blindly.
        /// </summary>
        public List<string> ExitGroups { get; set; } = new();

        /// <summary>
        /// Crosser names (beyond the canonical "Doorway"/"Bridge" pair) TileResolver.TryResolve treats
        /// as door-implying for its crosser+door-slot admission gate -- usually stamped from
        /// DungeonTilesetProfile.DoorSlotCrossers by DungeonComposition.BuildLayoutParameters. Empty =
        /// no alternate door-slot vocabulary (default; fully back-compat -- see TileResolver's class
        /// doc comment). Some onboarded tilesets rename their door-implying crosser entirely rather
        /// than merely renaming the Tunnel body half (e.g. Barrows/tbw01's "door_corridor" paired with
        /// its own "corridor" body crosser, see BaseGameTilesetProfiles.Barrows) -- declaring it here is
        /// what lets a door-slot tile carrying that crosser resolve as an ordinary structural tile the
        /// same way a canonical Doorway/Bridge door-slot tile always has.
        /// </summary>
        public List<string> DoorSlotCrossers { get; set; } = new();

        /// <summary>
        /// Physical tile IDs TileResolver must never place for this composition -- usually stamped
        /// from DungeonTilesetProfile.ExcludedTiles by DungeonComposition.BuildLayoutParameters. Empty
        /// = no exclusions (default; fully back-compat -- see DungeonTilesetProfile.ExcludedTiles for
        /// when a tileset profile declares this).
        /// </summary>
        public HashSet<int> ExcludedTiles { get; set; } = new();

        /// <summary>
        /// Number of raised-corner regions LayoutElevationPainter attempts to paint after fences are
        /// carved and before set pieces are stamped (see MacroLayoutGenerator.Generate). 0 = none
        /// (default; fully back-compat -- every existing caller keeps the flat-only legacy TileResolver
        /// pools/RNG sequence untouched, see CornerTerrainGrid.HasAnyHeight).
        ///
        /// Best-effort: LayoutElevationPainter shape-checks the composed tileset's real tile inventory
        /// (TileResolver.HasHeightAwareCandidate) before ever touching a corner, and silently paints
        /// fewer than requested (down to zero) when the tileset lacks rim vocabulary, no candidate
        /// region fits, or every candidate region conflicts with a transition anchor/crosser cell --
        /// there is no failure path, only "painted less than asked." Usually clamped down further by
        /// DungeonComposition.BuildLayoutParameters against DungeonTilesetProfile.MaxElevationRegions,
        /// the same "layout expresses intent, tileset profile caps to verified support" shape as
        /// AccentDensity/AccentChannels vs AccentTerrain/ChannelTerrain.
        /// </summary>
        public int ElevationRegions { get; set; } = 0;

        /// <summary>
        /// When true (default false; fully back-compat), LayoutElevationPainter additionally tries to
        /// splice a Ramp edge-crosser "lane" into one straight rim edge of each successfully-placed
        /// OpenTerrain split-level blob, connecting the raised patch back down to ground level via a
        /// real walkable ramp surface instead of a sheer step. Purely additive to an already-placed
        /// blob: only the shared EdgeCrosserGrid edges along the chosen rim run are rewritten (no
        /// corner/height/terrain change), verified live via TileResolver.HasHeightAwareCandidate before
        /// committing, and reverted with zero effect on the underlying blob when unsupported or when
        /// the blob's rim isn't at least 2 tiles long on some side (a 1-tile-long rim has no interior
        /// cell to carry the shared "Ramp" edge without touching a corner cell -- see
        /// LayoutElevationPainter.TryAddRampLane). Self-gated: never requires a tileset profile cap,
        /// mirroring LayoutFenceCarver's own probe-then-carve-or-noop convention.
        /// </summary>
        public bool ElevationRamps { get; set; } = false;

        /// <summary>
        /// Effective terrain LayoutElevationPoolPainter sinks pool interiors to (e.g.
        /// DungeonTilesetProfile.AccentTerrain's "Lava" on tde01). Empty = no depth pools (default;
        /// fully back-compat). Usually stamped from the tileset's own AccentTerrain by
        /// DungeonComposition.BuildLayoutParameters -- the same "layout expresses intent via a count,
        /// tileset profile supplies the terrain name" shape as AccentTerrain/ChannelTerrain vs
        /// AccentDensity/AccentChannels.
        /// </summary>
        public string PoolTerrain { get; set; } = string.Empty;

        /// <summary>
        /// Number of depth pools LayoutElevationPoolPainter attempts to paint strictly inside a room's
        /// own OpenTerrain interior: a small rectangle of PoolTerrain sunk one story below a raised
        /// Floor rim (reusing LayoutElevationPainter's own verified rectangle/rim machinery for the
        /// rim, then overwriting a smaller interior sub-rectangle with PoolTerrain at the original,
        /// unraised height). 0 = none (default; fully back-compat). Best-effort and shape-gated exactly
        /// like ElevationRegions -- silently paints fewer than requested when the tileset lacks
        /// verified pool-bank vocabulary or no candidate room fits.
        /// </summary>
        public int PoolRegions { get; set; } = 0;

        /// <summary>
        /// Number of room-scoped "terrain relief" passes LayoutReliefPainter runs after elevation
        /// blobs and depth pools are painted (see MacroLayoutGenerator.Generate): per-corner
        /// perturb-and-verify height painting that raises/lowers INDIVIDUAL corners (open or accent
        /// terrain alike) wherever the tileset's real inventory can still tile every touched cell --
        /// the mechanism that reaches per-corner-independent height content (same-terrain diagonal
        /// saddles, accent banks at mixed grades, raised accent corners) no uniform region-growth pass
        /// can produce. 0 = none (default; fully back-compat -- zero extra RNG draws). Best-effort and
        /// probe-gated exactly like ElevationRegions: every single perturbation is verified live via
        /// TileResolver's height-aware lookup and reverted when unsupported, so this is safe to
        /// request on any tileset (it silently does nothing where there is no relief vocabulary).
        /// Usually clamped by DungeonComposition.BuildLayoutParameters against
        /// DungeonTilesetProfile.MaxReliefRegions.
        /// </summary>
        public int ReliefRegions { get; set; } = 0;

        /// <summary>
        /// Optional "slope blend" terrain LayoutReliefPainter may flip individual open-terrain corners
        /// to (at either grade) while painting relief -- the terrain some tilesets use to render a
        /// gradual walkable slope between two floor heights instead of a sheer step (e.g. tdm01's
        /// GentleSlope/GentleDesert/GentleOrganic families). Empty = no blend terrain (default; relief
        /// perturbs heights only). Usually stamped from DungeonTilesetProfile.ReliefBlendTerrain by
        /// DungeonComposition.BuildLayoutParameters. Every flip is probe-verified and additionally
        /// guarded by a room-scoped open-corner connectivity check (a label flip removes the corner
        /// from the open graph, unlike a pure height change which never does).
        /// </summary>
        public string ReliefBlendTerrain { get; set; } = string.Empty;

        /// <summary>
        /// Crosser name ramp/slope lane splicing uses (LayoutElevationPainter.TryAddRampLane and
        /// LayoutReliefPainter's lane proposals) when it differs from the canonical "Ramp" (e.g.
        /// tdm01's "Slope" family). Empty = canonical "Ramp" (default; fully back-compat). Usually
        /// stamped from DungeonTilesetProfile.RampCrosser by DungeonComposition.BuildLayoutParameters.
        /// </summary>
        public string RampCrosser { get; set; } = string.Empty;

        public MacroLayoutParameters Clone()
        {
            // MemberwiseClone shares the FeatureTiles/SetPieces dictionary references with the
            // original rather than copying them. That's fine: callers only ever assign these from an
            // immutable tileset-profile dictionary and never mutate them post-construction.
            return (MacroLayoutParameters)MemberwiseClone();
        }
    }
}
