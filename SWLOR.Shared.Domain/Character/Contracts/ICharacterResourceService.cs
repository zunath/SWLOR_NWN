using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Shared.Domain.Character.Contracts
{
    public interface ICharacterResourceService
    {
        void RestoreHP(uint creature, int amount);
        void RestoreFP(uint creature, int amount);
        void RestoreSTM(uint creature, int amount);

        void ReduceFP(uint creature, int reduceBy, Player dbPlayer = null);
        void ReduceStamina(uint creature, int reduceBy, Player dbPlayer = null);

        void SetCurrentFP(uint creature, int amount);
        void SetCurrentSTM(uint creature, int amount);

        int GetCurrentHP(uint creature);
        int GetCurrentFP(uint creature);
        int GetCurrentSTM(uint creature);

        void NPCNaturalRegen();
    }
}
