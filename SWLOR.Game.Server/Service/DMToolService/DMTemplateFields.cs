using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.DMToolService
{
    /// <summary>
    /// Reads fields from a blueprint template in the GFF JSON form TemplateToJson returns.
    /// Every field is stored as {"type": ..., "value": ...}.
    /// </summary>
    public static class DMTemplateFields
    {
        /// <summary>
        /// Reads a localized string: its own text when it has any, otherwise its TLK string.
        /// TemplateToJson nests the TLK reference in "value" as "id"; GFF JSON tools put it beside "value".
        /// </summary>
        public static string ReadLocString(Json template, string field)
        {
            var locString = JsonObjectGet(template, field);
            if (JsonGetType(locString) != JsonType.Object)
                return string.Empty;

            var value = JsonObjectGet(locString, "value");
            if (JsonGetType(value) == JsonType.Object)
            {
                var keys = JsonObjectKeys(value);
                for (var index = 0; index < JsonGetLength(keys); index++)
                {
                    var key = JsonGetString(JsonArrayGet(keys, index));
                    if (key == "id")
                        continue;

                    var text = JsonGetString(JsonObjectGet(value, key));
                    if (!string.IsNullOrWhiteSpace(text))
                        return text;
                }
            }

            foreach (var id in new[] { JsonObjectGet(value, "id"), JsonObjectGet(locString, "id") })
            {
                if (JsonGetType(id) != JsonType.Integer)
                    continue;

                var strRef = JsonGetInt(id);
                if (strRef > 0)
                    return GetStringByStrRef(strRef);
            }

            return string.Empty;
        }

        /// <summary>
        /// Reads a string or resref field, or an empty string when it is missing.
        /// </summary>
        public static string ReadString(Json template, string field)
        {
            var value = JsonObjectGet(JsonObjectGet(template, field), "value");
            return JsonGetType(value) == JsonType.String
                ? JsonGetString(value)
                : string.Empty;
        }

        /// <summary>
        /// Reads a whole-number field (byte, word, int, dword...), or the fallback when it is missing.
        /// </summary>
        public static int ReadInt(Json template, string field, int fallback = 0)
        {
            var value = JsonObjectGet(JsonObjectGet(template, field), "value");
            return JsonGetType(value) == JsonType.Integer
                ? JsonGetInt(value)
                : fallback;
        }

        /// <summary>
        /// Reads a list field's entries, or an empty JSON array when it is missing.
        /// </summary>
        public static Json ReadList(Json template, string field)
        {
            var value = JsonObjectGet(JsonObjectGet(template, field), "value");
            return JsonGetType(value) == JsonType.Array
                ? value
                : JsonArray();
        }
    }
}
