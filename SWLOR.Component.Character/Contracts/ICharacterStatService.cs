using SWLOR.Shared.Domain.Character.Enums;

namespace SWLOR.Component.Character.Contracts
{
    public interface ICharacterStatService
    {
        void RegisterNPC(uint npc);
        void UnregisterNPC(uint npc);
        int GetStat(uint creature, StatType stat);
    }
}
