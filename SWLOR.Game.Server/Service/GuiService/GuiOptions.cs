using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiOptions: GuiWidget
    {
        public NuiDirection Direction { get; set; }
        public List<string> OptionLabels { get; set; }
        public int SelectedValue { get; set; }
        public string SelectedValueBindName { get; set; }
        public bool IsSelectedValueBound => !string.IsNullOrWhiteSpace(SelectedValueBindName);

        public GuiOptions()
        {
            Direction = NuiDirection.Horizontal;
            OptionLabels = new List<string>();
        }

        public override Json BuildElement()
        {
            var selectedValue = IsSelectedValueBound ? Nui.Bind(SelectedValueBindName) : JsonInt(SelectedValue);
            var optionLabels = JsonArray();

            foreach (var option in OptionLabels)
            {
                optionLabels = JsonArrayInsert(optionLabels, JsonString(option));
            }

            return Nui.Options(Direction, optionLabels, selectedValue);
        }
    }
}
