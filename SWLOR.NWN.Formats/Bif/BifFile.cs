// SPDX-License-Identifier: MIT

namespace SWLOR.NWN.Formats.Bif;

/// <summary>
/// BIF metadata with on-demand variable-resource extraction.
/// </summary>
public sealed class BifFile
{
    private readonly string? _path;
    private readonly byte[]? _bytes;

    internal BifFile(string path, IReadOnlyList<BifResourceEntry> entries)
    {
        _path = path;
        VariableResources = entries;
    }

    internal BifFile(byte[] bytes, IReadOnlyList<BifResourceEntry> entries)
    {
        _bytes = bytes;
        VariableResources = entries;
    }

    public IReadOnlyList<BifResourceEntry> VariableResources { get; }

    public byte[]? ExtractVariableResource(int index, int maximumBytes = int.MaxValue)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximumBytes);
        if (index < 0 || index >= VariableResources.Count)
            return null;

        var entry = VariableResources[index];
        var limit = Math.Min((long)maximumBytes, BifReader.MaximumResourceSize);
        if (entry.Size > limit)
        {
            throw new NwnFormatException(
                $"BIF resource {index} size {entry.Size} exceeds the {limit}-byte extraction limit.");
        }

        if (_bytes != null)
            return _bytes.AsSpan(checked((int)entry.Offset), checked((int)entry.Size)).ToArray();

        if (_path == null)
            return null;

        using var stream = new FileStream(
            _path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            FileOptions.RandomAccess);
        if (entry.Offset > stream.Length || entry.Size > stream.Length - entry.Offset)
            throw new NwnFormatException("BIF resource is outside the current archive.");
        stream.Seek(entry.Offset, SeekOrigin.Begin);
        var result = new byte[checked((int)entry.Size)];
        stream.ReadExactly(result);
        return result;
    }
}
