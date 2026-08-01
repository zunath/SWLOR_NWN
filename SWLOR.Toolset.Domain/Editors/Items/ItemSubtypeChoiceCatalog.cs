using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Domain.Editors.Items
{
    /// <summary>
    /// Generic labeled-row reader for any iprp_* (or similarly shaped) subtype table an
    /// <see cref="ItemMultiEntryDefinition"/> or <see cref="ItemEngineLegacyDefinition"/> names as
    /// its SubtypeTableResRef - iprp_foodtype, iprp_resperk, racialtypes, and the rest. Generalizes
    /// <see cref="ItemSpellChoiceCatalog"/>'s iprp_spells-specific reader to any table shaped the
    /// same way: a Label column naming the row, and an optional Name column holding a TLK StrRef.
    /// </summary>
    public static class ItemSubtypeChoiceCatalog
    {
        private const string LabelColumn = "Label";
        private const string NameColumn = "Name";

        /// <param name="twoDa">Null-tolerant, matching <see cref="ItemSpellChoiceCatalog"/>.</param>
        /// <param name="tableResRef">The 2da's file name without extension - matched case-insensitively.</param>
        /// <param name="tlk">
        /// Resolves a TLK StrRef to display text; null or a miss falls back to the Label column.
        /// </param>
        public static IReadOnlyList<BehaviorChoice> Read(
            TwoDaService? twoDa, string tableResRef, Func<int, string?>? tlk)
        {
            if (twoDa == null || string.IsNullOrWhiteSpace(tableResRef))
                return Array.Empty<BehaviorChoice>();

            if (!twoDa.TryGetTable(tableResRef, out var table) || table == null)
                return Array.Empty<BehaviorChoice>();

            var choices = new List<BehaviorChoice>();
            for (var row = 0; row < table.RowCount; row++)
            {
                var label = table.GetString(row, LabelColumn);
                if (!TwoDaChoicePolicy.IsSelectableLabel(label))
                    continue;

                var resolved = ResolveDisplay(table, row, tlk);

                choices.Add(new BehaviorChoice(row, resolved ?? label!));
            }

            return choices;
        }

        private static string? ResolveDisplay(TwoDaTable table, int row, Func<int, string?>? tlk)
        {
            if (tlk == null)
                return null;

            var strRef = table.GetInt(row, NameColumn);
            if (strRef is null)
                return null;

            var text = tlk(strRef.Value);
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

    }
}
