using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SWLOR.Game.Server.Service.AbilityService
{
    public enum CombatImpactAreaShape
    {
        Sphere = 1,
        Cone = 2,
        Line = 3
    }

    /// <summary>
    /// Shared geometry adjustments for directional areas. Moving the apex behind the caster gives
    /// melee targets room inside a line or cone while retaining the advertised forward reach.
    /// </summary>
    public static class CombatImpactShapeGeometry
    {
        public const float DirectionalOriginBackOffset = 1.5f;

        public static Vector3 ResolveOrigin(
            Vector3 casterPosition,
            float rotation,
            CombatImpactAreaShape shape)
        {
            if (shape is not (CombatImpactAreaShape.Cone or CombatImpactAreaShape.Line))
                return casterPosition;

            return new Vector3(
                casterPosition.X - (float)Math.Cos(rotation) * DirectionalOriginBackOffset,
                casterPosition.Y - (float)Math.Sin(rotation) * DirectionalOriginBackOffset,
                casterPosition.Z);
        }

        public static float ResolveLength(CombatImpactAreaShape shape, float forwardLength)
        {
            return shape is CombatImpactAreaShape.Cone or CombatImpactAreaShape.Line
                ? forwardLength + DirectionalOriginBackOffset
                : forwardLength;
        }

        /// <summary>
        /// Returns at most <paramref name="count"/> candidates ordered by horizontal distance from
        /// the same origin used to render the combat shape.
        /// </summary>
        public static IEnumerable<T> TakeClosestToOrigin<T>(
            IEnumerable<T> candidates,
            Vector3 origin,
            Func<T, Vector3> resolvePosition,
            int count)
        {
            if (count <= 0)
                return candidates;

            return candidates
                .OrderBy(candidate => HorizontalDistanceSquared(resolvePosition(candidate), origin))
                .Take(count);
        }

        private static float HorizontalDistanceSquared(Vector3 position, Vector3 origin)
        {
            var x = position.X - origin.X;
            var y = position.Y - origin.Y;
            return x * x + y * y;
        }
    }
}
