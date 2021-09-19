using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiDrawListItemPolyLine<T>: GuiDrawListItem<T>
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
        
        private List<GuiVector2> Points { get; set; }
        private string PointsBindName { get; set; }
        private bool IsPointsBound => !string.IsNullOrWhiteSpace(PointsBindName);
        
        public GuiDrawListItemPolyLine<T> SetColor(GuiColor color)
        {
            Color = color;
            return this;
        }

        public GuiDrawListItemPolyLine<T> SetColor(int red, int green, int blue, int alpha = 255)
        {
            Color = new GuiColor(red, green, blue, alpha);
            return this;
        }

        public GuiDrawListItemPolyLine<T> BindColor<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            ColorBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiDrawListItemPolyLine<T> SetIsFilled(bool isFilled)
        {
            IsFilled = isFilled;
            return this;
        }

        public GuiDrawListItemPolyLine<T> BindIsFilled<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            IsFilledBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiDrawListItemPolyLine<T> SetLineThickness(float lineThickness)
        {
            LineThickness = lineThickness;
            return this;
        }

        public GuiDrawListItemPolyLine<T> BindLineThickness<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            LineThicknessBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiDrawListItemPolyLine<T> AddPoint(float x, float y)
        {
            Points.Add(new GuiVector2(x, y));
            return this;
        }

        public GuiDrawListItemPolyLine<T> AddPoint(GuiVector2 point)
        {
            Points.Add(point);
            return this;
        }

        public GuiDrawListItemPolyLine<T> BindPoints<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            PointsBindName = GuiHelper<T>.GetPropertyName(expression);
            return this;
        }

        public GuiDrawListItemPolyLine()
        {
            Points = new List<GuiVector2>();
            Color = new GuiColor(0, 0, 0);
        }

        public override Json ToJson()
        {
            var isEnabled = IsEnabledBound ? Nui.Bind(IsEnabledBindName) : JsonBool(IsEnabled);
            var color = IsColorBound ? Nui.Bind(ColorBindName) : Color.ToJson();
            var isFilled = IsFilledBound ? Nui.Bind(IsFilledBindName) : JsonBool(IsFilled);
            var lineThickness = IsLineThicknessBound ? Nui.Bind(LineThicknessBindName) : JsonFloat(LineThickness);
            var points = JsonArray();

            if (IsPointsBound)
            {
                points = Nui.Bind(PointsBindName);
            }
            else
            {
                foreach (var point in Points)
                {
                    points = JsonArrayInsert(points, point.ToJson());
                }
            }

            return Nui.DrawListPolyLine(isEnabled, color, isFilled, lineThickness, points);
        }
    }
}
