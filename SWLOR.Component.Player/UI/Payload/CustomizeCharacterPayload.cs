using SWLOR.Shared.UI.Model;

namespace SWLOR.Component.Player.UI.Payload
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
