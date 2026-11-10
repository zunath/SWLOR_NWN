using SWLOR.Game.Server.Domain.UI;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
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
