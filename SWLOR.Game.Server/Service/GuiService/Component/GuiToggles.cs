using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.NWN.API;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiToggles<T> : GuiWidget<T, GuiToggles<T>>
        where T : IGuiViewModel
    {
        private NuiDirection Direction { get; set; }
        private List<string> ElementLabels { get; set; }

        private int SelectedValue { get; set; }
        private string SelectedValueBindName { get; set; }
        private bool IsSelectedValueBound => !string.IsNullOrWhiteSpace(SelectedValueBindName);

        /// <summary>
        /// Sets a static value for the direction of the options.
        /// </summary>
        /// <param name="direction">The direction to set.</param>
        public GuiToggles<T> SetDirection(NuiDirection direction)
        {
            Direction = direction;
            return this;
        }

        /// <summary>
        /// Adds an option to select from.
        /// </summary>
        /// <param name="option">The option to add.</param>
        public GuiToggles<T> AddOption(string option)
        {
            ElementLabels.Add(option);
            return this;
        }

        /// <summary>
        /// Sets a static value for the selected value.
        /// </summary>
        /// <param name="selectedValue">The value to set.</param>
        public GuiToggles<T> SetSelectedValue(int selectedValue)
        {
            SelectedValue = selectedValue;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the selected value.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiToggles<T> BindSelectedValue<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            SelectedValueBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiToggles()
        {
            Direction = NuiDirection.Horizontal;
            ElementLabels = new List<string>();
        }

        /// <summary>
        /// Builds the GuiToggles element.
        /// </summary>
        /// <returns>Json representing the toggles element.</returns>
        public override Json BuildElement()
        {
            var selectedValue = IsSelectedValueBound ? Nui.Bind(SelectedValueBindName) : JsonInt(SelectedValue);
            var optionLabels = JsonArray();

            foreach (var option in ElementLabels)
            {
                optionLabels = JsonArrayInsert(optionLabels, JsonString(option));
            }

            return Nui.Toggles(Direction, optionLabels, selectedValue);
        }
    }
}
