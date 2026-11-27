namespace SWLOR.Game.Server.Domain.UI
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
