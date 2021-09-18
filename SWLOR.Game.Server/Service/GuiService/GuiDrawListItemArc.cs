using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiDrawListItemArc: GuiDrawListItem
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

        public GuiVector2 Center { get; set; }
        public string CenterBindName { get; set; }
        public bool IsCenterBound => !string.IsNullOrWhiteSpace(CenterBindName);

        public float Radius { get; set; }
        public string RadiusBindName { get; set; }
        public bool IsRadiusBound => !string.IsNullOrWhiteSpace(RadiusBindName);


        public float AMinimum { get; set; }
        public string AMinimumBindName { get; set; }
        public bool IsAMinimumBound => !string.IsNullOrWhiteSpace(AMinimumBindName);

        public float AMaximum { get; set; }
        public string AMaximumBindName { get; set; }
        public bool IsAMaximumBound => !string.IsNullOrWhiteSpace(AMaximumBindName);

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
