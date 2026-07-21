using System.Numerics;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// One placed tile from an area's Tile_List, resolved against its tileset and (when
    /// resolvable) a shared <see cref="RenderModel"/>. World placement follows the 10-meter tile
    /// grid: tile <see cref="TileIndex"/> i occupies column <c>i % Width</c>, row <c>i / Width</c>
    /// (the area's Width), spanning [col*10,(col+1)*10] x [row*10,(row+1)*10]; <see cref="Transform"/>
    /// rotates the tile model about that square's center before translating it into place, matching
    /// how a 90-degree tile rotation keeps the tile's footprint on the same grid cell.
    /// </summary>
    public sealed class TilePlacement
    {
        /// <summary>Index into the area's Tile_List (row-major: index = row * Width + col).</summary>
        public required int TileIndex { get; init; }

        public required int Column { get; init; }
        public required int Row { get; init; }

        /// <summary>Raw Tile_ID: an index into the tileset's [TILEn] entries.</summary>
        public required int TileId { get; init; }

        /// <summary>Raw Tile_Orientation: 0-3, each step a 90-degree counter-clockwise turn.</summary>
        public required int Orientation { get; init; }

        /// <summary>Raw Tile_Height: an integer level multiplied by the tileset's Transition height.</summary>
        public required int HeightLevel { get; init; }

        /// <summary>World-space X of this tile's 10m-square center (before rotation is applied).</summary>
        public required float CenterX { get; init; }

        /// <summary>World-space Y of this tile's 10m-square center (before rotation is applied).</summary>
        public required float CenterY { get; init; }

        /// <summary>World-space Z offset: <see cref="HeightLevel"/> * the tileset's Transition height.</summary>
        public required float HeightOffset { get; init; }

        /// <summary>
        /// Full local-to-world transform for this tile's model: rotate about the tile square's
        /// center by <see cref="Orientation"/> * 90 degrees (CCW, about +Z), then translate to
        /// (<see cref="CenterX"/>, <see cref="CenterY"/>, <see cref="HeightOffset"/>).
        /// </summary>
        public required Matrix4x4 Transform { get; init; }

        /// <summary>The tile model's resref (from the tileset's TileDefinition.Model), or null when the tile id itself could not be resolved.</summary>
        public string? ModelResRef { get; init; }

        /// <summary>
        /// The shared, cached render geometry for <see cref="ModelResRef"/>, or null when this
        /// placement is a <see cref="IsFallback"/> (missing/unparseable model). Multiple
        /// placements across an area (and across a batch of areas) share the same instance.
        /// </summary>
        public RenderModel? Model { get; init; }

        /// <summary>
        /// True when the tile's model could not be resolved (bad Tile_ID, missing Model resref, or
        /// the model resource could not be found/parsed). A renderer should draw a unit-cube
        /// placeholder at <see cref="Transform"/> for these instead of skipping the tile.
        /// </summary>
        public required bool IsFallback { get; init; }
    }

    /// <summary>The kind of placed-instance list an <see cref="InstanceMarker"/> came from.</summary>
    public enum InstanceMarkerKind
    {
        Creature,
        Door,
        Encounter,
        Item,
        Placeable,
        Sound,
        Store,
        Trigger,
        Waypoint
    }

    /// <summary>
    /// A lightweight placement marker for one instance from a .git list. Deliberately does not
    /// resolve the instance's appearance model - that is WP4.5's concern once the actual GL area
    /// view exists; this is data assembly only.
    /// </summary>
    public sealed class InstanceMarker
    {
        public required InstanceMarkerKind Kind { get; init; }

        /// <summary>The blueprint resref this instance was placed from ("TemplateResRef", or "ResRef" for stores). Null for kinds with no single blueprint resref (e.g. encounters).</summary>
        public string? TemplateResRef { get; init; }

        public string? Tag { get; init; }

        public required Vector3 Position { get; init; }

        /// <summary>Heading as a (cos, sin) unit vector - XOrientation/YOrientation, or Bearing converted for placeables/doors. (1,0) when the instance carries no heading (ambient sounds, encounters).</summary>
        public required Vector2 Orientation { get; init; }

        /// <summary>Polygon points (world-space) for trigger/encounter volumes; null for every other kind or when absent.</summary>
        public IReadOnlyList<Vector3>? Geometry { get; init; }

        /// <summary>
        /// Resolved render geometry for kinds whose appearance lives on the instance itself
        /// (placeables via placeables.2da ModelName, doors via doortypes.2da Model), shared
        /// through the model cache. Null when unresolvable or when appearance services weren't
        /// supplied — the renderer draws the kind marker instead.
        /// </summary>
        public RenderModel? Model { get; init; }
    }

    /// <summary>Diagnostics collected while assembling one <see cref="AreaScene"/>.</summary>
    public sealed class AreaSceneDiagnostics
    {
        private readonly List<string> _missingModels = new();

        /// <summary>
        /// Human-readable notes for every tile placement that fell back to a placeholder: bad
        /// Tile_ID, a blank Model in the tileset, an unresolvable tileset, or a model resource that
        /// could not be found/parsed.
        /// </summary>
        public IReadOnlyList<string> MissingModels => _missingModels;

        internal void AddMissingModel(string message) => _missingModels.Add(message);
    }

    /// <summary>
    /// A render-ready scene description for one area: its tile grid placements and placed-instance
    /// markers, plus assembly diagnostics. Pure data - no GL/app dependency; consumed by the WP4.5
    /// area view.
    /// </summary>
    public sealed class AreaScene
    {
        public required string Tileset { get; init; }
        public required int Width { get; init; }
        public required int Height { get; init; }
        public required IReadOnlyList<TilePlacement> Tiles { get; init; }
        public required IReadOnlyList<InstanceMarker> Instances { get; init; }
        public required AreaSceneDiagnostics Diagnostics { get; init; }
    }
}
