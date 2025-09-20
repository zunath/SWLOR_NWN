using System;
using System.Linq.Expressions;
using SWLOR.NWN.API.Engine;
using SWLOR.Shared.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiDrawListItemText<T> : GuiDrawListItem<T, GuiDrawListItemText<T>>, IGuiDrawListItem
        where T: IGuiViewModel
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

        /// <summary>
        /// Sets a static value for the Color property.
        /// </summary>
        /// <param name="color">The color to set.</param>
        public GuiDrawListItemText<T> SetColor(GuiColor color)
        {
            Color = color;
            return this;
        }

        /// <summary>
        /// Sets a static value for the Color property.
        /// </summary>
        /// <param name="red">The amount of red to use. 0-255</param>
        /// <param name="green">The amount of green to use. 0-255</param>
        /// <param name="blue">The amount of blue to use. 0-255</param>
        /// <param name="alpha">The amount of alpha to use. 0-255</param>
        public GuiDrawListItemText<T> SetColor(byte red, byte green, byte blue, byte alpha = 255)
        {
            Color = new GuiColor(red, green, blue, alpha);
            return this;
        }

        /// <summary>
        /// Binds a dynamic color to this element.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemText<T> BindColor<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ColorBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the Bounds of the text.
        /// </summary>
        /// <param name="bounds">The bounds to set.</param>
        public GuiDrawListItemText<T> SetBounds(GuiRectangle bounds)
        {
            Bounds = bounds;
            return this;
        }

        /// <summary>
        /// Sets a static value for the Bounds of the text.
        /// </summary>
        /// <param name="x">The X coordinate</param>
        /// <param name="y">The Y coordinate</param>
        /// <param name="width">The width of the text.</param>
        /// <param name="height">The height of the text.</param>
        public GuiDrawListItemText<T> SetBounds(float x, float y, float width, float height)
        {
            Bounds = new GuiRectangle(x, y, width, height);
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the Bounds of the text.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemText<T> BindBounds<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            BoundsBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the Text.
        /// </summary>
        /// <param name="text">The text to set.</param>
        public GuiDrawListItemText<T> SetText(string text)
        {
            Text = text;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the Text property.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemText<T> BindText<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            TextBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Builds a GuiDrawListItemText element.
        /// </summary>
        /// <returns>Json representing the text draw list item.</returns>
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