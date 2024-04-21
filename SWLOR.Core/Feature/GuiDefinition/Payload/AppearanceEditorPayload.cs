using SWLOR.Core.Service.GuiService;

namespace SWLOR.Core.Feature.GuiDefinition.Payload
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
