using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiToggleButton<T> : GuiWidget<T, GuiToggleButton<T>>
        where T: IGuiDataModel
    {
        private string Text { get; set; }
        private string TextBindName { get; set; }
        private bool IsTextBound => !string.IsNullOrWhiteSpace(TextBindName);
        
        private bool IsToggled { get; set; }
        private string IsToggledBindName { get; set; }
        private bool IsToggledBound => !string.IsNullOrWhiteSpace(IsToggledBindName);

        public GuiToggleButton<T> SetText(string text)
        {
            Text = text;
            return this;
        }

        public GuiToggleButton<T> BindText<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            TextBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiToggleButton<T> SetIsToggled(bool isToggled)
        {
            IsToggled = isToggled;
            return this;
        }

        public GuiToggleButton<T> BindIsToggled<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            IsToggledBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiToggleButton<T> OnClicked(GuiEventDelegate clickAction)
        {
            if (string.IsNullOrWhiteSpace(Id))
                Id = Guid.NewGuid().ToString();

            Events["click"] = clickAction;

            return this;
        }

        public override Json BuildElement()
        {
            var text = IsTextBound ? Nui.Bind(TextBindName) : JsonString(Text);
            var isToggled = IsToggledBound ? Nui.Bind(IsToggledBindName) : JsonBool(IsToggled);

            return Nui.ButtonSelect(text, isToggled);
        }
    }
}
