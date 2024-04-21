using SWLOR.Core.Service.GuiService;

namespace SWLOR.Core.Feature.GuiDefinition.Payload
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
