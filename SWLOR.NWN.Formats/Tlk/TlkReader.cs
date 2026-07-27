// SPDX-License-Identifier: MIT

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
    private const int MaximumEntries = 8_000_000;

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
        if (count > MaximumEntries)
            throw new NwnFormatException($"TLK entry count {count} exceeds {MaximumEntries}.");
        var stringsOffset = reader.ReadUInt32(16);
        var tableBytes = GuardedBinaryReader.CheckedCount(count, EntrySize, MaximumEntries, "TLK entries");
        reader.ValidateRange(HeaderSize, tableBytes, "TLK entry table");
        if (stringsOffset < HeaderSize + tableBytes || stringsOffset > reader.Length)
            throw new NwnFormatException("TLK string-data offset is outside the valid data region.");

        var encoding = NwnTextEncoding.ForLanguage(languageId);
        var entries = new List<TlkEntry>(checked((int)count));
        for (var index = 0; index < count; index++)
        {
            var entryOffset = HeaderSize + (long)index * EntrySize;
            var flags = reader.ReadUInt32(entryOffset);
            var soundResRef = (flags & SoundPresent) == 0
                ? string.Empty
                : reader.ReadAscii(entryOffset + 4, 16, "TLK sound ResRef", trimNull: true);
            var relativeTextOffset = reader.ReadUInt32(entryOffset + 28);
            var textLength = reader.ReadUInt32(entryOffset + 32);
            var soundLength = (flags & SoundLengthPresent) == 0 ? 0f : reader.ReadSingle(entryOffset + 36);

            string? text = null;
            if ((flags & TextPresent) != 0)
            {
                var absoluteTextOffset = checked((long)stringsOffset + relativeTextOffset);
                reader.ValidateRange(absoluteTextOffset, textLength, $"TLK string {index}");
                text = encoding.GetString(reader.Slice(absoluteTextOffset, textLength, $"TLK string {index}"));
            }

            entries.Add(new TlkEntry(flags, soundResRef, soundLength, text));
        }

        return new TlkFile(languageId, entries);
    }
}
