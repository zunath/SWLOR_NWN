using System.Numerics;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;

namespace SWLOR.Toolset.Domain.AreaGeneration.Decoration;

/// <summary>
/// Final, deterministic clearance pass shared by preview and module creation. Footprints are
/// conservative circles, not model walkmeshes. Open tile surfaces, stamped geometry, chasm
/// quadrants and reserved routes constrain every ground prop, regardless of arrangement mechanism.
/// </summary>
public static class DecorationPlacementSafety
{
    private static readonly (int X, int Y, int Edge)[] Directions =
    [ (1, 0, EdgeSlot.Right), (0, 1, EdgeSlot.Top), (-1, 0, EdgeSlot.Left), (0, -1, EdgeSlot.Bottom) ];

    public static DecorationPlacementReport Apply(
        List<PlannedDecoration> plan, ResolvedLayout layout, DungeonTilesetProfile profile,
        DungeonDetail content, DecorationPlacementStyle style, TilesetModel? tileset = null)
    {
        if (!Enum.IsDefined(style))
            throw new ArgumentOutOfRangeException(nameof(style));

        var proposed = plan.Count;
        var routeRadius = RouteRadius(style);
        var spacing = style == DecorationPlacementStyle.Spacious ? 0.4f : 0.1f;
        var surface = BuildOpenSurface(layout);
        var routes = BuildRoutes(layout, surface, profile.RoadCrosser);
        // Lawn features explicitly declare that their flat surface accepts planted ensembles.
        // Other feature art (fountains, rocks, buildings) is not inferred to be usable floor.
        var propSurface = new HashSet<(int X, int Y)>(surface);
        foreach (var feature in layout.FeatureTileCells)
            if (profile.FeatureTileDressings.TryGetValue(feature.Value, out var dressing) && dressing == FeatureZoneDressing.Lawn)
                propSurface.Add(feature.Key);
        var reserved = layout.Rooms.Where(room => !room.IsSetPiece)
            .Select(room => (Point: Center(room.CenterTile), Radius: room.Role == RoomRole.Boss
                ? MathF.Max(1f, content.TreasurePlaceableFootprintRadius) : 1f)).ToList();
        foreach (var transition in layout.Transitions)
        {
            reserved.Add((transition.Style == TransitionStyle.Placeable
                    ? Center(transition.Tile) : new Vector2(transition.DoorX, transition.DoorY),
                transition.Style == TransitionStyle.Placeable
                    ? content.ExitPlaceableFootprintRadius : content.ExitDoorFootprintRadius));
        }

        var accepted = new List<PlannedDecoration>();
        var acceptedSet = new HashSet<PlannedDecoration>();
        var unsupported = 0;
        var routeConflicts = 0;
        var overlaps = 0;
        // Preserve planner order. Singles must not accidentally become one giant group (id 0).
        var groups = plan.Select((prop, index) => (prop, key: prop.ArrangementId == 0 ? -index - 1 : prop.ArrangementId))
            .GroupBy(item => item.key).Select(group => group.Select(item => item.prop).ToList());
        foreach (var group in groups)
        {
            var pending = new List<PlannedDecoration>();
            var reason = 0;
            foreach (var prop in group)
            {
                var point = new Vector2(prop.Position.X, prop.Position.Y);
                var radius = prop.FootprintRadius * prop.VisualScale;
                if (!float.IsFinite(point.X) || !float.IsFinite(point.Y) ||
                    !float.IsFinite(prop.Position.Z) || !float.IsFinite(prop.Facing) ||
                    !float.IsFinite(radius) || radius <= 0 || !float.IsFinite(prop.VisualScale) || prop.VisualScale <= 0)
                {
                    reason = 1;
                    break;
                }

                if (prop.SupportDecoration != null)
                {
                    if (!acceptedSet.Contains(prop.SupportDecoration))
                    {
                        reason = 1;
                        break;
                    }
                    pending.Add(prop);
                    continue;
                }

                // Structural frontages have their own support envelope, including deliberate
                // overhang; mounts attach to those faces. Never pretend their centers are floor.
                if (prop.Context is DecorationContext.BuildingFrontage or DecorationContext.FacadeMount)
                {
                    pending.Add(prop);
                    continue;
                }

                if (!HasSupport(point, radius, propSurface, layout, profile) ||
                    prop.BlocksMovement && tileset != null && !HasLevelSupport(point, radius, layout, tileset))
                {
                    reason = 1;
                    break;
                }
                if (prop.BlocksMovement &&
                    (reserved.Any(anchor => Vector2.DistanceSquared(point, anchor.Point) <
                        Square(radius + anchor.Radius + spacing)) ||
                     routes.Any(route => DistanceToSegmentSquared(point, route.Start, route.End) < Square(radius + routeRadius))))
                {
                    reason = 2;
                    break;
                }
                if (prop.BlocksMovement && accepted.Concat(pending).Any(other =>
                        other.BlocksMovement && other.SupportDecoration == null &&
                        (other.FootprintBounds is { } bounds
                            ? bounds.IntersectsCircle(point.X, point.Y, radius + spacing)
                            : Vector2.DistanceSquared(point, new Vector2(other.Position.X, other.Position.Y)) <
                              Square(radius + other.FootprintRadius * other.VisualScale + spacing))))
                {
                    reason = 3;
                    break;
                }
                pending.Add(prop);
            }

            if (reason != 0)
            {
                if (reason == 1) unsupported += group.Count;
                if (reason == 2) routeConflicts += group.Count;
                if (reason == 3) overlaps += group.Count;
                continue;
            }
            accepted.AddRange(pending);
            acceptedSet.UnionWith(pending);
        }

        // A rejected pile must not leave an isolated under-pile stain behind.
        var orphanDecals = accepted.Where(prop => prop.Context == DecorationContext.GroundDecal &&
            !accepted.Any(other => other.BlocksMovement && other.SupportDecoration == null &&
                other.Context != DecorationContext.BuildingFrontage &&
                Vector2.DistanceSquared(new(prop.Position.X, prop.Position.Y), new(other.Position.X, other.Position.Y)) <
                Square(prop.FootprintRadius * prop.VisualScale + other.FootprintRadius * other.VisualScale))).ToHashSet();
        unsupported += orphanDecals.Count;
        plan.Clear();
        plan.AddRange(accepted.Where(prop => !orphanDecals.Contains(prop)));
        return new(proposed, plan.Count, unsupported, routeConflicts, overlaps);
    }

    internal static bool HasLevelSupport(Vector2 point, float radius, ResolvedLayout layout, TilesetModel tileset)
    {
        if (layout.HeightTransition <= 0) return true;
        var min = ResolvedGround.HeightAt(layout, tileset, point.X, point.Y);
        var max = min;
        // Rigid props cannot conform to steep slopes or straddle a raised tile seam. Use the same
        // ground-height estimate as document creation; floor paint and structural overhangs differ.
        for (var index = 0; index < 8; index++)
        {
            var angle = index * MathF.PI / 4;
            var height = ResolvedGround.HeightAt(layout, tileset,
                point.X + radius * MathF.Cos(angle), point.Y + radius * MathF.Sin(angle));
            min = MathF.Min(min, height);
            max = MathF.Max(max, height);
        }
        return max - min <= 0.5f;
    }

    internal static HashSet<(int X, int Y)> BuildOpenSurface(ResolvedLayout layout)
    {
        var surface = layout.Rooms.Where(room => !room.IsSetPiece).SelectMany(room => room.Tiles).ToHashSet();
        if (layout.CornerTerrains != null)
        {
            bool Open(string label) => !string.IsNullOrEmpty(label) &&
                (label.Equals(layout.OpenTerrain, StringComparison.OrdinalIgnoreCase) ||
                 label.Equals(layout.SecondaryOpenTerrain, StringComparison.OrdinalIgnoreCase));
            for (var y = 0; y < layout.Height; y++)
            for (var x = 0; x < layout.Width; x++)
            {
                if (Open(layout.CornerTerrains.Labels[x, y]) && Open(layout.CornerTerrains.Labels[x + 1, y]) &&
                    Open(layout.CornerTerrains.Labels[x, y + 1]) && Open(layout.CornerTerrains.Labels[x + 1, y + 1]))
                    surface.Add((x, y));
            }
        }
        surface.ExceptWith(layout.StampedStructureTiles);
        surface.ExceptWith(layout.PlaceableStructureCells);
        surface.ExceptWith(layout.FeatureTileCells.Keys);
        surface.RemoveWhere(tile => tile.X < 0 || tile.Y < 0 || tile.X >= layout.Width || tile.Y >= layout.Height);
        return surface;
    }

    internal static bool HasSupport(Vector2 point, float radius, HashSet<(int X, int Y)> surface,
        ResolvedLayout layout, DungeonTilesetProfile profile)
    {
        if (point.X - radius < 0 || point.Y - radius < 0 ||
            point.X + radius > layout.Width * 10f || point.Y + radius > layout.Height * 10f)
            return false;
        for (var y = (int)MathF.Floor((point.Y - radius) / 10f); y <= (int)MathF.Floor((point.Y + radius) / 10f); y++)
        for (var x = (int)MathF.Floor((point.X - radius) / 10f); x <= (int)MathF.Floor((point.X + radius) / 10f); x++)
        {
            if (!surface.Contains((x, y)) && Intersects(point, radius, x * 10f, y * 10f, 10f))
                return false;
        }
        if (profile.ChasmTerrains.Count == 0)
            return true;
        if (layout.CornerTerrains == null)
            return false;
        // Check every intersected corner quadrant, including slivers between sample points.
        for (var y = Math.Max(0, (int)MathF.Floor((point.Y - radius + 5) / 10)); y <= Math.Min(layout.Height, (int)MathF.Floor((point.Y + radius + 5) / 10)); y++)
        for (var x = Math.Max(0, (int)MathF.Floor((point.X - radius + 5) / 10)); x <= Math.Min(layout.Width, (int)MathF.Floor((point.X + radius + 5) / 10)); x++)
        {
            var label = layout.CornerTerrains.Labels[x, y];
            if ((string.IsNullOrEmpty(label) || profile.ChasmTerrains.Contains(label, StringComparer.OrdinalIgnoreCase)) &&
                Intersects(point, radius, x * 10f - 5, y * 10f - 5, 10))
                return false;
        }
        return true;
    }

    /// <summary>Connect room hubs to every usable perimeter opening, including tunnel mouths.
    /// Paths follow room tiles via BFS; a straight hub-to-door line can cut through concave walls.</summary>
    internal static List<(Vector2 Start, Vector2 End)> BuildRoutes(
        ResolvedLayout layout, HashSet<(int X, int Y)> surface, string roadCrosser)
    {
        var routes = new HashSet<(Vector2 Start, Vector2 End)>();
        foreach (var room in layout.Rooms.Where(room => !room.IsSetPiece && room.Tiles.Count > 0))
        {
            var tiles = room.Tiles.Where(surface.Contains).ToHashSet();
            if (tiles.Count == 0) continue;
            var root = tiles.Contains(room.CenterTile) ? room.CenterTile : tiles.OrderBy(t => t.Y).ThenBy(t => t.X).First();
            var parents = new Dictionary<(int X, int Y), (int X, int Y)> { [root] = root };
            var queue = new Queue<(int X, int Y)>();
            queue.Enqueue(root);
            while (queue.TryDequeue(out var tile))
            {
                foreach (var direction in Directions)
                {
                    var neighbor = (tile.X + direction.X, tile.Y + direction.Y);
                    if (tiles.Contains(neighbor) && parents.TryAdd(neighbor, tile)) queue.Enqueue(neighbor);
                }
            }
            void Connect((int X, int Y) tile, Vector2 end)
            {
                routes.Add((Center(tile), end));
                while (parents.TryGetValue(tile, out var parent) && parent != tile)
                {
                    routes.Add((Center(tile), Center(parent)));
                    tile = parent;
                }
            }
            foreach (var tile in tiles)
            foreach (var direction in Directions)
            {
                var neighbor = (tile.X + direction.X, tile.Y + direction.Y);
                var edge = layout.Crossers?.GetEdge(tile.X, tile.Y, direction.Edge) ?? string.Empty;
                // Fence edges are barriers, not doorways. Named custom corridor crossers may vary.
                var passage = !string.IsNullOrEmpty(edge) && !edge.Contains("fence", StringComparison.OrdinalIgnoreCase);
                if (!tiles.Contains(neighbor) && (surface.Contains(neighbor) || passage))
                    Connect(tile, Center(tile) + new Vector2(direction.X * 5, direction.Y * 5));
            }
            foreach (var transition in layout.Transitions.Where(t => t.RoomId == room.Id && tiles.Contains(t.Tile)))
                Connect(transition.Tile, transition.Style == TransitionStyle.Placeable
                    ? Center(transition.Tile) : new Vector2(transition.DoorX, transition.DoorY));
        }
        if (!string.IsNullOrEmpty(roadCrosser) && layout.Crossers != null)
        {
            for (var y = 0; y < layout.Height; y++)
            for (var x = 0; x < layout.Width; x++)
            foreach (var direction in Directions)
            {
                if (string.Equals(layout.Crossers.GetEdge(x, y, direction.Edge), roadCrosser, StringComparison.OrdinalIgnoreCase))
                    routes.Add((Center((x, y)), Center((x, y)) + new Vector2(direction.X * 5, direction.Y * 5)));
            }
        }
        return routes.ToList();
    }

    private static Vector2 Center((int X, int Y) tile) => new(tile.X * 10 + 5, tile.Y * 10 + 5);
    internal static float RouteRadius(DecorationPlacementStyle style) => style == DecorationPlacementStyle.Spacious ? 1.25f : 0.6f;
    private static float Square(float value) => value * value;
    private static bool Intersects(Vector2 point, float radius, float x, float y, float size) =>
        Vector2.DistanceSquared(point, new(Math.Clamp(point.X, x, x + size), Math.Clamp(point.Y, y, y + size))) < Square(radius) - 0.0001f;
    private static float DistanceToSegmentSquared(Vector2 point, Vector2 start, Vector2 end)
    {
        var delta = end - start;
        var fraction = delta.LengthSquared() == 0 ? 0 : Math.Clamp(Vector2.Dot(point - start, delta) / delta.LengthSquared(), 0, 1);
        return Vector2.DistanceSquared(point, start + delta * fraction);
    }
}
