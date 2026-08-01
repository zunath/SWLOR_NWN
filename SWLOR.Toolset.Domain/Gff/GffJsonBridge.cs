using System.Globalization;
using System.Text;
using SWLOR.NWN.Formats.Gff;

namespace SWLOR.Toolset.Domain.Gff
{
    /// <summary>
    /// Converts the standalone formats library's read-only binary GFF model into the
    /// nwn_gff JSON document model used by the editors.
    /// </summary>
    public static class GffJsonBridge
    {
        /// <summary>The GFF "no strref" sentinel for a locstring's <c>StrRef</c>.</summary>
        private const uint NoStrRefSentinel = 0xFFFFFFFF;

        /// <summary>
        /// Encodes text as a raw JSON string token. Canonical module text stays CP-1252 when
        /// possible; text decoded from another NWN language codepage falls back to UTF-8 rather
        /// than being rejected or replaced.
        /// </summary>
        private static byte[] EncodeNwnString(string value)
        {
            try
            {
                return JsonStringCodec.Encode(value, UseUtf8Text.Value);
            }
            catch (EncoderFallbackException) when (!UseUtf8Text.Value)
            {
                return JsonStringCodec.Encode(value, useUtf8: true);
            }
        }

        /// <summary>
        /// Per-conversion text-encoding choice for <see cref="ToJsonDocument(GffFile, bool)"/>.
        /// AsyncLocal so nested/concurrent conversions cannot observe each other's flag.
        /// </summary>
        private static readonly AsyncLocal<bool> UseUtf8Text = new();

        /// <summary>
        /// Converts a parsed binary GFF into a JSON document. The result is brand new and owned by nobody,
        /// so the conversion runs as construction - see <see cref="Editing.EditScope.EnterConstruction"/>
        /// for why that is not merely an optimisation.
        /// </summary>
        public static JsonGffDocument ToJsonDocument(GffFile file) => ToJsonDocument(file, encodeTextAsUtf8: false);

        /// <summary>
        /// Converts with an explicit text-encoding choice. Pass true when the strings being
        /// re-emitted came from a UTF-8 source document so its tokens round-trip byte-identically;
        /// the default stays Windows-1252, the module's canonical storage.
        /// </summary>
        public static JsonGffDocument ToJsonDocument(GffFile file, bool encodeTextAsUtf8)
        {
            UseUtf8Text.Value = encodeTextAsUtf8;
            try
            {
                return ToJsonDocumentCore(file);
            }
            finally
            {
                UseUtf8Text.Value = false;
            }
        }

        private static JsonGffDocument ToJsonDocumentCore(GffFile file)
        {
            using var construction = Editing.EditScope.EnterConstruction();

            var root = new JsonGffStruct();
            foreach (var field in file.RootStruct.Fields)
                root.Add(field.Label, ConvertFieldToJson(field));

            return new JsonGffDocument(file.FileType, root);
        }

        private static JsonGffStruct ConvertNestedStructToJson(GffStruct source)
        {
            var target = new JsonGffStruct { RawStructId = EncodeUInt64(source.Type) };
            foreach (var field in source.Fields)
                target.Add(field.Label, ConvertFieldToJson(field));

            return target;
        }

        private static JsonGffField ConvertFieldToJson(GffField field)
        {
            switch (field.Type)
            {
                case GffField.BYTE:
                    return JsonGffField.CreateScalar(GffFieldType.Byte, EncodeUInt64((byte)field.Value!));
                case GffField.CHAR:
                    return JsonGffField.CreateScalar(GffFieldType.Char, EncodeInt64((sbyte)field.Value!));
                case GffField.WORD:
                    return JsonGffField.CreateScalar(GffFieldType.Word, EncodeUInt64((ushort)field.Value!));
                case GffField.SHORT:
                    return JsonGffField.CreateScalar(GffFieldType.Short, EncodeInt64((short)field.Value!));
                case GffField.DWORD:
                    return JsonGffField.CreateScalar(GffFieldType.Dword, EncodeUInt64((uint)field.Value!));
                case GffField.INT:
                    return JsonGffField.CreateScalar(GffFieldType.Int, EncodeInt64((int)field.Value!));
                case GffField.DWORD64:
                    return JsonGffField.CreateScalar(GffFieldType.Dword64, EncodeUInt64((ulong)field.Value!));
                case GffField.INT64:
                    return JsonGffField.CreateScalar(GffFieldType.Int64, EncodeInt64((long)field.Value!));
                case GffField.FLOAT:
                    // GFF float fields are float32 — format via the float32 overload so output
                    // matches nwn_gff, which prints the value through the same funnel.
                    return JsonGffField.CreateScalar(GffFieldType.Float, Ascii(NimFloatFormatter.Format((float)field.Value!)));
                case GffField.DOUBLE:
                    return JsonGffField.CreateScalar(GffFieldType.Double, Ascii(NimFloatFormatter.Format((double)field.Value!)));
                case GffField.CExoString:
                    return JsonGffField.CreateScalar(GffFieldType.CExoString, EncodeNwnString(field.Value as string ?? string.Empty));
                case GffField.CResRef:
                    return JsonGffField.CreateScalar(GffFieldType.ResRef, EncodeNwnString(field.Value as string ?? string.Empty));
                case GffField.VOID:
                    // Void payloads may not be valid UTF-8, so bridge them at the byte level.
                    return JsonGffField.CreateScalar(GffFieldType.Void, JsonStringCodec.EncodeBytes(field.Value as byte[] ?? Array.Empty<byte>()));
                case GffField.CExoLocString:
                    return ConvertLocStringToJson(field.Value as CExoLocString ?? new CExoLocString());
                case GffField.Struct:
                    return ConvertStructFieldToJson((GffStruct)field.Value!);
                case GffField.List:
                    return ConvertListFieldToJson((GffList)field.Value!);
                default:
                    throw new NotSupportedException($"Unsupported GFF field type '{field.Type}' on field '{field.Label}'.");
            }
        }

        private static JsonGffField ConvertLocStringToJson(CExoLocString loc)
        {
            var field = JsonGffField.CreateLocString();
            if (loc.StrRef != NoStrRefSentinel)
                field.RawLocStringId = EncodeUInt64(loc.StrRef);

            // Preserve the substrings' natural enumeration order (== insertion order, since we
            // never remove entries) rather than sorting by language id: real GFF data is not
            // always stored in ascending language-id order (e.g. legacy entries appended after
            // later ones), and nwn_gff round-trips whatever order the binary substring table
            // was in.
            foreach (var (languageId, text) in loc.LocalizedStrings)
            {
                var rawText = EncodeNwnString(text);
                field.LocStringEntries!.Add(new LocStringEntry(languageId.ToString(CultureInfo.InvariantCulture), rawText));
            }

            return field;
        }

        private static JsonGffField ConvertStructFieldToJson(GffStruct source)
        {
            var childStruct = ConvertNestedStructToJson(source);
            return new JsonGffField(GffFieldType.Struct)
            {
                RawFieldStructId = childStruct.RawStructId,
                Struct = childStruct
            };
        }

        private static JsonGffField ConvertListFieldToJson(GffList list)
        {
            var field = JsonGffField.CreateList();
            foreach (var element in list.Elements)
                field.Elements!.Add(ConvertNestedStructToJson(element));

            return field;
        }

        private static byte[] EncodeUInt64(ulong value)
        {
            return Encoding.ASCII.GetBytes(value.ToString(CultureInfo.InvariantCulture));
        }

        private static byte[] EncodeInt64(long value)
        {
            return Encoding.ASCII.GetBytes(value.ToString(CultureInfo.InvariantCulture));
        }

        private static byte[] Ascii(string text)
        {
            return Encoding.ASCII.GetBytes(text);
        }
    }
}
