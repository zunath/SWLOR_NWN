// SPDX-License-Identifier: MIT

namespace SWLOR.NWN.Formats.Gff;

/// <summary>
/// A typed GFF structure.
/// </summary>
public sealed class GffStruct
{
    public uint Type { get; init; }

    public IList<GffField> Fields { get; } = new List<GffField>();
}
