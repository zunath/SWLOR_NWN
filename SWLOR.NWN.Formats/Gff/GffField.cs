// SPDX-License-Identifier: MIT

namespace SWLOR.NWN.Formats.Gff;

/// <summary>
/// One labeled GFF value.
/// </summary>
public sealed class GffField
{
    public const uint BYTE = 0;
    public const uint CHAR = 1;
    public const uint WORD = 2;
    public const uint SHORT = 3;
    public const uint DWORD = 4;
    public const uint INT = 5;
    public const uint DWORD64 = 6;
    public const uint INT64 = 7;
    public const uint FLOAT = 8;
    public const uint DOUBLE = 9;
    public const uint CExoString = 10;
    public const uint CResRef = 11;
    public const uint CExoLocString = 12;
    public const uint VOID = 13;
    public const uint Struct = 14;
    public const uint List = 15;

    public GffField(uint type, string label, object? value)
    {
        Type = type;
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Value = value;
    }

    public uint Type { get; }

    public string Label { get; }

    public object? Value { get; }
}
