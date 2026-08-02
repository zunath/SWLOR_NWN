// SPDX-License-Identifier: MIT

namespace SWLOR.NWN.Formats.Bif;

/// <summary>
/// One variable-resource table entry in a BIF archive.
/// </summary>
public sealed class BifResourceEntry
{
    internal BifResourceEntry(uint id, uint offset, uint size, uint resourceType)
    {
        Id = id;
        Offset = offset;
        Size = size;
        ResourceType = resourceType;
    }

    public uint Id { get; }

    public uint Offset { get; }

    public uint Size { get; }

    public uint ResourceType { get; }
}
