using System.Globalization;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Stores NPC cooldown expiry times in local strings without losing fractional seconds.
    /// </summary>
    public static class RecastTimestamp
    {
        private static readonly string[] Formats = { "O", "yyyy-MM-dd HH:mm:ss" };

        public static string Format(DateTime endsAt)
        {
            return endsAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
        }

        public static bool TryParse(string value, out DateTime endsAt)
        {
            // Legacy locals were written from UtcNow without a timezone suffix.
            return DateTime.TryParseExact(value, Formats, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out endsAt);
        }
    }
}
