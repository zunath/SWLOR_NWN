using SWLOR.Shared.Core.Enums;

namespace SWLOR.Shared.Core.Contracts
{
    public interface ICombatPointService
    {
        void AddCombatPoint(uint player, uint creature, SkillType skill, int amount = 1);
        void AddCombatPointToAllTagged(uint activator, SkillType skill, int amount = 1);
        int GetRecentEnemyLevel(uint player);
        void ClearRecentEnemyLevel(uint player);
        int GetTaggedCreatureCount(uint player);
    }
}
