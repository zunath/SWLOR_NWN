// SPDX-License-Identifier: MIT

namespace SWLOR.NWN.Formats.Gff;

/// <summary>
/// A parsed Generic File Format document.
/// </summary>
public sealed class GffFile
{
    public string FileType { get; init; } = string.Empty;

    public string FileVersion { get; init; } = string.Empty;

    public required GffStruct RootStruct { get; init; }
}
