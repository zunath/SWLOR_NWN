// SPDX-License-Identifier: MIT

namespace SWLOR.NWN.Formats.Gff;

/// <summary>
/// A localized string reference plus its ordered embedded language/gender strings.
/// </summary>
public sealed class CExoLocString
{
    public uint StrRef { get; set; } = uint.MaxValue;

    public uint SubStringCount => checked((uint)LocalizedStrings.Count);

    public IDictionary<uint, string> LocalizedStrings { get; } = new Dictionary<uint, string>();

    public void SetString(uint stringId, string value)
    {
        LocalizedStrings[stringId] = value ?? throw new ArgumentNullException(nameof(value));
    }
}
