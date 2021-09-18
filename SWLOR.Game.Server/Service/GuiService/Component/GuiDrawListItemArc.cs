using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiDrawListItemArc: GuiDrawListItem
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

        public GuiDrawListItemArc SetColor(GuiColor color)
        {
            Color = color;
            return this;
        }

        public GuiDrawListItemArc SetColor(int red, int green, int blue, int alpha= 255)
        {
            Color = new GuiColor(red, green, blue, alpha);
            return this;
        }

        public GuiDrawListItemArc BindColor(string bindName)
        {
            ColorBindName = bindName;
            return this;
        }

        public GuiDrawListItemArc SetIsFilled(bool isFilled)
        {
            IsFilled = isFilled;
            return this;
        }

        public GuiDrawListItemArc BindIsFilled(string bindName)
        {
            IsFilledBindName = bindName;
            return this;
        }

        public GuiDrawListItemArc SetLineThickness(float lineThickness)
        {
            LineThickness = lineThickness;
            return this;
        }

        public GuiDrawListItemArc BindLineThickness(string bindName)
        {
            LineThicknessBindName = bindName;
            return this;
        }

        public GuiDrawListItemArc SetCenter(GuiVector2 center)
        {
            Center = center;
            return this;
        }

        public GuiDrawListItemArc SetCenter(float x, float y)
        {
            Center = new GuiVector2(x, y);
            return this;
        }

        public GuiDrawListItemArc BindCenter(string bindName)
        {
            CenterBindName = bindName;
            return this;
        }


        public GuiDrawListItemArc SetRadius(float radius)
        {
            Radius = radius;
            return this;
        }

        public GuiDrawListItemArc BindRadius(string bindName)
        {
            RadiusBindName = bindName;
            return this;
        }


        public GuiDrawListItemArc SetAMinimum(float aMinimum)
        {
            AMinimum = aMinimum;
            return this;
        }

        public GuiDrawListItemArc BindAMinimum(string bindName)
        {
            AMinimumBindName = bindName;
            return this;
        }

        public GuiDrawListItemArc SetAMaximum(float aMaximum)
        {
            AMaximum = aMaximum;
            return this;
        }

        public GuiDrawListItemArc BindAMaximum(string bindName)
        {
            AMaximumBindName = bindName;
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
