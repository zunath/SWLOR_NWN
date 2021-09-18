using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiButtonImage: GuiWidget
    {
        private string Resref { get; set; }
        private string ResrefBindName { get; set; }
        private bool IsResrefBound => !string.IsNullOrWhiteSpace(ResrefBindName);

        public GuiButtonImage SetResref(string resref)
        {
            Resref = resref;
            return this;
        }

        public GuiButtonImage BindResref(string bindName)
        {
            ResrefBindName = bindName;
            return this;
        }

        public override Json BuildElement()
        {
            var resref = IsResrefBound ? Nui.Bind(ResrefBindName) : JsonString(Resref);

            return Nui.ButtonImage(resref);
        }
    }
}
