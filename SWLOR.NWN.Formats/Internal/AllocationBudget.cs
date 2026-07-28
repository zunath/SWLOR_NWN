// SPDX-License-Identifier: MIT

namespace SWLOR.NWN.Formats.Internal;

/// <summary>
/// Applies a conservative cumulative ceiling to allocations derived from untrusted format tables.
/// The charge is an estimate of managed payload, not a measurement of CLR object layout.
/// </summary>
internal sealed class AllocationBudget
{
    public const long DefaultMaximumBytes = 64L * 1024 * 1024;

    private readonly long _maximumBytes;
    private readonly string _format;
    private long _reservedBytes;

    /// <summary>
    /// Total bytes reserved so far. Readers measure the delta across a nested parse to learn a
    /// subtree's retained cost, so aliased references can be charged that full cost again.
    /// </summary>
    public long ReservedBytes => _reservedBytes;

    public AllocationBudget(string format, long maximumBytes = DefaultMaximumBytes)
    {
        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentException("A format name is required.", nameof(format));
        if (maximumBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumBytes));

        _format = format;
        _maximumBytes = maximumBytes;
    }

    public void Reserve(long bytes, string context)
    {
        if (bytes < 0)
            throw new NwnFormatException($"{context} has a negative allocation size.");

        if (bytes > _maximumBytes - _reservedBytes)
        {
            throw new NwnFormatException(
                $"{_format} cumulative allocation budget exceeds {_maximumBytes} bytes while reading {context}.");
        }

        _reservedBytes += bytes;
    }

    public void ReserveElements(long count, int estimatedBytesPerElement, string context)
    {
        if (count < 0 || estimatedBytesPerElement < 0)
            throw new NwnFormatException($"{context} has an invalid allocation count.");

        long bytes;
        try
        {
            bytes = checked(count * estimatedBytesPerElement);
        }
        catch (OverflowException ex)
        {
            throw new NwnFormatException($"{context} allocation size overflows.", ex);
        }

        Reserve(bytes, context);
    }
}
