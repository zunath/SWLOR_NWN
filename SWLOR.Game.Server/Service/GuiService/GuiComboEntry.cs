using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiComboEntry: GuiWidget
    {
        public string Label { get; set; }
        public int Value { get; set; }

        public override Json BuildElement()
        {
            return Nui.ComboEntry(Label, Value);
        }
    }
}
