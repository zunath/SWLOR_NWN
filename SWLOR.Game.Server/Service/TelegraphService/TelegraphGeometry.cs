using System;
using System.Numerics;
using NumericsVector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Service.TelegraphService
{
    /// <summary>
    /// Immutable geometry of a displayed marker, retained after its native timer expires.
    /// </summary>
    public readonly record struct TelegraphGeometry(
        uint Area,
        TelegraphType Shape,
        NumericsVector3 Position,
        Vector2 Size,
        float Rotation)
    {
        /// <summary>
        /// Compares marker footprints within 1 cm and 0.001 radians, ignoring sphere rotation
        /// and treating directions separated by a full turn as equivalent.
        /// </summary>
        public bool Matches(TelegraphGeometry other)
        {
            const float distanceToleranceSquared = 0.0001f;
            const float rotationTolerance = 0.001f;

            if (Area != other.Area || Shape != other.Shape ||
                NumericsVector3.DistanceSquared(Position, other.Position) > distanceToleranceSquared ||
                Vector2.DistanceSquared(Size, other.Size) > distanceToleranceSquared)
                return false;

            return Shape == TelegraphType.Sphere ||
                   Math.Abs(Math.IEEERemainder(Rotation - other.Rotation, 2 * Math.PI)) <= rotationTolerance;
        }
    }
}
