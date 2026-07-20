using System.Globalization;
using System.Text;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors
{
    /// <summary>
    /// Interprets a FieldDescriptor against a JsonGffDocument root: typed reads with sensible
    /// defaults for absent fields, and writes that create the field (with the descriptor's
    /// GFF type, at nwn_gff's sorted position) when it does not exist yet. All writes flow
    /// through the ambient edit scope, so they participate in transactions and undo.
    /// </summary>
    public static class SchemaFieldAccessor
    {
        public static string GetText(JsonGffDocument document, FieldDescriptor descriptor)
        {
            var field = document.Root.GetOrNull(descriptor.FieldName);
            if (field == null)
                return string.Empty;

            return descriptor.Kind == EditorKind.LocString
                ? new LocString(field).Text ?? string.Empty
                : field.GetString();
        }

        public static long GetInteger(JsonGffDocument document, FieldDescriptor descriptor)
        {
            var field = document.Root.GetOrNull(descriptor.FieldName);
            return field?.GetInteger() ?? 0;
        }

        public static double GetFloat(JsonGffDocument document, FieldDescriptor descriptor)
        {
            var field = document.Root.GetOrNull(descriptor.FieldName);
            return field?.GetDouble() ?? 0.0;
        }

        public static bool GetBool(JsonGffDocument document, FieldDescriptor descriptor)
        {
            return GetInteger(document, descriptor) != 0;
        }

        public static void SetText(JsonGffDocument document, FieldDescriptor descriptor, string value)
        {
            if (descriptor.Kind == EditorKind.LocString)
            {
                var locField = GetOrCreate(document, descriptor);
                new LocString(locField).Text = value;
                return;
            }

            var field = document.Root.GetOrNull(descriptor.FieldName);
            if (field == null)
            {
                document.Root.Add(descriptor.FieldName,
                    JsonGffField.CreateScalar(descriptor.FieldType, JsonStringCodec.Encode(value)));
                return;
            }

            field.SetString(value);
        }

        public static void SetInteger(JsonGffDocument document, FieldDescriptor descriptor, long value)
        {
            var field = document.Root.GetOrNull(descriptor.FieldName);
            if (field == null)
            {
                var raw = Encoding.ASCII.GetBytes(value.ToString(CultureInfo.InvariantCulture));
                document.Root.Add(descriptor.FieldName, JsonGffField.CreateScalar(descriptor.FieldType, raw));
                return;
            }

            field.SetInteger(value);
        }

        public static void SetFloat(JsonGffDocument document, FieldDescriptor descriptor, double value)
        {
            var field = document.Root.GetOrNull(descriptor.FieldName);
            if (field == null)
            {
                var text = descriptor.FieldType == GffFieldType.Float
                    ? NimFloatFormatter.Format((float)value)
                    : NimFloatFormatter.Format(value);
                document.Root.Add(descriptor.FieldName,
                    JsonGffField.CreateScalar(descriptor.FieldType, Encoding.ASCII.GetBytes(text)));
                return;
            }

            if (descriptor.FieldType == GffFieldType.Float)
                field.SetSingle((float)value);
            else
                field.SetDouble(value);
        }

        public static void SetBool(JsonGffDocument document, FieldDescriptor descriptor, bool value)
        {
            SetInteger(document, descriptor, value ? 1 : 0);
        }

        private static JsonGffField GetOrCreate(JsonGffDocument document, FieldDescriptor descriptor)
        {
            var field = document.Root.GetOrNull(descriptor.FieldName);
            if (field != null)
                return field;

            field = descriptor.FieldType == GffFieldType.CExoLocString
                ? JsonGffField.CreateLocString()
                : JsonGffField.CreateScalar(descriptor.FieldType, JsonStringCodec.Encode(string.Empty));
            document.Root.Add(descriptor.FieldName, field);
            return field;
        }
    }
}
