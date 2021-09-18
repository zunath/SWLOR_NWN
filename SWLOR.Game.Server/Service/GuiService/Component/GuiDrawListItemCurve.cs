using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiDrawListItemCurve : GuiDrawListItem
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

        public GuiDrawListItemCurve SetColor(GuiColor color)
        {
            Color = color;
            return this;
        }
        public GuiDrawListItemCurve SetColor(int red, int green, int blue, int alpha = 255)
        {
            Color = new GuiColor(red, green, blue, alpha);
            return this;
        }

        public GuiDrawListItemCurve BindColor(string bindName)
        {
            ColorBindName = bindName;
            return this;
        }

        public GuiDrawListItemCurve SetLineThickness(float lineThickness)
        {
            LineThickness = lineThickness;
            return this;
        }

        public GuiDrawListItemCurve BindLineThickness(string bindName)
        {
            LineThicknessBindName = bindName;
            return this;
        }

        public GuiDrawListItemCurve SetA(GuiVector2 a)
        {
            A = a;
            return this;
        }

        public GuiDrawListItemCurve SetA(float x, float y)
        {
            A = new GuiVector2(x, y);
            return this;
        }

        public GuiDrawListItemCurve BindA(string bindName)
        {
            ABindName = bindName;
            return this;
        }

        public GuiDrawListItemCurve SetB(GuiVector2 b)
        {
            B = b;
            return this;
        }

        public GuiDrawListItemCurve SetB(float x, float y)
        {
            B = new GuiVector2(x, y);
            return this;
        }

        public GuiDrawListItemCurve BindB(string bindName)
        {
            BBindName = bindName;
            return this;
        }

        public GuiDrawListItemCurve SetCtrl0(GuiVector2 ctrl0)
        {
            Ctrl0 = ctrl0;
            return this;
        }

        public GuiDrawListItemCurve SetCtrl0(float x, float y)
        {
            Ctrl0 = new GuiVector2(x, y);
            return this;
        }

        public GuiDrawListItemCurve BindCtrl0(string bindName)
        {
            Ctrl0BindName = bindName;
            return this;
        }

        public GuiDrawListItemCurve SetCtrl1(GuiVector2 ctrl1)
        {
            Ctrl1 = ctrl1;
            return this;
        }

        public GuiDrawListItemCurve SetCtrl1(float x, float y)
        {
            Ctrl1 = new GuiVector2(x, y);
            return this;
        }

        public GuiDrawListItemCurve BindCtrl1(string bindName)
        {
            Ctrl1BindName = bindName;
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
