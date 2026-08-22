using System.Numerics;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Ground-height raycast for the walkmesh feature: given a world-space <see cref="PickRay"/>
    /// and an <see cref="AreaScene"/>, finds where the ray hits a tile's real walkmesh floor
    /// instead of the Z=0 ground-plane heuristic <see cref="AreaManipulation.IntersectRayWithHorizontalPlane"/>
    /// uses. No GL/UI dependency - the app's placement-click handler calls this to snap a new
    /// instance's Z to the actual floor height under the cursor.
    /// </summary>
    public static class AreaWalkmesh
    {
        private const float RayEpsilon = 1e-7f;

        /// <summary>
        /// Finds the closest world-space point where <paramref name="ray"/> hits a walkmesh face
        /// across every tile in <paramref name="scene"/> that has one (tiles with no resolvable
        /// <see cref="TilePlacement.Walkmesh"/> - no <see cref="TileWalkmeshCache"/> supplied at
        /// build time, or an unresolvable/unparseable .wok - are skipped, not treated as a miss
        /// for the whole scene). Each face's local-space vertices are transformed by its owning
        /// tile's <see cref="TilePlacement.Transform"/> (the same transform that places the
        /// tile's rendered model - walkmesh and model share the exact same local frame) before a
        /// two-sided ray-triangle test.
        ///
        /// <para>
        /// When <paramref name="preferWalkable"/> is true (the default), the closest hit among
        /// <see cref="WalkFace.Walkable"/> faces wins; if no walkable face is hit but some
        /// non-walkable face is, that closest hit is returned instead. When false, the closest
        /// hit among every face (walkable or not) wins. Returns null when the ray hits no
        /// walkmesh face at all. Never throws for a null <paramref name="scene"/> (returns null).
        /// </para>
        /// </summary>
        public static Vector3? RaycastGround(PickRay ray, AreaScene scene, bool preferWalkable = true)
        {
            if (scene == null)
                return null;

            float? closestWalkable = null;
            float? closestAny = null;

            foreach (var tile in scene.Tiles)
            {
                var mesh = tile.Walkmesh;
                if (mesh == null)
                    continue;

                var vertices = mesh.Vertices;

                foreach (var face in mesh.Faces)
                {
                    if (face.A < 0 || face.B < 0 || face.C < 0 ||
                        face.A >= vertices.Count || face.B >= vertices.Count || face.C >= vertices.Count)
                    {
                        continue; // Defensive: an out-of-range face index must not crash a raycast.
                    }

                    var v0 = Vector3.Transform(vertices[face.A], tile.Transform);
                    var v1 = Vector3.Transform(vertices[face.B], tile.Transform);
                    var v2 = Vector3.Transform(vertices[face.C], tile.Transform);

                    if (RayTriangleIntersect(ray, v0, v1, v2) is not { } t)
                        continue;

                    if (closestAny == null || t < closestAny)
                        closestAny = t;

                    if (face.Walkable && (closestWalkable == null || t < closestWalkable))
                        closestWalkable = t;
                }
            }

            var chosen = preferWalkable ? closestWalkable ?? closestAny : closestAny;
            return chosen is { } finalT ? ray.Origin + ray.Direction * finalT : null;
        }

        /// <summary>
        /// The walkmesh floor height under a world-space (x, y) point, or null when no tile's
        /// walkmesh covers it. The topmost walkable face wins; if only non-walkable faces cover the
        /// point (a wall top, a pit lining), the topmost of those is returned instead.
        /// </summary>
        /// <remarks>
        /// This is how Aurora places a trigger's outline: the stored per-vertex PointZ values in a
        /// .git are whatever height the camera-ground intersection happened to be when the builder
        /// clicked each vertex, and the corpus is full of polygons mixing floor heights that the
        /// reference toolset nevertheless draws flat on the floor - it drapes every vertex onto the
        /// walkmesh and ignores the stored Z. Only the tiles whose 10m cell contains the point are
        /// tested, so draping a whole area's triggers stays cheap.
        /// </remarks>
        public static float? GroundHeightAt(IReadOnlyList<TilePlacement> tiles, float x, float y)
        {
            if (tiles == null)
                return null;

            float? topWalkable = null;
            float? topAny = null;

            foreach (var tile in tiles)
            {
                var mesh = tile.Walkmesh;
                if (mesh == null)
                    continue;

                // Tile cells are axis-aligned 10m squares about (CenterX, CenterY); a face never
                // reaches outside its own tile, so anything further than half a tile (plus a hair
                // of float slack for a point exactly on a seam) cannot contain the point.
                const float half = AreaSceneBuilder.TileSize / 2f + 0.01f;
                if (MathF.Abs(x - tile.CenterX) > half || MathF.Abs(y - tile.CenterY) > half)
                    continue;

                var vertices = mesh.Vertices;
                foreach (var face in mesh.Faces)
                {
                    if (face.A < 0 || face.B < 0 || face.C < 0 ||
                        face.A >= vertices.Count || face.B >= vertices.Count || face.C >= vertices.Count)
                    {
                        continue;
                    }

                    var v0 = Vector3.Transform(vertices[face.A], tile.Transform);
                    var v1 = Vector3.Transform(vertices[face.B], tile.Transform);
                    var v2 = Vector3.Transform(vertices[face.C], tile.Transform);

                    if (TriangleHeightAt(v0, v1, v2, x, y) is not { } z)
                        continue;

                    if (topAny == null || z > topAny)
                        topAny = z;

                    if (face.Walkable && (topWalkable == null || z > topWalkable))
                        topWalkable = z;
                }
            }

            return topWalkable ?? topAny;
        }

        /// <summary>
        /// The Z of a triangle's plane at (x, y) when the point lies inside the triangle's XY
        /// projection (barycentric test), or null when it lies outside or the triangle is degenerate
        /// in XY (a vertical wall face, which has no height "under" a point).
        /// </summary>
        private static float? TriangleHeightAt(Vector3 v0, Vector3 v1, Vector3 v2, float x, float y)
        {
            var d = (v1.Y - v2.Y) * (v0.X - v2.X) + (v2.X - v1.X) * (v0.Y - v2.Y);
            if (MathF.Abs(d) < RayEpsilon)
                return null;

            var a = ((v1.Y - v2.Y) * (x - v2.X) + (v2.X - v1.X) * (y - v2.Y)) / d;
            var b = ((v2.Y - v0.Y) * (x - v2.X) + (v0.X - v2.X) * (y - v2.Y)) / d;
            var c = 1f - a - b;

            const float slack = -0.0001f; // a vertex exactly on a face edge must still drape
            if (a < slack || b < slack || c < slack)
                return null;

            return a * v0.Z + b * v1.Z + c * v2.Z;
        }

        /// <summary>
        /// Möller-Trumbore ray-triangle intersection, two-sided (no backface cull) - mirrors
        /// <c>AreaPicking</c>'s private test exactly (small duplication is precedented there: that
        /// method is private to its own hit-testing concern, and walkmesh faces have the same
        /// inconsistent winding as tile/prop meshes). Returns the hit distance along the ray, or
        /// null for a miss (parallel, outside the triangle, or behind the ray origin).
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
