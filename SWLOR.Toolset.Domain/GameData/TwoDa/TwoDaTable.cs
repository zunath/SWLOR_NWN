using System.Globalization;
using Nwn.Formats.TwoDa;
using SWLOR.NWN.Formats;

namespace SWLOR.Toolset.Domain.GameData.TwoDa
{
    /// <summary>
    /// Read-only view over a single parsed 2DA table. Thin wrapper around the shared format table
    /// <see cref="Nwn.Formats.TwoDa.TwoDaTable"/> that adds nullable int parsing and a table name for diagnostics.
    /// Row indices are positional (0-based, matching <see cref="RowCount"/>), not the row's
    /// LABEL column value.
    /// </summary>
    public sealed class TwoDaTable
    {
        private readonly Nwn.Formats.TwoDa.TwoDaTable _file;
        private readonly IReadOnlyList<string> _rowLabels;

        internal TwoDaTable(string name, Nwn.Formats.TwoDa.TwoDaTable file)
        {
            Name = name;
            _file = file;
            _rowLabels = file.Rows.Select(row => row.Label).ToArray();
        }

        internal static TwoDaTable Parse(string name, byte[] bytes)
        {
            try
            {
                return new TwoDaTable(name, TwoDaReader.Read(bytes, TwoDaReadOptions.EngineCompatible));
            }
            catch (FormatException exception) when (exception is not NwnFormatException)
            {
                throw new NwnFormatException(exception.Message, exception);
            }
        }

        internal Nwn.Formats.TwoDa.TwoDaTable NativeTable => _file;

        public string Name { get; }

        public int RowCount => _file.Rows.Count;

        public IReadOnlyList<string> ColumnNames => _file.Columns;

        public IReadOnlyList<string> RowLabels => _rowLabels;

        public bool HasColumn(string column) =>
            !string.IsNullOrWhiteSpace(column) && _file.Columns.Contains(column, StringComparer.OrdinalIgnoreCase);

        /// <summary>Returns the source row label, or null when the position is out of range.</summary>
        public string? GetRowLabel(int row) =>
            row >= 0 && row < _file.Rows.Count ? _file.Rows[row].Label : null;

        /// <summary>
        /// Returns the raw cell text, or null if the row/column is out of range, the column does
        /// not exist, or the cell is the 2DA empty marker (****).
        /// </summary>
        public string? GetString(int row, string column)
        {
            if (row < 0 || row >= _file.Rows.Count)
                return _file.DefaultValue;
            if (!HasColumn(column))
                return null;
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
