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
        /// <param name="resolveStrRef">
        /// Resolves a TLK strref, or null when no TLK is loaded. A localized field can carry a strref
        /// with no language-0 override - <c>zep_throwrug056.utp.json</c> stores its <c>LocName</c> that
        /// way - and reading only the override showed those blueprints with a blank Name, which looks
        /// like missing data rather than text that lives in the TLK.
        /// </param>
        public static string GetText(
            JsonGffDocument document, FieldDescriptor descriptor, Func<uint, string?>? resolveStrRef = null)
        {
            var field = document.Root.GetOrNull(descriptor.FieldName);
            if (field == null)
                return string.Empty;

            if (descriptor.Kind != EditorKind.LocString)
                return field.GetString();

            var locString = new LocString(field);
            var text = locString.Text;
            if (text != null)
                return text;

            // Only when there is no override to show. An empty override that a builder typed
            // deliberately still wins over the strref, because that is the value in this file.
            if (resolveStrRef != null && locString.StrRef is { } strRef)
                return resolveStrRef(strRef) ?? string.Empty;

            return text ?? string.Empty;
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

            JsonGffField.ValidateStringValue(descriptor.FieldType, value);
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
            JsonGffField.ValidateIntegerValue(descriptor.FieldType, value);
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
            // Validate the value as it will be stored: narrowing a large finite double to a
            // 32-bit float can itself overflow to infinity.
            JsonGffField.ValidateFiniteValue(
                descriptor.FieldType == GffFieldType.Float ? (float)value : value);
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
