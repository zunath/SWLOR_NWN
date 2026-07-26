using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Domain.Editors.Triggers
{
    /// <summary>
    /// The load screens a transition can show, each carrying the resref of its own artwork so the
    /// editor can offer the picture rather than the name.
    /// </summary>
    /// <remarks>
    /// Every SWLOR row points at the same generic StrRef, so the label is the only thing that tells
    /// one screen apart from another in text — which is exactly why the picture matters here.
    /// </remarks>
    public static class LoadScreenCatalog
    {
        private const string TableName = "loadscreens";
        private const string LabelColumn = "Label";
        private const string ImageColumn = "BMPResRef";

        public static IReadOnlyList<TriggerChoice> Read(TwoDaService? twoDa)
        {
            if (twoDa == null || !twoDa.TryGetTable(TableName, out var table) || table == null)
                return Array.Empty<TriggerChoice>();

            var screens = new List<TriggerChoice>();
            for (var row = 0; row < table.RowCount; row++)
            {
                var label = table.GetString(row, LabelColumn);
                if (string.IsNullOrWhiteSpace(label))
                    continue;

                screens.Add(new TriggerChoice(row, Humanise(label), table.GetString(row, ImageColumn)));
            }

            return screens;
        }

        /// <summary>
        /// Turns "SWLOR_17_Tatooine" into "17 Tatooine". The 2DA labels are identifiers rather than
        /// names, and a picker full of underscores and a repeated prefix reads as noise.
        /// </summary>
        private static string Humanise(string label)
        {
            var trimmed = label.Trim();
            if (trimmed.StartsWith("SWLOR_", StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed["SWLOR_".Length..];

            return trimmed.Replace('_', ' ');
        }
    }
}
