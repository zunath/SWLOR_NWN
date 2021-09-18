using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiDrawListItemText : GuiDrawListItem
    {
        public GuiColor Color { get; set; }
        public string ColorBindName { get; set; }
        public bool IsColorBound => !string.IsNullOrWhiteSpace(ColorBindName);

        public GuiRectangle Bounds { get; set; }
        public string BoundsBindName { get; set; }
        public bool IsBoundsBound => !string.IsNullOrWhiteSpace(BoundsBindName);

        public string Text { get; set; }
        public string TextBindName { get; set; }
        public bool IsTextBound => !string.IsNullOrWhiteSpace(TextBindName);

        public override Json ToJson()
        {
            var isEnabled = IsEnabledBound ? Nui.Bind(IsEnabledBindName) : JsonBool(IsEnabled);
            var color = IsColorBound ? Nui.Bind(ColorBindName) : Color.ToJson();
            var bounds = IsBoundsBound ? Nui.Bind(BoundsBindName) : Bounds.ToJson();
            var text = IsTextBound ? Nui.Bind(TextBindName) : JsonString(Text);

            return Nui.DrawListText(isEnabled, color, bounds, text); 
        }
    }
}