using System.Globalization;
using System.Text;

namespace SWLOR.CraftSim.Report
{
    /// <summary>Builds a markdown table and formats the numbers reports share.</summary>
    public sealed class MarkdownTable
    {
        private readonly List<string> _header;
        private readonly List<List<string>> _rows = new();

        public MarkdownTable(params string[] header)
        {
            _header = header.ToList();
        }

        public int RowCount => _rows.Count;

        public MarkdownTable Add(params string[] cells)
        {
            if (cells.Length != _header.Count)
                throw new ArgumentException($"A row needs {_header.Count} cells but got {cells.Length}.");

            _rows.Add(cells.ToList());
            return this;
        }

        public override string ToString()
        {
            var text = new StringBuilder();
            text.AppendLine("| " + string.Join(" | ", _header) + " |");
            text.AppendLine("| " + string.Join(" | ", _header.Select(_ => "---")) + " |");
            foreach (var row in _rows)
                text.AppendLine("| " + string.Join(" | ", row) + " |");

            return text.ToString();
        }

        public static string Percent(double fraction, int decimals = 0) =>
            (fraction * 100).ToString("F" + decimals, CultureInfo.InvariantCulture) + "%";

        public static string Number(double value, int decimals = 1) =>
            value.ToString("F" + decimals, CultureInfo.InvariantCulture);

        public static string Signed(int value) => value > 0 ? "+" + value.ToString(CultureInfo.InvariantCulture) : value.ToString(CultureInfo.InvariantCulture);
    }
}
