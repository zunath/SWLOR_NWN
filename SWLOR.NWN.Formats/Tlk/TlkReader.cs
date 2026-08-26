// SPDX-License-Identifier: MIT

using SWLOR.NWN.Formats.Common;
using SWLOR.NWN.Formats.Internal;

namespace SWLOR.NWN.Formats.Tlk;

/// <summary>
/// Reads BioWare TLK V3.0 talk tables.
/// </summary>
public static class TlkReader
{
    private const int HeaderSize = 20;
    private const int EntrySize = 40;
    private const uint TextPresent = 0x0001;
    private const uint SoundPresent = 0x0002;
    private const uint SoundLengthPresent = 0x0004;

    public static TlkFile Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Read(File.ReadAllBytes(path));
    }

    public static TlkFile Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        var reader = new GuardedBinaryReader(bytes);
        reader.ValidateRange(0, HeaderSize, "TLK header");
        if (reader.ReadAscii(0, 4, "TLK signature") != "TLK ")
            throw new NwnFormatException("Invalid TLK signature.");
        if (reader.ReadAscii(4, 4, "TLK version") != "V3.0")
            throw new NwnFormatException("Unsupported TLK version; expected V3.0.");

        var languageId = reader.ReadUInt32(8);
        var count = reader.ReadUInt32(12);
        if (count > TlkFormatLimits.MaximumEntryCount)
        {
            throw new NwnFormatException(
                $"TLK entry count {count} exceeds {TlkFormatLimits.MaximumEntryCount}.");
        }
        var stringsOffset = reader.ReadUInt32(16);
        var tableBytes = GuardedBinaryReader.CheckedCount(
            count,
            EntrySize,
            TlkFormatLimits.MaximumEntryCount,
            "TLK entries");
        reader.ValidateRange(HeaderSize, tableBytes, "TLK entry table");
        if (stringsOffset < HeaderSize + tableBytes || stringsOffset > reader.Length)
            throw new NwnFormatException("TLK string-data offset is outside the valid data region.");

        var encoding = NwnTextEncoding.ForLanguage(languageId);
        // The entry list, entry objects, and sound resrefs are allocations too - charge them
        // before building anything so an 8M-entry table cannot blow past the budget on metadata
        // alone, the same way the KEY and BIF readers budget their tables.
        var allocationBudget = new AllocationBudget(
            "TLK",
            TlkFormatLimits.MaximumDecodedAllocationBytes);
        allocationBudget.ReserveElements(
            count,
            TlkFormatLimits.EstimatedManagedBytesPerEntry,
            "TLK entry table");
        var entries = new List<TlkEntry>(checked((int)count));
        // Aliased entries (many records pointing at the same string-data range) share one decoded
        // string, and every unique decode is charged against a cumulative budget so a small file
        // cannot expand into gigabytes of managed strings.
        var decodedStrings = new Dictionary<(uint Offset, uint Length), string>();
        for (var index = 0; index < count; index++)
        {
            var entryOffset = HeaderSize + (long)index * EntrySize;
            var flags = reader.ReadUInt32(entryOffset);
            var soundResRef = (flags & SoundPresent) == 0
                ? string.Empty
                : reader.ReadAscii(
                    entryOffset + 4, NwnResRef.MaxLength, "TLK sound ResRef", trimNull: true);
            var relativeTextOffset = reader.ReadUInt32(entryOffset + 28);
            var textLength = reader.ReadUInt32(entryOffset + 32);
            var soundLength = (flags & SoundLengthPresent) == 0 ? 0f : reader.ReadSingle(entryOffset + 36);

            string? text = null;
            if ((flags & TextPresent) != 0)
            {
                if (!decodedStrings.TryGetValue((relativeTextOffset, textLength), out text))
                {
                    var absoluteTextOffset = checked((long)stringsOffset + relativeTextOffset);
                    reader.ValidateRange(absoluteTextOffset, textLength, $"TLK string {index}");
                    var textBytes = reader.Slice(absoluteTextOffset, textLength, $"TLK string {index}");
                    var characterCount = encoding.GetCharCount(textBytes);
                    allocationBudget.Reserve(
                        checked((long)characterCount * sizeof(char)),
                        $"TLK string {index}");
                    text = encoding.GetString(textBytes);
                    decodedStrings[(relativeTextOffset, textLength)] = text;
                }
            }

            entries.Add(new TlkEntry(flags, soundResRef, soundLength, text));
        }

        return new TlkFile(languageId, entries);
    }
}
