using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiComboBox<T> : GuiWidget<T, GuiComboBox<T>>
        where T: IGuiViewModel
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

        /// <summary>
        /// Adds a static option to the combo box.
        /// </summary>
        /// <param name="label">The text displayed to the user.</param>
        /// <param name="value">The underlying value used to identify this option.</param>
        public GuiComboBox<T> AddOption(string label, int value)
        {
            Options.Add(new GuiComboEntry(label, value));
            return this;
        }

        /// <summary>
        /// Binds a dynamic set of options to the combo box.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiComboBox<T> BindOptions<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            OptionsBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the selected index.
        /// </summary>
        /// <param name="selectedIndex">The value to set to the selected index.</param>
        public GuiComboBox<T> SetSelectedIndex(int selectedIndex)
        {
            SelectedIndex = selectedIndex;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the selected index.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiComboBox<T> BindSelectedIndex<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            SelectedIndexBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Builds the GuiComboBox element.
        /// </summary>
        /// <returns>Json representing the combo box element.</returns>
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
