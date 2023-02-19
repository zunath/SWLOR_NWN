using System;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiPropertyConverter
    {
        /// <summary>
        /// Converts an object to the Json representation.
        /// Only the following types are allowed:
        /// string, int, float, bool, GuiRectangle, GuiColor, GuiBindingList
        /// </summary>
        /// <param name="value">The object to convert.</param>
        /// <returns>Json representation of the object.</returns>
        public Json ToJson(object value)
        {
            if (value is string s)
            {
                return JsonString(s);
            }
            else if (value is int i)
            {
                return JsonInt(i);
            }
            else if (value is float f)
            {
                return JsonFloat(f);
            }
            else if (value is bool b)
            {
                return JsonBool(b);
            }
            else if (value is GuiRectangle rect)
            {
                return RectangleToJson(rect);
            }
            else if (value is GuiColor color)
            {
                return ColorToJson(color);
            }
            else if (value is GuiBindingList<string> bs)
            {
                var jsonArray = JsonArray();
                foreach (var val in bs)
                {
                    jsonArray = JsonArrayInsert(jsonArray, JsonString(val));
                }

                return jsonArray;
            }
            else if (value is GuiBindingList<int> bi)
            {
                var jsonArray = JsonArray();
                foreach (var val in bi)
                {
                    jsonArray = JsonArrayInsert(jsonArray, JsonInt(val));
                }

                return jsonArray;
            }
            else if (value is GuiBindingList<float> bf)
            {
                var jsonArray = JsonArray();
                foreach (var val in bf)
                {
                    jsonArray = JsonArrayInsert(jsonArray, JsonFloat(val));
                }

                return jsonArray;
            }
            else if (value is GuiBindingList<bool> bb)
            {
                var jsonArray = JsonArray();
                foreach (var val in bb)
                {
                    jsonArray = JsonArrayInsert(jsonArray, JsonBool(val));
                }

                return jsonArray;
            }
            else if (value is GuiBindingList<GuiRectangle> br)
            {
                var jsonArray = JsonArray();
                foreach (var val in br)
                {
                    jsonArray = JsonArrayInsert(jsonArray, val.ToJson());
                }

                return jsonArray;
            }
            else if (value is GuiBindingList<GuiColor> bc)
            {
                var jsonArray = JsonArray();
                foreach (var val in bc)
                {
                    jsonArray = JsonArrayInsert(jsonArray, val.ToJson());
                }

                return jsonArray;
            }
            else if (value is GuiBindingList<GuiComboEntry> ce)
            {
                var jsonArray = JsonArray();
                foreach (var val in ce)
                {
                    jsonArray = JsonArrayInsert(jsonArray, val.ToJson());
                }

                return jsonArray;
            }
            else
            {
                throw new Exception($"Converter is not defined for type {value.GetType()}");
            }
        }

        /// <summary>
        /// Converts json to the specified type of object.
        /// Only the following types are allowed:
        /// string, int, float, bool, GuiRectangle, GuiColor, GuiBindingList
        /// </summary>
        /// <param name="json">The Json to convert.</param>
        /// <param name="type">The type of object to create</param>
        /// <returns>A converted object.</returns>
        public object ToObject(Json json, Type type)
        {
            if (type == typeof(string))
            {
                return JsonGetString(json);
            }
            else if (type == typeof(int))
            {
                return JsonGetInt(json);
            }
            else if (type == typeof(float))
            {
                return JsonGetFloat(json);
            }
            else if (type == typeof(bool))
            {
                return JsonGetInt(json) == 1;
            }
            else if (type == typeof(GuiRectangle))
            {
                return JsonToRectangle(json);
            }
            else if (type == typeof(GuiColor))
            {
                return JsonToColor(json);
            }
            else if (type == typeof(GuiBindingList<string>))
            {
                var list = new GuiBindingList<string>();
                for (var index = 0; index <= JsonGetLength(json) - 1; index++)
                {
                    var record = JsonArrayGet(json, index);
                    list.Add(JsonGetString(record));
                }

                return list;
            }
            else if (type == typeof(GuiBindingList<int>))
            {
                var list = new GuiBindingList<int>();
                for (var index = 0; index <= JsonGetLength(json) - 1; index++)
                {
                    var record = JsonArrayGet(json, index);
                    list.Add(JsonGetInt(record));
                }

                return list;
            }
            else if (type == typeof(GuiBindingList<float>))
            {
                var list = new GuiBindingList<float>();
                for (var index = 0; index <= JsonGetLength(json) - 1; index++)
                {
                    var record = JsonArrayGet(json, index);
                    list.Add(JsonGetFloat(record));
                }

                return list;
            }
            else if (type == typeof(GuiBindingList<bool>))
            {
                var list = new GuiBindingList<bool>();
                for (var index = 0; index <= JsonGetLength(json) - 1; index++)
                {
                    var record = JsonArrayGet(json, index);
                    list.Add(JsonGetInt(record) == 1);
                }

                return list;
            }
            else if (type == typeof(GuiBindingList<GuiRectangle>))
            {
                var list = new GuiBindingList<GuiRectangle>();
                for (var index = 0; index <= JsonGetLength(json) - 1; index++)
                {
                    var record = JsonArrayGet(json, index);
                    list.Add(JsonToRectangle(record));
                }

                return list;
            }
            else if (type == typeof(GuiBindingList<GuiColor>))
            {
                var list = new GuiBindingList<GuiColor>();
                for (var index = 0; index <= JsonGetLength(json) - 1; index++)
                {
                    var record = JsonArrayGet(json, index);
                    list.Add(JsonToColor(record));
                }

                return list;
            }
            else
            {
                throw new Exception($"Converter is not defined for type {type}");
            }
        }

        private GuiColor JsonToColor(Json json)
        {
            // Using JSON.NET to parse this data. NWScript's methods were giving me recursion errors
            // during Client UI watch updates
            var jsonDump = JsonDump(json);
            var data = JObject.Parse(jsonDump);

            var color = new GuiColor(
                Convert.ToInt32(data["r"]),
                Convert.ToInt32(data["g"]),
                Convert.ToInt32(data["b"]),
                Convert.ToInt32(data["a"]));

            return color;
        }

        private Json ColorToJson(GuiColor color)
        {
            return Nui.Color(color.R, color.G, color.B, color.Alpha);
        }

        private GuiRectangle JsonToRectangle(Json json)
        {
            // Using JSON.NET to parse this data. NWScript's methods were giving me recursion errors
            // during Client UI watch updates
            var jsonDump = JsonDump(json);
            var data = JObject.Parse(jsonDump);

            var rect = new GuiRectangle(
                (float)Convert.ToDouble(data["x"]),
                (float)Convert.ToDouble(data["y"]),
                (float)Convert.ToDouble(data["w"]),
                (float)Convert.ToDouble(data["h"]));

            return rect;
        }

        private Json RectangleToJson(GuiRectangle rectangle)
        {
            return Nui.Rect(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        }
    }
}
