using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiProgressBar: GuiWidget
    {
        private float Value { get; set; }
        private string ValueBindName { get; set; }
        private bool IsValueBound => !string.IsNullOrWhiteSpace(ValueBindName);

        public GuiProgressBar SetValue(float value)
        {
            Value = value;
            return this;
        }

        public GuiProgressBar BindValue(string bindName)
        {
            ValueBindName = bindName;
            return this;
        }

        public override Json BuildElement()
        {
            var value = IsValueBound ? Nui.Bind(ValueBindName) : JsonFloat(Value);

            return Nui.Progress(value);
        }
    }
}
