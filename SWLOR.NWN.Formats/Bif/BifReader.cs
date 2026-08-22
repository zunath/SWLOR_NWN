// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using SWLOR.NWN.Formats.Internal;

namespace SWLOR.NWN.Formats.Bif;

/// <summary>
/// Reads BioWare BIF V1 metadata and validates all variable-resource ranges.
/// </summary>
public static class BifReader
{
    private const int HeaderSize = 20;
    private const int VariableEntrySize = 16;
    private const int MaximumResources = 16_000_000;
    internal const uint MaximumResourceSize = 256u * 1024 * 1024;

    public static BifFile ReadMetadataOnly(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var entries = ReadMetadata(stream, stream.Length);
        return new BifFile(path, entries);
    }

    public static BifFile Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using var stream = new MemoryStream(bytes, writable: false);
        var entries = ReadMetadata(stream, bytes.LongLength);
        return new BifFile(bytes, entries);
    }

    private static IReadOnlyList<BifResourceEntry> ReadMetadata(Stream stream, long length)
    {
        if (length < HeaderSize)
            throw new NwnFormatException("BIF header is truncated.");

        Span<byte> header = stackalloc byte[HeaderSize];
        stream.ReadExactly(header);
        if (!header[..4].SequenceEqual("BIFF"u8))
            throw new NwnFormatException("Invalid BIF signature.");
        if (!header.Slice(4, 4).SequenceEqual("V1  "u8))
            throw new NwnFormatException("Unsupported BIF version; expected V1.");

        var variableCount = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(8, 4));
        var fixedCount = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(12, 4));
        var tableOffset = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(16, 4));
        if (fixedCount != 0)
            throw new NwnFormatException("BIF fixed resources are not supported; BioWare documents them as unimplemented.");
        if (variableCount > MaximumResources)
            throw new NwnFormatException($"BIF resource count {variableCount} exceeds {MaximumResources}.");

        var allocationBudget = new AllocationBudget("BIF");
        allocationBudget.ReserveElements(variableCount, 64, "BIF resource metadata");

        long tableBytes;
        try
        {
            tableBytes = checked((long)variableCount * VariableEntrySize);
        }
        catch (OverflowException ex)
        {
            throw new NwnFormatException("BIF resource-table length overflows.", ex);
        }
        ValidateRange(tableOffset, tableBytes, length, "BIF variable-resource table");
        stream.Seek(tableOffset, SeekOrigin.Begin);

        var entries = new List<BifResourceEntry>(checked((int)variableCount));
        Span<byte> buffer = stackalloc byte[VariableEntrySize];
        for (var index = 0; index < variableCount; index++)
        {
            stream.ReadExactly(buffer);
            var id = BinaryPrimitives.ReadUInt32LittleEndian(buffer[..4]);
            var offset = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(4, 4));
            var size = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(8, 4));
            var resourceType = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(12, 4));
            if (size > MaximumResourceSize)
            {
                throw new NwnFormatException(
                    $"BIF resource {index} size {size} exceeds the {MaximumResourceSize}-byte extraction limit.");
            }
            ValidateRange(offset, size, length, $"BIF resource {index}");
            entries.Add(new BifResourceEntry(id, offset, size, resourceType));
        }

        return entries;
    }

    private static void ValidateRange(long offset, long count, long length, string context)
    {
        if (offset < 0 || count < 0 || offset > length || count > length - offset)
            throw new NwnFormatException($"{context} range is outside the {length}-byte BIF.");
        if (count > int.MaxValue)
            throw new NwnFormatException($"{context} exceeds the supported in-memory resource size.");
    }
}
