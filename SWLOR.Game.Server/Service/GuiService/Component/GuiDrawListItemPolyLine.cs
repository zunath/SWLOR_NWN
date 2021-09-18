using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiDrawListItemPolyLine: GuiDrawListItem
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
        
        public GuiDrawListItemPolyLine SetColor(GuiColor color)
        {
            Color = color;
            return this;
        }

        public GuiDrawListItemPolyLine SetColor(int red, int green, int blue, int alpha = 255)
        {
            Color = new GuiColor(red, green, blue, alpha);
            return this;
        }

        public GuiDrawListItemPolyLine BindColor(string bindName)
        {
            ColorBindName = bindName;
            return this;
        }

        public GuiDrawListItemPolyLine SetIsFilled(bool isFilled)
        {
            IsFilled = isFilled;
            return this;
        }

        public GuiDrawListItemPolyLine BindIsFilled(string bindName)
        {
            IsFilledBindName = bindName;
            return this;
        }

        public GuiDrawListItemPolyLine SetLineThickness(float lineThickness)
        {
            LineThickness = lineThickness;
            return this;
        }

        public GuiDrawListItemPolyLine BindLineThickness(string bindName)
        {
            LineThicknessBindName = bindName;
            return this;
        }

        public GuiDrawListItemPolyLine AddPoint(float x, float y)
        {
            Points.Add(new GuiVector2(x, y));
            return this;
        }

        public GuiDrawListItemPolyLine AddPoint(GuiVector2 point)
        {
            Points.Add(point);
            return this;
        }

        public GuiDrawListItemPolyLine BindPoints(string bindName)
        {
            PointsBindName = bindName;
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
