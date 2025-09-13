using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.NWN.API;
using SWLOR.NWN.API.Core.Engine;

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

        /// <summary>
        /// Sets a static value for the direction of the options.
        /// </summary>
        /// <param name="direction">The direction to set.</param>
        public GuiOptions<T> SetDirection(NuiDirection direction)
        {
            Direction = direction;
            return this;
        }

        /// <summary>
        /// Adds an option to select from.
        /// </summary>
        /// <param name="option">The option to add.</param>
        public GuiOptions<T> AddOption(string option)
        {
            OptionLabels.Add(option);
            return this;
        }

        /// <summary>
        /// Sets a static value for the selected value.
        /// </summary>
        /// <param name="selectedValue">The value to set.</param>
        public GuiOptions<T> SetSelectedValue(int selectedValue)
        {
            SelectedValue = selectedValue;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the selected value.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
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

        /// <summary>
        /// Builds the GuiOptions element.
        /// </summary>
        /// <returns>Json representing the options element.</returns>
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
