using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiComboBox<T> : GuiWidget<T, GuiComboBox<T>>
        where T: IGuiDataModel
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

        public GuiComboBox<T> AddOption(string label, int value)
        {
            Options.Add(new GuiComboEntry(label, value));
            return this;
        }

        public GuiComboBox<T> BindOptions<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            OptionsBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiComboBox<T> SetSelectedIndex(int selectedIndex)
        {
            SelectedIndex = selectedIndex;
            return this;
        }

        public GuiComboBox<T> BindSelectedIndex<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            SelectedIndexBindName = GuiHelper<T>.GetPropertyName(expression);
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
