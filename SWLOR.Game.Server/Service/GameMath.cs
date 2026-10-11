using System;

namespace SWLOR.Game.Server.Service
{
    public static class GameMath
    {
        public static float NormalizeDegrees(float degrees)
        {
            if (!float.IsFinite(degrees))
                throw new ArgumentOutOfRangeException(nameof(degrees), "Facing must be finite.");

            var normalized = degrees % 360f;
            if (normalized < 0f)
                normalized += 360f;

            // Adding 360 to a tiny negative float can round up to 360.
            return normalized >= 360f ? 0f : normalized;
        }

        public static int PercentOf(int value, int percent)
        {
            return Math.Max(1, (int)Math.Ceiling(value * (percent / 100f)));
        }
    }
}
