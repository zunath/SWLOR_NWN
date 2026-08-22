// SPDX-License-Identifier: MIT

namespace SWLOR.NWN.Formats.Key;

/// <summary>
/// One BIF archive declared by a KEY file.
/// </summary>
public sealed class KeyBifEntry
{
    internal KeyBifEntry(uint fileSize, string filename, ushort drives)
    {
        FileSize = fileSize;
        Filename = filename;
        Drives = drives;
    }

    public uint FileSize { get; }

    public string Filename { get; }

    public ushort Drives { get; }
}
