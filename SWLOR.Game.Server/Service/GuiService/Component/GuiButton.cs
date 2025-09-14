using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.NWN.API;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiButton<T>: GuiWidget<T, GuiButton<T>>
        where T: IGuiViewModel
    {
        private string Text { get; set; }
        private string TextBindName { get; set; }
        private bool IsTextBound => !string.IsNullOrWhiteSpace(TextBindName);

        /// <summary>
        /// Sets a static value to the Text property.
        /// </summary>
        /// <param name="text">The static value to set.</param>
        public GuiButton<T> SetText(string text)
        {
            Text = text;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value to the Text property.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiButton<T> BindText<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            TextBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Binds an action to the Click event of the button.
        /// Fires when the user clicks the button.
        /// </summary>
        /// <typeparam name="TMethod">The method of the view model.</typeparam>
        /// <param name="expression">Expression to target the method.</param>
        public GuiButton<T> BindOnClicked<TMethod>(Expression<Func<T, TMethod>> expression)
        {
            if (string.IsNullOrWhiteSpace(Id))
                Id = Guid.NewGuid().ToString();
            
            Events["click"] = GuiHelper<T>.GetMethodInfo(expression);
            
            return this;
        }

        /// <summary>
        /// Builds the GuiButton element.
        /// </summary>
        /// <returns>Json representing the button element.</returns>
        public override Json BuildElement()
        {
            var text = IsTextBound ? Nui.Bind(TextBindName) : JsonString(Text);

            return Nui.Button(text);
        }
    }
}
