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

        /// <summary>
        /// Dye indices for this instance's PLT layers, overriding whatever the model carries. Empty
        /// for everything undyed.
        /// </summary>
        /// <remarks>
        /// Here as well as on the model so that changing a dye does not have to produce new geometry:
        /// re-resolving the model meant re-reading 19 part files, re-composing them and re-uploading
        /// every vertex buffer on each click of a colour swatch, which is most of what made picking a
        /// colour feel slow. The geometry is the same body; only its colours changed.
        /// </remarks>
        public IReadOnlyDictionary<int, int> LayerColorIndices { get; init; } =
            new Dictionary<int, int>();

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

        /// <summary>
        /// This door's 2DA row declares <c>VisibleModel=0</c>, making it an invisible runtime area
        /// transition that the toolset must represent with translucent editor geometry.
        /// </summary>
        public bool IsDoorTransition { get; init; }

        /// <summary>
        /// A sound instance's MinDistance in metres - the range it plays at full volume, which
        /// Aurora draws as the dotted sphere around the marker. Null for every other kind and for
        /// sounds that carry no such field.
        /// </summary>
        public float? SoundMinDistance { get; init; }

        /// <summary>
        /// A sound instance's MaxDistance in metres - the range it is audible at all, which Aurora
        /// draws as the large circle around the marker. Null for every other kind and for sounds
        /// that carry no such field.
        /// </summary>
        public float? SoundMaxDistance { get; init; }

        /// <summary>
        /// Whether a sound instance is positional (Positional=1). Area-wide sounds get no distance
        /// rings - range means nothing for a sound that plays everywhere. Always false for other kinds.
        /// </summary>
        public bool IsPositionalSound { get; init; }

        /// <summary>
        /// This marker moved and/or turned, with everything else - kind, tag, resolved model, EE
        /// visual transform - carried across unchanged.
        /// </summary>
        /// <remarks>
        /// Exists so a drag or a rotate can update the scene in place instead of reparsing both
        /// documents and rebuilding every tile and instance to move one object. Nothing about a
        /// placement's own geometry depends on where a different instance sits, so a transform is the
        /// one edit that can be applied this cheaply.
        /// <para>
        /// <see cref="Geometry"/> is world-space here but authored as offsets from the instance's
        /// position, so it travels with a move. It does <b>not</b> turn with
        /// <see cref="Orientation"/> - a trigger's volume is stored unrotated, and rotating one in
        /// the toolset leaves its polygon where it was, which is what the engine does too.
        /// </para>
        /// </remarks>
        public InstanceMarker WithTransform(Vector3 position, Vector2 orientation)
        {
            var delta = position - Position;
            var geometry = Geometry;
            if (geometry != null && delta != Vector3.Zero)
            {
                var moved = new Vector3[geometry.Count];
                for (var i = 0; i < geometry.Count; i++)
                    moved[i] = geometry[i] + delta;
                geometry = moved;
            }

            return new InstanceMarker
            {
                Kind = Kind,
                TemplateResRef = TemplateResRef,
                Tag = Tag,
                Position = position,
                Orientation = orientation,
                VisualTransform = VisualTransform,
                Geometry = geometry,
                Model = Model,
                IsDoorTransition = IsDoorTransition,
                SoundMinDistance = SoundMinDistance,
                SoundMaxDistance = SoundMaxDistance,
                IsPositionalSound = IsPositionalSound
            };
        }
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
        /// Whether this area's tileset declares <c>Interior=1</c> in its .set - i.e. its tiles are
        /// rooms with a ceiling over them rather than open sky.
        /// </summary>
        /// <remarks>
        /// This is what decides whether the viewport's hide-ceilings pass applies. Interior and
        /// exterior tilesets both mark overhead geometry with the MDL <c>tilefade</c> flag, but they
        /// mean different things by it: zsf01 flags the ceiling slabs sealing each room, which have to
        /// go or the area reads as a field of blank plates; ttw01 flags the forest canopy, which
        /// Aurora draws - a wood with its treetops removed is a field of bare poles, and the reference
        /// toolset shows the canopy. False for an area whose tileset would not resolve, which is also
        /// the safe answer: nothing is hidden.
        /// </remarks>
        public bool IsInteriorTileset { get; init; }

        /// <summary>
        /// Every doorway the placed tiles declare, in world space - the only positions a door may be
        /// hung at. Empty for an area whose tileset would not resolve, or one laid entirely with
        /// tiles that carry no door nodes.
        /// </summary>
        public IReadOnlyList<TileDoorAnchor> DoorAnchors { get; init; } = Array.Empty<TileDoorAnchor>();

        /// <summary>
        /// How near a ground point has to be for a door to reach a doorway, in metres.
        /// </summary>
        /// <remarks>
        /// Half a tile, so the doorways of the tile under the point are in reach and a doorway two rooms
        /// away is not. This was once unbounded, on the reasoning that a door has to go somewhere and
        /// "nothing happened" is a worse answer than "it went to the doorway you were nearest". It is
        /// not: with no limit the placement ghost teleports to whichever doorway is nearest anywhere in
        /// the area, so the preview is never under the cursor and the click lands somewhere the builder
        /// never pointed at. Bounded, the door rides the cursor and drops into a doorway when it reaches
        /// one, which is both what Aurora does and what can be seen happening.
        /// <para>
        /// Measured with <see cref="DoorAnchorHeightWeight"/> applied, so the doorway a storey up is far
        /// out of reach from the floor below rather than merely losing a tie-break to it.
        /// </para>
        /// </remarks>
        public const float DoorSnapRadius = AreaSceneBuilder.TileSize / 2f;

        /// <summary>
        /// How much a metre of height counts for against a metre across the floor when choosing a
        /// doorway.
        /// </summary>
        /// <remarks>
        /// Elevation has to count for something or a multi-storey area cannot be edited: doorways stack
        /// almost exactly above one another, so ignoring Z entirely meant a click upstairs took whichever
        /// lower-floor anchor happened to be marginally nearer in X/Y. It must not dominate either -
        /// weighting height equally would let a doorway on the same floor lose to a nearer one above it.
        /// A tile storey is 10m at this tileset's transition height, so a modest weight separates floors
        /// decisively while leaving same-floor choices to the floor distance that the builder is aiming
        /// with.
        /// </remarks>
        private const float DoorAnchorHeightWeight = 4f;

        /// <summary>
        /// How close a door has to be to a doorway to count as filling it, in metres.
        /// </summary>
        /// <remarks>
        /// Deliberately tight. A door hung in a doorway sits exactly on the node, so the only thing this
        /// absorbs is float drift through the tile transform; anything further off is a door standing
        /// elsewhere on the same tile - the corpus has those - and must not blank out a doorway that is
        /// really empty. Height is weighted in as it is for the snap, so the door in the doorway directly
        /// overhead does not fill the one below it.
        /// </remarks>
        public const float DoorwayFilledRadius = 0.25f;

        /// <summary>
        /// Whether a door already hangs in <paramref name="anchor"/>. <paramref name="ignore"/> excludes
        /// one instance from the answer, which is how a door being dragged does not count as filling the
        /// doorway it is being dragged out of.
        /// </summary>
        /// <remarks>
        /// A doorway holds one door. The tile owns the gap in the wall and the walkmesh cut through it,
        /// so a second leaf in the same frame is not a second door - it is two doors occupying one
        /// another, which the engine draws as z-fighting and the builder cannot select apart.
        /// </remarks>
        public bool IsDoorwayFilled(TileDoorAnchor anchor, InstanceMarker? ignore = null)
        {
            ArgumentNullException.ThrowIfNull(anchor);

            foreach (var instance in Instances)
            {
                if (instance.Kind != InstanceMarkerKind.Door || ReferenceEquals(instance, ignore))
                    continue;

                if (DoorAnchorDistanceSquared(instance.Position, anchor.Position)
                    <= DoorwayFilledRadius * DoorwayFilledRadius)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Whether this area has a doorway that no door is hanging in yet.</summary>
        public bool HasEmptyDoorway()
        {
            foreach (var anchor in DoorAnchors)
            {
                if (!IsDoorwayFilled(anchor))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// The empty doorway within <see cref="DoorSnapRadius"/> of a point, nearest first and preferring
        /// the same storey, or null when the point is near none, the ones in reach are already filled, or
        /// this area declares no doorways at all. <paramref name="ignore"/> is the door being moved, which
        /// must not be treated as filling the doorway it currently sits in.
        /// </summary>
        /// <remarks>
        /// Lives here rather than in the viewport because two paths need the same answer - the placement
        /// ghost, and moving a door that is already placed, which without it could detach a door from its
        /// tile frame and walkmesh opening.
        /// </remarks>
        public TileDoorAnchor? NearestEmptyDoorway(Vector3 groundPoint, InstanceMarker? ignore = null)
        {
            TileDoorAnchor? best = null;
            var bestDistance = DoorSnapRadius * DoorSnapRadius;

            foreach (var anchor in DoorAnchors)
            {
                var distance = DoorAnchorDistanceSquared(anchor.Position, groundPoint);
                if (distance > bestDistance)
                    continue;

                if (IsDoorwayFilled(anchor, ignore))
                    continue;

                bestDistance = distance;
                best = anchor;
            }

            return best;
        }

        /// <summary>
        /// How far apart two points are for the purpose of choosing a doorway: across the floor, plus
        /// height counted at <see cref="DoorAnchorHeightWeight"/>. Squared, since every caller compares.
        /// </summary>
        private static float DoorAnchorDistanceSquared(Vector3 a, Vector3 b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            var dz = (a.Z - b.Z) * DoorAnchorHeightWeight;
            return dx * dx + dy * dy + dz * dz;
        }

        /// <summary>
        /// This scene with <paramref name="existing"/> swapped for <paramref name="replacement"/>, or
        /// null when that marker is not in this scene (a stale selection from a superseded build).
        /// </summary>
        /// <remarks>
        /// The tile list is carried across <b>by reference</b>, not copied - that is what lets the
        /// renderer notice the grid did not change and keep its uploaded tile batches and walkmesh
        /// buffers rather than rebuilding them to move one object.
        /// </remarks>
        public AreaScene? WithInstanceReplaced(InstanceMarker existing, InstanceMarker replacement)
        {
            var index = -1;
            for (var i = 0; i < Instances.Count; i++)
            {
                if (ReferenceEquals(Instances[i], existing))
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
                return null;

            var instances = Instances.ToArray();
            instances[index] = replacement;

            return WithInstances(instances);
        }

        /// <summary>
        /// This scene with one newly placed instance added, leaving every area-wide render input shared.
        /// </summary>
        /// <remarks>
        /// Full scene assembly groups markers by kind. Insert into that same ordering so the cheap
        /// placement path produces the same list shape as <see cref="AreaSceneBuilder.Build"/>, while
        /// preserving the tile list by reference so the renderer does not rebuild its tile batches.
        /// </remarks>
        public AreaScene WithInstanceAdded(InstanceMarker instance)
        {
            ArgumentNullException.ThrowIfNull(instance);

            var insertAt = Instances.Count;
            for (var i = 0; i < Instances.Count; i++)
            {
                if (Instances[i].Kind > instance.Kind)
                {
                    insertAt = i;
                    break;
                }
            }

            var instances = new InstanceMarker[Instances.Count + 1];
            for (var i = 0; i < insertAt; i++)
                instances[i] = Instances[i];
            instances[insertAt] = instance;
            for (var i = insertAt; i < Instances.Count; i++)
                instances[i + 1] = Instances[i];

            return WithInstances(instances);
        }

        private AreaScene WithInstances(IReadOnlyList<InstanceMarker> instances)
        {
            return new AreaScene
            {
                Tileset = Tileset,
                Width = Width,
                Height = Height,
                Tiles = Tiles,
                Instances = instances,
                Diagnostics = Diagnostics,
                DoorAnchors = DoorAnchors,
                IsInteriorTileset = IsInteriorTileset,
                Lighting = Lighting
            };
        }

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
