using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.NWN.API;
using SWLOR.NWN.API.Core.Engine;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiLabel<T> : GuiWidget<T, GuiLabel<T>>
        where T: IGuiViewModel
    {
        private string Text { get; set; }
        private string TextBindName { get; set; }
        private bool IsTextBound => !string.IsNullOrWhiteSpace(TextBindName);
        
        private NuiHorizontalAlign HorizontalAlign { get; set; }
        private string HorizontalAlignBindName { get; set; }
        private bool IsHorizontalAlignBound => !string.IsNullOrWhiteSpace(HorizontalAlignBindName);
        
        private NuiVerticalAlign VerticalAlign { get; set; }
        private string VerticalAlignBindName { get; set; }
        private bool IsVerticalAlignBound => !string.IsNullOrWhiteSpace(VerticalAlignBindName);

        /// <summary>
        /// Sets a static value for the text of the label.
        /// </summary>
        /// <param name="text">The text to set.</param>
        public GuiLabel<T> SetText(string text)
        {
            Text = text;
            return this;
        }

        /// <summary>
        /// Binds a static value for the text of the label.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiLabel<T> BindText<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            TextBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the horizontal alignment of the label.
        /// </summary>
        /// <param name="hAlign">The horizontal alignment</param>
        public GuiLabel<T> SetHorizontalAlign(NuiHorizontalAlign hAlign)
        {
            HorizontalAlign = hAlign;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the horizontal alignment of the label.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiLabel<T> BindHorizontalAlign<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            HorizontalAlignBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the vertical alignment of the label.
        /// </summary>
        /// <param name="vAlign">The vertical alignment.</param>
        public GuiLabel<T> SetVerticalAlign(NuiVerticalAlign vAlign)
        {
            VerticalAlign = vAlign;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the vertical alignment of the label.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiLabel<T> BindVerticalAlign<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            VerticalAlignBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiLabel()
        {
            Text = string.Empty;
            HorizontalAlign = NuiHorizontalAlign.Center;
            VerticalAlign = NuiVerticalAlign.Middle;
        }

        /// <summary>
        /// Builds the GuiLabel element.
        /// </summary>
        /// <returns>Json representing the label element.</returns>
        public override Json BuildElement()
        {
            var text = IsTextBound ? Nui.Bind(TextBindName) : JsonString(Text);
            var hAlign = IsHorizontalAlignBound ? Nui.Bind(HorizontalAlignBindName) : JsonInt((int) HorizontalAlign);
            var vAlign = IsVerticalAlignBound ? Nui.Bind(VerticalAlignBindName) : JsonInt((int)VerticalAlign);

            return Nui.Label(text, hAlign, vAlign);
        }
    }
}
