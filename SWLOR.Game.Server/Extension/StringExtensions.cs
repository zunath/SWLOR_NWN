namespace SWLOR.Game.Server.Extension
{
    public static class StringExtensions
    {
        /// <summary>
        /// Truncates the string to the specified maxLength.
        /// Text which falls outside this limit will be lost.
        /// </summary>
        /// <param name="value">The string to truncate.</param>
        /// <param name="maxLength">The maximum length the string may be.</param>
        /// <returns>The truncated string.</returns>
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
