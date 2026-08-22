// SPDX-License-Identifier: MIT

namespace SWLOR.NWN.Formats.Gff;

/// <summary>
/// An ordered GFF list of structures.
/// </summary>
public sealed class GffList
{
    public IList<GffStruct> Elements { get; } = new List<GffStruct>();
}
