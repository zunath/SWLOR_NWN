using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiImage: GuiWidget
    {
        private string Resref { get; set; }
        private string ResrefBindName { get; set; }
        private bool IsResrefBound => !string.IsNullOrWhiteSpace(ResrefBindName);
        
        private NuiAspect Aspect { get; set; }
        private string AspectBindName { get; set; }
        private bool IsAspectBound => !string.IsNullOrWhiteSpace(AspectBindName);
        
        private NuiHorizontalAlign HorizontalAlign { get; set; }
        private string HorizontalAlignBindName { get; set; }
        private bool IsHorizontalAlignBound => !string.IsNullOrWhiteSpace(HorizontalAlignBindName);
        
        private NuiVerticalAlign  VerticalAlign { get; set; }
        private string VerticalAlignBindName { get; set; }
        private bool IsVerticalAlignBound => !string.IsNullOrWhiteSpace(VerticalAlignBindName);

        public GuiImage SetResref(string resref)
        {
            Resref = resref;
            return this;
        }

        public GuiImage BindResref(string bindName)
        {
            ResrefBindName = bindName;
            return this;
        }

        public GuiImage SetAspect(NuiAspect aspect)
        {
            Aspect = aspect;
            return this;
        }

        public GuiImage BindAspect(string bindName)
        {
            AspectBindName = bindName;
            return this;
        }

        public GuiImage SetHorizontalAlign(NuiHorizontalAlign hAlign)
        {
            HorizontalAlign = hAlign;
            return this;
        }

        public GuiImage BindHorizontalAlign(string bindName)
        {
            HorizontalAlignBindName = bindName;
            return this;
        }

        public GuiImage SetVerticalAlign(NuiVerticalAlign vAlign)
        {
            VerticalAlign = vAlign;
            return this;
        }

        public GuiImage BindVerticalAlign(string bindName)
        {
            VerticalAlignBindName = bindName;
            return this;
        }

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
