using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Shared.Core.Contracts
{
    public interface IBeastMasteryService
    {
        void CacheData();
        BeastDetail GetBeastDetail(BeastType type);
        BeastRoleAttribute GetBeastRoleDetail(BeastRoleType type);
        string GetBeastId(uint beast);
        void SetBeastId(uint beast, string beastId);
        BeastType GetBeastType(uint beast);
        bool IsPlayerBeast(uint beast);
        void SetBeastType(uint beast, BeastType type);
        void GiveBeastXP(uint beast, int xp, bool ignoreBonuses);
        int GetRequiredXP(int level, int xpPenalty);
        void SpawnBeast(uint player, string beastId, int percentHeal);
        (BeastFoodType, BeastFoodType) GetLikedAndHatedFood();
        void CombatPointXPDistributed();

        /// <summary>
        /// When a droid acquires an item, it is stored into a persistent variable on the controller item.
        /// </summary>
        void OnAcquireItem();

        void BeastOnEndCombatRound();
        void BeastOnHeartbeat();
        void BeastOnRested();
        void BeastOnSpawn();
        void OpenStablesMenu();
        void UseIncubator();
    }
}