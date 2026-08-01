using System.Text;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Domain.Editors.Triggers
{
    /// <summary>
    /// The trap kinds a trap trigger can be, named rather than numbered. A builder cannot be asked
    /// to look a row up in traps.2da.
    /// </summary>
    public static class TrapTypeCatalog
    {
        public static IReadOnlyList<BehaviorChoice> Read(TwoDaService? twoDa)
        {
            var definition = TwoDaLookupTables.Trap;
            var requiredColumns = definition.RequiredColumns!;
            if (twoDa == null ||
                !twoDa.TryGetTable(definition.TableName, out var table) ||
                table == null ||
                !table.HasColumn(definition.LabelColumn) ||
                requiredColumns.Any(column => !table.HasColumn(column)))
            {
                return Array.Empty<BehaviorChoice>();
            }

            var traps = new List<BehaviorChoice>();
            for (var row = 0; row < table.RowCount; row++)
            {
                var label = table.GetString(row, definition.LabelColumn);
                if (!TwoDaChoicePolicy.IsSelectableLabel(label) ||
                    requiredColumns.Any(column =>
                        !TwoDaChoicePolicy.IsSelectableLabel(table.GetString(row, column))))
                {
                    continue;
                }

                traps.Add(new BehaviorChoice(row, Humanise(label!)));
            }

            return traps;
        }

        /// <summary>
        /// Splits the 2DA's run-together labels: "MinorSpike" reads as "Minor Spike". They are
        /// identifiers, and a list of them is harder to scan than it needs to be.
        /// </summary>
        private static string Humanise(string label)
        {
            var text = new StringBuilder(label.Length + 8);
            for (var i = 0; i < label.Length; i++)
            {
                if (i > 0 && char.IsUpper(label[i]) && !char.IsUpper(label[i - 1]))
                    text.Append(' ');

                text.Append(label[i]);
            }

            return text.ToString();
        }
    }
}
