using System;
using System.Linq.Expressions;
using SWLOR.NWN.API.Engine;
using SWLOR.Shared.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiCheckBox<T> : GuiWidget<T, GuiCheckBox<T>>
        where T: IGuiViewModel
    {
        private string Text { get; set; }
        private string TextBindName { get; set; }
        private bool IsTextBound => !string.IsNullOrWhiteSpace(TextBindName);
        
        private bool IsChecked { get; set; }
        private string IsCheckedBindName { get; set; }
        private bool IsCheckedBound => !string.IsNullOrWhiteSpace(IsCheckedBindName);

        /// <summary>
        /// Sets a static value for the text.
        /// </summary>
        /// <param name="text">The text to set.</param>
        public GuiCheckBox<T> SetText(string text)
        {
            Text = text;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the text.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiCheckBox<T> BindText<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            TextBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the check box.
        /// </summary>
        /// <param name="isChecked">true if checked, false otherwise</param>
        public GuiCheckBox<T> SetIsChecked(bool isChecked)
        {
            IsChecked = isChecked;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the check box.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiCheckBox<T> BindIsChecked<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            IsCheckedBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Builds the GuiCheckBox element.
        /// </summary>
        /// <returns>Json representing the checkbox element.</returns>
        public override Json BuildElement()
        {
            var text = IsTextBound ? Nui.Bind(TextBindName) : JsonString(Text);
            var isChecked = IsCheckedBound ? Nui.Bind(IsCheckedBindName) : JsonBool(IsChecked);

            return Nui.Check(text, isChecked);
        }
    }
}
