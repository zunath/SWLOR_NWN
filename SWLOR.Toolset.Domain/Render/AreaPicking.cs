using System.Numerics;
using System.Runtime.CompilerServices;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Instance hit-testing for the area-view picking feature: given a world-space
    /// <see cref="PickRay"/> (see <see cref="AreaCameraMath.ScreenPointToRay"/>) and an
    /// <see cref="AreaScene"/>, finds the closest <see cref="InstanceMarker"/> the ray hits. No
    /// GL/UI dependency - <c>GlAreaControl</c> calls this from its click handler using the same
    /// view/projection it rendered with, and the same marker-vs-model display rule
    /// (<see cref="DrawsAsModel"/>) its own <c>DrawInstanceMarkers</c> uses, so picking always
    /// matches what is actually drawn.
    /// </summary>
    public static class AreaPicking
    {
        /// <summary>Mirrors GlAreaControl's private marker-pyramid constants (base half-width, height, ground offset) - duplicated here because the App project's control has no Domain-visible state to share, and this shape is otherwise a fixed rendering constant, not scene data.</summary>
        private const float MarkerHalfWidth = 0.4f;
        private const float MarkerHeight = 1.2f;
        private const float MarkerGroundOffset = 0.05f;

        private const float RayEpsilon = 1e-7f;

        private sealed class LocalBounds
        {
            public required Vector3 Min { get; init; }
            public required Vector3 Max { get; init; }
        }

        /// <summary>
        /// Per-<see cref="RenderMesh"/> local-space AABB cache. Keyed by reference (RenderModel/
        /// RenderMesh instances are shared/cached across every placement of the same model - see
        /// <see cref="TileModelCache"/> - so this table stays small even across a whole area) and
        /// weak so it never keeps a mesh alive purely for picking.
        /// </summary>
        private static readonly ConditionalWeakTable<RenderMesh, LocalBounds> LocalBoundsCache = new();

        /// <summary>True when <paramref name="instance"/> should be hit-tested (and drawn) as its resolved model rather than its kind-colored marker pyramid - must mirror GlAreaControl.DrawsAsModel exactly so picking always matches what is on screen.</summary>
        public static bool DrawsAsModel(InstanceMarker instance, bool showPlaceableModels) =>
            !instance.IsDoorTransition &&
            instance.Model is { Meshes.Count: > 0 } &&
            (showPlaceableModels || instance.Kind != InstanceMarkerKind.Placeable);

        /// <summary>
        /// The local-to-world transform for one instance marker: apply its optional visual
        /// scale/rotation/translation, rotate about Z by the (cos, sin) heading, then translate to
        /// the instance's position. Mirrors
        /// GlAreaControl.DrawInstanceMarkers' instanceTransform exactly (both passes there use this
        /// same formula) so picking always matches what is drawn.
        /// </summary>
        public static Matrix4x4 ComputeInstanceTransform(InstanceMarker instance)
        {
            var heading = MathF.Atan2(instance.Orientation.Y, instance.Orientation.X);
            return instance.VisualTransform *
                   Matrix4x4.CreateRotationZ(heading) *
                   Matrix4x4.CreateTranslation(instance.Position);
        }

        /// <summary>
        /// Finds the closest instance in <paramref name="scene"/> that <paramref name="ray"/> hits,
        /// or null when the ray hits nothing. Model-carrying instances (per <paramref name="showPlaceableModels"/>
        /// and <see cref="DrawsAsModel"/>) are tested per-mesh: an AABB reject first, then an exact
        /// Möller-Trumbore ray-triangle test against every triangle in that mesh - a hit requires an
        /// actual triangle hit, not just an AABB overlap. Marker instances are tested against the
        /// marker pyramid's AABB only (sufficient because the pyramid is small and its
        /// exact silhouette isn't worth the extra precision).
        /// </summary>
        public static InstanceMarker? PickClosestInstance(PickRay ray, AreaScene scene, bool showPlaceableModels)
        {
            ArgumentNullException.ThrowIfNull(scene);

            InstanceMarker? closest = null;
            var closestDistance = float.PositiveInfinity;

            foreach (var instance in scene.Instances)
            {
                var hitDistance = instance.IsDoorTransition
                    ? PickDoorTransition(ray, instance)
                    : DrawsAsModel(instance, showPlaceableModels)
                        ? PickModelInstance(ray, instance)
                        : PickMarkerInstance(ray, instance);

                if (hitDistance is { } distance && distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = instance;
                }
            }

            return closest;
        }

        /// <summary>
        /// Hit-tests <paramref name="ray"/> against a single <paramref name="instance"/> (rather
        /// than scanning a whole scene), using the same marker-vs-model rule
        /// (<paramref name="drawsAsModel"/>) <see cref="PickClosestInstance"/> and drawing already
        /// use. Used by the move/rotate gizmo (GlAreaControl) to check whether a press landed
        /// specifically on the current selection before starting a manipulation drag, without
        /// re-scanning every instance in the scene.
        /// </summary>
        public static float? PickInstance(PickRay ray, InstanceMarker instance, bool drawsAsModel) =>
            instance.IsDoorTransition
                ? PickDoorTransition(ray, instance)
                : drawsAsModel
                    ? PickModelInstance(ray, instance)
                    : PickMarkerInstance(ray, instance);

        private static float? PickDoorTransition(PickRay ray, InstanceMarker instance)
        {
            if (instance.Model is { Meshes.Count: > 0 })
                return PickModelInstance(ray, instance);

            var bounds = ComputeDoorTransitionWorldBounds(instance);
            return RayAabbIntersect(ray, bounds.Min, bounds.Max);
        }

        /// <summary>World-space AABB of one instance's transformed marker pyramid.</summary>
        public static (Vector3 Min, Vector3 Max) ComputeMarkerWorldBounds(InstanceMarker instance)
        {
            var min = new Vector3(-MarkerHalfWidth, -MarkerHalfWidth, MarkerGroundOffset);
            var max = new Vector3(MarkerHalfWidth, MarkerHalfWidth, MarkerGroundOffset + MarkerHeight);
            return TransformAabb(min, max, ComputeInstanceTransform(instance));
        }

        /// <summary>Merged world-space AABB across every mesh of an instance's resolved model, or null when it has none.</summary>
        public static (Vector3 Min, Vector3 Max)? ComputeModelWorldBounds(InstanceMarker instance)
        {
            if (instance.Model == null || instance.Model.Meshes.Count == 0)
                return null;

            var instanceTransform = ComputeInstanceTransform(instance);
            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);
            var any = false;

            foreach (var mesh in instance.Model.Meshes)
            {
                var local = GetLocalAabb(mesh);
                var meshTransform = mesh.Transform * instanceTransform;
                var (meshMin, meshMax) = TransformAabb(local.Min, local.Max, meshTransform);
                min = Vector3.Min(min, meshMin);
                max = Vector3.Max(max, meshMax);
                any = true;
            }

            return any ? (min, max) : null;
        }

        /// <summary>
        /// World bounds of the translucent transition representation: its authored hidden-model
        /// geometry when available, otherwise the standard two-by-three-metre doorway plane.
        /// </summary>
        public static (Vector3 Min, Vector3 Max) ComputeDoorTransitionWorldBounds(InstanceMarker instance)
        {
            if (ComputeModelWorldBounds(instance) is { } modelBounds)
                return modelBounds;

            return TransformAabb(
                DoorTransitionMarker.LocalMinimum,
                DoorTransitionMarker.LocalMaximum,
                ComputeInstanceTransform(instance));
        }

        /// <summary>
        /// World-space AABB for an instance as it is currently displayed: its merged model bounds
        /// when <paramref name="drawsAsModel"/> (falling back to the marker bounds if it somehow has
        /// no mesh), otherwise the marker pyramid's bounds. Used by GlAreaControl to draw a
        /// selection-highlight wireframe box that always matches what picking/drawing used.
        /// </summary>
        public static (Vector3 Min, Vector3 Max) ComputeInstanceWorldBounds(InstanceMarker instance, bool drawsAsModel)
        {
            if (instance.IsDoorTransition)
                return ComputeDoorTransitionWorldBounds(instance);

            if (drawsAsModel && ComputeModelWorldBounds(instance) is { } modelBounds)
                return modelBounds;

            return ComputeMarkerWorldBounds(instance);
        }

        private static float? PickMarkerInstance(PickRay ray, InstanceMarker instance)
        {
            // A trigger's outline is what is drawn and what a builder aims at. Without this the only
            // clickable part is the 0.8m anchor pyramid, which for the 306 checked-in trigger polygons
            // wider than 10m - some over 200m - means hunting for a speck or giving up and using the
            // instance list.
            var polygon = PickPolygon(ray, instance);
            var marker = RayAabbIntersect(ray, ComputeMarkerWorldBounds(instance).Min,
                ComputeMarkerWorldBounds(instance).Max);

            return polygon == null ? marker
                : marker == null ? polygon
                : Math.Min(polygon.Value, marker.Value);
        }

        /// <summary>
        /// Hit-tests the instance's drawn geometry polygon, as a fan of triangles about its first point.
        /// Null when it has none. The polygon is flat, so a fan is exact for the convex case and close
        /// enough for the concave ones a builder still expects to be able to click.
        /// </summary>
        private static float? PickPolygon(PickRay ray, InstanceMarker instance)
        {
            var points = instance.Geometry;
            if (points == null || points.Count < 3)
                return null;

            float? closest = null;
            for (var i = 1; i + 1 < points.Count; i++)
            {
                var hit = RayTriangleIntersect(ray, points[0], points[i], points[i + 1]);
                if (hit is { } distance && (closest == null || distance < closest))
                    closest = distance;
            }

            return closest;
        }

        private static float? PickModelInstance(PickRay ray, InstanceMarker instance)
        {
            if (instance.Model == null)
                return null;

            var instanceTransform = ComputeInstanceTransform(instance);
            float? closest = null;

            foreach (var mesh in instance.Model.Meshes)
            {
                var meshTransform = mesh.Transform * instanceTransform;
                var local = GetLocalAabb(mesh);
                var (worldMin, worldMax) = TransformAabb(local.Min, local.Max, meshTransform);

                if (RayAabbIntersect(ray, worldMin, worldMax) == null)
                    continue; // Cheap reject before paying for per-triangle tests.

                var hit = RayIntersectsMeshTriangles(ray, mesh, meshTransform);
                if (hit is { } t && (closest == null || t < closest))
                    closest = t;
            }

            return closest;
        }

        private static LocalBounds GetLocalAabb(RenderMesh mesh)
        {
            if (LocalBoundsCache.TryGetValue(mesh, out var cached))
                return cached;

            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);

            for (var i = 0; i < mesh.VertexCount; i++)
            {
                var p = new Vector3(mesh.Positions[i * 3], mesh.Positions[i * 3 + 1], mesh.Positions[i * 3 + 2]);
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }

            if (mesh.VertexCount == 0)
            {
                min = Vector3.Zero;
                max = Vector3.Zero;
            }

            var bounds = new LocalBounds { Min = min, Max = max };
            LocalBoundsCache.Add(mesh, bounds);
            return bounds;
        }

        /// <summary>Transforms an axis-aligned box's 8 corners and takes the componentwise min/max of the results - a safe (if not perfectly tight) superset AABB under rotation, adequate for a cheap pre-triangle reject.</summary>
        private static (Vector3 Min, Vector3 Max) TransformAabb(Vector3 min, Vector3 max, Matrix4x4 transform)
        {
            Span<Vector3> corners = stackalloc Vector3[8]
            {
                new(min.X, min.Y, min.Z), new(max.X, min.Y, min.Z),
                new(min.X, max.Y, min.Z), new(max.X, max.Y, min.Z),
                new(min.X, min.Y, max.Z), new(max.X, min.Y, max.Z),
                new(min.X, max.Y, max.Z), new(max.X, max.Y, max.Z)
            };

            var worldMin = new Vector3(float.MaxValue);
            var worldMax = new Vector3(float.MinValue);

            foreach (var corner in corners)
            {
                var world = Vector3.Transform(corner, transform);
                worldMin = Vector3.Min(worldMin, world);
                worldMax = Vector3.Max(worldMax, world);
            }

            return (worldMin, worldMax);
        }

        /// <summary>Standard slab-method ray/AABB intersection. Returns the entry distance along the ray (clamped to 0 when the ray origin is already inside the box), or null for a miss/behind-the-ray box.</summary>
        internal static float? RayAabbIntersect(PickRay ray, Vector3 min, Vector3 max)
        {
            var tMin = float.NegativeInfinity;
            var tMax = float.PositiveInfinity;

            for (var axis = 0; axis < 3; axis++)
            {
                var origin = Component(ray.Origin, axis);
                var direction = Component(ray.Direction, axis);
                var lo = Component(min, axis);
                var hi = Component(max, axis);

                if (MathF.Abs(direction) < RayEpsilon)
                {
                    if (origin < lo || origin > hi)
                        return null; // Ray is parallel to this axis' slab and starts outside it.

                    continue;
                }

                var t1 = (lo - origin) / direction;
                var t2 = (hi - origin) / direction;
                if (t1 > t2)
                    (t1, t2) = (t2, t1);

                tMin = MathF.Max(tMin, t1);
                tMax = MathF.Min(tMax, t2);

                if (tMin > tMax)
                    return null;
            }

            return tMax < 0f ? null : MathF.Max(tMin, 0f);
        }

        private static float Component(Vector3 v, int axis) => axis switch { 0 => v.X, 1 => v.Y, _ => v.Z };

        private static float? RayIntersectsMeshTriangles(PickRay ray, RenderMesh mesh, Matrix4x4 meshTransform)
        {
            float? closest = null;
            var indices = mesh.Indices;

            for (var i = 0; i + 2 < indices.Length; i += 3)
            {
                var v0 = TransformedVertex(mesh, indices[i], meshTransform);
                var v1 = TransformedVertex(mesh, indices[i + 1], meshTransform);
                var v2 = TransformedVertex(mesh, indices[i + 2], meshTransform);

                var hit = RayTriangleIntersect(ray, v0, v1, v2);
                if (hit is { } t && (closest == null || t < closest))
                    closest = t;
            }

            return closest;
        }

        private static Vector3 TransformedVertex(RenderMesh mesh, int index, Matrix4x4 transform)
        {
            var local = new Vector3(
                mesh.Positions[index * 3], mesh.Positions[index * 3 + 1], mesh.Positions[index * 3 + 2]);
            return Vector3.Transform(local, transform);
        }

        /// <summary>
        /// Möller-Trumbore ray-triangle intersection. Two-sided (no backface cull) - NWN tile/prop
        /// meshes have inconsistent winding (see GlAreaControl's two-sided lighting comment), so a
        /// pick must hit a triangle regardless of which way it faces. Returns the hit distance along
        /// the ray, or null for a miss (parallel, outside the triangle, or behind the ray origin).
        /// </summary>
        private static float? RayTriangleIntersect(PickRay ray, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            var edge1 = v1 - v0;
            var edge2 = v2 - v0;
            var pVec = Vector3.Cross(ray.Direction, edge2);
            var det = Vector3.Dot(edge1, pVec);

            if (MathF.Abs(det) < RayEpsilon)
                return null; // Ray is parallel to the triangle's plane.

            var invDet = 1f / det;
            var tVec = ray.Origin - v0;
            var u = Vector3.Dot(tVec, pVec) * invDet;
            if (u < 0f || u > 1f)
                return null;

            var qVec = Vector3.Cross(tVec, edge1);
            var v = Vector3.Dot(ray.Direction, qVec) * invDet;
            if (v < 0f || u + v > 1f)
                return null;

            var t = Vector3.Dot(edge2, qVec) * invDet;
            return t > RayEpsilon ? t : null;
        }
    }
}
