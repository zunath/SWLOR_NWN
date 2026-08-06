// SPDX-License-Identifier: MIT

namespace SWLOR.NWN.Formats.Tlk;

/// <summary>
/// A parsed read-only TLK V3.0 talk table.
/// </summary>
public sealed class TlkFile
{
    internal TlkFile(uint languageId, IReadOnlyList<TlkEntry> entries)
    {
        LanguageId = languageId;
        Entries = entries;
    }

    public uint LanguageId { get; }

    public IReadOnlyList<TlkEntry> Entries { get; }

    public string? GetString(uint stringRef)
    {
        return stringRef < Entries.Count ? Entries[(int)stringRef].Text : null;
    }
}
