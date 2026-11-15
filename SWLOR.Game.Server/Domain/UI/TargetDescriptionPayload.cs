namespace SWLOR.Game.Server.Domain.UI
{
    public class TargetDescriptionPayload : GuiPayloadBase
    {
        public uint Target { get; set; }
        public TargetDescriptionPayload(uint target)
        {
            Target = target;
        }
    }
}
