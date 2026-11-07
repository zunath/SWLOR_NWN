using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>Timed stat modifiers a creature carries, grouped so a buff can replace, refresh or consume its own stacks.</summary>
    public interface ITemporaryStatModifierService
    {
        void ClearExitingObject();

        void PurgeAllExpired();

        void Add(uint creature, StatType statType, int amount, float durationSeconds, string group = null);

        void Add(uint creature, StatType statType, int amount, float durationSeconds, StatType groupStatType);

        void Replace(uint creature, StatType statType, int amount, float durationSeconds, string group = null);

        void Replace(uint creature, StatType statType, int amount, float durationSeconds, StatType groupStatType);

        int AddCapped(
            uint creature,
            StatType statType,
            int amount,
            float durationSeconds,
            int maxTotal,
            string group,
            int requestedStacks,
            bool refreshExistingStacks = false);

        int AddCapped(
            uint creature,
            StatType statType,
            int amount,
            float durationSeconds,
            int maxTotal,
            StatType groupStatType,
            int requestedStacks,
            bool refreshExistingStacks = false);

        int Consume(uint creature, StatType statType, string group = null);

        int Consume(uint creature, StatType statType, StatType groupStatType);

        /// <summary>Consumes only the payload belonging to a previously inspected temporary source.</summary>
        int ConsumeSource(uint creature, StatType statType, StatAdjustmentSource source);

        IReadOnlyList<StatAdjustmentSource> GetStatSources(uint creature, StatType payloadStat);

        int GetStatAdjustment(uint creature, StatType statType, string group = null);

        int GetStatAdjustment(uint creature, StatType statType, StatType groupStatType);

        void Refresh(uint creature, StatType statType, float durationSeconds, string group = null);

        void Refresh(uint creature, StatType statType, float durationSeconds, StatType groupStatType);

        void Clear(uint creature);
    }
}
