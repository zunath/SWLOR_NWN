namespace SWLOR.Toolset.Domain.Gff
{
    /// <summary>
    /// GFF field types as they appear in the "type" property of nwn_gff JSON documents.
    /// </summary>
    public enum GffFieldType
    {
        Byte,
        Char,
        Word,
        Short,
        Dword,
        Int,
        Dword64,
        Int64,
        Float,
        Double,
        CExoString,
        ResRef,
        CExoLocString,
        Void,
        Struct,
        List
    }

    public static class GffFieldTypeNames
    {
        private static readonly Dictionary<string, GffFieldType> _byName = new()
        {
            ["byte"] = GffFieldType.Byte,
            ["char"] = GffFieldType.Char,
            ["word"] = GffFieldType.Word,
            ["short"] = GffFieldType.Short,
            ["dword"] = GffFieldType.Dword,
            ["int"] = GffFieldType.Int,
            ["dword64"] = GffFieldType.Dword64,
            ["int64"] = GffFieldType.Int64,
            ["float"] = GffFieldType.Float,
            ["double"] = GffFieldType.Double,
            ["cexostring"] = GffFieldType.CExoString,
            ["resref"] = GffFieldType.ResRef,
            ["cexolocstring"] = GffFieldType.CExoLocString,
            ["void"] = GffFieldType.Void,
            ["struct"] = GffFieldType.Struct,
            ["list"] = GffFieldType.List
        };

        private static readonly Dictionary<GffFieldType, string> _byType =
            _byName.ToDictionary(pair => pair.Value, pair => pair.Key);

        public static GffFieldType Parse(string name)
        {
            if (!_byName.TryGetValue(name, out var type))
                throw new FormatException($"Unknown GFF field type name: '{name}'");

            return type;
        }

        public static string NameOf(GffFieldType type)
        {
            return _byType[type];
        }

        public static bool IsNumeric(GffFieldType type)
        {
            return type is GffFieldType.Byte or GffFieldType.Char or GffFieldType.Word
                or GffFieldType.Short or GffFieldType.Dword or GffFieldType.Int
                or GffFieldType.Dword64 or GffFieldType.Int64
                or GffFieldType.Float or GffFieldType.Double;
        }

        /// <summary>
        /// True for types whose value is legitimately text. Void is deliberately excluded even
        /// though it is stored inside a JSON string token: its payload is arbitrary binary, and
        /// transcoding it through the UTF-8/Windows-1252 text heuristic can silently corrupt
        /// bytes. Binary access goes through <see cref="JsonStringCodec.DecodeToBytes"/> /
        /// <see cref="JsonStringCodec.EncodeBytes"/> instead.
        /// </summary>
        public static bool IsString(GffFieldType type)
        {
            return type is GffFieldType.CExoString or GffFieldType.ResRef;
        }
    }
}
