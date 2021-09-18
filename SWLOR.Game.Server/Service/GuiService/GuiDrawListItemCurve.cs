using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiDrawListItemCurve : GuiDrawListItem
    {
        public GuiColor Color { get; set; }
        public string ColorBindName { get; set; }
        public bool IsColorBound => !string.IsNullOrWhiteSpace(ColorBindName);

        public float LineThickness { get; set; }
        public string LineThicknessBindName { get; set; }
        public bool IsLineThicknessBound => !string.IsNullOrWhiteSpace(LineThicknessBindName);
        
        public GuiVector2 A { get; set; }
        public string ABindName { get; set; }
        public bool IsABound => !string.IsNullOrWhiteSpace(ABindName);

        public GuiVector2 B { get; set; }
        public string BBindName { get; set; }
        public bool IsBBound => !string.IsNullOrWhiteSpace(BBindName);

        public GuiVector2 Ctrl0 { get; set; }
        public string Ctrl0BindName { get; set; }
        public bool IsCtrl0Bound => !string.IsNullOrWhiteSpace(Ctrl0BindName);

        public GuiVector2 Ctrl1 { get; set; }
        public string Ctrl1BindName { get; set; }
        public bool IsCtrl1Bound => !string.IsNullOrWhiteSpace(Ctrl1BindName);
        
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
