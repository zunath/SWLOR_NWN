using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiComboEntry
    {
        /// <summary>
        /// The text displayed to the user.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// The underlying value used to identify this option.
        /// </summary>
        public int Value { get; set; }

        public GuiComboEntry(string label, int value)
        {
            Label = label;
            Value = value;
        }
        
        /// <summary>
        /// Converts this combo entry to json able to be read by NWN.
        /// </summary>
        public Json ToJson()
        {
            return Nui.ComboEntry(Label, Value);
        }
    }
}
