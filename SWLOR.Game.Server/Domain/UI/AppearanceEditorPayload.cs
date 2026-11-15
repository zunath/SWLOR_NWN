namespace SWLOR.Game.Server.Domain.UI
{
    public class AppearanceEditorPayload: GuiPayloadBase
    {
        public uint Target { get; set; }

        public AppearanceEditorPayload()
        {
            Target = OBJECT_INVALID;
        }

        public AppearanceEditorPayload(uint target)
        {
            Target = target;
        }
    }
}
