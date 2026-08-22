using System.Globalization;
using SWLOR.NWN.Formats.TwoDA;

namespace SWLOR.Toolset.Domain.GameData.TwoDa
{
    /// <summary>
    /// Read-only view over a single parsed 2DA table. Thin wrapper around SWLOR.NWN.Formats'
    /// <see cref="TwoDAFile"/> that adds nullable int parsing and a table name for diagnostics.
    /// Row indices are positional (0-based, matching <see cref="RowCount"/>), not the row's
    /// LABEL column value.
    /// </summary>
    public sealed class TwoDaTable
    {
        private readonly TwoDAFile _file;

        internal TwoDaTable(string name, TwoDAFile file)
        {
            Name = name;
            _file = file;
        }

        public string Name { get; }

        public int RowCount => _file.RowCount;

        public IReadOnlyList<string> ColumnNames => _file.Columns;

        public bool HasColumn(string column) => _file.HasColumn(column);

        /// <summary>
        /// Returns the raw cell text, or null if the row/column is out of range, the column does
        /// not exist, or the cell is the 2DA empty marker (****).
        /// </summary>
        public string? GetString(int row, string column)
        {
            return _file.GetValue(row, column);
        }

        /// <summary>
        /// Returns the cell parsed as an integer, or null if the cell is empty/missing/out of
        /// range. Throws <see cref="FormatException"/> if the cell has text that is not a valid
        /// integer, so callers get a clear signal that the wrong column type was requested.
        /// </summary>
        public int? GetInt(int row, string column)
        {
            var raw = GetString(row, column);
            if (raw is null)
                return null;

            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                throw new FormatException(
                    $"2DA table '{Name}' row {row} column '{column}' value '{raw}' is not a valid integer.");
            }

            return value;
        }
    }
}
