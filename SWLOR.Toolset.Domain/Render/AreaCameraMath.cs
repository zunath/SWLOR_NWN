using System.Numerics;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// A world-space ray (origin + normalized direction) produced by unprojecting a screen point,
    /// consumed by <see cref="AreaPicking"/> for instance hit-testing (WP5.1).
    /// </summary>
    public readonly struct PickRay
    {
        public PickRay(Vector3 origin, Vector3 direction)
        {
            Origin = origin;
            Direction = direction;
        }

        public Vector3 Origin { get; }

        /// <summary>Normalized (unit-length) ray direction, except in the degenerate near==far case, where it falls back to +X.</summary>
        public Vector3 Direction { get; }
    }

    /// <summary>
    /// Pure orbit-camera math for the WP4.5 area viewport: initial framing from an area's tile-grid
    /// bounds, orbit/pan/zoom invariants, and the resulting eye offset. No GL/UI dependency - the
    /// GL control (<c>SWLOR.Toolset\Viewport\GlAreaControl.cs</c>) owns the mutable camera state
    /// (target/azimuth/elevation/distance) and calls these functions each time it changes; keeping
    /// the math itself stateless makes it unit-testable without a live GL context.
    /// </summary>
    public static class AreaCameraMath
    {
        /// <summary>Default orbit elevation above the ground plane for a freshly-framed area (~30 degrees).</summary>
        public const float DefaultElevationRadians = 0.523599f;

        /// <summary>Lowest orbit elevation allowed - never let the eye reach the ground plane itself (a degenerate LookAt with the up vector).</summary>
        public const float MinElevationRadians = 0.05f;

        /// <summary>Highest orbit elevation allowed - never quite straight overhead (azimuth becomes degenerate at the pole).</summary>
        public const float MaxElevationRadians = 1.5208f; // Ï€/2 - 0.05

        /// <summary>Nearest the camera may zoom in to its orbit target.</summary>
        public const float MinDistance = 1f;

        /// <summary>Farthest the camera may zoom out, expressed as a multiple of the scene's initial framing distance.</summary>
        public const float MaxDistanceMultiplier = 20f;

        /// <summary>Fractional slack added around the fitted area footprint so it doesn't touch the frustum edges.</summary>
        private const float FramingSlack = 1.25f;

        /// <summary>
        /// Initial camera framing for a freshly-opened area view: the target is the ground-plane
        /// (Z=0) center of the area's tile-grid bounds (<paramref name="areaWidthTiles"/> *
        /// <paramref name="areaHeightTiles"/> tiles, each <paramref name="tileSize"/> meters); the
        /// distance is picked so the whole footprint fits within <paramref name="verticalFovRadians"/>
        /// at <see cref="DefaultElevationRadians"/>, accounting for <paramref name="aspectRatio"/>
        /// (viewport width/height) so a narrow or wide viewport doesn't clip the shorter axis.
        /// </summary>
        public static (Vector3 Target, float Distance) ComputeInitialFraming(
            int areaWidthTiles,
            int areaHeightTiles,
            float tileSize,
            float verticalFovRadians,
            float aspectRatio)
        {
            var worldWidth = MathF.Max(areaWidthTiles, 0) * tileSize;
            var worldHeight = MathF.Max(areaHeightTiles, 0) * tileSize;
            var target = new Vector3(worldWidth / 2f, worldHeight / 2f, 0f);

            // Half-extents that must remain inside the frustum at the target's depth.
            var halfVertical = MathF.Max(worldHeight / 2f, 0.001f);
            var halfHorizontal = MathF.Max(worldWidth / 2f, 0.001f);

            var halfFovTanVertical = MathF.Tan(verticalFovRadians / 2f);
            var halfFovTanHorizontal = halfFovTanVertical * MathF.Max(aspectRatio, 0.001f);

            var distanceForVertical = halfVertical / halfFovTanVertical;
            var distanceForHorizontal = halfHorizontal / halfFovTanHorizontal;

            var distance = MathF.Max(distanceForVertical, distanceForHorizontal);

            // Slack keeps the footprint off the frustum edges; dividing by cos(elevation) keeps
            // the same ground footprint visible once the camera tilts up from directly overhead.
            distance = distance * FramingSlack / MathF.Cos(DefaultElevationRadians);

            return (target, MathF.Max(distance, MinDistance));
        }

        /// <summary>Clamps an orbit elevation angle to the allowed range (never flat, never straight overhead).</summary>
        public static float ClampElevation(float elevationRadians) =>
            Math.Clamp(elevationRadians, MinElevationRadians, MaxElevationRadians);

        /// <summary>Clamps an orbit distance to [<see cref="MinDistance"/>, initialDistance * <see cref="MaxDistanceMultiplier"/>].</summary>
        public static float ClampDistance(float distance, float initialDistance) =>
            Math.Clamp(distance, MinDistance, MathF.Max(initialDistance, MinDistance) * MaxDistanceMultiplier);

        /// <summary>
        /// Eye position relative to the orbit target for the given spherical orbit angles/distance.
        /// Z-up (matching NWN/Aurora world axes): azimuth 0 places the eye along +X from the
        /// target; increasing azimuth orbits counter-clockwise viewed from above +Z, matching
        /// <see cref="AreaSceneBuilder"/>'s counter-clockwise tile-rotation convention. Elevation 0
        /// is level with the ground plane; positive elevation looks down at the target from above.
        /// </summary>
        public static Vector3 OrbitEyeOffset(float azimuthRadians, float elevationRadians, float distance)
        {
            var cosElevation = MathF.Cos(elevationRadians);
            var x = distance * cosElevation * MathF.Cos(azimuthRadians);
            var y = distance * cosElevation * MathF.Sin(azimuthRadians);
            var z = distance * MathF.Sin(elevationRadians);
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// World-space pan delta for a screen-pixel drag: moves the orbit target along the
        /// camera's own ground-plane right/up axes (derived from azimuth only, so pan feels
        /// screen-relative regardless of the current elevation), scaled by
        /// <paramref name="worldPerPixel"/>. The "up" axis is always world +Z since the ground-plane
        /// focus never rolls.
        /// </summary>
        public static Vector3 PanDelta(float azimuthRadians, float dxPixels, float dyPixels, float worldPerPixel)
        {
            var right = new Vector3(-MathF.Sin(azimuthRadians), MathF.Cos(azimuthRadians), 0f);
            var up = Vector3.UnitZ;
            return right * (-dxPixels * worldPerPixel) + up * (dyPixels * worldPerPixel);
        }

        /// <summary>
        /// World units covered by one screen pixel at <paramref name="distance"/>, given the
        /// vertical FOV and viewport height in pixels - used to convert pixel-space pan drags into
        /// world-space motion. Returns 0 for a not-yet-laid-out viewport (height &lt;= 0).
        /// </summary>
        public static float WorldUnitsPerPixel(float distance, float verticalFovRadians, int viewportHeightPixels)
        {
            if (viewportHeightPixels <= 0)
                return 0f;

            var halfFovTan = MathF.Tan(verticalFovRadians / 2f);
            return 2f * distance * halfFovTan / viewportHeightPixels;
        }

        /// <summary>
        /// Unprojects a logical-pixel screen point (Y-down, origin top-left - matching Avalonia
        /// pointer coordinates) into a world-space <see cref="PickRay"/>, using the exact same
        /// <paramref name="view"/>/<paramref name="projection"/> matrices <c>GlAreaControl.DrawScene</c>
        /// uploaded for that frame. Unprojects the near and far NDC points (Z=0 and Z=1 - the
        /// System.Numerics/D3D-style depth range <see cref="Matrix4x4.CreatePerspectiveFieldOfView"/>
        /// targets) through the inverse of <c>view * projection</c> (row-vector convention, matching
        /// how <see cref="Vector4.Transform(Vector4,Matrix4x4)"/> and every other matrix composition
        /// in this codebase already works) and builds a ray from the two unprojected points. Returns
        /// a degenerate ray at the origin, pointing along +X, for a not-yet-laid-out viewport
        /// (width/height &lt;= 0) or a singular view*projection (should not occur for any real
        /// camera state, but must never throw here).
        /// </summary>
        public static PickRay ScreenPointToRay(
            Vector2 screenPoint, int viewportWidth, int viewportHeight, Matrix4x4 view, Matrix4x4 projection)
        {
            if (viewportWidth <= 0 || viewportHeight <= 0)
                return new PickRay(Vector3.Zero, Vector3.UnitX);

            var ndcX = screenPoint.X / viewportWidth * 2f - 1f;
            var ndcY = 1f - screenPoint.Y / viewportHeight * 2f; // screen Y-down -> NDC Y-up

            var viewProjection = view * projection;
            if (!Matrix4x4.Invert(viewProjection, out var inverseViewProjection))
                return new PickRay(Vector3.Zero, Vector3.UnitX);

            var nearWorld = UnprojectNdcPoint(new Vector4(ndcX, ndcY, 0f, 1f), inverseViewProjection);
            var farWorld = UnprojectNdcPoint(new Vector4(ndcX, ndcY, 1f, 1f), inverseViewProjection);

            var direction = farWorld - nearWorld;
            var length = direction.Length();
            return length < 1e-6f
                ? new PickRay(nearWorld, Vector3.UnitX)
                : new PickRay(nearWorld, direction / length);
        }

        private static Vector3 UnprojectNdcPoint(Vector4 ndcPoint, Matrix4x4 inverseViewProjection)
        {
            var world = Vector4.Transform(ndcPoint, inverseViewProjection);
            return MathF.Abs(world.W) < 1e-8f
                ? new Vector3(world.X, world.Y, world.Z)
                : new Vector3(world.X / world.W, world.Y / world.W, world.Z / world.W);
        }
    }
}
