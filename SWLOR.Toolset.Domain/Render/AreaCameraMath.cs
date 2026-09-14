using System.Numerics;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// A world-space ray (origin + normalized direction) produced by unprojecting a screen point,
    /// consumed by <see cref="AreaPicking"/> for instance hit-testing.
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
    /// Pure orbit-camera math for the area viewport: initial framing from an area's tile-grid
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

        /// <summary>
        /// The near clip plane to use at a given orbit distance.
        /// </summary>
        /// <remarks>
        /// Scaled with the distance rather than fixed, because a depth buffer's precision is governed
        /// by the ratio between the planes and almost all of it is spent in the first few metres. A
        /// fixed 0.1m near plane against a far plane hundreds of metres out leaves so little precision
        /// at working range that coplanar surfaces cannot be told apart: the paintings hung on a wall
        /// flickered against the wall behind them whenever the camera moved, because a picture is a
        /// decal a centimetre off the surface it hangs on.
        /// <para>
        /// A twentieth of the orbit distance keeps the ratio constant as the builder zooms, so the
        /// precision at whatever is being looked at does not change with the zoom. At a typical
        /// interior framing of 20m that is a 1m near plane against the old 0.1m - twenty times the
        /// depth resolution at working range. The floor is a tenth of <see cref="MinDistance"/>,
        /// which is nearer than the camera can be brought, so nothing is clipped by it.
        /// </para>
        /// </remarks>
        public static float NearPlaneFor(float distance) =>
            MathF.Max(MinDistance / 10f, MathF.Abs(distance) / 20f);

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

        /// <summary>
        /// Initial framing for a single-model preview: the target is the model's own bounding-box
        /// centre and the distance is the one that fits its bounding sphere, so a 0.3m sword and a
        /// 2m mannequin both fill the box.
        /// </summary>
        /// <remarks>
        /// Deliberately NOT the area framing above: that one frames a tile grid, so a one-tile
        /// preview scene held the camera 10m back regardless of what it contained and every item
        /// rendered as a speck in the middle of the viewport.
        /// </remarks>
        public static (Vector3 Target, float Distance) ComputeModelFraming(
            Vector3 minimum,
            Vector3 maximum,
            float verticalFovRadians,
            float aspectRatio)
        {
            var target = (minimum + maximum) / 2f;
            var extent = maximum - minimum;
            var radius = MathF.Max(extent.Length() / 2f, 0.001f);

            var halfFovTanVertical = MathF.Tan(verticalFovRadians / 2f);
            var halfFovTanHorizontal = halfFovTanVertical * MathF.Max(aspectRatio, 0.001f);
            var tightest = MathF.Min(halfFovTanVertical, halfFovTanHorizontal);

            // The bounding sphere is orientation-independent, so this framing holds however the
            // model is orbited - no clipping when a long blade swings toward the camera.
            var distance = radius / MathF.Max(tightest, 0.001f) * ModelFramingSlack;
            return (target, MathF.Max(distance, MinDistance));
        }

        /// <summary>
        /// Breathing room around a previewed model, as a multiple of its fitted distance. Tighter
        /// than <see cref="FramingSlack"/>: an area needs its edges clear of the frustum, while a
        /// single model wants to fill the small preview box.
        /// </summary>
        private const float ModelFramingSlack = 1.1f;

        /// <summary>
        /// Initial framing for any scene: a single-model preview (one instance carrying geometry,
        /// no tiles) frames that model where it actually stands, and everything else frames the
        /// tile grid.
        /// </summary>
        /// <remarks>
        /// The instance offset is the whole reason this lives here rather than at the two call
        /// sites: a preview scene parks its one instance at the centre of its nominal tile, so
        /// framing the model's LOCAL bounds aims the camera metres away from the geometry and the
        /// preview box renders empty. Bounds are a box in model space; adding the instance
        /// position puts them where the renderer will draw them.
        /// </remarks>
        public static (Vector3 Target, float Distance) ComputeSceneFraming(
            AreaScene scene,
            float tileSize,
            float verticalFovRadians,
            float aspectRatio)
        {
            ArgumentNullException.ThrowIfNull(scene);

            if (scene.Tiles.Count == 0 && scene.Instances.Count == 1)
            {
                var instance = scene.Instances[0];
                var bounds = instance.Model?.ComputeBounds();
                if (bounds == null && instance.IsDoorTransition)
                {
                    bounds = (
                        DoorTransitionMarker.LocalMinimum,
                        DoorTransitionMarker.LocalMaximum);
                }

                if (bounds is { } previewBounds)
                {
                    var offset = instance.Position;
                    return ComputeModelFraming(
                        previewBounds.Minimum + offset,
                        previewBounds.Maximum + offset,
                        verticalFovRadians,
                        aspectRatio);
                }
            }

            return ComputeInitialFraming(
                scene.Width, scene.Height, tileSize, verticalFovRadians, aspectRatio);
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
        /// World-space pan delta for a screen-pixel drag: slides the orbit target across the ground
        /// plane along the camera's own right and forward axes (derived from azimuth only, so panning
        /// feels screen-relative whatever the elevation), scaled by <paramref name="worldPerPixel"/>.
        /// </summary>
        /// <remarks>
        /// Vertical panning travels FORWARD along the ground, not up into the air. Raising the camera
        /// shifts the view vertically too, so the two look similar for a moment, but altitude changes
        /// how much of the map is in shot and eventually flies the camera off the scene, whereas a
        /// builder panning up means "show me further on". Forward is the eye offset's ground-plane
        /// projection reversed - <see cref="OrbitEyeOffset"/> puts the eye at (cos, sin) from the
        /// target, so the camera looks along (-cos, -sin).
        /// </remarks>
        public static Vector3 PanDelta(float azimuthRadians, float dxPixels, float dyPixels, float worldPerPixel)
        {
            var right = new Vector3(-MathF.Sin(azimuthRadians), MathF.Cos(azimuthRadians), 0f);
            var forward = new Vector3(-MathF.Cos(azimuthRadians), -MathF.Sin(azimuthRadians), 0f);

            // Positive dy carries the camera FORWARD, which slides the scene down the screen. That is
            // the direction a downward grab-drag has to produce for the ground to stay under the
            // cursor, and it is also what the pad's up arrow wants.
            return right * (-dxPixels * worldPerPixel) + forward * (dyPixels * worldPerPixel);
        }

        /// <summary>
        /// World-space pan delta for a model preview's screen-pixel drag. Unlike
        /// <see cref="PanDelta"/>, both axes lie in the camera's screen plane: dragging vertically
        /// moves the model vertically on screen instead of travelling across an area's ground plane.
        /// </summary>
        public static Vector3 ScreenPanDelta(
            float azimuthRadians,
            float elevationRadians,
            float dxPixels,
            float dyPixels,
            float worldPerPixel)
        {
            var eyeDirection = Vector3.Normalize(OrbitEyeOffset(
                azimuthRadians,
                elevationRadians,
                distance: 1f));
            var forward = -eyeDirection;
            var right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitZ));
            var up = Vector3.Normalize(Vector3.Cross(right, forward));

            // Move the camera opposite a horizontal drag and with a downward drag along its screen
            // up axis. The rendered model consequently follows the pointer in both directions.
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
        /// Builds the viewport projection. Area editing retains its perspective view, while a
        /// single-model preview uses the orthographic lens from Aurora's item-property viewer.
        /// </summary>
        /// <remarks>
        /// The orthographic height is the perspective frustum's height at the orbit target. That
        /// keeps the existing model fit, wheel zoom, and pixel-to-world pan scale unchanged while
        /// removing depth foreshortening. Without it, garment details that project toward the
        /// camera (such as a robe's diagonal sash end) appear enlarged and displaced over adjacent
        /// rigid chest pieces even though their model-space vertices are correct.
        /// </remarks>
        public static Matrix4x4 CreateProjection(
            bool isSingleModelPreview,
            float distance,
            float verticalFovRadians,
            float aspectRatio,
            float nearPlane,
            float farPlane)
        {
            if (!isSingleModelPreview)
            {
                return Matrix4x4.CreatePerspectiveFieldOfView(
                    verticalFovRadians, aspectRatio, nearPlane, farPlane);
            }

            var orthographicHeight =
                2f * MathF.Max(MathF.Abs(distance), MinDistance) *
                MathF.Tan(verticalFovRadians / 2f);
            return Matrix4x4.CreateOrthographic(
                orthographicHeight * MathF.Max(aspectRatio, 0.001f),
                orthographicHeight,
                nearPlane,
                farPlane);
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
