using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiComboBox: GuiWidget
    {
        private List<GuiComboEntry> Options { get; set; }
        private string OptionsBindName { get; set; }
        private bool IsOptionsBound => !string.IsNullOrWhiteSpace(OptionsBindName);
        
        private int SelectedIndex { get; set; }
        private string SelectedIndexBindName { get; set; }
        private bool IsSelectedIndexBound => !string.IsNullOrWhiteSpace(SelectedIndexBindName);

        public GuiComboBox()
        {
            Options = new List<GuiComboEntry>();
        }

        public GuiComboBox AddOption(string label, int value)
        {
            Options.Add(new GuiComboEntry(label, value));
            return this;
        }

        public GuiComboBox BindOptions(string bindName)
        {
            OptionsBindName = bindName;
            return this;
        }

        public GuiComboBox SetSelectedIndex(int selectedIndex)
        {
            SelectedIndex = selectedIndex;
            return this;
        }

        public GuiComboBox BindSelectedIndex(string bindName)
        {
            SelectedIndexBindName = bindName;
            return this;
        }

        public override Json BuildElement()
        {
            var selectedIndex = IsSelectedIndexBound ? Nui.Bind(SelectedIndexBindName) : JsonInt(SelectedIndex);
            var options = JsonArray();

            if (IsOptionsBound)
            {
                options = Nui.Bind(OptionsBindName);
            }
            else
            {
                foreach (var option in Options)
                {
                    options = JsonArrayInsert(options, option.ToJson());
                }
            }

            return Nui.Combo(options, selectedIndex);
        }
    }
}
