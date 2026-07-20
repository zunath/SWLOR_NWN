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
        DoorwayFlank = 3,
        /// <summary>
        /// The central anchor of a composed courtyard arrangement (see
        /// DungeonDecorationPlanner.PlanCourtyard): one item standing at the interior anchor tile of a
        /// large plaza-like room, ringed by <see cref="Courtyard"/> items. Mined from hand-built fcx01
        /// interior arrangements (items &gt;2 tiles from walls/roads cluster as a centerpiece -- floor
        /// decal, floor light, small structure -- with a 4-13-item ring at radius ~4-9m around it).
        /// Only placed as part of a courtyard; never used as generic scatter.
        /// </summary>
        CourtyardCenter = 4,
        /// <summary>
        /// A ring/surround member of a composed courtyard arrangement (light poles, containers,
        /// planters, kiosks standing around a <see cref="CourtyardCenter"/> item) -- see
        /// DungeonDecorationPlanner.PlanCourtyard. Only placed as part of a courtyard.
        /// </summary>
        Courtyard = 5,
        /// <summary>
        /// Stands within one tile of a stamped multi-tile structure (an OpenSetPiece building
        /// footprint pinned by LayoutGroupStamper): sign panels, holo billboards, barriers that read
        /// as attached to a building's frontage rather than free-standing street furniture. Falls
        /// back to nothing (NOT WallAdjacent) when a layout has no stamped structures, so entries
        /// curated here never free-stand in the open -- the reported "sign panel next to a knee-high
        /// divider" artifact.
        /// </summary>
        StructureAdjacent = 6,
        /// <summary>
        /// OUTPUT-ONLY context (never curate palette entries under it): a member of a composed
        /// clutter pile (see DungeonDecorationPlanner.PlanClutterPile) -- 3-8 junk items packed
        /// within a ~1.2-2.5m radius, drawn from the room's own junk motif over the palette's
        /// <see cref="DecorationRole.Clutter"/> entries. This is the dominant hand-built arrangement
        /// (75% of hand-built fcx01 decoratives sit within 3m of another decorative; all-NN median
        /// 1.6m) that independent per-tile scatter cannot produce.
        /// </summary>
        ClutterPile = 7,
        /// <summary>
        /// OUTPUT-ONLY context (never curate palette entries under it): a ground decal (dirt patch,
        /// floor stain, floor marking) layered UNDER a clutter pile -- see
        /// DungeonDecorationPlanner.PlanClutterPile. Decals never stand alone: hand-built areas use
        /// them exclusively as layering beneath junk arrangements, so the planner only ever emits a
        /// <see cref="DecorationRole.GroundDecal"/> entry through a pile (or as a courtyard center
        /// that gets clutter layered on top).
        /// </summary>
        GroundDecal = 8,
        /// <summary>
        /// OUTPUT-ONLY context (never curate palette entries under it): one member of a composed
        /// industrial cargo-yard row (see DungeonDecorationPlanner.PlanCargoYard) -- 2-3 copies of
        /// one <see cref="DecorationSize.Huge"/> building-scale model standing on consecutive wall
        /// tiles at shared bearing, in an <see cref="DistrictFlavor.Industrial"/>-flavor room. The
        /// ONLY mechanism that may place Huge art.
        /// </summary>
        CargoYard = 9,
        /// <summary>
        /// OUTPUT-ONLY context (never curate palette entries under it): the central item of a
        /// composed mid-room ensemble (see DungeonDecorationPlanner.PlanInteriorEnsemble /
        /// PlanZoneDressings) -- a civic monument garden's monument, a commercial plaza island's
        /// kiosk, a park lawn's tree. Always committed together with at least the ensemble's
        /// minimum satellite count, never free-standing.
        /// </summary>
        EnsembleCenter = 10,
        /// <summary>
        /// OUTPUT-ONLY context (never curate palette entries under it): a satellite member of a
        /// composed mid-room ensemble (benches/planters/lamps facing the
        /// <see cref="EnsembleCenter"/> item, or the facing surround of a dressed feature tile).
        /// </summary>
        EnsembleMember = 11,
        /// <summary>
        /// OUTPUT-ONLY context (never curate palette entries under it): one crate/cargo unit of a
        /// composed industrial DEPOT block (see DungeonDecorationPlanner.PlanDepotBlock) -- dense
        /// butt-jointed rows at near-model-width pitch with a shared bearing, mixed crate heights,
        /// and end-of-row satellite props. The hand-built shipyard/dock storage pattern
        /// (crate-family nearest-neighbor median under 1m, 93% within 2.2m, colinear runs of 4-12,
        /// cluster bearing dominant-share 0.81 -- _scratch_decor/mine_r9_interiors.py).
        /// </summary>
        DepotRow = 12,
        /// <summary>
        /// OUTPUT-ONLY context (never curate palette entries under it): one structural building
        /// placeable of a composed street-frontage line (see BuildingFrontagePlanner) -- a
        /// skyscraper/tower model standing on the non-walkable margin flush against an open cell's
        /// boundary, bearing = the face's outward normal, at ~10m pitch along the run. The
        /// hand-built promenade-family canyon mechanism: pw_ar_narpromena (12x12) walls its plaza
        /// with 30 swd_build* placeables on flat cobble (zero building tiles), build007 rows at
        /// 9.8-10.1m center pitch, 100% cardinal bearings -- _scratch_decor/r11_mine_buildings.py.
        /// Deliberately a separate channel from decoration clutter: DecorationAnchoring.Excluded
        /// still strips whole-building art from every scatter palette.
        /// </summary>
        BuildingFrontage = 13,
        /// <summary>
        /// OUTPUT-ONLY context (never curate palette entries under it): a wall-mounted sign/holo
        /// panel placed on a building FACE (a stamped structure tile's face or a
        /// <see cref="BuildingFrontage"/> placeable's face) at an evidence-derived height band,
        /// slightly proud of the face, bearing = the face's outward normal -- see
        /// BuildingFrontagePlanner.PlanFacadeMounts. Hand-built dense city areas carry 0.13-0.23
        /// of their decoratives above Z 0.5m, dominated by holo signage attached to building
        /// faces (sign-family median face distance ~0, Z medians 2.4-6.6m).
        /// </summary>
        FacadeMount = 14
    }

    /// <summary>
    /// Semantic role of a curated decoration entry, driving which ARRANGEMENT mechanisms may place
    /// it (see DungeonDecorationPlanner). Independent of <see cref="DecorationContext"/> (where a
    /// single placement may anchor): the role says what KIND of thing the art is, mined from how
    /// hand-built reference areas actually arrange that resref.
    /// </summary>
    public enum DecorationRole
    {
        /// <summary>Default: an ordinary fixture placed by the run/centerpiece/flank/courtyard
        /// mechanisms exactly as before roles existed.</summary>
        Fixture = 0,
        /// <summary>
        /// Pile-able junk (crates, containers, barrels, rubble, trash): eligible for the clutter-pile
        /// arrangement (see DungeonDecorationPlanner.PlanClutterPile) IN ADDITION to whatever context
        /// bucket the entry is curated under. A palette with no Clutter entries gets no pile pass at
        /// all (and an unchanged budget split / RNG stream).
        /// </summary>
        Clutter = 1,
        /// <summary>
        /// A flat ground decal (dirt patch, stain, floor marking). NEVER placed stand-alone by any
        /// run/centerpiece/flank mechanism -- only layered under a clutter pile, or as a courtyard
        /// center that receives clutter on top. Hand-built evidence: decals appear as layering under
        /// junk arrangements, not as lone patches in open plazas.
        /// </summary>
        GroundDecal = 2,
        /// <summary>
        /// A large narrative one-off (parked/crashed vehicle, monument, altar). Must read as
        /// anchored to something: only placed via StructureAdjacent/CorridorSide (road-side)
        /// buckets, doorway flanks, or as a curated vignette member -- the planner strips Landmark
        /// entries out of the RoomCenter and WallAdjacent buckets so one can never float alone in
        /// the middle of an open plaza.
        /// </summary>
        Landmark = 3
    }

    /// <summary>
    /// STRUCTURAL anchoring requirement of a curated decoration entry -- what kind of architecture
    /// (if any) the blueprint's own art demands before a placement of it can read as intentional.
    /// Mined per resref from hand-built reference usage (placement count, distance-to-nearest
    /// building-architecture distribution, same-family nearest-neighbor spacing) plus the blueprint
    /// model's measured footprint -- see the July 2026 fcx01 semantic-context pass
    /// (_scratch_decor/mine_r7_semantics.py): a fence DOOR standing alone in an open plaza (the
    /// reported "gate without a wall") is the artifact class this classification exists to prevent.
    /// Orthogonal to <see cref="DecorationRole"/> (what arrangement mechanisms may place it) and
    /// <see cref="DecorationContext"/> (which bucket it is curated under): anchoring says what the
    /// ART physically needs.
    /// </summary>
    public enum DecorationAnchoring
    {
        /// <summary>Default: the art legitimately stands on its own (crates, planters, lamps,
        /// kiosks, rubble) subject only to its curated context bucket's rules.</summary>
        FreeStanding = 0,

        /// <summary>
        /// Must sit flush against a real architecture face -- within
        /// DungeonDecorationPlanner.FlushWallGap of a stamped structure footprint's cardinal face,
        /// bearing = that face's outward normal -- or not place at all. Mined from entries whose
        /// hand-built placements have median building-architecture distance ~0 (cargo stacked
        /// against tower walls: flush fraction 0.55-0.90 in the fcx01 reference areas). Never
        /// eligible for clutter piles, courtyard rings, doorway flanks, or plain wall runs, all of
        /// which would strand the item away from the face it needs.
        /// </summary>
        WallFlush = 1,

        /// <summary>
        /// Only meaningful as one segment of a composed multi-segment run (fence lines and their
        /// gate pieces). NO run-composition mechanism exists in DungeonDecorationPlanner -- the
        /// hand-built evidence shows these are butt-jointed chains at model-width pitch (fence
        /// family nearest-neighbor median 6.58m against a 7.12m segment model), a sub-tile
        /// continuous placement contract the per-tile stamping model cannot honor (and the fence
        /// door model measures 11.87m, wider than a whole 10m tile) -- so the planner strips
        /// RunSegment entries from every palette outright: absence is better than a free-standing
        /// gate. Composed fencing belongs to TILE vocabulary instead (see tds01's LayoutFenceCarver
        /// + FenceDoor01/02 CorridorInsert set pieces). The classification is kept so a future
        /// curated entry cannot silently leak back in as scatter.
        /// </summary>
        RunSegment = 2,

        /// <summary>
        /// Never placed by the generator under any mechanism: blueprints that are whole
        /// architecture fragments (swd_build007's model measures 10.92x10.92m -- an entire
        /// building), or that have no hand-built usage evidence in any curated family. Stripped
        /// from every palette at merge time as a hard guard.
        /// </summary>
        Excluded = 3
    }

    /// <summary>
    /// District flavor of one open room in an urban-grammar area (see
    /// DungeonDecorationPlanner.AssignDistrictFlavors): hand-built city repetition is
    /// DISTRICT-SCOPED, not globally uniform -- big cargo concentrates in industrial yards
    /// (swd_conta004 mined 61x in the pw_ar_nsshipyard shipyard and ZERO in the commercial
    /// promenades), while promenades use kiosks/benches/signage and plazas use pillars/monuments.
    /// Rooms are assigned a flavor deterministically (no RNG) from road frontage, stamped-structure
    /// adjacency, entrance distance, and interior depth; palette entries then opt into flavors via
    /// <see cref="DungeonDecorationEntry.DistrictWeights"/>. None = no district system (every
    /// non-urban tileset).
    /// </summary>
    public enum DistrictFlavor
    {
        /// <summary>No district assignment (non-urban tilesets; entries use their base Weight).</summary>
        None = 0,
        /// <summary>Cargo yards, docks, machinery: big containers, tanks, pipes, dumpsters,
        /// work lighting. The ONLY flavor whose zones may host <see cref="DecorationSize.Huge"/>
        /// building-scale placements.</summary>
        Industrial = 1,
        /// <summary>Market/promenade frontage: kiosk rows, benches, holo signage, market goods,
        /// street lamps. Prefers road-frontage rooms.</summary>
        Commercial = 2,
        /// <summary>Civic plazas: pillars/colonnades, holo monuments, clean floors, courtyards.
        /// Prefers rooms with a real interior (courtyard anchor).</summary>
        Civic = 3
    }

    /// <summary>
    /// How a 1x1 area-marking FEATURE TILE (see <see cref="DungeonTilesetProfile.FeatureTiles"/>)
    /// obligates set dressing when it lands inside an open room under a tileset's urban placement
    /// grammar -- the "empty zone decal" rule: a large area-marking tile IMPLIES content (a grass
    /// patch implies a park; a fountain court implies seating), so placing one obligates a composed
    /// ensemble instead of a bare marker (the reported "a park with no park" artifact -- a bare
    /// green lawn tile standing in a civic plaza). Declared per feature group name via
    /// DungeonTilesetProfileBuilder.FeatureTile; consumed by
    /// DungeonDecorationPlanner.PlanZoneDressings. Feature tiles whose own art already fills the
    /// cell (trees, water pools, treasure mounds, pillars) declare nothing and stay untouched.
    /// </summary>
    public enum FeatureZoneDressing
    {
        /// <summary>No dressing obligation (default; every pre-existing feature tile).</summary>
        None = 0,
        /// <summary>
        /// A flat, empty area marker (grass lawn, bare court): obligates a FULL ensemble ON the
        /// tile -- a centerpiece (tree/monument) plus a facing satellite ring (benches, lights).
        /// </summary>
        Lawn = 1,
        /// <summary>
        /// The tile's own art occupies the cell center (a fountain): obligates only a facing
        /// satellite surround at the tile margin -- no centerpiece item.
        /// </summary>
        Centerpiece = 2
    }

    /// <summary>
    /// Approximate physical size class of a decoration entry's ART, measured from the blueprint
    /// model's XY bounding box (decompiled .mdl verts -- see the round-8 size audit in
    /// _scratch_decor/r8_model_sizes.json): Small &lt; 1.2m, Medium 1.2-3m, Large 3-8m, Huge &gt;= 8m
    /// footprint. Drives size-aware repetition control under the urban grammar
    /// (DungeonDecorationPlanner): Huge entries place ONLY via composed industrial-yard rows;
    /// Large entries cap their per-row repeats so 6m containers never wall up back-to-back.
    /// Medium is the enum default (0) so every pre-existing palette entry keeps its behavior
    /// without a declaration.
    /// </summary>
    public enum DecorationSize
    {
        /// <summary>1.2-3m footprint (default -- ordinary fixtures/furniture).</summary>
        Medium = 0,
        /// <summary>Under 1.2m footprint (crates, trash cans, small lamps).</summary>
        Small = 1,
        /// <summary>3-8m footprint (shipping containers, vehicles, kiosks): per-row repeat caps
        /// and same-model spacing apply under the urban grammar.</summary>
        Large = 2,
        /// <summary>8m+ footprint (storage silos, industrial towers, parked starfighters):
        /// building-scale. Placed ONLY as composed industrial-yard rows/pairs with shared bearing,
        /// never by the generic run/pile/courtyard mechanisms, and never outside industrial-flavor
        /// zones.</summary>
        Huge = 3
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
        /// <summary>Semantic role driving arrangement eligibility -- see <see cref="DecorationRole"/>.</summary>
        public DecorationRole Role { get; set; } = DecorationRole.Fixture;

        /// <summary>Structural anchoring requirement of the blueprint's art -- see
        /// <see cref="DecorationAnchoring"/>.</summary>
        public DecorationAnchoring Anchoring { get; set; } = DecorationAnchoring.FreeStanding;

        /// <summary>
        /// True only for street-furniture entries that legitimately stand ON a carved road lane
        /// (streetlight/lamp-family fixtures -- hand-built fcx01 streets carry their lamps and light
        /// strips on the road surface itself). Under a tileset's urban placement grammar (see
        /// <see cref="DungeonTilesetProfile.UrbanDressing"/>) every OTHER placement keeps the road
        /// ribbon clear: a tile whose own edges carry the road crosser only ever hosts entries with
        /// this flag; everything else sets back to the adjacent road-margin tiles, facing the street.
        /// Inert (never read) for tilesets without the urban grammar.
        /// </summary>
        public bool AllowOnRoadSurface { get; set; }

        /// <summary>
        /// Physical size class of the blueprint's art -- see <see cref="DecorationSize"/>. Only
        /// read under the urban placement grammar (size-aware repetition control); the Medium
        /// default keeps every non-declaring entry's behavior unchanged.
        /// </summary>
        public DecorationSize Size { get; set; } = DecorationSize.Medium;

        /// <summary>
        /// District affinity of this entry (see <see cref="DistrictFlavor"/>), evidence-derived
        /// from which hand-built area TYPE uses the resref: an EMPTY map means the entry serves
        /// every district at its base <see cref="Weight"/> (and is the only state non-urban
        /// tilesets ever use); a non-empty map means the entry's effective weight in a room of
        /// flavor F is DistrictWeights[F], and the entry is EXCLUDED from rooms whose flavor is
        /// absent from the map. Inert for tilesets without the urban grammar.
        /// </summary>
        public Dictionary<DistrictFlavor, int> DistrictWeights { get; set; } = new();

        /// <summary>
        /// Hard per-area placement cap for this resref across every arrangement mechanism
        /// (0 = uncapped), derived from the hand-built per-area p95 within the entry's district --
        /// the size-aware repetition-control backstop that keeps one fixture from blanketing a
        /// whole generated area. Only enforced under the urban grammar.
        /// </summary>
        public int MaxPerArea { get; set; }
    }

    /// <summary>
    /// One weighted structural building placeable the frontage system (BuildingFrontagePlanner) may
    /// erect along open-area perimeter edges and street margins to form canyon walls -- the
    /// hand-built promenade-family mechanism (skyscraper placeables standing on the margin, flush
    /// lines at ~10m pitch). DELIBERATELY separate from <see cref="DungeonDecorationEntry"/>:
    /// whole-building art is structure, not dressing (DecorationAnchoring.Excluded strips it from
    /// every scatter palette), and placement is geometric (footprint fit against the walkable grid)
    /// rather than palette-bucket driven. Footprints are measured model XY extents
    /// (_scratch_decor/r11_model_sizes.json): FaceWidth spans along the fronted face, Depth extends
    /// into the non-walkable margin.
    /// </summary>
    public class BuildingFrontageEntry
    {
        public string Resref { get; set; } = string.Empty;
        public int Weight { get; set; } = 1;
        /// <summary>Model footprint extent (meters) along the fronted face.</summary>
        public float FaceWidth { get; set; } = 10f;
        /// <summary>Model footprint extent (meters) into the margin, perpendicular to the face.</summary>
        public float Depth { get; set; } = 10f;
        /// <summary>Hard per-area placement cap (0 = uncapped). Derived from the hand-built
        /// per-area counts: the dominant wall models stay uncapped, accents cap near their
        /// hand-built maxima so no single accent tower can blanket an area.</summary>
        public int MaxPerArea { get; set; }
    }

    /// <summary>
    /// One weighted wall-mounted sign/holo/banner placeable the facade-mount pass
    /// (BuildingFrontagePlanner.PlanFacadeMounts) may hang on a building face -- stamped structure
    /// tile faces and <see cref="BuildingFrontageEntry"/> placeable faces -- at an evidence-derived
    /// height band. Mined from the dense hand-built fcx01 city areas' elevated (Z &gt; 0.5m)
    /// decoratives: sign-family items sit ON building faces (median face distance ~0) at per-resref
    /// Z bands between 1.1 and 7.0m -- _scratch_decor/r11_mine_buildings.py.
    /// </summary>
    public class FacadeMountEntry
    {
        public string Resref { get; set; } = string.Empty;
        public int Weight { get; set; } = 1;
        /// <summary>Bottom of the mined mounting-height band (meters above ground).</summary>
        public float MinHeight { get; set; } = 2f;
        /// <summary>Top of the mined mounting-height band (meters above ground).</summary>
        public float MaxHeight { get; set; } = 6f;
    }

    /// <summary>
    /// A NAMED alternate decoration palette a tileset profile can declare alongside its standard one
    /// (see <see cref="DungeonTilesetProfile.DecorationProfiles"/>) -- e.g. fcx01's "ruined" profile,
    /// which carries the wreckage/rubble/debris/dirt-decal destruction content the STANDARD clean
    /// city palette deliberately excludes. A named profile fully REPLACES the standard
    /// Decorations/Vignettes lists when selected (no merging), so each profile reads as one coherent
    /// visual statement; the theme's own small accent list still layers on top as usual. Selected via
    /// <see cref="DungeonDetail.DecorationProfile"/> (theme declaration) or
    /// AreaGenerationRequest.DecorationProfile / the review tooling's decoration-profile override
    /// (explicit per-request pick). Unknown/empty names fall back to the standard palette.
    /// </summary>
    public class DungeonDecorationProfile
    {
        public string Name { get; set; } = string.Empty;
        public List<DungeonDecorationEntry> Decorations { get; set; } = new();
        public List<DungeonVignette> Vignettes { get; set; } = new();

        /// <summary>
        /// True when this profile's clutter is genuinely organic junk (collapse debris, rubble
        /// drifts) whose pile members keep fully random rotations even under the tileset's urban
        /// placement grammar -- the one sanctioned exception to bearing alignment. The standard
        /// clean-city palette leaves this false so cargo reads as stacked/aligned goods.
        /// </summary>
        public bool OrganicClutterRotation { get; set; }
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
        /// Room-size floor (in corners) this tileset's configured multi-tile OpenSetPiece groups need
        /// to ever stamp: LayoutGroupStamper.IsOpenSetPieceSiteValid requires a group's footprint PLUS
        /// a 1-cell margin ring PLUS at least one spare center-relocation tile all inside ONE room, so
        /// an NxM-tile group needs a room strictly larger than (N+2)x(M+2) tiles -- corner size 6+ for
        /// a 2x2 group, 7+ for a 3x3 (see OpenSetPiecePlacementRateTests' documented Complex room-size
        /// ceiling, which this knob is the anticipated "room-size-aware layout knob" fix for).
        /// Compositions raise the layout profile's MaxRoomCornerSize to at least this, mirroring
        /// CorridorWidth/MinimumOpeningWidth's own "layout expresses intent, tileset declares physical
        /// need" shape. 0 (default) = no floor; the layout profile's own room sizes stand unchanged.
        /// Only meaningful alongside configured SetPieces; declare it on tilesets whose visual identity
        /// depends on stamped structures standing in open districts (e.g. fcx01's city towers --
        /// hand-built fcx01 areas' group-tile share is ~15% of the area, all of it multi-tile
        /// building/platform footprints that need plaza-sized rooms to exist at all).
        /// </summary>
        public int SetPieceRoomCornerFloor { get; set; }

        /// <summary>
        /// True for a tileset whose visual identity depends on MANY stamped OpenSetPiece structures
        /// per area (e.g. fcx01's city districts, where hand-built areas measure ~0.15 group-tile
        /// share -- dozens of multi-tile buildings each): compositions additionally scale the layout
        /// profile's ROOM COUNTS with area so larger areas actually carry proportionally more
        /// stampable rooms (see LayoutParameterConstraints.ApplySetPieceRoomSupplyScaling). Without
        /// this, room supply is flat in area size -- Halls/Complex hardcode MinRooms/MaxRooms and
        /// PackedRooms caps its reported room list at MaxRooms, so a 32x32 area hosts the same ~8
        /// stampable rooms as a 20x20 and group-tile share collapses as areas grow (measured flat
        /// 0.039-0.040 at 32x32 on fcx01 regardless of SetPiece budgets, vs 0.15 hand-built).
        ///
        /// Deliberately a SEPARATE declaration from SetPieceRoomCornerFloor: four interior profiles
        /// (secretbase/modernfacility/labstorage/officeinteriors) declare the corner floor for their
        /// occasional 2x2 room-groups but are NOT set-piece-heavy -- their layouts must stay
        /// byte-identical (see RoomSupplyScalingIsolationTests), so the floor alone must never
        /// trigger room-count scaling. Only meaningful alongside configured SetPieces and a declared
        /// SetPieceRoomCornerFloor; inert below the 20x20 tuning baseline either way.
        /// </summary>
        public bool SetPieceRoomSupplyScaling { get; set; }

        /// <summary>
        /// True for a tileset whose hand-built city references assemble stamped buildings into
        /// CONTIGUOUS blocks -- multiple tower/platform groups sharing footprint edges so streets read
        /// as canyons walled by building mass, not isolated towers on an open field. Hand-built
        /// tile-built fcx01 city areas (ns_comrcial_ka, pw_ar_nsshipyard, vrotrnsslums,
        /// narshadaar_promi) measure building blocks of 24-48 contiguous tiles (several adjoined
        /// groups; single largest group footprint is 36) at 0.17-0.28 building-tile share, dominated
        /// by same-group self-tiling (Tower06 beside Tower06, d_platform2 tiled into mega-platforms)
        /// with corner-label agreement at every seam.
        ///
        /// When declared, LayoutGroupStamper's OpenSetPiece placement (a) accepts a site whose margin
        /// ring touches an already-stamped OpenSetPiece footprint, PROVIDED every shared corner label
        /// and edge crosser the new stamp would write agrees with what the earlier stamp already wrote
        /// (so seams are only ever formed between visually compatible faces), (b) prefers sites that
        /// front a carved road AND adjoin an existing building (canyon walls along streets) over
        /// road-only over building-only over free-standing, (c) re-verifies that consuming the site
        /// does not split the room's remaining open tiles into more disconnected pieces than before,
        /// and (d) doubles the road-scaled SetPiece attempt budget (adjacency unlocks sites the
        /// isolated-margin rule physically could not host). Default false -- every composition that
        /// never declares it keeps byte-identical layouts (RoomSupplyScalingIsolationTests).
        /// Only meaningful alongside configured SetPieces, and -- like SetPieceRoomSupplyScaling and
        /// the road-scaled budget -- inert at or below the 20x20 tuning baseline
        /// (LayoutParameterConstraints.RoomSupplyBaselineTiles): baseline-size compositions keep the
        /// pre-mechanism placement byte-for-byte, because both the per-tileset budgets and the urban
        /// dressing-density gates are tuned against 20x20 evidence and block assembly there starved
        /// the street-margin dressing pools below the hand-built density band.
        /// </summary>
        public bool BuildingBlockContiguity { get; set; }

        /// <summary>
        /// Edge-crosser name this tileset's road/route-marking tile family carves (e.g. fcx01's
        /// "Routes") -- see LayoutRoadCarver/RoadVocabularyCheck. Unlike ChannelTerrain/AccentTerrain,
        /// a road never repaints corner terrain: every road cell stays this composition's own
        /// PrimaryOpenTerrain, so no separate terrain slot is needed. Empty = the tileset has no
        /// verified road-lane vocabulary; compositions skip road carving entirely (fully back-compat).
        /// Only set after verifying RoadVocabularyCheck.SupportsRoads returns true, mirroring every
        /// other tileset-declared capability in this file.
        /// </summary>
        public string RoadCrosser { get; set; } = string.Empty;

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
        /// Dressing obligation per feature group name (see <see cref="FeatureZoneDressing"/>): an
        /// area-marking feature tile (grass lawn, fountain court) that lands inside an open room
        /// must carry a composed ensemble rather than standing bare. Only consumed under the urban
        /// placement grammar (<see cref="UrbanDressing"/>); feature groups absent from this map
        /// (trees, water, treasure mounds -- art that already fills the cell) are never dressed.
        /// </summary>
        public Dictionary<string, FeatureZoneDressing> FeatureTileDressings { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);

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

        /// <summary>
        /// This tileset FAMILY's own evidence-derived decorative-placeable density (placeables per
        /// total area tile at 100% request density), overriding the composed theme's
        /// <see cref="DungeonDetail.DecorationBaseDensity"/> when set (&gt; 0). Dressing INTENSITY is
        /// a property of the visual family, not the content theme: hand-built fcx01 city areas
        /// measure ~1.6 decoratives/tile aggregate while cave families measure ~0.15 -- a theme
        /// composed onto a city tileset must dress at CITY density. 0 (the default) keeps the
        /// theme-owned density, so families without their own mined density band behave exactly as
        /// before this override existed. Inherited by palette variants the same way as
        /// <see cref="Decorations"/> (see DungeonTilesetPaletteInheritance).
        /// </summary>
        public double DecorationDensityPerTile { get; set; }

        /// <summary>
        /// NAMED alternate decoration palettes (key = profile name, case-insensitive) selectable per
        /// theme/request -- see <see cref="DungeonDecorationProfile"/>. The standard palette stays in
        /// <see cref="Decorations"/>/<see cref="Vignettes"/>; entries here fully replace it when
        /// selected. Inherited by palette variants alongside the standard palette (see
        /// DungeonTilesetPaletteInheritance).
        /// </summary>
        public Dictionary<string, DungeonDecorationProfile> DecorationProfiles { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// True for a tileset whose hand-built reference areas follow an URBAN placement grammar,
        /// enabling DungeonDecorationPlanner's city composition rules for this family only (every
        /// other tileset's plan stays byte-identical): bearing alignment (placements face the wall/
        /// road/facade they belong to, quantized to cardinals -- hand-built fcx01 measures 73%
        /// cardinal-aligned vs 29% for random spin), road integrity (the carved road ribbon stays a
        /// clear walkway; only <see cref="DungeonDecorationEntry.AllowOnRoadSurface"/> lamp-family
        /// entries may stand on it, everything else sets back and faces the street), facade rows
        /// (road-margin and structure-frontage runs repeat a single resref at an even rhythm with a
        /// shared bearing), cargo grids (structure/corner clutter snaps to a small wall-aligned grid
        /// instead of a loose disc), and zone discipline (clutter piles anchor only against walls,
        /// structure bases, and corners -- never free-floating in plaza centers, which are reserved
        /// for composed courtyards/centerpieces). Evidence: the July 2026 city review pass measured
        /// generated fcx01 areas at chance-level bearing alignment with kiosks standing on the road
        /// ribbon and junk piles mid-plaza -- "a scattering of different objects randomly placed".
        /// </summary>
        public bool UrbanDressing { get; set; }

        /// <summary>
        /// Structural building placeables this tileset family erects along open-area perimeter
        /// edges and street margins to form canyon walls (see <see cref="BuildingFrontageEntry"/>
        /// and BuildingFrontagePlanner). Empty = no placeable-frontage system for this family
        /// (every non-city tileset; their plans and layouts stay byte-identical). Only meaningful
        /// alongside <see cref="UrbanDressing"/>. At promenade scale (12x12-20x20) this is the
        /// PRIMARY canyon mechanism -- the hand-built flagship's walls are placeables, not tiles;
        /// at 24-32 it complements the tile-block mechanism
        /// (<see cref="BuildingBlockContiguity"/>), fronting tile-block faces and walling the
        /// remaining margins. Inherited by palette variants like <see cref="Decorations"/>.
        /// </summary>
        public List<BuildingFrontageEntry> FrontageBuildings { get; set; } = new();

        /// <summary>
        /// Wall-mounted sign/holo placeables this tileset family hangs on building faces (see
        /// <see cref="FacadeMountEntry"/> and BuildingFrontagePlanner.PlanFacadeMounts). Empty =
        /// no facade-mount pass. Only meaningful alongside <see cref="UrbanDressing"/>.
        /// Inherited by palette variants like <see cref="Decorations"/>.
        /// </summary>
        public List<FacadeMountEntry> FacadeMounts { get; set; } = new();
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
        /// Name of the composed tileset's decoration profile this theme requests (see
        /// <see cref="DungeonTilesetProfile.DecorationProfiles"/>) -- e.g. a ruin-flavored theme may
        /// request a city tileset's "ruined" destruction palette. Empty (the default) = the
        /// tileset's standard palette. A per-request override (AreaGenerationRequest.
        /// DecorationProfile) wins over this declaration; a name the composed tileset never declared
        /// falls back to the standard palette.
        /// </summary>
        public string DecorationProfile { get; set; } = string.Empty;

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
            // Roads have no terrain slot of their own (see DungeonTilesetProfile.RoadCrosser's doc
            // comment) -- just the crosser name, gated the same "layout expresses intent via RoadLanes,
            // tileset caps it to verified support" shape as every other declared capability here.
            parameters.RoadCrosser = Tileset.RoadCrosser ?? string.Empty;
            if (parameters.RoadCrosser.Length == 0)
            {
                parameters.RoadLanes = 0;
            }
            else
            {
                // A road lane can only occupy FULLY-open tiles (LayoutRoadCarver never repaints
                // corners), and a 1-wide open lane has no fully-open tile at all -- its cells all
                // straddle the lane's own edges -- so on a road-declaring composition, 1-wide
                // corridors would confine every street to room interiors (measured on
                // fcx01/futcity_plaza at size 20: road-edge share 0.016 at width 1 vs ~0.10 at
                // width 2, against the hand-built fcx01 reference's 0.102). 2-wide lanes are also
                // what hand-built fcx01 streets are. Independent of MinimumOpeningWidth (a pathnode
                // WALKABILITY floor -- Cobble2's partially-open tiles genuinely walk fine at width 1,
                // see PathNodeOpeningWidthAudit); this is road GEOMETRY.
                parameters.CorridorWidth = Math.Max(parameters.CorridorWidth, 2);
            }
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
            // Multi-tile OpenSetPiece stamping needs rooms bigger than the group footprint + margin +
            // one spare tile (see DungeonTilesetProfile.SetPieceRoomCornerFloor's doc comment) -- floor
            // the layout's room-size ceiling to the tileset's declared physical need, mirroring the
            // CorridorWidth/MinimumOpeningWidth floor above and the PoolRegions room-size floor below.
            // MinRoomCornerSize is floored 2 corners lower (never above the ceiling): if the layout
            // kept rolling its own small-room minimum, most rooms would still be too small to host any
            // stamp -- measured on fcx01/Complex at size 20, Min left at 3 placed a 2x2 group on only
            // 17/30 seeds (mean 2.4 group tiles/area) vs the same composition with Min floored to 5.
            // LayoutParameterConstraints.ClampToValid still applies its own empirically-measured
            // per-size ceiling afterward (e.g. corner 6 at size 20), so this floor never pushes a
            // composition past the measured-safe room size for its area dimensions.
            // Gated on configured SetPieces so a declared floor with nothing to stamp stays inert.
            if (Tileset.SetPieceRoomCornerFloor > 0 && Tileset.SetPieces.Count > 0)
            {
                parameters.MaxRoomCornerSize = Math.Max(parameters.MaxRoomCornerSize, Tileset.SetPieceRoomCornerFloor);
                parameters.MinRoomCornerSize = Math.Max(parameters.MinRoomCornerSize, Tileset.SetPieceRoomCornerFloor - 2);
                parameters.MinRoomCornerSize = Math.Min(parameters.MinRoomCornerSize, parameters.MaxRoomCornerSize);
                // Room-supply scaling is only ever stamped inside the same gate: a set-piece-heavy
                // declaration without a corner floor or without configured SetPieces has nothing to
                // scale FOR (the room-count derivation is sized off the floored room envelope above).
                // Width/Height are not known here (LayoutSolver stamps them per attempt), so this only
                // records intent; MacroLayoutGenerator.Generate applies the actual derivation via
                // LayoutParameterConstraints.ApplySetPieceRoomSupplyScaling once dimensions exist.
                parameters.SetPieceRoomSupplyScaling = Tileset.SetPieceRoomSupplyScaling;
            }
            // Contiguous building blocks: only meaningful with configured SetPieces (same gate shape
            // as the supply-scaling stamp above, minus the corner-floor requirement -- adjacency is a
            // placement rule, not a room-size need) and inert without a road-declaring composition
            // (LayoutGroupStamper's OpenSetPiece path is the only consumer).
            if (Tileset.SetPieces.Count > 0)
                parameters.BuildingBlockContiguity = Tileset.BuildingBlockContiguity;
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
        /// Requests a NAMED decoration profile of whatever tileset this theme composes with -- see
        /// <see cref="DungeonDetail.DecorationProfile"/>. Falls back to the tileset's standard
        /// palette when the composed tileset never declared the name.
        /// </summary>
        public DungeonDefinitionBuilder DecorationProfile(string profileName)
        {
            _activeDungeon.DecorationProfile = profileName ?? string.Empty;
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
            _activeDecorationProfile = null;
            _activeVignette = null;
            _lastDecorationEntry = null;
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
        /// Declares the room-size floor (in corners) this tileset's multi-tile OpenSetPiece groups
        /// need to ever stamp -- see DungeonTilesetProfile.SetPieceRoomCornerFloor. Compositions raise
        /// the layout profile's MaxRoomCornerSize to at least this whenever the profile also configures
        /// SetPieces.
        /// </summary>
        public DungeonTilesetProfileBuilder SetPieceRoomCornerFloor(int cornerSize)
        {
            _active.SetPieceRoomCornerFloor = cornerSize;
            return this;
        }

        /// <summary>
        /// Declares this tileset as set-piece-heavy: compositions scale the layout profile's room
        /// counts with area so larger areas carry proportionally more stampable rooms -- see
        /// DungeonTilesetProfile.SetPieceRoomSupplyScaling for the measured rationale and why this
        /// is separate from SetPieceRoomCornerFloor. Declare only alongside SetPieceRoomCornerFloor
        /// and configured SetPieces.
        /// </summary>
        public DungeonTilesetProfileBuilder SetPieceRoomSupplyScaling()
        {
            _active.SetPieceRoomSupplyScaling = true;
            return this;
        }

        /// <summary>
        /// Declares that this tileset's stamped buildings assemble into contiguous blocks walling the
        /// street network, the hand-built city pattern -- see
        /// DungeonTilesetProfile.BuildingBlockContiguity for the measured evidence and the exact
        /// placement-rule changes. Declare only alongside configured SetPieces on a road-declaring
        /// (RoadCrosser) tileset whose building groups carry mutually compatible perimeter corner
        /// labels (verified for fcx01: every Cobble-district tower group has uniform open-cornered,
        /// crosser-free perimeter faces, and the Cobble2 district's towers likewise agree with each
        /// other).
        /// </summary>
        public DungeonTilesetProfileBuilder BuildingBlockContiguity()
        {
            _active.BuildingBlockContiguity = true;
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
        public DungeonTilesetProfileBuilder FeatureTile(string groupName, int weight = 1,
            FeatureZoneDressing dressing = FeatureZoneDressing.None)
        {
            _active.FeatureTiles[groupName] = weight;
            if (dressing != FeatureZoneDressing.None)
                _active.FeatureTileDressings[groupName] = dressing;
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
        /// Declares the edge-crosser name this tileset's road/route-marking tile family carves (e.g.
        /// fcx01's "Routes") -- see DungeonTilesetProfile.RoadCrosser. Only call this after verifying
        /// RoadVocabularyCheck.SupportsRoads(tileset, openTerrain, crosserName) returns true against
        /// the real tileset data, not merely that the crosser name appears in the .set CROSSER TYPES
        /// list.
        /// </summary>
        public DungeonTilesetProfileBuilder RoadCrosser(string crosserName)
        {
            _active.RoadCrosser = crosserName;
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

        private DungeonDecorationProfile _activeDecorationProfile;

        /// <summary>
        /// Starts a NAMED alternate decoration palette on this tileset profile (see
        /// <see cref="DungeonTilesetProfile.DecorationProfiles"/>): every subsequent
        /// <see cref="Decoration"/>/<see cref="Vignette"/>/<see cref="VignetteMember"/> call routes
        /// into it instead of the standard palette, until the next Create()/DecorationProfile() call.
        /// Declare the standard palette FIRST, then each named profile.
        /// </summary>
        public DungeonTilesetProfileBuilder DecorationProfile(string name, bool organicClutterRotation = false)
        {
            _activeDecorationProfile = new DungeonDecorationProfile
            {
                Name = name,
                OrganicClutterRotation = organicClutterRotation
            };
            _active.DecorationProfiles[name] = _activeDecorationProfile;
            _lastDecorationEntry = null;
            return this;
        }

        /// <summary>
        /// Declares this tileset family as following an urban placement grammar -- see
        /// <see cref="DungeonTilesetProfile.UrbanDressing"/>. Only declare it for families whose
        /// hand-built reference areas measurably follow the grammar (bearing alignment, clear road
        /// ribbons, facade rows); every non-declaring tileset's plan stays byte-identical.
        /// </summary>
        public DungeonTilesetProfileBuilder UrbanDressing()
        {
            _active.UrbanDressing = true;
            return this;
        }

        /// <summary>
        /// Adds a weighted decorative placeable to this tileset FAMILY's own bulk palette for one
        /// placement context — see <see cref="DungeonTilesetProfile.Decorations"/>. This is where the
        /// bulk of a generated area's visual dressing should live; theme definitions should only add a
        /// small handful of their own genuinely theme-flavored accents. Routes into the active NAMED
        /// decoration profile instead once <see cref="DecorationProfile"/> has been called.
        /// </summary>
        public DungeonTilesetProfileBuilder Decoration(string resref, int weight, DecorationContext context,
            DecorationRole role = DecorationRole.Fixture, bool allowOnRoadSurface = false,
            DecorationAnchoring anchoring = DecorationAnchoring.FreeStanding,
            DecorationSize size = DecorationSize.Medium)
        {
            var target = _activeDecorationProfile?.Decorations ?? _active.Decorations;
            _lastDecorationEntry = new DungeonDecorationEntry
            {
                Resref = resref,
                Weight = weight,
                Context = context,
                Role = role,
                AllowOnRoadSurface = allowOnRoadSurface,
                Anchoring = anchoring,
                Size = size
            };
            target.Add(_lastDecorationEntry);

            return this;
        }

        private DungeonDecorationEntry _lastDecorationEntry;

        /// <summary>
        /// Declares the LAST <see cref="Decoration"/> entry's district affinity (see
        /// <see cref="DungeonDecorationEntry.DistrictWeights"/>): the entry only places in rooms
        /// of the listed flavors, at the listed per-flavor weight. Omit entirely for an entry that
        /// serves every district at its base weight.
        /// </summary>
        public DungeonTilesetProfileBuilder Districts(params (DistrictFlavor Flavor, int Weight)[] weights)
        {
            if (_lastDecorationEntry == null)
                throw new System.InvalidOperationException("Districts() must follow a Decoration() call.");
            foreach (var (flavor, weight) in weights)
                _lastDecorationEntry.DistrictWeights[flavor] = weight;
            return this;
        }

        /// <summary>
        /// Declares the LAST <see cref="Decoration"/> entry's hard per-area placement cap (see
        /// <see cref="DungeonDecorationEntry.MaxPerArea"/>) -- derive it from the hand-built
        /// per-area p95 within the entry's district.
        /// </summary>
        public DungeonTilesetProfileBuilder MaxPerArea(int cap)
        {
            if (_lastDecorationEntry == null)
                throw new System.InvalidOperationException("MaxPerArea() must follow a Decoration() call.");
            _lastDecorationEntry.MaxPerArea = cap;
            return this;
        }

        /// <summary>
        /// Sets this tileset family's own evidence-derived decorative density (placeables per total
        /// area tile at 100% request density), overriding the composed theme's own base density --
        /// see <see cref="DungeonTilesetProfile.DecorationDensityPerTile"/>.
        /// </summary>
        public DungeonTilesetProfileBuilder DecorationDensity(double perTile)
        {
            _active.DecorationDensityPerTile = perTile;
            return this;
        }

        /// <summary>
        /// Adds a weighted structural building placeable to this tileset family's frontage system
        /// (see <see cref="DungeonTilesetProfile.FrontageBuildings"/>). Footprint dimensions are
        /// the measured model XY extents: faceWidth along the fronted face, depth into the margin.
        /// Only declare on urban families whose hand-built references wall their open space with
        /// building placeables.
        /// </summary>
        public DungeonTilesetProfileBuilder FrontageBuilding(string resref, int weight, float faceWidth, float depth,
            int maxPerArea = 0)
        {
            _active.FrontageBuildings.Add(new BuildingFrontageEntry
            {
                Resref = resref,
                Weight = weight,
                FaceWidth = faceWidth,
                Depth = depth,
                MaxPerArea = maxPerArea
            });
            return this;
        }

        /// <summary>
        /// Adds a weighted wall-mounted sign/holo placeable to this tileset family's facade-mount
        /// pass (see <see cref="DungeonTilesetProfile.FacadeMounts"/>). The height band is the
        /// mined per-resref Z band of the hand-built elevated placements.
        /// </summary>
        public DungeonTilesetProfileBuilder FacadeMount(string resref, int weight, float minHeight, float maxHeight)
        {
            _active.FacadeMounts.Add(new FacadeMountEntry
            {
                Resref = resref,
                Weight = weight,
                MinHeight = minHeight,
                MaxHeight = maxHeight
            });
            return this;
        }

        private DungeonVignette _activeVignette;

        /// <summary>
        /// Starts a new evidence-backed vignette grouping (see <see cref="DungeonVignette"/>) on this
        /// tileset profile. Follow with one or more <see cref="VignetteMember"/> calls. Routes into
        /// the active NAMED decoration profile once <see cref="DecorationProfile"/> has been called.
        /// </summary>
        public DungeonTilesetProfileBuilder Vignette(string key, int weight = 1)
        {
            _activeVignette = new DungeonVignette { Key = key, Weight = weight };
            (_activeDecorationProfile?.Vignettes ?? _active.Vignettes).Add(_activeVignette);
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
                if (!profile.IsPaletteVariant)
                    continue;

                if (profile.Decorations.Count == 0 && profile.Vignettes.Count == 0)
                {
                    var basis = profiles.Values.FirstOrDefault(p =>
                        !p.IsPaletteVariant && p.TilesetResref == profile.TilesetResref &&
                        (p.Decorations.Count > 0 || p.Vignettes.Count > 0));

                    if (basis != null)
                    {
                        profile.Decorations = basis.Decorations;
                        profile.Vignettes = basis.Vignettes;
                        // Named alternate palettes travel with the standard one: a variant that
                        // declared no dressing of its own offers the same selectable profiles
                        // (e.g. fcx01's "ruined") as its family basis. Shared reference is fine --
                        // palettes are never mutated after build, only read.
                        if (profile.DecorationProfiles.Count == 0)
                            profile.DecorationProfiles = basis.DecorationProfiles;
                    }
                }

                // Structural frontage and facade mounts are family properties like the palette
                // above: a variant that declared none of its own walls/dresses like its family
                // basis (fcx01's Cobble2 district shares the Cobble district's swd_build canyon
                // vocabulary -- the hand-built evidence spans both districts' areas).
                if (profile.FrontageBuildings.Count == 0)
                {
                    var frontageBasis = profiles.Values.FirstOrDefault(p =>
                        !p.IsPaletteVariant && p.TilesetResref == profile.TilesetResref &&
                        p.FrontageBuildings.Count > 0);
                    if (frontageBasis != null)
                        profile.FrontageBuildings = frontageBasis.FrontageBuildings;
                }

                if (profile.FacadeMounts.Count == 0)
                {
                    var mountBasis = profiles.Values.FirstOrDefault(p =>
                        !p.IsPaletteVariant && p.TilesetResref == profile.TilesetResref &&
                        p.FacadeMounts.Count > 0);
                    if (mountBasis != null)
                        profile.FacadeMounts = mountBasis.FacadeMounts;
                }

                // The urban placement grammar is a family property like density below: a variant of
                // an urban family dresses under the same grammar unless it declared its own palette
                // AND its evidence genuinely differs (in which case declare UrbanDressing on the
                // variant directly).
                if (!profile.UrbanDressing)
                {
                    profile.UrbanDressing = profiles.Values.Any(p =>
                        !p.IsPaletteVariant && p.TilesetResref == profile.TilesetResref && p.UrbanDressing);
                }

                // Family density (see DungeonTilesetProfile.DecorationDensityPerTile) inherits
                // independently of the palette lists: a variant that curated its own small palette
                // still dresses at its family's own measured intensity unless it declared one.
                if (profile.DecorationDensityPerTile <= 0)
                {
                    var densityBasis = profiles.Values.FirstOrDefault(p =>
                        !p.IsPaletteVariant && p.TilesetResref == profile.TilesetResref &&
                        p.DecorationDensityPerTile > 0);

                    if (densityBasis != null)
                        profile.DecorationDensityPerTile = densityBasis.DecorationDensityPerTile;
                }
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
