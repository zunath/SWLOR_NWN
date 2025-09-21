using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Shared.UI.Model;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class RenameItemPayload: GuiPayloadBase
    {
        public uint Target { get; set; }
        public RenameItemPayload(uint target)
        {
            Target = target;
        }
    }
}
