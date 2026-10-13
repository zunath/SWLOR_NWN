using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Service.AIService
{
    public sealed class WanderBounds
    {
        public const float StepDistance = 3f;
        public float MinX { get; }
        public float MinY { get; }
        public float MaxX { get; }
        public float MaxY { get; }

        public WanderBounds(float minX, float minY, float maxX, float maxY)
        {
            if (!IsValid(minX, minY, maxX, maxY))
                throw new ArgumentException("Wander bounds must be finite and have positive width and height.");
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        public static bool IsValid(float minX, float minY, float maxX, float maxY) =>
            float.IsFinite(minX) && float.IsFinite(minY) && float.IsFinite(maxX) && float.IsFinite(maxY) &&
            minX < maxX && minY < maxY;

        public bool Contains(Vector3 position) =>
            float.IsFinite(position.Z) && position.X >= MinX && position.X <= MaxX &&
            position.Y >= MinY && position.Y <= MaxY;

        public Vector3 Constrain(Vector3 position) => new(
            Math.Clamp(position.X, MinX, MaxX), Math.Clamp(position.Y, MinY, MaxY), position.Z);

        public bool AllowsStep(Vector3 current, Vector3 home, Vector3 destination, float homeRadius) =>
            Contains(current) && Contains(home) && Contains(destination) &&
            Vector3.DistanceSquared(current, destination) >= 0.25f &&
            Vector3.DistanceSquared(current, destination) <= StepDistance * StepDistance &&
            Vector3.DistanceSquared(home, destination) <= homeRadius * homeRadius;
    }
}
