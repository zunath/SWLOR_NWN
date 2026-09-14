// SPDX-License-Identifier: MIT

namespace SWLOR.NWN.Formats.Tlk;

/// <summary>
/// One TLK string-table entry.
/// </summary>
public sealed class TlkEntry
{
    internal TlkEntry(uint flags, string soundResRef, float soundLength, string? text)
    {
        Flags = flags;
        SoundResRef = soundResRef;
        SoundLength = soundLength;
        Text = text;
    }

    public uint Flags { get; }

    public string SoundResRef { get; }

    public float SoundLength { get; }

    public string? Text { get; }
}
