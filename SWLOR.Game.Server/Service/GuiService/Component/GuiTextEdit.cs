using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiTextEdit<T> : GuiWidget<T, GuiTextEdit<T>>
        where T: IGuiDataModel
    {
        private string Placeholder { get; set; }
        private string PlaceholderBindName { get; set; }
        private bool IsPlaceholderBound => !string.IsNullOrWhiteSpace(PlaceholderBindName);
        
        private string Value { get; set; }
        private string ValueBindName { get; set; }
        private bool IsValueBound => !string.IsNullOrWhiteSpace(ValueBindName);
        
        private int MaxLength { get; set; }
        private bool IsMultiLine { get; set; }

        public GuiTextEdit<T> SetPlaceholder(string placeholder)
        {
            Placeholder = placeholder;
            return this;
        }

        public GuiTextEdit<T> BindPlaceholder<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            PlaceholderBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiTextEdit<T> SetValue(string value)
        {
            Value = value;
            return this;
        }

        public GuiTextEdit<T> BindValue<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ValueBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiTextEdit<T> SetMaxLength(int maxLength)
        {
            MaxLength = maxLength;
            return this;
        }

        public GuiTextEdit<T> SetIsMultiline(bool isMultiLine)
        {
            IsMultiLine = isMultiLine;
            return this;
        }


        public override Json BuildElement()
        {
            var placeholder = IsPlaceholderBound ? Nui.Bind(PlaceholderBindName) : JsonString(Placeholder);
            var value = IsValueBound ? Nui.Bind(ValueBindName) : JsonString(Value);

            return Nui.TextEdit(placeholder, value, MaxLength, IsMultiLine);
        }
    }
}
