using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiOptions<T> : GuiWidget<T, GuiOptions<T>>
        where T: IGuiViewModel
    {
        private NuiDirection Direction { get; set; }
        private List<string> OptionLabels { get; set; }

        private int SelectedValue { get; set; }
        private string SelectedValueBindName { get; set; }
        private bool IsSelectedValueBound => !string.IsNullOrWhiteSpace(SelectedValueBindName);

        public GuiOptions<T> SetDirection(NuiDirection direction)
        {
            Direction = direction;
            return this;
        }

        public GuiOptions<T> AddOption(string option)
        {
            OptionLabels.Add(option);
            return this;
        }

        public GuiOptions<T> SetSelectedValue(int selectedValue)
        {
            SelectedValue = selectedValue;
            return this;
        }

        public GuiOptions<T> BindSelectedValue<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            SelectedValueBindName = GuiHelper<T>.GetPropertyName(expression);
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
