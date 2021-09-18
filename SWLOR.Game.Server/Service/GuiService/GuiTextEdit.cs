using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiTextEdit: GuiWidget
    {
        public string Placeholder { get; set; }
        public string PlaceholderBindName { get; set; }
        public bool IsPlaceholderBound => !string.IsNullOrWhiteSpace(PlaceholderBindName);

        public string Value { get; set; }
        public string ValueBindName { get; set; }
        public bool IsValueBound => !string.IsNullOrWhiteSpace(ValueBindName);

        public int MaxLength { get; set; }
        public bool IsMultiLine { get; set; }

        public override Json BuildElement()
        {
            var placeholder = IsPlaceholderBound ? Nui.Bind(PlaceholderBindName) : JsonString(Placeholder);
            var value = IsValueBound ? Nui.Bind(ValueBindName) : JsonString(Value);

            return Nui.TextEdit(placeholder, value, MaxLength, IsMultiLine);
        }
    }
}
