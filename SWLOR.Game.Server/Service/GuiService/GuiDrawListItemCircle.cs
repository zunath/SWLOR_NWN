using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiDrawListItemCircle : GuiDrawListItem
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

        public GuiRectangle Bounds { get; set; }
        public string BoundsBindName { get; set; }
        public bool IsBoundsBound => !string.IsNullOrWhiteSpace(BoundsBindName);
        
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
