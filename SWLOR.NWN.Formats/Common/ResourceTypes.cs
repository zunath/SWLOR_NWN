// SPDX-License-Identifier: MIT

namespace SWLOR.NWN.Formats.Common;

/// <summary>
/// Aurora resource type identifiers and their conventional file extensions.
/// </summary>
public static class ResourceTypes
{
    public const ushort Invalid = ushort.MaxValue;

    private static readonly IReadOnlyDictionary<string, ushort> ExtensionToType;
    private static readonly IReadOnlyDictionary<ushort, string> TypeToExtension;

    static ResourceTypes()
    {
        var pairs = new (ushort Type, string Extension)[]
        {
            (0, "res"), (1, "bmp"), (2, "mve"), (3, "tga"), (4, "wav"), (5, "wfx"),
            (6, "plt"), (7, "ini"), (8, "mp3"), (9, "mpg"), (10, "txt"),
            (2000, "plh"), (2001, "tex"), (2002, "mdl"), (2003, "thg"), (2005, "fnt"),
            (2007, "lua"), (2008, "slt"), (2009, "nss"), (2010, "ncs"), (2011, "mod"),
            (2012, "are"), (2013, "set"), (2014, "ifo"), (2015, "bic"), (2016, "wok"),
            (2017, "2da"), (2018, "tlk"), (2022, "txi"), (2023, "git"), (2024, "bti"),
            (2025, "uti"), (2026, "btc"), (2027, "utc"), (2029, "dlg"), (2030, "itp"),
            (2031, "btt"), (2032, "utt"), (2033, "dds"), (2034, "bts"), (2035, "uts"),
            (2036, "ltr"), (2037, "gff"), (2038, "fac"), (2039, "bte"), (2040, "ute"),
            (2041, "btd"), (2042, "utd"), (2043, "btp"), (2044, "utp"), (2045, "dft"),
            (2046, "gic"), (2047, "gui"), (2048, "css"), (2049, "ccs"), (2050, "btm"),
            (2051, "utm"), (2052, "dwk"), (2053, "pwk"), (2054, "btg"), (2055, "utg"),
            (2056, "jrl"), (2057, "sav"), (2058, "utw"), (2059, "4pc"), (2060, "ssf"),
            (2061, "hak"), (2062, "nwm"), (2063, "bik"), (2064, "ndb"), (2065, "ptm"),
            (2066, "ptt"), (2067, "bak"), (2068, "dat"), (2069, "shd"), (2070, "xbc"),
            (2071, "wbm"), (2072, "mtr"), (2073, "ktx"), (2074, "ttf"), (2075, "sql"),
            (2076, "tml"), (2077, "sq3"), (2078, "lod"), (2079, "gif"), (2080, "png"),
            (2081, "jpg"), (2082, "caf"), (9996, "ids"), (9997, "erf"), (9998, "bif"),
            (9999, "key")
        };

        ExtensionToType = pairs.ToDictionary(
            pair => pair.Extension,
            pair => pair.Type,
            StringComparer.OrdinalIgnoreCase);
        TypeToExtension = pairs.ToDictionary(pair => pair.Type, pair => pair.Extension);
    }

    public static ushort FromExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return Invalid;

        var normalized = extension.Trim().TrimStart('.');
        return ExtensionToType.TryGetValue(normalized, out var type) ? type : Invalid;
    }

    public static string GetExtension(ushort resourceType)
    {
        return TypeToExtension.TryGetValue(resourceType, out var extension) ? extension : string.Empty;
    }
}
