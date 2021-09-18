using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiComboEntry
    {
        public string Label { get; set; }
        public int Value { get; set; }

        public GuiComboEntry(string label, int value)
        {
            Label = label;
            Value = value;
        }
        
        public Json ToJson()
        {
            return Nui.ComboEntry(Label, Value);
        }
    }
}
