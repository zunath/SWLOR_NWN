using System;
using System.Linq.Expressions;
using SWLOR.NWN.API.Engine;
using SWLOR.Shared.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiToggleButton<TDataModel> : GuiWidget<TDataModel, GuiToggleButton<TDataModel>>
        where TDataModel: IGuiViewModel
    {
        private string Text { get; set; }
        private string TextBindName { get; set; }
        private bool IsTextBound => !string.IsNullOrWhiteSpace(TextBindName);
        
        private bool IsToggled { get; set; }
        private string IsToggledBindName { get; set; }
        private bool IsToggledBound => !string.IsNullOrWhiteSpace(IsToggledBindName);
        
        /// <summary>
        /// Sets a static value for the text property of the button.
        /// </summary>
        /// <param name="text">The text to set.</param>
        public GuiToggleButton<TDataModel> SetText(string text)
        {
            Text = text;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value to the text property of the button.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiToggleButton<TDataModel> BindText<TProperty>(Expression<Func<TDataModel, TProperty>> expression)
        {
            TextBindName = GuiHelper<TDataModel>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for whether the button is toggled.
        /// </summary>
        /// <param name="isToggled">true if toggled, false otherwise</param>
        public GuiToggleButton<TDataModel> SetIsToggled(bool isToggled)
        {
            IsToggled = isToggled;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for whether the button is toggled.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiToggleButton<TDataModel> BindIsToggled<TProperty>(Expression<Func<TDataModel, TProperty>> expression)
        {
            IsToggledBindName = GuiHelper<TDataModel>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Binds an action to the Click event of the button.
        /// </summary>
        /// <typeparam name="TMethod">The method of the view model.</typeparam>
        /// <param name="expression">Expression to target the method.</param>
        public GuiToggleButton<TDataModel> BindOnClicked<TMethod>(Expression<Func<TDataModel, TMethod>> expression)
        {
            if (string.IsNullOrWhiteSpace(Id))
                Id = Guid.NewGuid().ToString();
            
            Events["click"] = GuiHelper<TDataModel>.GetMethodInfo(expression);

            return this;
        }

        /// <summary>
        /// Builds the GuiToggleButton element.
        /// </summary>
        /// <returns>Json representing the toggle button element.</returns>
        public override Json BuildElement()
        {
            var text = IsTextBound ? Nui.Bind(TextBindName) : JsonString(Text);
            var isToggled = IsToggledBound ? Nui.Bind(IsToggledBindName) : JsonBool(IsToggled);

            return Nui.ButtonSelect(text, isToggled);
        }
    }
}
