using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiDrawListItemArc<T>: GuiDrawListItem<T, GuiDrawListItemArc<T>>, IGuiDrawListItem
        where T: IGuiDataModel
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

        public GuiDrawListItemArc<T> SetColor(GuiColor color)
        {
            Color = color;
            return this;
        }

        public GuiDrawListItemArc<T> SetColor(int red, int green, int blue, int alpha= 255)
        {
            Color = new GuiColor(red, green, blue, alpha);
            return this;
        }

        public GuiDrawListItemArc<T> BindColor<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ColorBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiDrawListItemArc<T> SetIsFilled(bool isFilled)
        {
            IsFilled = isFilled;
            return this;
        }

        public GuiDrawListItemArc<T> BindIsFilled<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            IsFilledBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiDrawListItemArc<T> SetLineThickness(float lineThickness)
        {
            LineThickness = lineThickness;
            return this;
        }

        public GuiDrawListItemArc<T> BindLineThickness<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            LineThicknessBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiDrawListItemArc<T> SetCenter(GuiVector2 center)
        {
            Center = center;
            return this;
        }

        public GuiDrawListItemArc<T> SetCenter(float x, float y)
        {
            Center = new GuiVector2(x, y);
            return this;
        }

        public GuiDrawListItemArc<T> BindCenter<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            CenterBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }


        public GuiDrawListItemArc<T> SetRadius(float radius)
        {
            Radius = radius;
            return this;
        }

        public GuiDrawListItemArc<T> BindRadius<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            RadiusBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }


        public GuiDrawListItemArc<T> SetAMinimum(float aMinimum)
        {
            AMinimum = aMinimum;
            return this;
        }

        public GuiDrawListItemArc<T> BindAMinimum<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            AMinimumBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiDrawListItemArc<T> SetAMaximum(float aMaximum)
        {
            AMaximum = aMaximum;
            return this;
        }

        public GuiDrawListItemArc<T> BindAMaximum<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            AMaximumBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

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
