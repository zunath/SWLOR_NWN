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

        /// <summary>
        /// This tile's walkmesh in tile-local space (apply <see cref="Transform"/> to reach world
        /// space), or null when the tile has no resolvable .wok. Shared/cached per tile-model
        /// resref like <see cref="Model"/>.
        /// </summary>
        public WalkMesh? Walkmesh { get; init; }
    }

    /// <summary>The kind of placed-instance list an <see cref="InstanceMarker"/> came from.</summary>
    public enum InstanceMarkerKind
    {
        Creature,
        Door,
        Item,
        Placeable,
        Sound,
        Store,
        Trigger,
        Waypoint
    }

    /// <summary>
    /// A lightweight placement marker for one instance from a .git list. Deliberately does not
    /// resolve the instance's appearance model - that is the GL area renderer's concern
    /// view exists; this is data assembly only.
    /// </summary>
    public sealed class InstanceMarker
    {
        public required InstanceMarkerKind Kind { get; init; }

        /// <summary>The blueprint resref this instance was placed from ("TemplateResRef", or "ResRef" for stores).</summary>
        public string? TemplateResRef { get; init; }

        public string? Tag { get; init; }

        public required Vector3 Position { get; init; }

        /// <summary>Heading as a (cos, sin) unit vector - XOrientation/YOrientation, or Bearing converted for placeables/doors. (1,0) when the instance carries no heading (ambient sounds).</summary>
        public required Vector2 Orientation { get; init; }

        /// <summary>
        /// Optional enhanced-edition model transform (scale, Euler rotation, translation) in
        /// instance-local space. Composed before <see cref="Orientation"/> and
        /// <see cref="Position"/> for rendering and picking.
        /// </summary>
        public Matrix4x4 VisualTransform { get; init; } = Matrix4x4.Identity;

        /// <summary>Polygon points (world-space) for a trigger's volume; null for every other kind or when absent.</summary>
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
    /// markers, plus assembly diagnostics. Pure data - no GL/app dependency; consumed by the
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

        /// <summary>
        /// Every doorway the placed tiles declare, in world space - the only positions a door may be
        /// hung at. Empty for an area whose tileset would not resolve, or one laid entirely with
        /// tiles that carry no door nodes.
        /// </summary>
        public IReadOnlyList<TileDoorAnchor> DoorAnchors { get; init; } = Array.Empty<TileDoorAnchor>();

        /// <summary>
        /// The area's decoded ambient/diffuse lighting, from its .are sun colors by day or
        /// moon colors at night. Defaults to a neutral mid-gray when not set (e.g. synthetic test
        /// scenes) so existing callers that build an <see cref="AreaScene"/> directly still compile.
        /// </summary>
        public AreaLighting Lighting { get; init; } = AreaLighting.Default;
    }

    /// <summary>
    /// True per-area lighting decoded from the .are: the sun ambient/diffuse colors during
    /// the day, the moon colors at night. Colors are linear RGB in 0..1. This is the faithful area
    /// color - a presentation layer (the GL area view) may brighten it for editor visibility, since
    /// authored night colors are near-black.
    /// </summary>
    public sealed class AreaLighting
    {
        public required Vector3 AmbientColor { get; init; }
        public required Vector3 DiffuseColor { get; init; }
        public required bool IsNight { get; init; }

        /// <summary>The area's fog colour, sun or moon to match <see cref="IsNight"/>.</summary>
        public Vector3 FogColor { get; init; }

        /// <summary>
        /// Fog thickness as a per-metre extinction coefficient, converted from the .are's 0-15
        /// FogAmount. Zero means the area authored no fog.
        /// </summary>
        public float FogDensity { get; init; }

        /// <summary>Turns the .are's 0-15 FogAmount into a per-metre extinction coefficient.</summary>
        /// <remarks>
        /// At the top of the range this puts roughly half the light through at 25m, which is dense
        /// enough to read as weather without hiding the far side of a normal interior.
        /// </remarks>
        public static float DecodeFogDensity(int fogAmount) =>
            Math.Clamp(fogAmount, 0, 15) * 0.0018f;

        /// <summary>Neutral mid-gray fallback for scenes/areas that carry no lighting fields.</summary>
        public static AreaLighting Default { get; } = new()
        {
            AmbientColor = new Vector3(0.5f, 0.5f, 0.5f),
            DiffuseColor = new Vector3(0.5f, 0.5f, 0.5f),
            IsNight = false
        };

        /// <summary>
        /// Decodes an NWN packed area color (0x00BBGGRR: red = low byte, green = middle, blue =
        /// high) into a linear 0..1 RGB vector.
        /// </summary>
        public static Vector3 DecodeColor(uint packed) => new(
            (packed & 0xFF) / 255f,
            ((packed >> 8) & 0xFF) / 255f,
            ((packed >> 16) & 0xFF) / 255f);
    }
}
