using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Character.UI.Payload
{
    public class CharacterSheetPayload: IGuiPayload
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
