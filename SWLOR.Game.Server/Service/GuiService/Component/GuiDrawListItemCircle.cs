using System;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiDrawListItemCircle<T> : GuiDrawListItem<T, GuiDrawListItemCircle<T>>, IGuiDrawListItem
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
        
        private GuiRectangle Bounds { get; set; }
        private string BoundsBindName { get; set; }
        private bool IsBoundsBound => !string.IsNullOrWhiteSpace(BoundsBindName);

        public GuiDrawListItemCircle()
        {
            Color = new GuiColor(0, 0, 0);
            Bounds = new GuiRectangle(0, 0, 0, 0);
        }

        public GuiDrawListItemCircle<T> SetColor(GuiColor color)
        {
            Color = color;
            return this;
        }
        public GuiDrawListItemCircle<T> SetColor(int red, int green, int blue, int alpha = 255)
        {
            Color = new GuiColor(red, green, blue, alpha);
            return this;
        }

        public GuiDrawListItemCircle<T> BindColor<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ColorBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiDrawListItemCircle<T> SetIsFilled(bool isFilled)
        {
            IsFilled = isFilled;
            return this;
        }

        public GuiDrawListItemCircle<T> BindIsFilled<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            IsFilledBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiDrawListItemCircle<T> SetLineThickness(float lineThickness)
        {
            LineThickness = lineThickness;
            return this;
        }

        public GuiDrawListItemCircle<T> BindLineThickness<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            LineThicknessBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiDrawListItemCircle<T> SetBounds(GuiRectangle bounds)
        {
            Bounds = bounds;
            return this;
        }

        public GuiDrawListItemCircle<T> SetBounds(float x, float y, float width, float height)
        {
            Bounds = new GuiRectangle(x, y, width, height);
            return this;
        }

        public GuiDrawListItemCircle<T> BindBounds<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            BoundsBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

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
