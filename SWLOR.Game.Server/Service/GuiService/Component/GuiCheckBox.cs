using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

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

        public GuiCheckBox<T> SetText(string text)
        {
            Text = text;
            return this;
        }

        public GuiCheckBox<T> BindText<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            TextBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiCheckBox<T> SetIsChecked(bool isChecked)
        {
            IsChecked = isChecked;
            return this;
        }

        public GuiCheckBox<T> BindIsChecked<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            IsCheckedBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public override Json BuildElement()
        {
            var text = IsTextBound ? Nui.Bind(TextBindName) : JsonString(Text);
            var isChecked = IsCheckedBound ? Nui.Bind(IsCheckedBindName) : JsonBool(IsChecked);

            return Nui.Check(text, isChecked);
        }
    }
}
