// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using System.Text;
using SWLOR.NWN.Formats.Internal;

namespace SWLOR.NWN.Formats.TwoDA;

/// <summary>
/// Reads Aurora text <c>2DA V2.0</c> and binary <c>2DA V2.b</c> tables.
/// </summary>
public static class TwoDAReader
{
    private static readonly byte[] TextSignature = Encoding.ASCII.GetBytes("2DA V2.0");
    private static readonly byte[] BinarySignature = Encoding.ASCII.GetBytes("2DA V2.b\n");
    private const int MaximumColumns = 16_384;
    private const int MaximumRows = 4_000_000;
    private const int MaximumCells = 32_000_000;

    public static TwoDAFile Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Read(File.ReadAllBytes(path));
    }

    public static TwoDAFile Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        var span = bytes.AsSpan();
        if (span.StartsWith(Encoding.UTF8.Preamble))
            span = span[Encoding.UTF8.Preamble.Length..];

        if (span.StartsWith(BinarySignature))
            return ReadBinary(span.ToArray());
        if (span.StartsWith(TextSignature))
            return ReadText(span);

        throw new NwnFormatException("The resource is neither text 2DA V2.0 nor binary 2DA V2.b.");
    }

    private static TwoDAFile ReadText(ReadOnlySpan<byte> bytes)
    {
        var text = NwnTextEncoding.DecodeGeneral(bytes);
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        var cursor = 0;

        var signature = NextNonEmpty(lines, ref cursor);
        if (!string.Equals(signature.Trim(), "2DA V2.0", StringComparison.Ordinal))
            throw new NwnFormatException("Invalid text 2DA signature.");

        string? defaultValue = null;
        var next = NextNonEmpty(lines, ref cursor);
        if (next.TrimStart().StartsWith("DEFAULT:", StringComparison.OrdinalIgnoreCase))
        {
            var defaultTokens = Tokenize(next[(next.IndexOf(':') + 1)..]);
            defaultValue = defaultTokens.Count == 0 ? string.Empty : defaultTokens[0].Value;
            next = NextNonEmpty(lines, ref cursor);
        }

        var columns = Tokenize(next).Select(token => token.Value).ToArray();
        if (columns.Length == 0 || columns.Length > MaximumColumns)
            throw new NwnFormatException($"2DA column count {columns.Length} is invalid.");
        if (columns.Any(string.IsNullOrWhiteSpace))
            throw new NwnFormatException("2DA column names must not be empty.");
        if (columns.Distinct(StringComparer.OrdinalIgnoreCase).Count() != columns.Length)
            throw new NwnFormatException("2DA column names must be unique.");

        ValidateTextShape(lines, cursor, columns.Length);

        var labels = new List<string>();
        var rows = new List<IReadOnlyList<string?>>();
        for (; cursor < lines.Length; cursor++)
        {
            if (string.IsNullOrWhiteSpace(lines[cursor]))
                continue;

            if (rows.Count >= MaximumRows)
                throw new NwnFormatException($"2DA row count exceeds {MaximumRows}.");

            var tokens = Tokenize(lines[cursor]);
            if (tokens.Count == 0)
                continue;

            labels.Add(tokens[0].Value);
            var cells = tokens.Skip(1).Select(token => token.Value).ToList();

            // Shipped BioWare tables are permissive at the edges: some end with a label-only
            // sentinel row, while a few put an unquoted phrase in their final column. Missing
            // cells are null; surplus tokens belong to the last declared column. This preserves
            // the data instead of rejecting resources the engine itself consumes.
            while (cells.Count < columns.Length)
                cells.Add("****");
            if (cells.Count > columns.Length)
            {
                cells[columns.Length - 1] = string.Join(' ', cells.Skip(columns.Length - 1));
                cells.RemoveRange(columns.Length, cells.Count - columns.Length);
            }

            rows.Add(cells
                .Select(value => value == "****" ? null : value)
                .ToArray());
        }

        return new TwoDAFile(columns, labels, rows, defaultValue);
    }

    private static TwoDAFile ReadBinary(byte[] bytes)
    {
        var reader = new GuardedBinaryReader(bytes);
        var cursor = BinarySignature.Length;
        var columns = ReadDelimitedStrings(reader, ref cursor, (byte)'\t', stopAtNull: true, MaximumColumns, "2DA columns");
        if (columns.Count == 0)
            throw new NwnFormatException("Binary 2DA contains no columns.");
        if (columns.Any(string.IsNullOrWhiteSpace))
            throw new NwnFormatException("Binary 2DA column names must not be empty.");
        if (columns.Distinct(StringComparer.OrdinalIgnoreCase).Count() != columns.Count)
            throw new NwnFormatException("Binary 2DA column names must be unique.");

        var rowCount = reader.ReadUInt32(cursor);
        cursor += 4;
        if (rowCount > MaximumRows)
            throw new NwnFormatException($"Binary 2DA row count {rowCount} exceeds {MaximumRows}.");

        var rowLabels = ReadDelimitedStrings(
            reader,
            ref cursor,
            (byte)'\t',
            stopAtNull: false,
            checked((int)rowCount),
            "2DA row labels");
        if (rowLabels.Count != rowCount)
            throw new NwnFormatException("Binary 2DA row label count does not match the header.");

        long cellCount;
        try
        {
            cellCount = checked((long)rowCount * columns.Count);
        }
        catch (OverflowException ex)
        {
            throw new NwnFormatException("Binary 2DA cell count overflows.", ex);
        }
        if (cellCount > MaximumCells)
            throw new NwnFormatException($"Binary 2DA cell count {cellCount} exceeds {MaximumCells}.");

        var offsetsByteCount = checked(cellCount * 2);
        reader.ValidateRange(cursor, offsetsByteCount + 2, "2DA cell offsets and padding");
        var offsetsStart = cursor;
        cursor = checked(cursor + (int)offsetsByteCount);
        // The two bytes after the offset table declare the string-data section's size; every cell
        // offset and its terminator must land inside that declared window, not merely inside the
        // file, or a malformed table reads arbitrary trailing bytes as cell text.
        var dataSize = reader.ReadUInt16(cursor);
        cursor += 2;
        var dataStart = cursor;
        reader.ValidateRange(dataStart, dataSize, "2DA string data");
        var dataEnd = dataStart + dataSize;

        var rows = new List<IReadOnlyList<string?>>(checked((int)rowCount));
        for (var row = 0; row < rowCount; row++)
        {
            var values = new string?[columns.Count];
            for (var column = 0; column < columns.Count; column++)
            {
                var cellIndex = checked((long)row * columns.Count + column);
                var relativeOffset = reader.ReadUInt16(offsetsStart + cellIndex * 2);
                if (relativeOffset >= dataSize)
                    throw new NwnFormatException("Binary 2DA cell offset lies outside the declared string data.");
                var value = ReadNullTerminated(reader, dataStart + relativeOffset, "2DA cell", dataEnd);
                values[column] = value == "****" ? null : value;
            }
            rows.Add(values);
        }

        return new TwoDAFile(columns, rowLabels, rows, defaultValue: null);
    }

    private static void ValidateTextShape(string[] lines, int cursor, int columnCount)
    {
        long rowCount = 0;
        for (var index = cursor; index < lines.Length; index++)
        {
            if (!string.IsNullOrWhiteSpace(lines[index]))
                rowCount++;
        }

        if (rowCount > MaximumRows)
            throw new NwnFormatException($"2DA row count {rowCount} exceeds {MaximumRows}.");

        long cellCount;
        try
        {
            cellCount = checked(rowCount * columnCount);
        }
        catch (OverflowException ex)
        {
            throw new NwnFormatException("Text 2DA cell count overflows.", ex);
        }

        if (cellCount > MaximumCells)
            throw new NwnFormatException($"Text 2DA cell count {cellCount} exceeds {MaximumCells}.");
    }

    private static string NextNonEmpty(string[] lines, ref int cursor)
    {
        while (cursor < lines.Length)
        {
            var line = lines[cursor++];
            if (!string.IsNullOrWhiteSpace(line))
                return line;
        }

        throw new NwnFormatException("2DA ended before its required header rows.");
    }

    private static List<Token> Tokenize(string line)
    {
        var result = new List<Token>();
        var cursor = 0;
        while (cursor < line.Length)
        {
            while (cursor < line.Length && char.IsWhiteSpace(line[cursor]))
                cursor++;
            if (cursor >= line.Length)
                break;

            if (line[cursor] == '"')
            {
                var start = ++cursor;
                while (cursor < line.Length && line[cursor] != '"')
                    cursor++;
                if (cursor >= line.Length)
                    throw new NwnFormatException("Unterminated quoted 2DA cell.");
                result.Add(new Token(line[start..cursor]));
                cursor++;
                if (cursor < line.Length && !char.IsWhiteSpace(line[cursor]))
                    throw new NwnFormatException("Unexpected text after a quoted 2DA cell.");
            }
            else
            {
                var start = cursor;
                while (cursor < line.Length && !char.IsWhiteSpace(line[cursor]))
                    cursor++;
                result.Add(new Token(line[start..cursor]));
            }
        }

        return result;
    }

    private static List<string> ReadDelimitedStrings(
        GuardedBinaryReader reader,
        ref int cursor,
        byte delimiter,
        bool stopAtNull,
        int maximumCount,
        string context)
    {
        var result = new List<string>();
        var start = cursor;
        while (cursor < reader.Length)
        {
            var value = reader.ReadByte(cursor++);
            if (stopAtNull && value == 0)
            {
                if (cursor - 1 > start)
                    throw new NwnFormatException($"{context} has an unterminated final value.");
                return result;
            }

            if (value != delimiter)
                continue;

            var length = cursor - start - 1;
            result.Add(Encoding.ASCII.GetString(reader.Slice(start, length, context)));
            if (result.Count > maximumCount)
                throw new NwnFormatException($"{context} count exceeds {maximumCount}.");
            start = cursor;
            if (!stopAtNull && result.Count == maximumCount)
                return result;
        }

        throw new NwnFormatException($"{context} is truncated.");
    }

    private static string ReadNullTerminated(GuardedBinaryReader reader, long offset, string context, long limit)
    {
        var end = offset;
        while (end < limit && reader.ReadByte(end) != 0)
            end++;
        if (end >= limit)
            throw new NwnFormatException($"{context} is not null terminated within its declared data section.");
        return NwnTextEncoding.DecodeGeneral(reader.Slice(offset, end - offset, context));
    }

    private readonly record struct Token(string Value);
}
