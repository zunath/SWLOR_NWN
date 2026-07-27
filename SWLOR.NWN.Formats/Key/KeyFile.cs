// SPDX-License-Identifier: MIT

namespace SWLOR.NWN.Formats.Key;

/// <summary>
/// Read-only metadata from one KEY archive index.
/// </summary>
public sealed class KeyFile
{
    internal KeyFile(IReadOnlyList<KeyBifEntry> bifEntries, IReadOnlyList<KeyResourceEntry> resourceEntries)
    {
        BifEntries = bifEntries;
        ResourceEntries = resourceEntries;
    }

    public IReadOnlyList<KeyBifEntry> BifEntries { get; }

    public IReadOnlyList<KeyResourceEntry> ResourceEntries { get; }

    public KeyBifEntry? GetBifForResource(KeyResourceEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return entry.BifIndex >= 0 && entry.BifIndex < BifEntries.Count
            ? BifEntries[entry.BifIndex]
            : null;
    }
}
