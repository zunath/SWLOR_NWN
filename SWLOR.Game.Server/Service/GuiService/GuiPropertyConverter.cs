using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.GuiService.Converter;

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
            else
            {
                throw new Exception($"Converter is not defined for type {type}");
            }
        }

    }
}
