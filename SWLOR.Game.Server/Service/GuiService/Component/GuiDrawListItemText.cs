using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiDrawListItemText<T> : GuiDrawListItem<T, GuiDrawListItemText<T>>, IGuiDrawListItem
        where T: IGuiDataModel
    {
        private GuiColor Color { get; set; }
        private string ColorBindName { get; set; }
        private bool IsColorBound => !string.IsNullOrWhiteSpace(ColorBindName);
        
        private GuiRectangle Bounds { get; set; }
        private string BoundsBindName { get; set; }
        private bool IsBoundsBound => !string.IsNullOrWhiteSpace(BoundsBindName);
        
        private string Text { get; set; }
        private string TextBindName { get; set; }
        private bool IsTextBound => !string.IsNullOrWhiteSpace(TextBindName);

        public GuiDrawListItemText()
        {
            Color = new GuiColor(0, 0, 0);
            Bounds = new GuiRectangle(0, 0, 0, 0);
        }

        public GuiDrawListItemText<T> SetColor(GuiColor color)
        {
            Color = color;
            return this;
        }

        public GuiDrawListItemText<T> SetColor(int red, int green, int blue, int alpha = 255)
        {
            Color = new GuiColor(red, green, blue, alpha);
            return this;
        }

        public GuiDrawListItemText<T> BindColor<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ColorBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiDrawListItemText<T> SetBounds(GuiRectangle bounds)
        {
            Bounds = bounds;
            return this;
        }

        public GuiDrawListItemText<T> SetBounds(float x, float y, float width, float height)
        {
            Bounds = new GuiRectangle(x, y, width, height);
            return this;
        }

        public GuiDrawListItemText<T> BindBounds<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            BoundsBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiDrawListItemText<T> SetText(string text)
        {
            Text = text;
            return this;
        }

        public GuiDrawListItemText<T> BindText<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            TextBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public override Json ToJson()
        {
            var isEnabled = IsEnabledBound ? Nui.Bind(IsEnabledBindName) : JsonBool(IsEnabled);
            var color = IsColorBound ? Nui.Bind(ColorBindName) : Color.ToJson();
            var bounds = IsBoundsBound ? Nui.Bind(BoundsBindName) : Bounds.ToJson();
            var text = IsTextBound ? Nui.Bind(TextBindName) : JsonString(Text);

            return Nui.DrawListText(isEnabled, color, bounds, text); 
        }
    }
}