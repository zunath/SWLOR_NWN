namespace SWLOR.Game.Server.Service.DMToolService
{
    /// <summary>
    /// Shortens text for fixed-width list columns in the DM windows.
    /// </summary>
    public static class DMListText
    {
        private const string Ellipsis = "...";

        /// <summary>
        /// Returns the text unchanged when it fits, otherwise cut and ended with "...".
        /// NUI labels draw past the edge of their list cell, so long values would run into the next column.
        /// </summary>
        /// <param name="text">The full value.</param>
        /// <param name="maxLength">The most characters the column shows, including the "...".</param>
        public static string Fit(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text ?? string.Empty;

            return text[..Math.Max(0, maxLength - Ellipsis.Length)].TrimEnd() + Ellipsis;
        }
    }
}
