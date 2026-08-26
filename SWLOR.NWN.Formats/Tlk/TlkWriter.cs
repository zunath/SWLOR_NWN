// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using System.Text;
using SWLOR.NWN.Formats.Internal;

namespace SWLOR.NWN.Formats.Tlk;

/// <summary>
/// Writes text-only BioWare TLK V3.0 talk tables.
/// </summary>
public static class TlkWriter
{
    private const int HeaderSize = 20;
    private const int EntrySize = 40;
    private const uint TextPresent = 0x0001;

    /// <summary>
    /// Writes a sparse set of text entries to a TLK file. Missing IDs are emitted as blank records.
    /// </summary>
    public static void Write(
        string path,
        uint languageId,
        IReadOnlyDictionary<int, string> entries)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        File.WriteAllBytes(path, Write(languageId, entries));
    }

    /// <summary>
    /// Creates a TLK containing the supplied text entries. The table extends through the greatest
    /// supplied ID, leaving all missing IDs blank.
    /// </summary>
    public static byte[] Write(uint languageId, IReadOnlyDictionary<int, string> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var encoding = NwnTextEncoding.ForLanguageStrict(languageId);
        var encodedEntries = new SortedDictionary<int, byte[]>();
        var maximumId = -1;
        long totalTextLength = 0;
        long totalDecodedTextBytes = 0;

        foreach (var (id, text) in entries)
        {
            if (id < 0 || id > TlkFormatLimits.MaximumEntryId)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(entries),
                    id,
                    $"TLK entry IDs must be between 0 and {TlkFormatLimits.MaximumEntryId}.");
            }

            if (text is null)
            {
                throw new ArgumentException($"TLK entry {id} has null text.", nameof(entries));
            }

            byte[] encodedText;
            try
            {
                encodedText = encoding.GetBytes(text);
            }
            catch (EncoderFallbackException exception)
            {
                throw new ArgumentException(
                    $"TLK entry {id} contains text that cannot be encoded for language {languageId}.",
                    nameof(entries),
                    exception);
            }

            if (!encodedEntries.TryAdd(id, encodedText))
            {
                throw new ArgumentException($"TLK entry ID {id} occurs more than once.", nameof(entries));
            }

            totalTextLength = checked(totalTextLength + encodedText.Length);
            totalDecodedTextBytes = checked(totalDecodedTextBytes + (long)text.Length * sizeof(char));
            maximumId = Math.Max(maximumId, id);
        }

        var entryCount = maximumId + 1;
        var estimatedDecodedAllocation = checked(
            (long)entryCount * TlkFormatLimits.EstimatedManagedBytesPerEntry + totalDecodedTextBytes);
        if (estimatedDecodedAllocation > TlkFormatLimits.MaximumDecodedAllocationBytes)
        {
            throw new ArgumentException(
                $"TLK decoded metadata and text require an estimated {estimatedDecodedAllocation} bytes, " +
                $"exceeding the supported {TlkFormatLimits.MaximumDecodedAllocationBytes}-byte budget.",
                nameof(entries));
        }

        var tableLength = checked((long)entryCount * EntrySize);
        var stringsOffset = checked(HeaderSize + tableLength);
        var fileLength = checked(stringsOffset + totalTextLength);
        if (fileLength > int.MaxValue)
        {
            throw new ArgumentException(
                $"TLK output is {fileLength} bytes, exceeding the supported maximum of {int.MaxValue} bytes.",
                nameof(entries));
        }

        var bytes = new byte[(int)fileLength];
        "TLK "u8.CopyTo(bytes);
        "V3.0"u8.CopyTo(bytes.AsSpan(4));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), languageId);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), (uint)entryCount);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16), (uint)stringsOffset);

        var relativeTextOffset = 0;
        foreach (var (id, encodedText) in encodedEntries)
        {
            var entryOffset = HeaderSize + id * EntrySize;
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(entryOffset), TextPresent);
            BinaryPrimitives.WriteUInt32LittleEndian(
                bytes.AsSpan(entryOffset + 28),
                (uint)relativeTextOffset);
            BinaryPrimitives.WriteUInt32LittleEndian(
                bytes.AsSpan(entryOffset + 32),
                (uint)encodedText.Length);

            encodedText.CopyTo(bytes, checked((int)stringsOffset + relativeTextOffset));
            relativeTextOffset = checked(relativeTextOffset + encodedText.Length);
        }

        return bytes;
    }
}
