// SPDX-License-Identifier: MIT

namespace SWLOR.NWN.Formats.Key;

/// <summary>
/// One resource identity and BIF-table location declared by a KEY file.
/// </summary>
public sealed class KeyResourceEntry
{
    internal KeyResourceEntry(string resRef, ushort resourceType, uint resourceId)
    {
        ResRef = resRef;
        ResourceType = resourceType;
        ResourceId = resourceId;
        BifIndex = checked((int)(resourceId >> 20));
        VariableTableIndex = checked((int)(resourceId & 0x000F_FFFF));
    }

    public string ResRef { get; }

    public ushort ResourceType { get; }

    public uint ResourceId { get; }

    public int BifIndex { get; }

    public int VariableTableIndex { get; }
}
