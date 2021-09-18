using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiDrawListItemPolyLine: GuiDrawListItem
    {
        public GuiColor Color { get; set; }
        public string ColorBindName { get; set; }
        public bool IsColorBound => !string.IsNullOrWhiteSpace(ColorBindName);

        public bool IsFilled { get; set; }
        public string IsFilledBindName { get; set; }
        public bool IsFilledBound => !string.IsNullOrWhiteSpace(IsFilledBindName);

        public float LineThickness { get; set; }
        public string LineThicknessBindName { get; set; }
        public bool IsLineThicknessBound => !string.IsNullOrWhiteSpace(LineThicknessBindName);

        public List<GuiVector2> Points { get; set; }
        public string PointsBindName { get; set; }
        public bool IsPointsBound => !string.IsNullOrWhiteSpace(PointsBindName);


        public GuiDrawListItemPolyLine()
        {
            Points = new List<GuiVector2>();
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
