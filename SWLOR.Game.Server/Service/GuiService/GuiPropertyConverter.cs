using System;
using System.ComponentModel;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Service.GuiService.Component;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiPropertyConverter
    {
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
            else if (value is BindingList<string> bs)
            {
                var jsonArray = JsonArray();
                foreach (var val in bs)
                {
                    jsonArray = JsonArrayInsert(jsonArray, JsonString(val));
                }

                return jsonArray;
            }
            else if (value is BindingList<int> bi)
            {
                var jsonArray = JsonArray();
                foreach (var val in bi)
                {
                    jsonArray = JsonArrayInsert(jsonArray, JsonInt(val));
                }

                return jsonArray;
            }
            else if (value is BindingList<float> bf)
            {
                var jsonArray = JsonArray();
                foreach (var val in bf)
                {
                    jsonArray = JsonArrayInsert(jsonArray, JsonFloat(val));
                }

                return jsonArray;
            }
            else if (value is BindingList<bool> bb)
            {
                var jsonArray = JsonArray();
                foreach (var val in bb)
                {
                    jsonArray = JsonArrayInsert(jsonArray, JsonBool(val));
                }

                return jsonArray;
            }
            else if (value is BindingList<GuiRectangle> br)
            {
                var jsonArray = JsonArray();
                foreach (var val in br)
                {
                    jsonArray = JsonArrayInsert(jsonArray, val.ToJson());
                }

                return jsonArray;
            }
            else if (value is BindingList<GuiColor> bc)
            {
                var jsonArray = JsonArray();
                foreach (var val in bc)
                {
                    jsonArray = JsonArrayInsert(jsonArray, val.ToJson());
                }

                return jsonArray;
            }
            else if (value is BindingList<GuiComboEntry> ce)
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
            else if (type == typeof(BindingList<string>))
            {
                var list = new BindingList<string>();
                for (var index = 0; index <= JsonGetLength(json) - 1; index++)
                {
                    var record = JsonArrayGet(json, index);
                    list.Add(JsonGetString(record));
                }

                return list;
            }
            else if (type == typeof(BindingList<int>))
            {
                var list = new BindingList<int>();
                for (var index = 0; index <= JsonGetLength(json) - 1; index++)
                {
                    var record = JsonArrayGet(json, index);
                    list.Add( JsonGetInt(record));
                }

                return list;
            }
            else if (type == typeof(BindingList<float>))
            {
                var list = new BindingList<float>();
                for (var index = 0; index <= JsonGetLength(json) - 1; index++)
                {
                    var record = JsonArrayGet(json, index);
                    list.Add(JsonGetFloat(record));
                }

                return list;
            }
            else if (type == typeof(BindingList<bool>))
            {
                var list = new BindingList<bool>();
                for (var index = 0; index <= JsonGetLength(json) - 1; index++)
                {
                    var record = JsonArrayGet(json, index);
                    list.Add(JsonGetInt(record) == 1);
                }

                return list;
            }
            else if (type == typeof(BindingList<GuiRectangle>))
            {
                var list = new BindingList<GuiRectangle>();
                for (var index = 0; index <= JsonGetLength(json) - 1; index++)
                {
                    var record = JsonArrayGet(json, index);
                    list.Add(JsonToRectangle(record));
                }

                return list;
            }
            else if (type == typeof(BindingList<GuiColor>))
            {
                var list = new BindingList<GuiColor>();
                for (var index = 0; index <= JsonGetLength(json) - 1; index++)
                {
                    var record = JsonArrayGet(json, index);
                    list.Add(JsonToColor(record));
                }

                return list;
            }
            else if (type == typeof(BindingList<GuiComboEntry>))
            {
                var list = new BindingList<GuiComboEntry>();
                for (var index = 0; index <= JsonGetLength(json) - 1; index++)
                {
                    var record = JsonArrayGet(json, index);
                    //list.Add(JsonTo);
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
            return Nui.Color(color.Red, color.Green, color.Blue, color.Alpha);
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
