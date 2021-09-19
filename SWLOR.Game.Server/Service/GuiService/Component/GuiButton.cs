using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiButton<T>: GuiWidget<T, GuiButton<T>>
        where T: IGuiViewModel
    {
        private string Text { get; set; }
        private string TextBindName { get; set; }
        private bool IsTextBound => !string.IsNullOrWhiteSpace(TextBindName);

        public GuiButton<T> SetText(string text)
        {
            Text = text;
            return this;
        }

        public GuiButton<T> BindText<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            TextBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiButton<T> BindOnClicked<TMethod>(Expression<Func<T, TMethod>> expression)
        {
            if (string.IsNullOrWhiteSpace(Id))
                Id = Guid.NewGuid().ToString();
            
            Events["click"] = GuiHelper<T>.GetMethodInfo(expression);

            return this;
        }

        public override Json BuildElement()
        {
            var text = IsTextBound ? Nui.Bind(TextBindName) : JsonString(Text);

            return Nui.Button(text);
        }
    }
}
