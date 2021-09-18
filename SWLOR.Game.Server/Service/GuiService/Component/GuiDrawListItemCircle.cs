using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiDrawListItemCircle : GuiDrawListItem
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

        public GuiDrawListItemCircle SetColor(GuiColor color)
        {
            Color = color;
            return this;
        }
        public GuiDrawListItemCircle SetColor(int red, int green, int blue, int alpha = 255)
        {
            Color = new GuiColor(red, green, blue, alpha);
            return this;
        }

        public GuiDrawListItemCircle BindColor(string bindName)
        {
            ColorBindName = bindName;
            return this;
        }

        public GuiDrawListItemCircle SetIsFilled(bool isFilled)
        {
            IsFilled = isFilled;
            return this;
        }

        public GuiDrawListItemCircle BindIsFilled(string bindName)
        {
            IsFilledBindName = bindName;
            return this;
        }

        public GuiDrawListItemCircle SetLineThickness(float lineThickness)
        {
            LineThickness = lineThickness;
            return this;
        }

        public GuiDrawListItemCircle BindLineThickness(string bindName)
        {
            LineThicknessBindName = bindName;
            return this;
        }

        public GuiDrawListItemCircle SetBounds(GuiRectangle bounds)
        {
            Bounds = bounds;
            return this;
        }

        public GuiDrawListItemCircle SetBounds(float x, float y, float width, float height)
        {
            Bounds = new GuiRectangle(x, y, width, height);
            return this;
        }

        public GuiDrawListItemCircle BindBounds(string bindName)
        {
            BoundsBindName = bindName;
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
