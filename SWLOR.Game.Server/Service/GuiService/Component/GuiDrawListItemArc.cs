using System;
using System.Linq.Expressions;
using SWLOR.NWN.API.Engine;
using SWLOR.Shared.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiDrawListItemArc<T>: GuiDrawListItem<T, GuiDrawListItemArc<T>>, IGuiDrawListItem
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
        
        private GuiVector2 Center { get; set; }
        private string CenterBindName { get; set; }
        private bool IsCenterBound => !string.IsNullOrWhiteSpace(CenterBindName);
        
        private float Radius { get; set; }
        private string RadiusBindName { get; set; }
        private bool IsRadiusBound => !string.IsNullOrWhiteSpace(RadiusBindName);
        
        private float AMinimum { get; set; }
        private string AMinimumBindName { get; set; }
        private bool IsAMinimumBound => !string.IsNullOrWhiteSpace(AMinimumBindName);
        
        private float AMaximum { get; set; }
        private string AMaximumBindName { get; set; }
        private bool IsAMaximumBound => !string.IsNullOrWhiteSpace(AMaximumBindName);

        public GuiDrawListItemArc()
        {
            Color = new GuiColor(0, 0, 0);
            Center = new GuiVector2(0f, 0f);
        }

        /// <summary>
        /// Sets a static value for the Color property.
        /// </summary>
        /// <param name="color">The color to set.</param>
        public GuiDrawListItemArc<T> SetColor(GuiColor color)
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
        public GuiDrawListItemArc<T> SetColor(byte red, byte green, byte blue, byte alpha = 255)
        {
            Color = new GuiColor(red, green, blue, alpha);
            return this;
        }

        /// <summary>
        /// Binds a dynamic color to this element.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemArc<T> BindColor<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ColorBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the Filled property.
        /// </summary>
        /// <param name="isFilled">true if filled, false otherwise</param>
        public GuiDrawListItemArc<T> SetIsFilled(bool isFilled)
        {
            IsFilled = isFilled;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value to the Filled property.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemArc<T> BindIsFilled<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            IsFilledBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the line thickness.
        /// </summary>
        /// <param name="lineThickness">The thickness to use.</param>
        public GuiDrawListItemArc<T> SetLineThickness(float lineThickness)
        {
            LineThickness = lineThickness;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the line thickness.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemArc<T> BindLineThickness<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            LineThicknessBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the Center property.
        /// </summary>
        /// <param name="center">The value to set for the center.</param>
        public GuiDrawListItemArc<T> SetCenter(GuiVector2 center)
        {
            Center = center;
            return this;
        }

        /// <summary>
        /// Sets a static value for the Center property.
        /// </summary>
        /// <param name="x">The X value to set for the center.</param>
        /// <param name="y">The Y value to set for the center.</param>
        public GuiDrawListItemArc<T> SetCenter(float x, float y)
        {
            Center = new GuiVector2(x, y);
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the Center property.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemArc<T> BindCenter<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            CenterBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the Radius property.
        /// </summary>
        /// <param name="radius">The value to set for the Radius.</param>
        public GuiDrawListItemArc<T> SetRadius(float radius)
        {
            Radius = radius;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the Radius property.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemArc<T> BindRadius<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            RadiusBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the A Minimum property.
        /// </summary>
        /// <param name="aMinimum">The value to set for the A Minimum property.</param>
        public GuiDrawListItemArc<T> SetAMinimum(float aMinimum)
        {
            AMinimum = aMinimum;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the A Minimum property.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemArc<T> BindAMinimum<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            AMinimumBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the A Maximum property.
        /// </summary>
        /// <param name="aMaximum">The value to set for the A Maximum property.</param>
        public GuiDrawListItemArc<T> SetAMaximum(float aMaximum)
        {
            AMaximum = aMaximum;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the A Maximum property.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemArc<T> BindAMaximum<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            AMaximumBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Builds the GuiDrawListItemArc element.
        /// </summary>
        /// <returns>Json representing the arc draw list item.</returns>
        public override Json ToJson()
        {
            var isEnabled = IsEnabledBound ? Nui.Bind(IsEnabledBindName) : JsonBool(IsEnabled);
            var color = IsColorBound ? Nui.Bind(ColorBindName) : Color.ToJson();
            var isFilled = IsFilledBound ? Nui.Bind(IsFilledBindName) : JsonBool(IsFilled);
            var lineThickness = IsLineThicknessBound ? Nui.Bind(LineThicknessBindName) : JsonFloat(LineThickness);
            var center = IsCenterBound ? Nui.Bind(CenterBindName) : Center.ToJson();
            var radius = IsRadiusBound ? Nui.Bind(RadiusBindName) : JsonFloat(Radius);
            var aMinimum = IsAMinimumBound ? Nui.Bind(AMinimumBindName) : JsonFloat(AMinimum);
            var aMaximum = IsAMaximumBound ? Nui.Bind(AMaximumBindName) : JsonFloat(AMaximum);

            return Nui.DrawListArc(isEnabled, color, isFilled, lineThickness, center, radius, aMinimum, aMaximum);
        }
    }
}
