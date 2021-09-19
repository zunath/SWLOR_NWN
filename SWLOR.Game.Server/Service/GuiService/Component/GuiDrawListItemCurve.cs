using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiDrawListItemCurve<T> : GuiDrawListItem<T>
        where T: IGuiDataModel
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

        public GuiDrawListItemCurve<T> SetColor(GuiColor color)
        {
            Color = color;
            return this;
        }
        public GuiDrawListItemCurve<T> SetColor(int red, int green, int blue, int alpha = 255)
        {
            Color = new GuiColor(red, green, blue, alpha);
            return this;
        }

        public GuiDrawListItemCurve<T> BindColor<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ColorBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiDrawListItemCurve<T> SetLineThickness(float lineThickness)
        {
            LineThickness = lineThickness;
            return this;
        }

        public GuiDrawListItemCurve<T> BindLineThickness<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            LineThicknessBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiDrawListItemCurve<T> SetA(GuiVector2 a)
        {
            A = a;
            return this;
        }

        public GuiDrawListItemCurve<T> SetA(float x, float y)
        {
            A = new GuiVector2(x, y);
            return this;
        }

        public GuiDrawListItemCurve<T> BindA<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ABindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiDrawListItemCurve<T> SetB(GuiVector2 b)
        {
            B = b;
            return this;
        }

        public GuiDrawListItemCurve<T> SetB(float x, float y)
        {
            B = new GuiVector2(x, y);
            return this;
        }

        public GuiDrawListItemCurve<T> BindB<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            BBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiDrawListItemCurve<T> SetCtrl0(GuiVector2 ctrl0)
        {
            Ctrl0 = ctrl0;
            return this;
        }

        public GuiDrawListItemCurve<T> SetCtrl0(float x, float y)
        {
            Ctrl0 = new GuiVector2(x, y);
            return this;
        }

        public GuiDrawListItemCurve<T> BindCtrl0<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            Ctrl0BindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiDrawListItemCurve<T> SetCtrl1(GuiVector2 ctrl1)
        {
            Ctrl1 = ctrl1;
            return this;
        }

        public GuiDrawListItemCurve<T> SetCtrl1(float x, float y)
        {
            Ctrl1 = new GuiVector2(x, y);
            return this;
        }

        public GuiDrawListItemCurve<T> BindCtrl1<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            Ctrl1BindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

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
