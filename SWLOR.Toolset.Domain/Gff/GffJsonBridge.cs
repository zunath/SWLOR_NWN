using System.Globalization;
using System.Text;
using Radoub.Formats.Gff;

namespace SWLOR.Toolset.Domain.Gff
{
    /// <summary>
    /// Bridges the nwn_gff JSON document model (<see cref="JsonGffDocument"/>) and
    /// Radoub.Formats' binary GFF model (<see cref="GffFile"/>). This exists to feed a later
    /// 3D preview / binary export pipeline; nothing else in the toolset consumes it yet.
    /// </summary>
    public static class GffJsonBridge
    {
        /// <summary>Radoub's "no strref" sentinel for a locstring's <c>StrRef</c>.</summary>
        private const uint NoStrRefSentinel = 0xFFFFFFFF;

        /// <summary>All GFF files on disk are version "V3.2"; nwn_gff JSON never records it.</summary>
        private const string GffVersion = "V3.2";

        // ---------------------------------------------------------------
        // JSON -> GffFile
        // ---------------------------------------------------------------

        public static GffFile ToGffFile(JsonGffDocument document)
        {
            return new GffFile
            {
                FileType = document.DataType,
                FileVersion = GffVersion,
                RootStruct = ConvertStructToGff(document.Root)
            };
        }

        private static GffStruct ConvertStructToGff(JsonGffStruct source)
        {
            var target = new GffStruct
            {
                Type = source.RawStructId != null ? ParseUInt32(source.RawStructId) : 0u
            };

            foreach (var (name, field) in source.Entries)
                AddFieldToGff(target, name, field);

            return target;
        }

        private static void AddFieldToGff(GffStruct parent, string name, JsonGffField field)
        {
            switch (field.Type)
            {
                case GffFieldType.Byte:
                    GffFieldBuilder.AddByteField(parent, name, (byte)field.GetUnsignedInteger());
                    break;
                case GffFieldType.Char:
                    GffFieldBuilder.AddCharField(parent, name, (sbyte)field.GetInteger());
                    break;
                case GffFieldType.Word:
                    GffFieldBuilder.AddWordField(parent, name, (ushort)field.GetUnsignedInteger());
                    break;
                case GffFieldType.Short:
                    GffFieldBuilder.AddShortField(parent, name, (short)field.GetInteger());
                    break;
                case GffFieldType.Dword:
                    GffFieldBuilder.AddDwordField(parent, name, (uint)field.GetUnsignedInteger());
                    break;
                case GffFieldType.Int:
                    GffFieldBuilder.AddIntField(parent, name, (int)field.GetInteger());
                    break;
                case GffFieldType.Dword64:
                    GffFieldBuilder.AddDword64Field(parent, name, field.GetUnsignedInteger());
                    break;
                case GffFieldType.Int64:
                    GffFieldBuilder.AddInt64Field(parent, name, field.GetInteger());
                    break;
                case GffFieldType.Float:
                    GffFieldBuilder.AddFloatField(parent, name, field.GetSingle());
                    break;
                case GffFieldType.Double:
                    GffFieldBuilder.AddDoubleField(parent, name, field.GetDouble());
                    break;
                case GffFieldType.CExoString:
                    GffFieldBuilder.AddCExoStringField(parent, name, DecodeNwnString(field.RawValue!));
                    break;
                case GffFieldType.ResRef:
                    GffFieldBuilder.AddCResRefField(parent, name, DecodeNwnString(field.RawValue!));
                    break;
                case GffFieldType.CExoLocString:
                    GffFieldBuilder.AddLocStringField(parent, name, ConvertLocStringToGff(field));
                    break;
                case GffFieldType.Void:
                    // Void payloads embed raw binary (possibly invalid UTF-8) directly in the
                    // JSON string token, so they must be bridged at the byte level rather than
                    // through a .NET string (see JsonStringCodec.DecodeToBytes).
                    GffFieldBuilder.AddVoidField(parent, name, JsonStringCodec.DecodeToBytes(field.RawValue!));
                    break;
                case GffFieldType.Struct:
                    GffFieldBuilder.AddStructField(parent, name, ConvertStructToGff(field.Struct!));
                    break;
                case GffFieldType.List:
                    GffFieldBuilder.AddListField(parent, name, field.Elements!.Select(ConvertStructToGff));
                    break;
                default:
                    throw new NotSupportedException($"Unsupported GFF field type '{field.Type}' on field '{name}'.");
            }
        }

        private static CExoLocString ConvertLocStringToGff(JsonGffField field)
        {
            var loc = new CExoLocString();
            if (field.RawLocStringId != null)
                loc.StrRef = ParseUInt32(field.RawLocStringId);

            foreach (var entry in field.LocStringEntries!)
            {
                var languageId = uint.Parse(entry.LanguageKey, CultureInfo.InvariantCulture);
                loc.LocalizedStrings[languageId] = DecodeNwnString(entry.RawText);
            }

            loc.SubStringCount = (uint)loc.LocalizedStrings.Count;
            return loc;
        }

        private static uint ParseUInt32(byte[] raw)
        {
            return uint.Parse(Encoding.ASCII.GetString(raw), NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        /// <summary>Decodes a raw JSON string token to text via the byte-level codec + CP-1252,
        /// preserving any embedded non-UTF-8 bytes (e.g. color codes) losslessly.</summary>
        private static string DecodeNwnString(byte[] rawValue)
        {
            return JsonStringCodec.Decode(rawValue);
        }

        /// <summary>Inverse of <see cref="DecodeNwnString"/>: encodes text back to a raw JSON
        /// string token via CP-1252 + the byte-level codec.</summary>
        private static byte[] EncodeNwnString(string value)
        {
            return JsonStringCodec.Encode(value, UseUtf8Text.Value);
        }

        /// <summary>
        /// Per-conversion text-encoding choice for <see cref="ToJsonDocument(GffFile, bool)"/>.
        /// AsyncLocal so nested/concurrent conversions cannot observe each other's flag.
        /// </summary>
        private static readonly AsyncLocal<bool> UseUtf8Text = new();

        // ---------------------------------------------------------------
        // GffFile -> JSON
        // ---------------------------------------------------------------

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
                    // See AddFieldToGff: void payloads may not be valid UTF-8, so they are
                    // bridged at the byte level via JsonStringCodec.EncodeBytes.
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
