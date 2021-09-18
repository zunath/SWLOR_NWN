using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiImage: GuiWidget
    {
        public string Resref { get; set; }
        public string ResrefBindName { get; set; }
        public bool IsResrefBound => !string.IsNullOrWhiteSpace(ResrefBindName);

        public NuiAspect Aspect { get; set; }
        public string AspectBindName { get; set; }
        public bool IsAspectBound => !string.IsNullOrWhiteSpace(AspectBindName);

        public NuiHorizontalAlign HorizontalAlign { get; set; }
        public string HorizontalAlignBindName { get; set; }
        public bool IsHorizontalAlignBound => !string.IsNullOrWhiteSpace(HorizontalAlignBindName);

        public NuiVerticalAlign  VerticalAlign { get; set; }
        public string VerticalAlignBindName { get; set; }
        public bool IsVerticalAlignBound => !string.IsNullOrWhiteSpace(VerticalAlignBindName);

        public override Json BuildElement()
        {
            var resref = IsResrefBound ? Nui.Bind(ResrefBindName) : JsonString(Resref);
            var aspect = IsAspectBound ? Nui.Bind(AspectBindName) : JsonInt((int) Aspect);
            var hAlign = IsHorizontalAlignBound ? Nui.Bind(HorizontalAlignBindName) : JsonInt((int) HorizontalAlign);
            var vAlign = IsVerticalAlignBound ? Nui.Bind(VerticalAlignBindName) : JsonInt((int) VerticalAlign);

            return Nui.Image(resref, aspect, hAlign, vAlign);
        }
    }
}
