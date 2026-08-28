using System;
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

        public static float ResolveWidth(
            CombatImpactAreaShape shape,
            float forwardLength,
            float endWidth)
        {
            if (shape != CombatImpactAreaShape.Cone || forwardLength <= 0f || endWidth <= 0f)
                return endWidth;

            // Preserve the declared cone angle after extending its length backward. At the original
            // forward endpoint the width therefore remains exactly the authored end width.
            return endWidth * ResolveLength(shape, forwardLength) / forwardLength;
        }
    }
}
