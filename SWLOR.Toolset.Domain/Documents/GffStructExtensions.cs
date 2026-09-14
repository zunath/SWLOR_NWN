using System.Globalization;
using System.Text;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>
    /// Typed convenience accessors shared by every document view in this namespace. Getters
    /// return null when the named field is absent; setters mutate the field in place when it
    /// already exists, or create it (with the caller-supplied <see cref="GffFieldType"/>) at
    /// nwn_gff's sorted insertion position when it does not.
    /// </summary>
    public static class GffStructExtensions
    {
        public static string? GetStringOrNull(this JsonGffStruct target, string name)
        {
            return target.TryGet(name, out var field) ? field.GetString() : null;
        }

        public static void SetString(this JsonGffStruct target, string name, GffFieldType type, string value)
        {
            JsonGffField.ValidateStringValue(type, value);
            if (target.TryGet(name, out var field))
            {
                field.SetString(value);
                return;
            }

            target.Add(name, JsonGffField.CreateScalar(type, JsonStringCodec.Encode(value)));
        }

        public static int? GetIntOrNull(this JsonGffStruct target, string name)
        {
            // Checked: a Dword sentinel such as 0xFFFFFFFF must surface as an error rather than
            // silently wrapping to -1. Fields that legitimately hold such values are read through
            // GetUIntOrNull.
            return target.TryGet(name, out var field) ? checked((int)field.GetInteger()) : null;
        }

        public static void SetInt(this JsonGffStruct target, string name, GffFieldType type, int value)
        {
            JsonGffField.ValidateIntegerValue(type, value);
            if (target.TryGet(name, out var field))
            {
                field.SetInteger(value);
                return;
            }

            target.Add(name, JsonGffField.CreateScalar(type,
                Encoding.ASCII.GetBytes(value.ToString(CultureInfo.InvariantCulture))));
        }

        public static void SetLong(this JsonGffStruct target, string name, GffFieldType type, long value)
        {
            JsonGffField.ValidateIntegerValue(type, value);
            if (target.TryGet(name, out var field))
            {
                field.SetInteger(value);
                return;
            }

            target.Add(name, JsonGffField.CreateScalar(type,
                Encoding.ASCII.GetBytes(value.ToString(CultureInfo.InvariantCulture))));
        }

        public static uint? GetUIntOrNull(this JsonGffStruct target, string name)
        {
            return target.TryGet(name, out var field) ? (uint)field.GetUnsignedInteger() : null;
        }

        public static void SetUInt(this JsonGffStruct target, string name, GffFieldType type, uint value)
        {
            JsonGffField.ValidateUnsignedIntegerValue(type, value);
            if (target.TryGet(name, out var field))
            {
                field.SetUnsignedInteger(value);
                return;
            }

            target.Add(name, JsonGffField.CreateScalar(type,
                Encoding.ASCII.GetBytes(value.ToString(CultureInfo.InvariantCulture))));
        }

        public static void SetULong(this JsonGffStruct target, string name, GffFieldType type, ulong value)
        {
            JsonGffField.ValidateUnsignedIntegerValue(type, value);
            if (target.TryGet(name, out var field))
            {
                field.SetUnsignedInteger(value);
                return;
            }

            target.Add(name, JsonGffField.CreateScalar(type,
                Encoding.ASCII.GetBytes(value.ToString(CultureInfo.InvariantCulture))));
        }

        public static float? GetSingleOrNull(this JsonGffStruct target, string name)
        {
            return target.TryGet(name, out var field) ? field.GetSingle() : null;
        }

        public static void SetSingle(this JsonGffStruct target, string name, float value)
        {
            JsonGffField.ValidateFiniteValue(value);
            if (target.TryGet(name, out var field))
            {
                field.SetSingle(value);
                return;
            }

            target.Add(name, JsonGffField.CreateScalar(GffFieldType.Float,
                Encoding.ASCII.GetBytes(NimFloatFormatter.Format(value))));
        }

        public static LocString? GetLocStringOrNull(this JsonGffStruct target, string name)
        {
            return target.TryGet(name, out var field) ? new LocString(field) : null;
        }

        /// <summary>Gets the named cexolocstring field's view, creating an empty one if absent.</summary>
        public static LocString GetOrAddLocString(this JsonGffStruct target, string name)
        {
            if (target.TryGet(name, out var field))
                return new LocString(field);

            var newField = JsonGffField.CreateLocString();
            target.Add(name, newField);
            return new LocString(newField);
        }

        public static IReadOnlyList<JsonGffStruct> GetListOrEmpty(this JsonGffStruct target, string name)
        {
            return target.TryGet(name, out var field) && field.Elements != null
                ? field.Elements
                : Array.Empty<JsonGffStruct>();
        }

        /// <summary>Gets the named list field's backing elements, creating an empty list if absent.</summary>
        public static List<JsonGffStruct> GetOrAddList(this JsonGffStruct target, string name)
        {
            if (target.TryGet(name, out var field))
                return field.Elements ??= new List<JsonGffStruct>();

            var newField = JsonGffField.CreateList();
            target.Add(name, newField);
            return newField.Elements!;
        }

        public static JsonGffStruct? GetStructOrNull(this JsonGffStruct target, string name)
        {
            return target.TryGet(name, out var field) ? field.Struct : null;
        }
    }
}
