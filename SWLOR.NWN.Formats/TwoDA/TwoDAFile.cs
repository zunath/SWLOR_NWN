// SPDX-License-Identifier: MIT

namespace SWLOR.NWN.Formats.TwoDA;

/// <summary>
/// A parsed two-dimensional array. Row access is positional; source row labels are preserved
/// separately for diagnostics and compatibility tooling.
/// </summary>
public sealed class TwoDAFile
{
    private readonly IReadOnlyList<IReadOnlyList<string?>> _rows;
    private readonly Dictionary<string, int> _columnIndices;

    internal TwoDAFile(
        IReadOnlyList<string> columns,
        IReadOnlyList<string> rowLabels,
        IReadOnlyList<IReadOnlyList<string?>> rows,
        string? defaultValue)
    {
        Columns = columns;
        RowLabels = rowLabels;
        _rows = rows;
        DefaultValue = defaultValue;
        _columnIndices = columns
            .Select((name, index) => (name, index))
            .ToDictionary(pair => pair.name, pair => pair.index, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> Columns { get; }

    public IReadOnlyList<string> RowLabels { get; }

    public string? DefaultValue { get; }

    public int RowCount => _rows.Count;

    public bool HasColumn(string column)
    {
        return !string.IsNullOrWhiteSpace(column) && _columnIndices.ContainsKey(column);
    }

    public string? GetValue(int row, string column)
    {
        if (row < 0 || row >= _rows.Count || !_columnIndices.TryGetValue(column, out var columnIndex))
            return DefaultValue;

        return _rows[row][columnIndex];
    }
}
