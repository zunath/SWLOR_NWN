using System.Text;

namespace SWLOR.Toolset.Domain.Gff
{
    /// <summary>
    /// Serializes a JsonGffDocument back to nwn_gff JSON: 2-space indentation, field-object
    /// members in id/__struct_id/type/value order, raw scalar tokens re-emitted verbatim, and
    /// the document's original EOL style and trailing-newline state preserved.
    /// </summary>
    public static class GffJsonWriter
    {
        public static byte[] Write(JsonGffDocument document)
        {
            var output = new MemoryStream();
            var newLine = document.UsesCrLf ? "\r\n"u8.ToArray() : "\n"u8.ToArray();
            var writer = new Emitter(output, newLine);

            writer.Ascii("{");
            writer.NewLine();
            writer.Indent(1);
            writer.Ascii("\"__data_type\": ");
            writer.Raw(JsonStringCodec.Encode(document.DataType));

            if (document.Root.RawStructId != null)
            {
                writer.Ascii(",");
                writer.NewLine();
                writer.Indent(1);
                writer.Ascii("\"__struct_id\": ");
                writer.Raw(document.Root.RawStructId);
            }

            foreach (var (name, field) in document.Root.Entries)
            {
                writer.Ascii(",");
                writer.NewLine();
                WriteField(writer, name, field, 1);
            }

            writer.NewLine();
            writer.Ascii("}");
            if (document.HasTrailingNewline)
                writer.Raw(document.TrailingNewlineUsesCrLf ? "\r\n"u8.ToArray() : "\n"u8.ToArray());

            return output.ToArray();
        }

        private static void WriteField(Emitter writer, string name, JsonGffField field, int depth)
        {
            writer.Indent(depth);
            writer.Raw(JsonStringCodec.Encode(name));
            writer.Ascii(": {");
            writer.NewLine();

            if (field.RawLocStringId != null)
            {
                writer.Indent(depth + 1);
                writer.Ascii("\"id\": ");
                writer.Raw(field.RawLocStringId);
                writer.Ascii(",");
                writer.NewLine();
            }

            if (field.RawFieldStructId != null)
            {
                writer.Indent(depth + 1);
                writer.Ascii("\"__struct_id\": ");
                writer.Raw(field.RawFieldStructId);
                writer.Ascii(",");
                writer.NewLine();
            }

            writer.Indent(depth + 1);
            writer.Ascii($"\"type\": \"{GffFieldTypeNames.NameOf(field.Type)}\",");
            writer.NewLine();
            writer.Indent(depth + 1);
            writer.Ascii("\"value\": ");
            WriteValue(writer, field, depth + 1);
            writer.NewLine();
            writer.Indent(depth);
            writer.Ascii("}");
        }

        private static void WriteValue(Emitter writer, JsonGffField field, int depth)
        {
            switch (field.Type)
            {
                case GffFieldType.Struct:
                    WriteStructObject(writer, field.Struct!, depth);
                    break;
                case GffFieldType.List:
                    WriteList(writer, field.Elements!, depth);
                    break;
                case GffFieldType.CExoLocString:
                    WriteLocString(writer, field.LocStringEntries!, depth);
                    break;
                default:
                    writer.Raw(field.RawValue!);
                    break;
            }
        }

        private static void WriteStructObject(Emitter writer, JsonGffStruct value, int depth)
        {
            if (value.RawStructId == null && value.Count == 0)
            {
                writer.Ascii("{}");
                return;
            }

            writer.Ascii("{");
            writer.NewLine();

            var first = true;
            if (value.RawStructId != null)
            {
                writer.Indent(depth + 1);
                writer.Ascii("\"__struct_id\": ");
                writer.Raw(value.RawStructId);
                first = false;
            }

            foreach (var (name, field) in value.Entries)
            {
                if (!first)
                {
                    writer.Ascii(",");
                    writer.NewLine();
                }

                first = false;
                WriteField(writer, name, field, depth + 1);
            }

            writer.NewLine();
            writer.Indent(depth);
            writer.Ascii("}");
        }

        private static void WriteList(Emitter writer, List<JsonGffStruct> elements, int depth)
        {
            if (elements.Count == 0)
            {
                writer.Ascii("[]");
                return;
            }

            writer.Ascii("[");
            writer.NewLine();

            for (var i = 0; i < elements.Count; i++)
            {
                if (i > 0)
                {
                    writer.Ascii(",");
                    writer.NewLine();
                }

                writer.Indent(depth + 1);
                WriteStructObject(writer, elements[i], depth + 1);
            }

            writer.NewLine();
            writer.Indent(depth);
            writer.Ascii("]");
        }

        private static void WriteLocString(Emitter writer, List<LocStringEntry> entries, int depth)
        {
            if (entries.Count == 0)
            {
                writer.Ascii("{}");
                return;
            }

            writer.Ascii("{");
            writer.NewLine();

            for (var i = 0; i < entries.Count; i++)
            {
                if (i > 0)
                {
                    writer.Ascii(",");
                    writer.NewLine();
                }

                writer.Indent(depth + 1);
                writer.Raw(JsonStringCodec.Encode(entries[i].LanguageKey));
                writer.Ascii(": ");
                writer.Raw(entries[i].RawText);
            }

            writer.NewLine();
            writer.Indent(depth);
            writer.Ascii("}");
        }

        private sealed class Emitter
        {
            private readonly MemoryStream _output;
            private readonly byte[] _newLine;

            public Emitter(MemoryStream output, byte[] newLine)
            {
                _output = output;
                _newLine = newLine;
            }

            public void Raw(byte[] bytes)
            {
                _output.Write(bytes, 0, bytes.Length);
            }

            public void Ascii(string text)
            {
                var bytes = Encoding.ASCII.GetBytes(text);
                _output.Write(bytes, 0, bytes.Length);
            }

            public void NewLine()
            {
                _output.Write(_newLine, 0, _newLine.Length);
            }

            public void Indent(int depth)
            {
                for (var i = 0; i < depth * 2; i++)
                    _output.WriteByte((byte)' ');
            }
        }
    }
}
