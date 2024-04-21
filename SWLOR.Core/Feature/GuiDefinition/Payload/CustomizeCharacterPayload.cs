using SWLOR.Core.Service.GuiService;

namespace SWLOR.Core.Feature.GuiDefinition.Payload
{
    public class CustomizeCharacterPayload: GuiPayloadBase
    {
        public uint Target { get; set; }

        public CustomizeCharacterPayload(uint target)
        {
            Target = target;
        }
    }
}
