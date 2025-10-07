namespace SWLOR.Shared.Domain.Character.Contracts
{
    public interface ICharacterResourceService
    {
        void RestoreHP(uint creature, int amount);
        void RestoreFP(uint creature, int amount);
        void RestoreSTM(uint creature, int amount);

        int GetCurrentHP(uint creature);
        int GetCurrentFP(uint creature);
        int GetCurrentSTM(uint creature);
    }
}
