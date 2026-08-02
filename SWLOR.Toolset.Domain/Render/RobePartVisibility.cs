using System.Globalization;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Reads the body-part concealment flags NWN assigns to each robe appearance.
    /// </summary>
    /// <remarks>
    /// A robe is not simply "full body" or "partial". <c>parts_robe.2da</c> independently decides
    /// whether the selected robe hides the chest, belt, pelvis, each limb segment, and so on.
    /// Honoring those flags is what lets a coat replace the ordinary arm meshes while retaining
    /// the wearer's trousers and hands.
    /// </remarks>
    public static class RobePartVisibility
    {
        private const string TableName = "parts_robe";

        private static readonly (string Column, string PartType)[] HideColumns =
        {
            ("HIDEFOOTR", "footr"),
            ("HIDEFOOTL", "footl"),
            ("HIDESHINR", "shinr"),
            ("HIDESHINL", "shinl"),
            ("HIDELEGR", "legr"),
            ("HIDELEGL", "legl"),
            ("HIDEPELVIS", "pelvis"),
            ("HIDECHEST", "chest"),
            ("HIDEBELT", "belt"),
            ("HIDENECK", "neck"),
            ("HIDEFORER", "forer"),
            ("HIDEFOREL", "forel"),
            ("HIDEBICEPR", "bicepr"),
            ("HIDEBICEPL", "bicepl"),
            ("HIDESHOR", "shor"),
            ("HIDESHOL", "shol"),
            ("HIDEHANDR", "handr"),
            ("HIDEHANDL", "handl"),
            ("HIDEHEAD", "head")
        };

        /// <summary>
        /// Returns true when the robe resource maps to an available <c>parts_robe.2da</c> row.
        /// <paramref name="hiddenParts"/> is empty for a valid row whose flags are all clear.
        /// </summary>
        public static bool TryGetHiddenParts(
            TwoDaService? twoDa,
            string? robeModelResRef,
            out IReadOnlySet<string> hiddenParts)
        {
            var hidden = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            hiddenParts = hidden;

            if (twoDa == null ||
                !TryReadAppearanceNumber(robeModelResRef, out var appearance) ||
                !twoDa.TryGetTable(TableName, out var table) ||
                table == null ||
                appearance < 0 ||
                appearance >= table.RowCount)
            {
                return false;
            }

            try
            {
                foreach (var (column, partType) in HideColumns)
                {
                    if (table.GetInt(appearance, column) == 1)
                        hidden.Add(partType);
                }
            }
            catch (FormatException)
            {
                hidden.Clear();
                return false;
            }

            return true;
        }

        private static bool TryReadAppearanceNumber(string? modelResRef, out int appearance)
        {
            appearance = -1;
            if (string.IsNullOrWhiteSpace(modelResRef))
                return false;

            const string marker = "_robe";
            var markerIndex = modelResRef.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
                return false;

            var number = modelResRef[(markerIndex + marker.Length)..];
            return int.TryParse(
                number,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out appearance);
        }
    }
}
