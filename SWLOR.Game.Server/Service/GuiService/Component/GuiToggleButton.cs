using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

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

        public GuiToggleButton<TDataModel> SetText(string text)
        {
            Text = text;
            return this;
        }

        public GuiToggleButton<TDataModel> BindText<TProperty>(Expression<Func<TDataModel, TProperty>> expression)
        {
            TextBindName = GuiHelper<TDataModel>.GetPropertyName(expression);
            return this;
        }

        public GuiToggleButton<TDataModel> SetIsToggled(bool isToggled)
        {
            IsToggled = isToggled;
            return this;
        }

        public GuiToggleButton<TDataModel> BindIsToggled<TProperty>(Expression<Func<TDataModel, TProperty>> expression)
        {
            IsToggledBindName = GuiHelper<TDataModel>.GetPropertyName(expression);
            return this;
        }

        public GuiToggleButton<TDataModel> OnClicked(GuiEventDelegate<IGuiViewModel> clickAction)
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
