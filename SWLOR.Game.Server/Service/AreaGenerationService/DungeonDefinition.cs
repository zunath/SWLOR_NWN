using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.AreaGenerationService
{
    /// <summary>
    /// A single weighted creature choice within a dungeon tier's ambient spawn pool.
    /// </summary>
    public class DungeonCreatureEntry
    {
        public string Resref { get; set; } = string.Empty;
        public int Weight { get; set; } = 1;
    }

    /// <summary>
    /// Placement context a curated decoration entry is eligible for, chosen from the evidence mined
    /// against hand-built reference areas (see scratchpad decoration_evidence/ mining notes referenced
    /// in the DungeonDecorationPlanner doc comment): most hand-built decorative placeables hug a room's
    /// perimeter (streetlights, planters, crates, wall clutter), a minority sit as room centerpieces,
    /// long/narrow "corridor-like" rooms get a lighter lining, and small clutter clusters near doorways.
    /// </summary>
    public enum DecorationContext
    {
        /// <summary>Hugs a room's perimeter, offset toward the wall and facing back into the room.</summary>
        WallAdjacent = 0,
        /// <summary>A rare centerpiece placed near (never on) a large room's CenterTile.</summary>
        RoomCenter = 1,
        /// <summary>Lines the perimeter of a long/narrow "corridor-like" room.</summary>
        CorridorSide = 2,
        /// <summary>Small clutter near a transition's doorway.</summary>
        DoorwayFlank = 3
    }

    /// <summary>
    /// A single weighted decorative placeable choice for one <see cref="DecorationContext"/> bucket
    /// within a curated palette. See <see cref="DungeonDetail.Decorations"/> (theme accents) and
    /// <see cref="DungeonTilesetProfile.Decorations"/> (the tileset family's own bulk palette).
    /// </summary>
    public class DungeonDecorationEntry
    {
        public string Resref { get; set; } = string.Empty;
        public int Weight { get; set; } = 1;
        public DecorationContext Context { get; set; } = DecorationContext.WallAdjacent;
    }

    /// <summary>
    /// One placeable within a <see cref="DungeonVignette"/>: a resref plus its offset (in world units,
    /// pre-rotation) from the vignette's anchor tile and an additional facing offset (degrees) applied
    /// on top of the anchor's own "face into the room" facing. Mined from hand-built co-occurrence
    /// evidence (nearest-neighbor placeable pairs/triples within ~3-5m — see decoration_evidence/
    /// mine_evidence.py's pairwise clustering pass) — e.g. a bench+lamp or table+chairs grouping.
    /// </summary>
    public class DungeonVignetteMember
    {
        public string Resref { get; set; } = string.Empty;
        public float OffsetX { get; set; }
        public float OffsetY { get; set; }
        public float FacingOffset { get; set; }
    }

    /// <summary>
    /// A small, evidence-backed multi-placeable grouping (e.g. crate stack, bench+lamp) placed as a
    /// single unit by <see cref="DungeonDecorationPlanner"/> rather than each member rolling
    /// independently — see <see cref="DungeonTilesetProfile.Vignettes"/>.
    /// </summary>
    public class DungeonVignette
    {
        public string Key { get; set; } = string.Empty;
        public int Weight { get; set; } = 1;
        public List<DungeonVignetteMember> Members { get; set; } = new();
    }

    /// <summary>
    /// Tier-specific content for one difficulty tier of a dungeon theme: ambient spawn pool,
    /// per-room spawn counts, boss, and treasure. Consumed by DungeonContentPlacer.Populate.
    /// </summary>
    public class DungeonTierDetail
    {
        public int Tier { get; set; }
        public List<DungeonCreatureEntry> Creatures { get; set; } = new();
        public int MinCreaturesPerRoom { get; set; } = 1;
        public int MaxCreaturesPerRoom { get; set; } = 2;
        public string BossResref { get; set; } = string.Empty;
        public string TreasureLootTableId { get; set; } = string.Empty;
        public int TreasureItemCount { get; set; } = 1;
        /// <summary>Free-text balance note for programmer/DM reference. Not used by the system.</summary>
        public string LevelNote { get; set; } = string.Empty;
    }

    /// <summary>
    /// Per-tile lighting written into every generated tile of a theme. Values are tile light
    /// color indices (see TILE_MAIN_LIGHT_COLOR_* / TILE_SOURCE_LIGHT_COLOR_*); defaults match
    /// the hand-built tdt01 cave areas.
    /// </summary>
    public class DungeonTileLighting
    {
        public int MainLight1 { get; set; }
        public int MainLight2 { get; set; }
        public int SourceLight1 { get; set; } = 8;
        public int SourceLight2 { get; set; } = 8;
    }

    /// <summary>
    /// Everything that is genuinely bound to a tileset: the tileset itself, the placeholder
    /// area cloned for instances, tile lighting, and what the tileset calls its accent terrain
    /// (water pools, pit channels). Layout shapes and content packages compose with any profile.
    /// </summary>
    public class DungeonTilesetProfile
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string TilesetResref { get; set; } = string.Empty;
        public string PlaceholderResref { get; set; } = string.Empty;
        public DungeonTileLighting Lighting { get; set; } = new();

        /// <summary>
        /// Terrain name this tileset uses for accent patches (e.g. "Water" on tdt01, "Pit" on
        /// tds01). Empty = the tileset has no verified accent coverage; compositions skip accents.
        /// </summary>
        public string AccentTerrain { get; set; } = string.Empty;

        /// <summary>
        /// Terrain name this tileset uses for its accent CHANNEL/bank vocabulary (LayoutAccentChannelCarver)
        /// when it differs from the blob-patch AccentTerrain — e.g. vmr01's "Chasm", which has verified
        /// bank/span tile coverage against its primary open terrain (Plaza) but no verified blob-patch
        /// coverage, so AccentTerrain stays empty while this is set. Empty = channels fall back to
        /// AccentTerrain (the original, single-terrain behavior).
        /// </summary>
        public string ChannelTerrain { get; set; } = string.Empty;

        /// <summary>
        /// Narrowest corridor/door-gap width (in corners) this tileset can path through. Some
        /// tilesets (zsf01) give every partially-open corner combo a movement-restricted pathnode,
        /// so 1-wide openings fail the engine's path check; 2-wide openings put a fully-open
        /// (pathnode A) tile in the middle. Compositions raise CorridorWidth to at least this.
        /// </summary>
        public int MinimumOpeningWidth { get; set; } = 1;

        /// <summary>
        /// Terrain used for the SOLID (wall/impassable) mass. Empty = the tileset's declared Default
        /// terrain, which is correct for every interior tileset (their GENERAL Default IS the wall).
        /// EXTERIOR tilesets invert this: ttd01/ttf01/ttf02 declare Default==Floor=="Desert"/"Forest"
        /// (the WALKABLE ground -- their fully-open Desert/Forest tiles are pathnode A) while the
        /// impassable enclosure terrain is "Cliff" (whose fully-Cliff tile is pathnode-restricted), so
        /// composing with the default solid would carve unwalkable rock "rooms" out of a walkable
        /// "wall" mass. Declaring SolidTerrainOverride("Cliff") + PrimaryOpenTerrain("Desert") gives
        /// real dungeon-style enclosure: cliff-walled canyons with walkable clearings. Consumed by
        /// DungeonComposition.BuildLayoutParameters; LayoutSolver.Solve/AreaGeneration keep stamping
        /// the tileset Default whenever the composed parameters carry no explicit solid.
        /// </summary>
        public string SolidTerrainOverride { get; set; } = string.Empty;

        /// <summary>
        /// Terrain used for open/walkable space. Empty = the tileset's declared Floor terrain.
        /// Some tilesets keep their richest room vocabulary on a different terrain: zsf01's declared
        /// floor has a single fully-open tile while its 'floor' terrain carries the hand-built room
        /// variants, and vmr01's 'Plaza' has 11 fully-open variants vs 4 on 'Floor'.
        /// </summary>
        public string PrimaryOpenTerrain { get; set; } = string.Empty;

        /// <summary>
        /// Optional second open-terrain label this tileset offers for multi-terrain districts (see
        /// MacroLayoutParameters.SecondaryOpenTerrain), e.g. zsf01's "Floor2" alongside its
        /// PrimaryOpenTerrain "floor", or vmr01's "Floor" alongside "Plaza". Empty = no districts for
        /// this tileset. Only takes effect when the composed layout profile uses RoomsAndCorridors in
        /// Tunnel mode; only set this after verifying full (secondary, solid) corner coverage AND
        /// Doorway-junction tiles for the secondary terrain (see MultiTerrainDistrictTests).
        /// </summary>
        public string SecondaryOpenTerrain { get; set; } = string.Empty;

        /// <summary>
        /// 1x1 "group" tiles (treasure mounds, pillars, hot springs, ...) this tileset offers as rare
        /// decorative sprinkles into open room space, keyed by the .set [GROUPn] Name with a relative
        /// weight. Empty = no feature tiles configured for this tileset. TileResolver re-verifies each
        /// name's structural eligibility (1x1, flat, doorless, crosser-free, pathnode A) at resolve
        /// time and silently drops any that fail rather than trusting this list blindly.
        /// </summary>
        public Dictionary<string, int> FeatureTiles { get; set; } = new();

        /// <summary>
        /// Tileset "group" set pieces (wall-bounded rooms hanging off Tunnel corridors, or floor-level
        /// decorative pieces dropped into open room interiors) this tileset offers, keyed by the .set
        /// [GROUPn] Name with a max-instances-per-area count. Empty = no set pieces configured for
        /// this tileset. LayoutGroupStamper re-verifies each name's structural eligibility (shape,
        /// corners, crossers) at stamp time rather than trusting this list blindly.
        /// </summary>
        public Dictionary<string, int> SetPieces { get; set; } = new();

        /// <summary>
        /// Themed 1x1 "exit" group names (e.g. tdt01 Exit01-03) this tileset offers as a GroupExit
        /// substitution for Exit-kind transitions, in priority order tried by GroupExitPlanner.
        /// Empty = no group-exit substitution configured for this tileset (e.g. zsf01/Facility).
        /// GroupExitPlanner re-verifies each name's structural eligibility (1x1, flat, crosser-free,
        /// has a door slot) at resolve time rather than trusting this list blindly.
        /// </summary>
        public List<string> ExitGroups { get; set; } = new();

        /// <summary>
        /// Largest MacroLayoutParameters.ElevationRegions value this tileset's real tile inventory has
        /// verified rim vocabulary for (see LayoutElevationPainter.HasRimVocabulary and the census
        /// notes on BaseGameTilesetProfiles.Dungeon). 0 = no verified elevation vocabulary; a layout
        /// profile's own ElevationRegions request is clamped down to this by
        /// DungeonComposition.BuildLayoutParameters, the same "layout expresses intent, tileset caps to
        /// verified support" shape as AccentTerrain/ChannelTerrain vs AccentDensity/AccentChannels.
        /// LayoutElevationPainter re-verifies live against the real TilesetModel regardless -- this cap
        /// only controls how many regions a composition ASKS for, never whether painting one is safe.
        /// </summary>
        public int MaxElevationRegions { get; set; } = 0;

        /// <summary>
        /// Largest MacroLayoutParameters.PoolRegions value this tileset's real tile inventory has
        /// verified depth-pool vocabulary for (see LayoutElevationPoolPainter.HasPoolVocabulary). 0 = no
        /// verified pool vocabulary; a layout profile's own PoolRegions request is clamped down to this
        /// by DungeonComposition.BuildLayoutParameters, the same "layout expresses intent, tileset caps
        /// to verified support" shape as MaxElevationRegions. Pools use this profile's own AccentTerrain
        /// as the pool terrain (the same terrain LayoutAccentPainter/LayoutAccentChannelCarver already
        /// use for this tileset's secondary hazard/liquid terrain), so this only takes effect when
        /// AccentTerrain is also set.
        /// </summary>
        public int MaxPoolRegions { get; set; } = 0;

        /// <summary>
        /// Largest MacroLayoutParameters.ReliefRegions value this tileset's real tile inventory has
        /// verified per-corner relief vocabulary for (see LayoutReliefPainter's capability gate and
        /// the census notes on BaseGameTilesetProfiles.Dungeon/MinesAndCaverns). 0 = no verified
        /// relief vocabulary; a layout profile's own ReliefRegions request is clamped down to this by
        /// DungeonComposition.BuildLayoutParameters, the same "layout expresses intent, tileset caps
        /// to verified support" shape as MaxElevationRegions/MaxPoolRegions. LayoutReliefPainter
        /// re-verifies every individual perturbation live against the real TilesetModel regardless.
        /// </summary>
        public int MaxReliefRegions { get; set; } = 0;

        /// <summary>
        /// Optional "slope blend" terrain LayoutReliefPainter may flip individual open-terrain
        /// corners to while painting relief -- the terrain this tileset uses to render a gradual
        /// walkable slope between two floor grades (e.g. tdm01's GentleSlope against its [Cave]
        /// Floor, GentleDesert against Desert, GentleOrganic against Organic). Empty = no blend
        /// terrain (relief perturbs heights only). Only set after verifying full flat
        /// (open, blend) corner coverage among resolver-usable tiles -- the same verification bar as
        /// AccentTerrain -- since every blend flip's neighbors resolve from that flat vocabulary.
        /// </summary>
        public string ReliefBlendTerrain { get; set; } = string.Empty;

        /// <summary>
        /// Alternate ramp-lane edge-crosser name this tileset's raised-tile family carries instead of
        /// the canonical "Ramp" (e.g. tdm01's "Slope"). Empty = canonical "Ramp". Consumed by both
        /// LayoutElevationPainter.TryAddRampLane and LayoutReliefPainter's lane proposals; both
        /// re-verify every spliced lane live against the real TilesetModel regardless, so this only
        /// selects which name is tried, never whether a lane is safe.
        /// </summary>
        public string RampCrosser { get; set; } = string.Empty;

        /// <summary>
        /// Alternate Tunnel-mode body crosser this tileset's district/palette carves instead of the
        /// canonical "Corridor" (e.g. tdc01's "[Grey]" district uses "GreyCorridor") -- mechanically
        /// identical vocabulary, just a different string the tileset's own art was authored under.
        /// Empty = no alternate body vocabulary; the composed layout keeps using canonical Corridor/
        /// Doorway. Only takes effect when paired with TunnelPortCrosser (both or neither) and when the
        /// composed layout profile requests Corridor-type Tunnel mode (see
        /// DungeonComposition.BuildLayoutParameters, which switches CorridorCrosserType to Custom).
        /// Only set after verifying the full body/port SHAPE inventory with TunnelVocabularyCheck.
        /// SupportsTunnels (Custom overload), not merely that both crosser names appear somewhere in the
        /// tileset's declared vocabulary.
        /// </summary>
        public string TunnelBodyCrosser { get; set; } = string.Empty;

        /// <summary>See <see cref="TunnelBodyCrosser"/>. May equal the canonical "Doorway" (several
        /// districts rename only their body crosser and keep door transitions on canonical "Doorway"),
        /// or a district-specific name (e.g. a district that renames both, like tdc01's "[Dwarven]"
        /// district's "DwarvenDoorway" -- verify independently, since renaming the port is a materially
        /// different shape probe than a body-only rename).</summary>
        public string TunnelPortCrosser { get; set; } = string.Empty;

        /// <summary>
        /// Crosser names (beyond the canonical "Doorway"/"Bridge" pair) this tileset's real tile
        /// inventory uses for a door-implying crosser under a completely different name -- e.g.
        /// Barrows/tbw01's "door_corridor", paired with its own "corridor" Tunnel body crosser (see
        /// TunnelBodyCrosser/TunnelPortCrosser above) rather than the canonical Corridor/Doorway pair.
        /// Declaring a name here is what lets TileResolver register a door-slot tile carrying that
        /// crosser as an ordinary structural candidate (see TileResolver's class doc comment) --
        /// without it, every such tile is excluded from candidate lookup entirely regardless of shape.
        /// Empty = no alternate door-slot vocabulary (every tileset except one that renames its
        /// door-implying crosser entirely). Distinct from TunnelPortCrosser: a tileset may need this
        /// declared even when its Tunnel body/port pair stays canonical, and TunnelPortCrosser itself
        /// is NOT automatically credited here -- declare it explicitly if it also carries door slots.
        /// </summary>
        public List<string> DoorSlotCrossers { get; set; } = new();

        /// <summary>
        /// Physical tile IDs this tileset's real .set data resolves to structurally (real, non-PADDING
        /// corners -- the resolver would legitimately place them), but whose MODEL is confirmed
        /// placeholder/stub art that renders wrong in-game (e.g. twc03's "xyz" family: 15 tile IDs
        /// whose models are literal ASCII stubs with an untextured -- bitmap NULL -- rendered trimesh
        /// node sitting on real geometry, verified directly by dumping the raw .mdl content; they
        /// render as flat white tiles in Fort Complex generations). Empty = no exclusions (default;
        /// every existing tileset profile). Consumed by DungeonComposition.BuildLayoutParameters,
        /// threaded through MacroLayoutParameters/MacroLayout, and enforced by TileResolver at the
        /// lowest shared candidate-lookup level so every placement path (legacy flat, height-aware,
        /// feature sprinkling) skips them uniformly. Does NOT affect LayoutGroupStamper's pinned-tile
        /// path (set pieces/exit groups bypass candidate lookup entirely) -- see
        /// ExcludedTileRegressionTests, which statically asserts none of these IDs are members of any
        /// SetPieces/ExitGroups group this profile wires, so that bypass never matters in practice.
        /// </summary>
        public HashSet<int> ExcludedTiles { get; set; } = new();

        /// <summary>
        /// True for a profile that recomposes an ALREADY-onboarded tileset resref against a different
        /// terrain/district palette (e.g. "crypt_grey" recomposing tdc01's Grey palette alongside the
        /// base "crypt" profile's Tan palette) rather than onboarding a new physical tileset. Palette
        /// variants exist purely to close tile-coverage census exemptions and offer the palette as a
        /// composable option -- they are deliberately excluded from SWLOR.ProcgenReview's --matrix
        /// full cross-product (tileset x layout) to keep the review module's area count from ballooning
        /// as more palettes are onboarded; each variant instead gets exactly one showcase area appended
        /// via --extra-areas. See TileCoverageCensusTests, which iterates every profile sharing a
        /// TilesetResref (variant or not) so a tile counts as reachable if ANY of them composes it.
        /// </summary>
        public bool IsPaletteVariant { get; set; } = false;

        /// <summary>
        /// This tileset FAMILY's own bulk "set dressing" palette, mined from its own hand-built
        /// reference areas (decoration_evidence/evidence_by_tileset.json,
        /// decoration_evidence/evidence_by_theme_keyword.json, decoration_evidence/
        /// evidence_named_exemplars.json) rather than from the theme composed onto it — decoration is a
        /// function of the VISUAL family (what the tileset's own art depicts), not the content theme.
        /// A palette-variant profile (<see cref="IsPaletteVariant"/>) with no entries of its own
        /// automatically inherits the palette of the first non-variant profile sharing its
        /// <see cref="TilesetResref"/> (see DungeonContentPlacer.GetEffectiveTilesetDecorations) — only
        /// declare entries here directly when that district's own evidence genuinely differs. Empty on
        /// a base (non-variant) profile = no mined evidence exists for this family yet; documented
        /// per-profile rather than silently guessing another family's dressing.
        /// </summary>
        public List<DungeonDecorationEntry> Decorations { get; set; } = new();

        /// <summary>
        /// Evidence-backed multi-placeable groupings (see <see cref="DungeonVignette"/>) this tileset
        /// family offers as a unit placement — vignette members are never sampled independently.
        /// Inherited the same way as <see cref="Decorations"/> for palette variants.
        /// </summary>
        public List<DungeonVignette> Vignettes { get; set; } = new();
    }

    /// <summary>
    /// A named layout shape: style plus tuning knobs, independent of any tileset. The template's
    /// AccentTerrain stays empty — a nonzero AccentDensity expresses intent, and the actual
    /// terrain name comes from the tileset profile at composition time.
    /// </summary>
    public class DungeonLayoutProfile
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public MacroLayoutParameters Template { get; set; } = new();
    }

    /// <summary>
    /// A content package plus its default composition: per-tier creatures/boss/treasure, exit and
    /// treasure placeables, and the default tileset/layout profile keys. Any tileset or layout
    /// profile can be substituted at request time — nothing here is tileset-bound.
    /// Definitions are discovered via reflection over IDungeonListDefinition (see DungeonContentPlacer),
    /// mirroring ISpawnListDefinition/ILootTableDefinition/IAbilityListDefinition in this codebase.
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
        /// Every tile door slot the generator uses is generic (Type=0), so any generic-door utd fits.
        /// </summary>
        public string ExitDoorResref { get; set; } = "_mdrn_dt_wood";

        /// <summary>Treasure container spawned in the Boss room. Must have HasInventory=1 and a real appearance.</summary>
        public string TreasurePlaceableResref { get; set; } = "structure_rubble";
        public string TreasureDisplayName { get; set; } = "Treasure Cache";

        /// <summary>
        /// Weighted "set dressing" placeable palette curated from hand-built reference areas of this
        /// theme's family (see decoration_evidence/ mining notes), grouped by <see cref="DecorationContext"/>.
        /// Empty = no decoration pass for this theme (DungeonDecorationPlanner.Plan returns nothing).
        /// </summary>
        public List<DungeonDecorationEntry> Decorations { get; set; } = new();

        /// <summary>
        /// Target decorative placeables PER TOTAL AREA TILE (layout.Width * layout.Height) at 100%
        /// request density — evidence-derived per theme from the mined decorative-placeable density of
        /// its hand-built reference areas (decoration_evidence/mine_evidence.py's own "placeables per
        /// tile (area Width*Height)" convention). DungeonDecorationPlanner.Plan converts this into a
        /// per-eligible-tile placement probability sized so the EXPECTED realized count converges on
        /// DecorationBaseDensity * totalTiles, not a literal per-eligible-tile coin-flip chance (the
        /// eligible pool — room perimeter cells only — is much smaller than the total area). Scaled by
        /// AreaGenerationRequest.DecorationDensityPercent (0-200, default 100).
        /// </summary>
        public double DecorationBaseDensity { get; set; } = 0.2;

        public Dictionary<int, DungeonTierDetail> Tiers { get; set; } = new();
    }

    /// <summary>
    /// A resolved (content, tileset, layout) triple ready to drive a generation request.
    /// </summary>
    public class DungeonComposition
    {
        public DungeonDetail Content { get; set; }
        public DungeonTilesetProfile Tileset { get; set; }
        public DungeonLayoutProfile Layout { get; set; }

        /// <summary>
        /// Clones the layout template and stamps the tileset's accent terrain name when the
        /// layout wants accents and the tileset supports them; otherwise accents are disabled.
        /// </summary>
        public MacroLayoutParameters BuildLayoutParameters()
        {
            var parameters = Layout.Template.Clone();
            // Exterior solid inversion (see DungeonTilesetProfile.SolidTerrainOverride): stamp the
            // profile's declared solid so LayoutSolver.Solve/AreaGeneration keep it instead of
            // defaulting to the tileset's GENERAL Default terrain. Empty for every interior profile --
            // Solve's own empty-means-Default stamp is unchanged there.
            parameters.SolidTerrain = Tileset.SolidTerrainOverride ?? string.Empty;
            parameters.AccentTerrain =
                parameters.AccentDensity > 0 && !string.IsNullOrEmpty(Tileset.AccentTerrain)
                    ? Tileset.AccentTerrain
                    : string.Empty;
            if (parameters.AccentTerrain.Length == 0)
                parameters.AccentDensity = 0;
            // Channels have their own terrain slot (ChannelTerrain) because some tilesets have
            // verified channel/bank coverage against a terrain with no verified blob-patch coverage
            // (vmr01's Chasm) — fall back to AccentTerrain when the tileset never set it separately,
            // preserving the original single-terrain behavior for every other tileset.
            var channelSource = !string.IsNullOrEmpty(Tileset.ChannelTerrain) ? Tileset.ChannelTerrain : Tileset.AccentTerrain;
            parameters.ChannelTerrain =
                parameters.AccentChannels > 0 && !string.IsNullOrEmpty(channelSource)
                    ? channelSource
                    : string.Empty;
            if (parameters.ChannelTerrain.Length == 0)
                parameters.AccentChannels = 0;
            parameters.CorridorWidth = Math.Max(parameters.CorridorWidth, Tileset.MinimumOpeningWidth);
            // Tunnel body/port crosser vocabulary: a tileset profile may declare an alternate crosser
            // family (e.g. tdc01's GreyCorridor body paired with the canonical Doorway port) that is
            // mechanically identical to the canonical Corridor/Doorway family LayoutTunnelCarver
            // defaults to, just under different names. Only takes effect for Tunnel-mode Corridor-type
            // layouts -- an Alley-mode (Streets-style) layout profile keeps its own vmr01-verified
            // vocabulary untouched -- and only when the tileset actually declared both halves of the
            // pair; MacroLayoutGenerator still re-probes the real shape inventory before dispatch
            // (see its Custom-mode downgrade), the same "tileset declares intent, generator re-verifies"
            // shape as every other tileset-declared capability here.
            if (parameters.CorridorCrosserType == CorridorCrosserType.Corridor &&
                !string.IsNullOrEmpty(Tileset.TunnelBodyCrosser) && !string.IsNullOrEmpty(Tileset.TunnelPortCrosser))
            {
                parameters.CorridorCrosserType = CorridorCrosserType.Custom;
                parameters.TunnelBodyCrosser = Tileset.TunnelBodyCrosser;
                parameters.TunnelPortCrosser = Tileset.TunnelPortCrosser;
            }
            // Unconditional pass-through: RoomsAndCorridorsLayout itself gates all district behavior
            // (and every extra RNG draw) behind CorridorMode == Tunnel, so stamping this even for a
            // layout profile that never uses Tunnel mode is inert.
            parameters.SecondaryOpenTerrain = Tileset.SecondaryOpenTerrain ?? string.Empty;
            // Shared reference is fine: FeatureTiles/SetPieces are never mutated after a tileset
            // profile is built, only read by the resolver/stamper.
            parameters.FeatureTiles = Tileset.FeatureTiles;
            parameters.SetPieces = Tileset.SetPieces;
            parameters.ExitGroups = Tileset.ExitGroups;
            parameters.DoorSlotCrossers = Tileset.DoorSlotCrossers;
            parameters.ExcludedTiles = Tileset.ExcludedTiles;
            // Layout expresses intent (e.g. StandardLayoutProfiles.Complex's ElevationRegions), the
            // tileset profile caps it to verified support -- 0 on every profile except
            // BaseGameTilesetProfiles.Dungeon means this is a no-op everywhere else today.
            parameters.ElevationRegions = Math.Min(parameters.ElevationRegions, Tileset.MaxElevationRegions);
            // Depth pools reuse the tileset's own blob-patch AccentTerrain as the pool terrain (e.g.
            // tde01's "Lava") -- never enabled without one, and clamped to the tileset's own verified
            // pool-vocabulary cap, mirroring ElevationRegions/MaxElevationRegions immediately above.
            parameters.PoolTerrain =
                parameters.PoolRegions > 0 && !string.IsNullOrEmpty(Tileset.AccentTerrain)
                    ? Tileset.AccentTerrain
                    : string.Empty;
            parameters.PoolRegions = parameters.PoolTerrain.Length == 0
                ? 0
                : Math.Min(parameters.PoolRegions, Tileset.MaxPoolRegions);
            // Per-corner relief mirrors the elevation/pool clamp shape exactly; the blend terrain and
            // ramp-crosser name are pure tileset vocabulary (stamped unconditionally -- both are inert
            // whenever the passes that read them are inactive or the names never resolve).
            parameters.ReliefRegions = Math.Min(parameters.ReliefRegions, Tileset.MaxReliefRegions);
            parameters.ReliefBlendTerrain = Tileset.ReliefBlendTerrain ?? string.Empty;
            parameters.RampCrosser = Tileset.RampCrosser ?? string.Empty;
            // A pool's own room-scoped rim+interior+rim footprint (LayoutElevationPoolPainter's
            // MinOuterSpan, 3 tiles) needs a room at least MinOuterSpan+2 tiles wide/tall on the
            // placement axis (the mechanism's own 1-corner-inset room-boundary margin on top of that).
            // Empirically, a layout profile's nominal MaxRoomCornerSize ceiling (e.g. Complex's 5) is
            // rarely actually realized once RoomsAndCorridorsLayout's own placement-attempt "degrade"
            // and overlap rejection are in play, so floor generously (+2 tiles of headroom) rather than
            // to the bare minimum -- mirroring CorridorWidth's own floor against
            // Tileset.MinimumOpeningWidth immediately above. A no-op whenever PoolRegions ended up 0.
            if (parameters.PoolRegions > 0)
            {
                var floor = Layouts.LayoutElevationPoolPainter.MinOuterSpan + 4;
                parameters.MaxRoomCornerSize = Math.Max(parameters.MaxRoomCornerSize, floor);
                parameters.MinRoomCornerSize = Math.Min(parameters.MinRoomCornerSize, parameters.MaxRoomCornerSize);
            }
            return parameters;
        }
    }

    /// <summary>
    /// Fluent builder for dungeon theme definitions, matching the SpawnTableBuilder/LootTableBuilder/
    /// PropertyLayoutBuilder convention: Create() starts a new active entry, chained calls mutate it,
    /// Build() returns the completed dictionary.
    /// </summary>
    public class DungeonDefinitionBuilder
    {
        private readonly Dictionary<string, DungeonDetail> _dungeons = new();
        private DungeonDetail _activeDungeon;
        private DungeonTierDetail _activeTier;

        /// <summary>
        /// Creates a new dungeon theme definition with the specified theme key.
        /// </summary>
        /// <param name="themeKey">Unique theme key, e.g. "minecave".</param>
        /// <param name="displayName">Human-readable name for programmer/DM reference.</param>
        public DungeonDefinitionBuilder Create(string themeKey, string displayName)
        {
            _activeDungeon = new DungeonDetail
            {
                ThemeKey = themeKey,
                DisplayName = displayName
            };
            _dungeons[themeKey] = _activeDungeon;
            _activeTier = null;

            return this;
        }

        /// <summary>
        /// Sets the default tileset profile this content package composes with. Overridable per request.
        /// </summary>
        public DungeonDefinitionBuilder TilesetProfile(string tilesetProfileKey)
        {
            _activeDungeon.TilesetProfileKey = tilesetProfileKey;
            return this;
        }

        /// <summary>
        /// Sets the default layout profile this content package composes with. Overridable per request.
        /// </summary>
        public DungeonDefinitionBuilder LayoutProfile(string layoutProfileKey)
        {
            _activeDungeon.LayoutProfileKey = layoutProfileKey;
            return this;
        }

        /// <summary>
        /// Sets the allowed width/height range for generated instances of this theme.
        /// </summary>
        public DungeonDefinitionBuilder SizeRange(int minSize, int maxSize)
        {
            _activeDungeon.MinSize = minSize;
            _activeDungeon.MaxSize = maxSize;
            return this;
        }

        /// <summary>
        /// Sets the exit placeable spawned in the Entrance room and its display name.
        /// </summary>
        public DungeonDefinitionBuilder ExitPlaceable(string resref, string displayName)
        {
            _activeDungeon.ExitPlaceableResref = resref;
            _activeDungeon.ExitDisplayName = displayName;
            return this;
        }

        /// <summary>
        /// Sets the door blueprint used when a transition is realized as a real tileset door.
        /// Must be a generic-door utd (tile door slots are Type=0 generic).
        /// </summary>
        public DungeonDefinitionBuilder ExitDoor(string resref)
        {
            _activeDungeon.ExitDoorResref = resref;
            return this;
        }

        /// <summary>
        /// Sets the treasure container spawned in the Boss room and its display name.
        /// </summary>
        public DungeonDefinitionBuilder TreasurePlaceable(string resref, string displayName)
        {
            _activeDungeon.TreasurePlaceableResref = resref;
            _activeDungeon.TreasureDisplayName = displayName;
            return this;
        }

        /// <summary>
        /// Adds a weighted decorative placeable to the theme's curated palette for one placement
        /// context. Call repeatedly to build out each <see cref="DecorationContext"/> bucket.
        /// </summary>
        public DungeonDefinitionBuilder Decoration(string resref, int weight, DecorationContext context)
        {
            _activeDungeon.Decorations.Add(new DungeonDecorationEntry
            {
                Resref = resref,
                Weight = weight,
                Context = context
            });

            return this;
        }

        /// <summary>
        /// Sets the theme's base decoration density (target placeables per total area tile at 100%
        /// request density). See <see cref="DungeonDetail.DecorationBaseDensity"/>.
        /// </summary>
        public DungeonDefinitionBuilder DecorationDensity(double baseDensity)
        {
            _activeDungeon.DecorationBaseDensity = baseDensity;
            return this;
        }

        /// <summary>
        /// Starts a new tier definition. Tiers must be declared in contiguous order starting at 1
        /// (enforced by DungeonDefinitionTests, not at runtime).
        /// </summary>
        public DungeonDefinitionBuilder Tier(int tier)
        {
            _activeTier = new DungeonTierDetail
            {
                Tier = tier
            };
            _activeDungeon.Tiers[tier] = _activeTier;

            return this;
        }

        /// <summary>
        /// Adds a weighted creature choice to the active tier's ambient spawn pool.
        /// </summary>
        public DungeonDefinitionBuilder AddCreature(string resref, int weight = 10)
        {
            _activeTier.Creatures.Add(new DungeonCreatureEntry
            {
                Resref = resref,
                Weight = weight
            });

            return this;
        }

        /// <summary>
        /// Sets the min/max number of ambient creatures spawned per Standard room for the active tier.
        /// </summary>
        public DungeonDefinitionBuilder CreaturesPerRoom(int min, int max)
        {
            _activeTier.MinCreaturesPerRoom = min;
            _activeTier.MaxCreaturesPerRoom = max;
            return this;
        }

        /// <summary>
        /// Sets the boss creature resref spawned once in the Boss room for the active tier.
        /// </summary>
        public DungeonDefinitionBuilder Boss(string bossResref)
        {
            _activeTier.BossResref = bossResref;
            return this;
        }

        /// <summary>
        /// Sets the loot table and item count used to fill the Boss room's treasure container for the active tier.
        /// </summary>
        public DungeonDefinitionBuilder Treasure(string lootTableId, int itemCount)
        {
            _activeTier.TreasureLootTableId = lootTableId;
            _activeTier.TreasureItemCount = itemCount;
            return this;
        }

        /// <summary>
        /// Attaches a free-text balance note to the active tier for programmer/DM reference.
        /// </summary>
        public DungeonDefinitionBuilder LevelNote(string note)
        {
            _activeTier.LevelNote = note;
            return this;
        }

        /// <summary>
        /// Builds a dictionary of dungeon theme definitions, keyed by theme key.
        /// </summary>
        public Dictionary<string, DungeonDetail> Build()
        {
            return _dungeons;
        }
    }

    /// <summary>Fluent builder for tileset profiles, same conventions as DungeonDefinitionBuilder.</summary>
    public class DungeonTilesetProfileBuilder
    {
        private readonly Dictionary<string, DungeonTilesetProfile> _profiles = new();
        private DungeonTilesetProfile _active;

        public DungeonTilesetProfileBuilder Create(string key, string displayName)
        {
            _active = new DungeonTilesetProfile
            {
                Key = key,
                DisplayName = displayName
            };
            _profiles[key] = _active;
            return this;
        }

        public DungeonTilesetProfileBuilder Tileset(string tilesetResref)
        {
            _active.TilesetResref = tilesetResref;
            return this;
        }

        public DungeonTilesetProfileBuilder Placeholder(string placeholderResref)
        {
            _active.PlaceholderResref = placeholderResref;
            return this;
        }

        public DungeonTilesetProfileBuilder TileLighting(int mainLight1, int mainLight2, int sourceLight1, int sourceLight2)
        {
            _active.Lighting = new DungeonTileLighting
            {
                MainLight1 = mainLight1,
                MainLight2 = mainLight2,
                SourceLight1 = sourceLight1,
                SourceLight2 = sourceLight2
            };
            return this;
        }

        /// <summary>
        /// Declares the terrain name this tileset uses for accent patches. Only set this after
        /// verifying full (open, accent) corner coverage among resolver-usable tiles.
        /// </summary>
        public DungeonTilesetProfileBuilder AccentTerrain(string terrainName)
        {
            _active.AccentTerrain = terrainName;
            return this;
        }

        /// <summary>
        /// Declares a separate terrain for accent CHANNEL/bank coverage when it differs from
        /// AccentTerrain (the blob-patch terrain). Only set this after verifying channel/bank tile
        /// coverage against the current PrimaryOpenTerrain (see LayoutAccentChannelCarver).
        /// </summary>
        public DungeonTilesetProfileBuilder ChannelTerrain(string terrainName)
        {
            _active.ChannelTerrain = terrainName;
            return this;
        }

        /// <summary>
        /// Declares the narrowest opening width (in corners) this tileset can path through. Set to
        /// 2 for tilesets whose partially-open corner combos all carry movement-restricted pathnodes.
        /// </summary>
        public DungeonTilesetProfileBuilder MinimumOpeningWidth(int width)
        {
            _active.MinimumOpeningWidth = width;
            return this;
        }

        /// <summary>
        /// Declares the largest MacroLayoutParameters.ElevationRegions request this tileset's real tile
        /// inventory has verified rim vocabulary for. Only set after verifying with
        /// LayoutElevationPainter's shape probe (TileResolver.HasHeightAwareCandidate) against the
        /// composed PrimaryOpenTerrain/solid terrain -- see BaseGameTilesetProfiles.Dungeon.
        /// </summary>
        public DungeonTilesetProfileBuilder MaxElevationRegions(int count)
        {
            _active.MaxElevationRegions = count;
            return this;
        }

        /// <summary>
        /// Declares the largest MacroLayoutParameters.PoolRegions request this tileset's real tile
        /// inventory has verified depth-pool vocabulary for. Only set after verifying with
        /// LayoutElevationPoolPainter's shape probe (TileResolver.HasHeightAwareCandidate) against the
        /// composed PrimaryOpenTerrain/AccentTerrain pairing -- see BaseGameTilesetProfiles.Dungeon.
        /// </summary>
        public DungeonTilesetProfileBuilder MaxPoolRegions(int count)
        {
            _active.MaxPoolRegions = count;
            return this;
        }

        /// <summary>
        /// Overrides the terrain used for open space. Only set after verifying full (open, solid)
        /// corner coverage for that terrain among resolver-usable tiles.
        /// </summary>
        /// <summary>
        /// Overrides the SOLID (wall) terrain for tilesets whose GENERAL Default is actually the
        /// walkable ground (the exterior inversion) -- see DungeonTilesetProfile.SolidTerrainOverride.
        /// Only set after verifying full 16/16 simple-tile coverage of PrimaryOpenTerrain against this
        /// solid AND that the fully-open terrain tile is pathnode A.
        /// </summary>
        public DungeonTilesetProfileBuilder SolidTerrainOverride(string terrainName)
        {
            _active.SolidTerrainOverride = terrainName;
            return this;
        }

        public DungeonTilesetProfileBuilder PrimaryOpenTerrain(string terrainName)
        {
            _active.PrimaryOpenTerrain = terrainName;
            return this;
        }

        /// <summary>
        /// Declares a second open terrain this tileset offers for multi-terrain districts. Only set
        /// after verifying full (secondary, solid) corner coverage AND Doorway-junction tiles for the
        /// secondary terrain among resolver-usable tiles (see MultiTerrainDistrictTests).
        /// </summary>
        public DungeonTilesetProfileBuilder SecondaryOpenTerrain(string terrainName)
        {
            _active.SecondaryOpenTerrain = terrainName;
            return this;
        }

        /// <summary>
        /// Adds a rare decorative "group" tile (treasure mound, pillar, hot spring, ...) this
        /// tileset can sprinkle into open room space, with a relative weight (default 1; e.g.
        /// treasure mounds are commonly weighted 2). TileResolver re-verifies the named group's
        /// structural eligibility at resolve time rather than trusting this call blindly.
        /// </summary>
        public DungeonTilesetProfileBuilder FeatureTile(string groupName, int weight = 1)
        {
            _active.FeatureTiles[groupName] = weight;
            return this;
        }

        /// <summary>
        /// Adds a tileset "group" set piece (a WallRoom hanging off a Tunnel corridor, or an
        /// OpenSetPiece dropped into open room space) with a max-instances-per-area count (default 1).
        /// LayoutGroupStamper re-verifies the named group's structural eligibility at stamp time
        /// rather than trusting this call blindly.
        /// </summary>
        public DungeonTilesetProfileBuilder SetPiece(string groupName, int maxPerArea = 1)
        {
            _active.SetPieces[groupName] = maxPerArea;
            return this;
        }

        /// <summary>
        /// Adds a themed 1x1 "exit" group (e.g. tdt01 Exit01-03) this tileset offers as a GroupExit
        /// substitution for Exit-kind transitions. Call order is priority order: GroupExitPlanner
        /// tries each configured name in the order added here. GroupExitPlanner re-verifies the named
        /// group's structural eligibility at resolve time rather than trusting this call blindly.
        /// </summary>
        public DungeonTilesetProfileBuilder ExitGroup(string groupName)
        {
            _active.ExitGroups.Add(groupName);
            return this;
        }

        /// <summary>
        /// Declares the largest MacroLayoutParameters.ReliefRegions request this tileset's real tile
        /// inventory has verified per-corner relief vocabulary for. Only set after verifying with
        /// LayoutReliefPainter's capability probes (TileResolver.HasHeightAwareCandidate for a lone
        /// raised open corner, or a flat open/blend flip) -- see BaseGameTilesetProfiles.Dungeon.
        /// </summary>
        public DungeonTilesetProfileBuilder MaxReliefRegions(int count)
        {
            _active.MaxReliefRegions = count;
            return this;
        }

        /// <summary>
        /// Declares the "slope blend" terrain LayoutReliefPainter may flip individual open corners to
        /// -- see DungeonTilesetProfile.ReliefBlendTerrain. Only set after verifying full flat
        /// (open, blend) corner coverage among resolver-usable tiles.
        /// </summary>
        public DungeonTilesetProfileBuilder ReliefBlendTerrain(string terrainName)
        {
            _active.ReliefBlendTerrain = terrainName;
            return this;
        }

        /// <summary>
        /// Declares the alternate ramp-lane edge-crosser name this tileset's raised-tile family
        /// carries instead of the canonical "Ramp" (e.g. tdm01's "Slope") -- see
        /// DungeonTilesetProfile.RampCrosser.
        /// </summary>
        public DungeonTilesetProfileBuilder RampCrosser(string crosserName)
        {
            _active.RampCrosser = crosserName;
            return this;
        }

        /// <summary>
        /// Declares an alternate Tunnel-mode body/port crosser pair this tileset's district/palette
        /// carves instead of the canonical Corridor/Doorway names -- see
        /// DungeonTilesetProfile.TunnelBodyCrosser. Only call this after verifying the full shape
        /// inventory with TunnelVocabularyCheck.SupportsTunnels(..., CorridorCrosserType.Custom, body,
        /// port), not merely that both names appear in the tileset's declared crosser list.
        /// </summary>
        public DungeonTilesetProfileBuilder TunnelCrossers(string bodyCrosser, string portCrosser)
        {
            _active.TunnelBodyCrosser = bodyCrosser;
            _active.TunnelPortCrosser = portCrosser;
            return this;
        }

        /// <summary>
        /// Declares one or more crosser names (beyond the canonical "Doorway"/"Bridge" pair) this
        /// tileset's real tile inventory uses for a door-implying crosser -- see
        /// DungeonTilesetProfile.DoorSlotCrossers. Only call this after confirming (via a direct
        /// TileResolver.HasCandidate/TunnelVocabularyCheck.SupportsTunnels probe passing the same names)
        /// that declaring it actually closes real tile-coverage gaps, not merely that the crosser name
        /// appears in the tileset's declared vocabulary.
        /// </summary>
        public DungeonTilesetProfileBuilder DoorSlotCrossers(params string[] crossers)
        {
            _active.DoorSlotCrossers.AddRange(crossers);
            return this;
        }

        /// <summary>
        /// Declares one or more physical tile IDs this profile must never place, regardless of how
        /// structurally valid the corner/edge/group data looks -- see
        /// DungeonTilesetProfile.ExcludedTiles for when to use this (confirmed placeholder/stub art,
        /// not a structural gap). Only call this after confirming the model itself is broken (dump
        /// the raw .mdl and verify a rendered mesh node has no real texture) -- TileResolver still
        /// trusts every OTHER tile's shape data blindly, so this is a deliberate art-only override,
        /// not a structural re-verification.
        /// </summary>
        public DungeonTilesetProfileBuilder ExcludedTiles(params int[] tileIds)
        {
            foreach (var tileId in tileIds)
                _active.ExcludedTiles.Add(tileId);
            return this;
        }

        /// <summary>
        /// Marks the active profile as a palette/district variant of an already-onboarded tileset
        /// resref (same .set file, different terrain composition) -- see
        /// DungeonTilesetProfile.IsPaletteVariant.
        /// </summary>
        public DungeonTilesetProfileBuilder PaletteVariant()
        {
            _active.IsPaletteVariant = true;
            return this;
        }

        /// <summary>
        /// Adds a weighted decorative placeable to this tileset FAMILY's own bulk palette for one
        /// placement context — see <see cref="DungeonTilesetProfile.Decorations"/>. This is where the
        /// bulk of a generated area's visual dressing should live; theme definitions should only add a
        /// small handful of their own genuinely theme-flavored accents.
        /// </summary>
        public DungeonTilesetProfileBuilder Decoration(string resref, int weight, DecorationContext context)
        {
            _active.Decorations.Add(new DungeonDecorationEntry
            {
                Resref = resref,
                Weight = weight,
                Context = context
            });

            return this;
        }

        private DungeonVignette _activeVignette;

        /// <summary>
        /// Starts a new evidence-backed vignette grouping (see <see cref="DungeonVignette"/>) on this
        /// tileset profile. Follow with one or more <see cref="VignetteMember"/> calls.
        /// </summary>
        public DungeonTilesetProfileBuilder Vignette(string key, int weight = 1)
        {
            _activeVignette = new DungeonVignette { Key = key, Weight = weight };
            _active.Vignettes.Add(_activeVignette);
            return this;
        }

        /// <summary>
        /// Adds one placeable to the active vignette. Offsets are world units relative to the
        /// vignette's anchor tile, BEFORE the anchor's own wall-facing rotation is applied (see
        /// DungeonDecorationPlanner.PlaceVignette) — author offsets as if the anchor faces "north"
        /// (+Y) into the room.
        /// </summary>
        public DungeonTilesetProfileBuilder VignetteMember(string resref, float offsetX, float offsetY, float facingOffset = 0f)
        {
            _activeVignette.Members.Add(new DungeonVignetteMember
            {
                Resref = resref,
                OffsetX = offsetX,
                OffsetY = offsetY,
                FacingOffset = facingOffset
            });
            return this;
        }

        public Dictionary<string, DungeonTilesetProfile> Build()
        {
            return _profiles;
        }
    }

    /// <summary>
    /// One-time post-processing step for a fully-discovered tileset-profile dictionary: a palette
    /// variant (<see cref="DungeonTilesetProfile.IsPaletteVariant"/>) that declared no
    /// Decorations/Vignettes of its own inherits them in place from the first non-variant profile
    /// registered under the same <see cref="DungeonTilesetProfile.TilesetResref"/> — see the
    /// Decorations doc comment. Every consumer that discovers tileset profiles (runtime
    /// DungeonContentPlacer.Bootstrap, SWLOR.ProcgenReview, SWLOR.ContentBuilder's DefinitionCatalog)
    /// calls this exactly once right after discovery, so an ordinary `tileset.Decorations`/
    /// `tileset.Vignettes` read anywhere else in the codebase already reflects the effective palette —
    /// no call site needs its own inheritance-lookup logic.
    /// </summary>
    public static class DungeonTilesetPaletteInheritance
    {
        public static void Apply(Dictionary<string, DungeonTilesetProfile> profiles)
        {
            foreach (var profile in profiles.Values)
            {
                if (!profile.IsPaletteVariant || profile.Decorations.Count > 0 || profile.Vignettes.Count > 0)
                    continue;

                var basis = profiles.Values.FirstOrDefault(p =>
                    !p.IsPaletteVariant && p.TilesetResref == profile.TilesetResref &&
                    (p.Decorations.Count > 0 || p.Vignettes.Count > 0));

                if (basis == null)
                    continue;

                profile.Decorations = basis.Decorations;
                profile.Vignettes = basis.Vignettes;
            }
        }
    }

    /// <summary>Fluent builder for layout profiles, same conventions as DungeonDefinitionBuilder.</summary>
    public class DungeonLayoutProfileBuilder
    {
        private readonly Dictionary<string, DungeonLayoutProfile> _profiles = new();
        private DungeonLayoutProfile _active;

        public DungeonLayoutProfileBuilder Create(string key, string displayName)
        {
            _active = new DungeonLayoutProfile
            {
                Key = key,
                DisplayName = displayName
            };
            _profiles[key] = _active;
            return this;
        }

        /// <summary>
        /// Configures the layout style and tuning knobs. Width/height/terrain labels are stamped
        /// per-request; leave AccentTerrain empty — set AccentDensity to express accent intent and
        /// the composed tileset profile supplies the terrain name.
        /// </summary>
        public DungeonLayoutProfileBuilder Configure(Action<MacroLayoutParameters> configure)
        {
            configure(_active.Template);
            return this;
        }

        public Dictionary<string, DungeonLayoutProfile> Build()
        {
            return _profiles;
        }
    }
}
