using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public abstract class GuiWidget
    {
        public string Id { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float AspectRatio { get; set; }
        
        public bool IsEnabled { get; set; }
        public string IsEnabledBindName { get; set; }
        public bool IsEnableBound => !string.IsNullOrWhiteSpace(IsEnabledBindName);

        public bool IsVisible { get; set; }
        public string IsVisibleBindName { get; set; }
        public bool IsVisibleBound => !string.IsNullOrWhiteSpace(IsVisibleBindName);

        public float Margin { get; set; }
        public float Padding { get; set; }

        public string Tooltip { get; set; }
        public string TooltipBindName { get; set; }
        public bool IsTooltipBound => !string.IsNullOrWhiteSpace(TooltipBindName);

        public GuiColor? Color { get; set; }
        public string ColorBindName { get; set; }
        public bool IsColorBound => !string.IsNullOrWhiteSpace(ColorBindName);

        public abstract Json BuildElement();

        protected GuiWidget()
        {
            IsEnabled = true;
            IsVisible = true;
        }

        public virtual Json ToJson()
        {
            var element = BuildElement();

            // Nui Id
            if (!string.IsNullOrWhiteSpace(Id))
            {
                element = Nui.Id(element, Id);
            }

            // Width
            if (Width > 0f)
            {
                element = Nui.Width(element, Width);
            }

            // Height
            if (Height > 0f)
            {
                element = Nui.Height(element, Height);
            }

            // Aspect Ratio
            if (AspectRatio > 0f)
            {
                element = Nui.Aspect(element, AspectRatio);
            }

            // Is Enabled (Can be bound)
            if (IsEnableBound)
            {
                var binding = Nui.Bind(IsEnabledBindName);
                element = Nui.Enabled(element, binding);
            }
            else
            {
                element = Nui.Enabled(element, JsonBool(IsEnabled));
            }

            // Is Visible (Can be bound)
            if (IsVisibleBound)
            {
                var binding = Nui.Bind(IsVisibleBindName);
                element = Nui.Visible(element, binding);
            }
            else
            {
                element = Nui.Visible(element, JsonBool(IsVisible));
            }

            // Margin
            if (Margin > 0f)
            {
                element = Nui.Margin(element, Margin);
            }

            // Padding
            if (Padding > 0f)
            {
                element = Nui.Padding(element, Padding);
            }

            // Tooltip (Can be bound)
            if (IsTooltipBound)
            {
                var binding = Nui.Bind(TooltipBindName);
                element = Nui.Tooltip(element, binding);
            }
            else if(!string.IsNullOrWhiteSpace(Tooltip))
            {
                element = Nui.Tooltip(element, JsonString(Tooltip));
            }

            // Color
            if (IsColorBound)
            {
                var binding = Nui.Bind(ColorBindName);
                element = Nui.StyleForegroundColor(element, binding);
            }
            else if (Color != null)
            {
                element = Nui.StyleForegroundColor(element, Color.ToJson());
            }

            return element;
        }
    }
}
