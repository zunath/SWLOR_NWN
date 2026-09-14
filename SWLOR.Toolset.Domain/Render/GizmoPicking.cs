using System.Numerics;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Hit-testing for the transform gizmo's own geometry: the three axis arms and the rotation ring.
    /// </summary>
    /// <remarks>
    /// The arms and the ring are drawn well outside most objects' bounds, so testing only the
    /// instance body meant a press on a visible handle fell through to camera panning and the handles
    /// could not actually be grabbed. Kept here rather than in the control so the geometry the press
    /// is tested against is the same geometry the drawing code describes, and so it can be tested
    /// without a GL context.
    /// </remarks>
    public static class GizmoPicking
    {
        /// <summary>Segments the ring is drawn with; the hit test walks the same chords.</summary>
        public const int RingSegments = 36;

        /// <summary>The ring sits just off the ground plane, matching how it is drawn.</summary>
        public const float RingGroundOffset = 0.05f;

        /// <summary>
        /// Which handle a ray passes closest to, or <see cref="GizmoHandle.None"/> when it misses
        /// every one of them by more than <paramref name="tolerance"/>.
        /// </summary>
        /// <param name="ray">The pick ray, in world space.</param>
        /// <param name="origin">Where the gizmo is drawn - the instance's displayed position.</param>
        /// <param name="armLength">Length of each axis arm.</param>
        /// <param name="ringRadius">Radius of the rotation ring.</param>
        /// <param name="tolerance">
        /// How near the ray must pass, in world units. The caller scales this with camera distance so
        /// a handle stays grabbable when zoomed out, where it covers fewer pixels.
        /// </param>
        public static GizmoHandle Pick(
            PickRay ray, Vector3 origin, float armLength, float ringRadius, float tolerance)
        {
            var best = GizmoHandle.None;
            var bestDistance = tolerance;

            foreach (var axis in new[]
                     {
                         new Vector3(armLength, 0, 0),
                         new Vector3(0, armLength, 0),
                         new Vector3(0, 0, armLength)
                     })
            {
                var distance = RayToSegment(ray, origin, origin + axis);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = GizmoHandle.Axis;
                }
            }

            for (var i = 0; i < RingSegments; i++)
            {
                var distance = RayToSegment(ray, RingPoint(origin, ringRadius, i), RingPoint(origin, ringRadius, i + 1));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = GizmoHandle.Ring;
                }
            }

            return best;
        }

        private static Vector3 RingPoint(Vector3 origin, float radius, int index)
        {
            var angle = index / (float)RingSegments * MathF.Tau;
            return origin + new Vector3(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius, RingGroundOffset);
        }

        /// <summary>
        /// Shortest distance between a ray and a line segment, clamped to both (the ray forward only,
        /// the segment to its ends). Parallel and degenerate cases fall back to the endpoint distance
        /// rather than dividing by zero.
        /// </summary>
        private static float RayToSegment(PickRay ray, Vector3 start, Vector3 end)
        {
            var segment = end - start;
            var offset = ray.Origin - start;

            var rayDotRay = Vector3.Dot(ray.Direction, ray.Direction);
            var rayDotSegment = Vector3.Dot(ray.Direction, segment);
            var segmentDotSegment = Vector3.Dot(segment, segment);
            var rayDotOffset = Vector3.Dot(ray.Direction, offset);
            var segmentDotOffset = Vector3.Dot(segment, offset);

            var denominator = rayDotRay * segmentDotSegment - rayDotSegment * rayDotSegment;

            float alongSegment;
            if (MathF.Abs(denominator) < 1e-8f || segmentDotSegment < 1e-8f)
            {
                // Parallel, or a zero-length segment: any point on it is as good as another, so fall
                // back to the start and let the ray clamp below do the rest.
                alongSegment = 0f;
            }
            else
            {
                alongSegment = Math.Clamp(
                    (rayDotRay * segmentDotOffset - rayDotSegment * rayDotOffset) / denominator, 0f, 1f);
            }

            var closestOnSegment = start + segment * alongSegment;

            // Forward only. Without the clamp a handle behind the camera reports the same distance as
            // one in front of it.
            var alongRay = rayDotRay < 1e-8f
                ? 0f
                : MathF.Max(0f, Vector3.Dot(closestOnSegment - ray.Origin, ray.Direction) / rayDotRay);

            return Vector3.Distance(closestOnSegment, ray.Origin + ray.Direction * alongRay);
        }
    }
}
