using System.Globalization;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Domain.Editors.Items
{
    /// <summary>
    /// The iprp_spells.2da rows a CastSpell property (itempropdef.2da row 15) can name. A choice's
    /// <see cref="BehaviorChoice.Value"/> is the row id, which is exactly the Subtype value a
    /// Consumable/Grenade's property 15 entry stores - see <see cref="ItemValueStore"/>.
    /// </summary>
    public static class ItemSpellChoiceCatalog
    {
        /// <param name="twoDa">Null-tolerant, matching <see cref="Triggers.LoadScreenCatalog"/>.</param>
        /// <param name="tlk">
        /// Resolves a TLK StrRef to display text; null or a miss falls back to the Label column.
        /// </param>
        public static IReadOnlyList<BehaviorChoice> Read(TwoDaService? twoDa, Func<int, string?>? tlk)
        {
            var definition = TwoDaLookupTables.ItemSpell;
            var requiredColumns = definition.RequiredColumns ?? Array.Empty<string>();
            if (twoDa == null ||
                !twoDa.TryGetTable(definition.TableName, out var table) ||
                table == null ||
                !table.HasColumn(definition.LabelColumn) ||
                requiredColumns.Any(column => !table.HasColumn(column)))
            {
                return Array.Empty<BehaviorChoice>();
            }

            var spells = new List<BehaviorChoice>();
            for (var row = 0; row < table.RowCount; row++)
            {
                var label = table.GetString(row, definition.LabelColumn);
                if (!TwoDaChoicePolicy.IsSelectableLabel(label) ||
                    requiredColumns.Any(column => string.IsNullOrWhiteSpace(table.GetString(row, column))))
                {
                    continue;
                }

                var rawStrRef = table.GetString(row, definition.StrRefColumn!);
                if (!int.TryParse(
                        rawStrRef,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var strRef))
                {
                    continue;
                }

                spells.Add(new BehaviorChoice(
                    row,
                    ResolveDisplay(strRef, tlk) ?? label!));
            }

            return spells;
        }

        private static string? ResolveDisplay(int strRef, Func<int, string?>? tlk)
        {
            if (tlk == null)
                return null;

            var text = tlk(strRef);
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
    }
}
