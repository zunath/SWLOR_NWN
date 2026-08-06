using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.GameData.Lookups;

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

        /// <summary>The row that leaves the screen to the destination area, and has no artwork.</summary>
        private const string AnyLabel = "Random";

        /// <summary>
        /// The engine's placeholder for a screen a script sets. Not offered: it names nothing a
        /// builder can point at, and on a page of pictures it is an empty tile that reads as a
        /// screen whose artwork failed to load.
        /// </summary>
        private const string ScriptedLabel = "UserDefined";

        public static IReadOnlyList<BehaviorChoice> Read(TwoDaService? twoDa)
        {
            if (twoDa == null ||
                !twoDa.TryGetTable(TableName, out var table) ||
                table == null ||
                !table.HasColumn(LabelColumn) ||
                !table.HasColumn(ImageColumn))
            {
                return Array.Empty<BehaviorChoice>();
            }

            var screens = new List<BehaviorChoice>();
            for (var row = 0; row < table.RowCount; row++)
            {
                var label = table.GetString(row, LabelColumn);
                if (!TwoDaChoicePolicy.IsSelectableLabel(label) ||
                    string.Equals(label, ScriptedLabel, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var selectableLabel = label!;
                var isAny = selectableLabel.Equals(AnyLabel, StringComparison.OrdinalIgnoreCase);
                var imageResRef = table.GetString(row, ImageColumn);
                if (!isAny && !TwoDaChoicePolicy.IsSelectableLabel(imageResRef))
                    continue;

                screens.Add(new BehaviorChoice(
                    row,
                    Humanise(selectableLabel),
                    imageResRef)
                {
                    IsAny = isAny
                });
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
