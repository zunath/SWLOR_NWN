using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiOptions: GuiWidget
    {
        private NuiDirection Direction { get; set; }
        private List<string> OptionLabels { get; set; }

        private int SelectedValue { get; set; }
        private string SelectedValueBindName { get; set; }
        private bool IsSelectedValueBound => !string.IsNullOrWhiteSpace(SelectedValueBindName);

        public GuiOptions SetDirection(NuiDirection direction)
        {
            Direction = direction;
            return this;
        }

        public GuiOptions AddOption(string option)
        {
            OptionLabels.Add(option);
            return this;
        }

        public GuiOptions SetSelectedValue(int selectedValue)
        {
            SelectedValue = selectedValue;
            return this;
        }

        public GuiOptions BindSelectedValue(string bindName)
        {
            SelectedValueBindName = bindName;
            return this;
        }

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
