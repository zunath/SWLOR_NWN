namespace SWLOR.Game.Server.Domain.UI
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
