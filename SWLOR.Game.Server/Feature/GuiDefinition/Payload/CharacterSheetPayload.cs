using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class CharacterSheetPayload: GuiPayloadBase
    {
        public uint Target { get; set; }
        public bool IsPlayerMode { get; set; }

        public CharacterSheetPayload(uint target, bool isPlayerMode)
        {
            Target = target;
            IsPlayerMode = isPlayerMode;
        }
    }
}
