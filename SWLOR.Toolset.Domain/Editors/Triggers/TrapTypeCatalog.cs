using System.Text;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Domain.Editors.Triggers
{
    /// <summary>
    /// The trap kinds a trap trigger can be, named rather than numbered. A builder cannot be asked
    /// to look a row up in traps.2da.
    /// </summary>
    public static class TrapTypeCatalog
    {
        private const string TableName = "traps";
        private const string LabelColumn = "Label";

        public static IReadOnlyList<BehaviorChoice> Read(TwoDaService? twoDa)
        {
            if (twoDa == null || !twoDa.TryGetTable(TableName, out var table) || table == null)
                return Array.Empty<BehaviorChoice>();

            var traps = new List<BehaviorChoice>();
            for (var row = 0; row < table.RowCount; row++)
            {
                var label = table.GetString(row, LabelColumn);
                if (string.IsNullOrWhiteSpace(label) || IsPlaceholder(label))
                    continue;

                traps.Add(new BehaviorChoice(row, Humanise(label)));
            }

            return traps;
        }

        /// <summary>
        /// Whether a row is padding rather than a trap. More than half of traps.2da is: 52 rows
        /// labelled <c>Bio_reserved</c> and 20 labelled <c>USER</c>, all of them holding a row index
        /// open and none of them a kind a trigger can be. Offering them buries the 57 real traps.
        /// </summary>
        private static bool IsPlaceholder(string label) =>
            label.StartsWith("Bio_reserved", StringComparison.OrdinalIgnoreCase) ||
            label.Equals("USER", StringComparison.OrdinalIgnoreCase);

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
