using System;
using System.ComponentModel;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.GuiService.Converter;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiPropertyConverter
    {
        private readonly GuiStringConverter _stringConverter = new();
        private readonly GuiIntConverter _intConverter = new();
        private readonly GuiFloatConverter _floatConverter = new();
        private readonly GuiBoolConverter _boolConverter = new();
        private readonly GuiRectangleConverter _rectangleConverter = new();
        private readonly GuiColorConverter _colorConverter = new();

        public Json ToJson(object value)
        {
            if (value is string s)
            {
                return _stringConverter.ToJson(s);
            }
            else if (value is int i)
            {
                return _intConverter.ToJson(i);
            }
            else if (value is float f)
            {
                return _floatConverter.ToJson(f);
            }
            else if (value is bool b)
            {
                return _boolConverter.ToJson(b);
            }
            else if (value is GuiRectangle rect)
            {
                return _rectangleConverter.ToJson(rect);
            }
            else if (value is GuiColor color)
            {
                return _colorConverter.ToJson(color);
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
            else
            {
                throw new Exception($"Converter is not defined for type {value.GetType()}");
            }
        }

        public object ToObject(Json json, Type type)
        {
            if (type == typeof(string))
            {
                return _stringConverter.ToObject(json);
            }
            else if (type == typeof(int))
            {
                return _intConverter.ToObject(json);
            }
            else if (type == typeof(float))
            {
                return _floatConverter.ToObject(json);
            }
            else if (type == typeof(bool))
            {
                return _boolConverter.ToObject(json);
            }
            else if (type == typeof(GuiRectangle))
            {
                return _rectangleConverter.ToObject(json);
            }
            else if (type == typeof(GuiColor))
            {
                return _colorConverter.ToObject(json);
            }
            else if (type == typeof(BindingList<string>))
            {
                var list = new BindingList<string>();
                for (var index = 0; index <= JsonGetLength(json) - 1; index++)
                {
                    var record = JsonArrayGet(json, index);
                    list.Add(_stringConverter.ToObject(record));
                }

                return list;
            }
            else if (type == typeof(BindingList<int>))
            {
                var list = new BindingList<int>();
                for (var index = 0; index <= JsonGetLength(json) - 1; index++)
                {
                    var record = JsonArrayGet(json, index);
                    list.Add(_intConverter.ToObject(record));
                }

                return list;
            }
            else if (type == typeof(BindingList<float>))
            {
                var list = new BindingList<float>();
                for (var index = 0; index <= JsonGetLength(json) - 1; index++)
                {
                    var record = JsonArrayGet(json, index);
                    list.Add(_floatConverter.ToObject(record));
                }

                return list;
            }
            else if (type == typeof(BindingList<bool>))
            {
                var list = new BindingList<bool>();
                for (var index = 0; index <= JsonGetLength(json) - 1; index++)
                {
                    var record = JsonArrayGet(json, index);
                    list.Add(_boolConverter.ToObject(record));
                }

                return list;
            }
            else if (type == typeof(BindingList<GuiRectangle>))
            {
                var list = new BindingList<GuiRectangle>();
                for (var index = 0; index <= JsonGetLength(json) - 1; index++)
                {
                    var record = JsonArrayGet(json, index);
                    list.Add(_rectangleConverter.ToObject(record));
                }

                return list;
            }
            else if (type == typeof(BindingList<GuiColor>))
            {
                var list = new BindingList<GuiColor>();
                for (var index = 0; index <= JsonGetLength(json) - 1; index++)
                {
                    var record = JsonArrayGet(json, index);
                    list.Add(_colorConverter.ToObject(record));
                }

                return list;
            }
            else
            {
                throw new Exception($"Converter is not defined for type {type}");
            }
        }

    }
}
