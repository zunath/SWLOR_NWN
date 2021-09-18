using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiTextEdit: GuiWidget
    {
        private string Placeholder { get; set; }
        private string PlaceholderBindName { get; set; }
        private bool IsPlaceholderBound => !string.IsNullOrWhiteSpace(PlaceholderBindName);
        
        private string Value { get; set; }
        private string ValueBindName { get; set; }
        private bool IsValueBound => !string.IsNullOrWhiteSpace(ValueBindName);
        
        private int MaxLength { get; set; }
        private bool IsMultiLine { get; set; }

        public GuiTextEdit SetPlaceholder(string placeholder)
        {
            Placeholder = placeholder;
            return this;
        }

        public GuiTextEdit BindPlaceholder(string bindName)
        {
            PlaceholderBindName = bindName;
            return this;
        }

        public GuiTextEdit SetValue(string value)
        {
            Value = value;
            return this;
        }

        public GuiTextEdit BindValue(string bindName)
        {
            ValueBindName = bindName;
            return this;
        }

        public GuiTextEdit SetMaxLength(int maxLength)
        {
            MaxLength = maxLength;
            return this;
        }

        public GuiTextEdit SetIsMultiline(bool isMultiLine)
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
