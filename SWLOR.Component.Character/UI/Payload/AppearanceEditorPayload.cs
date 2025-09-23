using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Character.UI.Payload
{
    public class AppearanceEditorPayload: IGuiPayload
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
