using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Character.UI.Payload
{
    public class CustomizeCharacterPayload: IGuiPayload
    {
        public uint Target { get; set; }

        public CustomizeCharacterPayload(uint target)
        {
            Target = target;
        }
    }
}
