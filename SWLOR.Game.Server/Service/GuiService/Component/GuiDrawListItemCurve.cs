using System;
using System.Linq.Expressions;
using SWLOR.NWN.API.Engine;
using SWLOR.Shared.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiDrawListItemCurve<T> : GuiDrawListItem<T, GuiDrawListItemCurve<T>>, IGuiDrawListItem
        where T: IGuiViewModel
    {
        private GuiColor Color { get; set; }
        private string ColorBindName { get; set; }
        private bool IsColorBound => !string.IsNullOrWhiteSpace(ColorBindName);
        
        private float LineThickness { get; set; }
        private string LineThicknessBindName { get; set; }
        private bool IsLineThicknessBound => !string.IsNullOrWhiteSpace(LineThicknessBindName);
        
        private GuiVector2 A { get; set; }
        private string ABindName { get; set; }
        private bool IsABound => !string.IsNullOrWhiteSpace(ABindName);
        
        private GuiVector2 B { get; set; }
        private string BBindName { get; set; }
        private bool IsBBound => !string.IsNullOrWhiteSpace(BBindName);
        
        private GuiVector2 Ctrl0 { get; set; }
        private string Ctrl0BindName { get; set; }
        private bool IsCtrl0Bound => !string.IsNullOrWhiteSpace(Ctrl0BindName);
        
        private GuiVector2 Ctrl1 { get; set; }
        private string Ctrl1BindName { get; set; }
        private bool IsCtrl1Bound => !string.IsNullOrWhiteSpace(Ctrl1BindName);

        public GuiDrawListItemCurve()
        {
            Color = new GuiColor(0, 0, 0);
            A = new GuiVector2(0, 0);
            B = new GuiVector2(0, 0);
            Ctrl0 = new GuiVector2(0, 0);
            Ctrl1 = new GuiVector2(0, 0);
        }

        /// <summary>
        /// Sets a static value for the Color property.
        /// </summary>
        /// <param name="color">The color to set.</param>
        public GuiDrawListItemCurve<T> SetColor(GuiColor color)
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
        public GuiDrawListItemCurve<T> SetColor(byte red, byte green, byte blue, byte alpha = 255)
        {
            Color = new GuiColor(red, green, blue, alpha);
            return this;
        }

        /// <summary>
        /// Binds a dynamic color to this element.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemCurve<T> BindColor<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ColorBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the line thickness.
        /// </summary>
        /// <param name="lineThickness">The thickness to use.</param>
        public GuiDrawListItemCurve<T> SetLineThickness(float lineThickness)
        {
            LineThickness = lineThickness;
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the line thickness.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemCurve<T> BindLineThickness<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            LineThicknessBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }
        
        /// <summary>
        /// Sets a static value for the A property of the curve.
        /// </summary>
        /// <param name="a">The A value</param>
        public GuiDrawListItemCurve<T> SetA(GuiVector2 a)
        {
            A = a;
            return this;
        }

        /// <summary>
        /// Sets a static value for the A property of the curve.
        /// </summary>
        /// <param name="x">The X coordinate</param>
        /// <param name="y">The Y coordinate</param>
        public GuiDrawListItemCurve<T> SetA(float x, float y)
        {
            A = new GuiVector2(x, y);
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the A property of the curve.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemCurve<T> BindA<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ABindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the B property of the curve.
        /// </summary>
        /// <param name="b">The B value</param>
        public GuiDrawListItemCurve<T> SetB(GuiVector2 b)
        {
            B = b;
            return this;
        }

        /// <summary>
        /// Sets a static value for the B property of the curve.
        /// </summary>
        /// <param name="x">The X coordinate</param>
        /// <param name="y">The Y coordinate</param>
        public GuiDrawListItemCurve<T> SetB(float x, float y)
        {
            B = new GuiVector2(x, y);
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the B property of the curve.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemCurve<T> BindB<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            BBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the Ctrl0 property of the curve.
        /// </summary>
        /// <param name="ctrl0">The B value</param>
        public GuiDrawListItemCurve<T> SetCtrl0(GuiVector2 ctrl0)
        {
            Ctrl0 = ctrl0;
            return this;
        }

        /// <summary>
        /// Sets a static value for the Ctrl0 property of the curve.
        /// </summary>
        /// <param name="x">The X coordinate</param>
        /// <param name="y">The Y coordinate</param>
        public GuiDrawListItemCurve<T> SetCtrl0(float x, float y)
        {
            Ctrl0 = new GuiVector2(x, y);
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the Ctrl0 property of the curve.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemCurve<T> BindCtrl0<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            Ctrl0BindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Sets a static value for the Ctrl1 property of the curve.
        /// </summary>
        /// <param name="ctrl1">The B value</param>
        public GuiDrawListItemCurve<T> SetCtrl1(GuiVector2 ctrl1)
        {
            Ctrl1 = ctrl1;
            return this;
        }

        /// <summary>
        /// Sets a static value for the Ctrl1 property of the curve.
        /// </summary>
        /// <param name="x">The X coordinate</param>
        /// <param name="y">The Y coordinate</param>
        public GuiDrawListItemCurve<T> SetCtrl1(float x, float y)
        {
            Ctrl1 = new GuiVector2(x, y);
            return this;
        }

        /// <summary>
        /// Binds a dynamic value for the Ctrl1 property of the curve.
        /// </summary>
        /// <typeparam name="TProperty">The property of the view model.</typeparam>
        /// <param name="expression">Expression to target the property.</param>
        public GuiDrawListItemCurve<T> BindCtrl1<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            Ctrl1BindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        /// <summary>
        /// Builds a GuiDrawListItemCurve element.
        /// </summary>
        /// <returns>Json representing the curve draw list item.</returns>
        public override Json ToJson()
        {
            var isEnabled = IsEnabledBound ? Nui.Bind(IsEnabledBindName) : JsonBool(IsEnabled);
            var color = IsColorBound ? Nui.Bind(ColorBindName) : Color.ToJson();
            var lineThickness = IsLineThicknessBound ? Nui.Bind(LineThicknessBindName) : JsonFloat(LineThickness);
            var a = IsABound ? Nui.Bind(ABindName) : A.ToJson();
            var b = IsBBound ? Nui.Bind(BBindName) : B.ToJson();
            var ctrl0 = IsCtrl0Bound ? Nui.Bind(Ctrl0BindName) : Ctrl0.ToJson();
            var ctrl1 = IsCtrl1Bound ? Nui.Bind(Ctrl1BindName) : Ctrl1.ToJson();

            return Nui.DrawListCurve(isEnabled, color, lineThickness, a, b, ctrl0, ctrl1);
        }
    }
}
