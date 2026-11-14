using SWLOR.Game.Server.Domain.UI;

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
