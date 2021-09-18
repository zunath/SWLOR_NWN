using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiDrawListItemImage: GuiDrawListItem
    {
        public string Resref { get; set; }
        public string ResrefBindName { get; set; }
        public bool IsResrefBound => !string.IsNullOrWhiteSpace(ResrefBindName);

        public GuiRectangle Position { get; set; }
        public string PositionBindName { get; set; }
        public bool IsPositionBound => !string.IsNullOrWhiteSpace(PositionBindName);

        public NuiAspect Aspect { get; set; }
        public string AspectBindName { get; set; }
        public bool IsAspectBound => !string.IsNullOrWhiteSpace(AspectBindName);

        public NuiHorizontalAlign HorizontalAlign { get; set; }
        public string HorizontalAlignBindName { get; set; }
        public bool IsHorizontalAlignBound => !string.IsNullOrWhiteSpace(HorizontalAlignBindName);

        public NuiVerticalAlign VerticalAlign { get; set; }
        public string VerticalAlignBindName { get; set; }
        public bool IsVerticalAlignBound => !string.IsNullOrWhiteSpace(VerticalAlignBindName);


        public override Json ToJson()
        {
            var isEnabled = IsEnabledBound ? Nui.Bind(IsEnabledBindName) : JsonBool(IsEnabled);
            var resref = IsResrefBound ? Nui.Bind(ResrefBindName) : JsonString(Resref);
            var position = IsPositionBound ? Nui.Bind(PositionBindName) : Position.ToJson();
            var aspect = IsAspectBound ? Nui.Bind(AspectBindName) : JsonInt((int)Aspect);
            var hAlign = IsHorizontalAlignBound ? Nui.Bind(HorizontalAlignBindName) : JsonInt((int)HorizontalAlign);
            var vAlign = IsVerticalAlignBound ? Nui.Bind(VerticalAlignBindName) : JsonInt((int)VerticalAlign);

            return Nui.DrawListImage(isEnabled, resref, position, aspect, hAlign, vAlign);
        }
    }
}
