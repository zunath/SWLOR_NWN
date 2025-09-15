using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiText<T> : GuiWidget<T, GuiText<T>>
        where T: IGuiViewModel
    {
        private string Text { get; set; }
        private string TextBindName { get; set; }
        private bool IsTextBound => !string.IsNullOrWhiteSpace(TextBindName);

        private bool ShowBorder { get; set; }
        private NuiScrollbars Scrollbars { get; set; }

        public GuiText()
        {
            ShowBorder = true;
            Scrollbars = NuiScrollbars.Auto;
        }

        /// <summary>
        /// Sets a static value for the Text property.
        /// </summary>
        /// <param name="text">The text to set.</param>
        public GuiText<T> SetText(string text)
        {
            Text = text;
            return this;
        }

        /// <summary>
        /// Sets a static value for whether the borders will be displayed.
        /// </summary>
        /// <param name="showBorder">The value to set.</param>
        public GuiText<T> SetShowBorder(bool showBorder)
        {
            ShowBorder = showBorder;
            return this;
        }

        /// <summary>
        /// Sets a static value for the scroll bars property.
        /// </summary>
        /// <param name="scrollbars">The scroll bar setting to use.</param>
        public GuiText<T> SetScrollbars(NuiScrollbars scrollbars)
        {
            Scrollbars = scrollbars;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the Text property.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiText<T> BindText<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            TextBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }


        public override Json BuildElement()
        {
            var text = IsTextBound ? Nui.Bind(TextBindName) : JsonString(Text);

            return Nui.Text(text, ShowBorder, Scrollbars);
        }
    }
}
