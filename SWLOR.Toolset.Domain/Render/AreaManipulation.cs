using System.Numerics;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Pure math for the in-viewport move/rotate gizmo: ray/horizontal-plane intersection
    /// (dragging an instance across the ground plane at its current Z) and grid-snap rounding. No
    /// GL/UI/document dependency - <c>GlAreaControl</c> drives a live drag preview with this, and
    /// <c>AreaEditorViewModel</c> commits the final values through <c>InstanceFieldMap</c>'s
    /// existing setters only once the drag releases (the same setter the instance-list detail
    /// form's X/Y/Z and Facing X/Y editors already use).
    /// </summary>
    public static class AreaManipulation
    {
        /// <summary>Default move-gizmo grid cell size (meters) applied while the snap modifier (Ctrl) is held.</summary>
        public const float DefaultGridSnapMeters = 0.5f;

        /// <summary>Radians of Z-rotation applied per pixel of horizontal rotate-drag movement - matches GlAreaControl's orbit-camera OrbitSensitivity feel.</summary>
        public const float RotateRadiansPerPixel = 0.01f;

        private const float MinRayPlaneDenominator = 1e-6f;

        /// <summary>
        /// Intersects <paramref name="ray"/> with the horizontal (Z-constant) plane at
        /// <paramref name="planeZ"/>, returning the world-space hit point, or null when the ray is
        /// (near-)parallel to the plane or the plane lies behind the ray's origin (negative t).
        /// </summary>
        public static Vector3? IntersectRayWithHorizontalPlane(PickRay ray, float planeZ)
        {
            var denominator = ray.Direction.Z;
            if (MathF.Abs(denominator) < MinRayPlaneDenominator)
                return null; // Ray runs parallel to the plane - no single intersection point.

            var t = (planeZ - ray.Origin.Z) / denominator;
            return t < 0f ? null : ray.Origin + ray.Direction * t;
        }

        /// <summary>
        /// Rounds a world position's X/Y to the nearest multiple of <paramref name="cellSize"/>
        /// (Z passes through unchanged - the move gizmo never changes Z). A non-positive cell size
        /// disables snapping and returns <paramref name="position"/> unchanged.
        /// </summary>
        public static Vector3 SnapToGridXy(Vector3 position, float cellSize)
        {
            if (cellSize <= 0f)
                return position;

            return new Vector3(
                MathF.Round(position.X / cellSize) * cellSize,
                MathF.Round(position.Y / cellSize) * cellSize,
                position.Z);
        }

        /// <summary>Converts a heading angle (radians, matching <c>Atan2(orientation.Y, orientation.X)</c>) back to the (cos,sin) orientation vector every InstanceMarker/InstanceFieldMap heading uses.</summary>
        public static Vector2 HeadingToOrientation(float headingRadians) =>
            new(MathF.Cos(headingRadians), MathF.Sin(headingRadians));
    }
}
