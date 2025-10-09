namespace SWLOR.Shared.Domain.Character.Contracts
{
    public interface IStatApplicationService
    {
        void ApplyCharacterMaxHP(uint creature);
        void ApplyCharacterMight(uint creature);
        void ApplyCharacterPerception(uint creature);
        void ApplyCharacterVitality(uint creature);
        void ApplyCharacterWillpower(uint creature);
        void ApplyCharacterAgility(uint creature);
        void ApplyCharacterSocial(uint creature);
        void ApplyNPCStats(uint npc);
    }
}