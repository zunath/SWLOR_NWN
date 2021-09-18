using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiProgressBar: GuiWidget
    {
        public float Value { get; set; }
        public string ValueBindName { get; set; }
        public bool IsValueBound => !string.IsNullOrWhiteSpace(ValueBindName);

        public override Json BuildElement()
        {
            var value = IsValueBound ? Nui.Bind(ValueBindName) : JsonFloat(Value);

            return Nui.Progress(value);
        }
    }
}
