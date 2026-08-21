// SPDX-License-Identifier: MIT

namespace SWLOR.NWN.Formats.Common;

/// <summary>Shared engine limits and shape checks for Aurora resource references.</summary>
public static class NwnResRef
{
    /// <summary>
    /// Maximum resource-reference length imposed by NWN's fixed-width resource indexes and GFF
    /// CResRef storage.
    /// </summary>
    public const int MaxLength = 16;

    /// <summary>Whether a non-empty value is a legal case-insensitive Aurora resource reference.</summary>
    public static bool IsValid(string? value) =>
        value is { Length: >= 1 and <= MaxLength } &&
        value.All(character =>
            char.IsAsciiLetterOrDigit(character) ||
            character == '_');

    /// <summary>Whether a non-empty value is in the lowercase canonical form used by module files.</summary>
    public static bool IsCanonical(string? value) =>
        value is { Length: >= 1 and <= MaxLength } &&
        value.All(character =>
            character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_');
}
