using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiComboBox: GuiWidget
    {
        public List<GuiComboEntry> Options { get; set; }
        public string OptionsBindName { get; set; }
        public bool IsOptionsBound => !string.IsNullOrWhiteSpace(OptionsBindName);

        public int SelectedIndex { get; set; }
        public string SelectedIndexBindName { get; set; }
        public bool IsSelectedIndexBound => !string.IsNullOrWhiteSpace(SelectedIndexBindName);

        public GuiComboBox()
        {
            Options = new List<GuiComboEntry>();
        }

        public override Json BuildElement()
        {
            var selectedIndex = IsSelectedIndexBound ? Nui.Bind(SelectedIndexBindName) : JsonInt(SelectedIndex);
            var options = JsonArray();

            foreach (var option in Options)
            {
                options = JsonArrayInsert(options, option.ToJson());
            }

            return Nui.Combo(options, selectedIndex);
        }
    }
}
