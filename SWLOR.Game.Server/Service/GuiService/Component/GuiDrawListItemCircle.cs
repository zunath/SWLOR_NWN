using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.NWN.API;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiDrawListItemCircle<T> : GuiDrawListItem<T, GuiDrawListItemCircle<T>>, IGuiDrawListItem
        where T: IGuiViewModel
    {
        private GuiColor Color { get; set; }
        private string ColorBindName { get; set; }
        private bool IsColorBound => !string.IsNullOrWhiteSpace(ColorBindName);
        
        private bool IsFilled { get; set; }
        private string IsFilledBindName { get; set; }
        private bool IsFilledBound => !string.IsNullOrWhiteSpace(IsFilledBindName);
        
        private float LineThickness { get; set; }
        private string LineThicknessBindName { get; set; }
        private bool IsLineThicknessBound => !string.IsNullOrWhiteSpace(LineThicknessBindName);
        
        private GuiRectangle Bounds { get; set; }
        private string BoundsBindName { get; set; }
        private bool IsBoundsBound => !string.IsNullOrWhiteSpace(BoundsBindName);

        public GuiDrawListItemCircle()
        {
            Color = new GuiColor(0, 0, 0);
            Bounds = new GuiRectangle(0, 0, 0, 0);
        }

        /// <summary>
        /// Sets a static value for the Color property.
        /// </summary>
        /// <param name="color">The color to set.</param>
        public GuiDrawListItemCircle<T> SetColor(GuiColor color)
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
        public GuiDrawListItemCircle<T> SetColor(byte red, byte green, byte blue, byte alpha = 255)
        {
            Color = new GuiColor(red, green, blue, alpha);
            return this;
        }

        /// <summary>
        /// Binds a dynamic color to this element.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemCircle<T> BindColor<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ColorBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the Filled property.
        /// </summary>
        /// <param name="isFilled">true if filled, false otherwise</param>
        public GuiDrawListItemCircle<T> SetIsFilled(bool isFilled)
        {
            IsFilled = isFilled;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value to the Filled property.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemCircle<T> BindIsFilled<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            IsFilledBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the line thickness.
        /// </summary>
        /// <param name="lineThickness">The thickness to use.</param>
        public GuiDrawListItemCircle<T> SetLineThickness(float lineThickness)
        {
            LineThickness = lineThickness;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the line thickness.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemCircle<T> BindLineThickness<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            LineThicknessBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the bounds.
        /// </summary>
        /// <param name="bounds">The bounds to use.</param>
        public GuiDrawListItemCircle<T> SetBounds(GuiRectangle bounds)
        {
            Bounds = bounds;
            return this;
        }

        /// <summary>
        /// Sets a static value for the bounds.
        /// </summary>
        /// <param name="x">The X coordinate</param>
        /// <param name="y">The Y coordinate</param>
        /// <param name="width">The width of the circle</param>
        /// <param name="height">The height of the circle</param>
        public GuiDrawListItemCircle<T> SetBounds(float x, float y, float width, float height)
        {
            Bounds = new GuiRectangle(x, y, width, height);
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the bounds.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemCircle<T> BindBounds<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            BoundsBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Builds a GuiDrawListItemCircle element.
        /// </summary>
        /// <returns>Json representing the circle draw list item.</returns>
        public override Json ToJson()
        {
            var isEnabled = IsEnabledBound ? Nui.Bind(IsEnabledBindName) : JsonBool(IsEnabled);
            var color = IsColorBound ? Nui.Bind(ColorBindName) : Color.ToJson();
            var isFilled = IsFilledBound ? Nui.Bind(IsFilledBindName) : JsonBool(IsFilled);
            var lineThickness = IsLineThicknessBound ? Nui.Bind(LineThicknessBindName) : JsonFloat(LineThickness);
            var bounds = IsBoundsBound ? Nui.Bind(BoundsBindName) : Bounds.ToJson();

            return Nui.DrawListCircle(isEnabled, color, isFilled, lineThickness, bounds);
        }
    }
}
