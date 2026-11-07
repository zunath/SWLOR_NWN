using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>Timed stat modifiers a creature carries. The logic lives in <see cref="ITemporaryStatModifierService"/>; the event handlers stay here and every member forwards to the container's instance.</summary>
    public static class TemporaryStatModifier
    {
        private static ITemporaryStatModifierService Instance => FacadeAccessor.Resolve<ITemporaryStatModifierService>();

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ClearExitingObject() => Instance.ClearExitingObject();

        [NWNEventHandler(ScriptName.OnModuleHeartbeat)]
        public static void PurgeAllExpired() => Instance.PurgeAllExpired();

        public static void Add(uint creature, StatType statType, int amount, float durationSeconds, string group = null) =>
            Instance.Add(creature, statType, amount, durationSeconds, group);

        public static void Add(uint creature, StatType statType, int amount, float durationSeconds, StatType groupStatType) =>
            Instance.Add(creature, statType, amount, durationSeconds, groupStatType);

        public static void Replace(uint creature, StatType statType, int amount, float durationSeconds, string group = null) =>
            Instance.Replace(creature, statType, amount, durationSeconds, group);

        public static void Replace(uint creature, StatType statType, int amount, float durationSeconds, StatType groupStatType) =>
            Instance.Replace(creature, statType, amount, durationSeconds, groupStatType);

        public static int AddCapped(
            uint creature,
            StatType statType,
            int amount,
            float durationSeconds,
            int maxTotal,
            string group,
            int requestedStacks,
            bool refreshExistingStacks = false) =>
            Instance.AddCapped(creature, statType, amount, durationSeconds, maxTotal, group, requestedStacks, refreshExistingStacks);

        public static int AddCapped(
            uint creature,
            StatType statType,
            int amount,
            float durationSeconds,
            int maxTotal,
            StatType groupStatType,
            int requestedStacks,
            bool refreshExistingStacks = false) =>
            Instance.AddCapped(creature, statType, amount, durationSeconds, maxTotal, groupStatType, requestedStacks, refreshExistingStacks);

        public static int Consume(uint creature, StatType statType, string group = null) =>
            Instance.Consume(creature, statType, group);

        public static int Consume(uint creature, StatType statType, StatType groupStatType) =>
            Instance.Consume(creature, statType, groupStatType);

        /// <summary>Consumes only the payload belonging to a previously inspected temporary source.</summary>
        public static int ConsumeSource(uint creature, StatType statType, StatAdjustmentSource source) =>
            Instance.ConsumeSource(creature, statType, source);

        public static IReadOnlyList<StatAdjustmentSource> GetStatSources(uint creature, StatType payloadStat) =>
            Instance.GetStatSources(creature, payloadStat);

        public static int GetStatAdjustment(uint creature, StatType statType, string group = null) =>
            Instance.GetStatAdjustment(creature, statType, group);

        public static int GetStatAdjustment(uint creature, StatType statType, StatType groupStatType) =>
            Instance.GetStatAdjustment(creature, statType, groupStatType);

        public static void Refresh(uint creature, StatType statType, float durationSeconds, string group = null) =>
            Instance.Refresh(creature, statType, durationSeconds, group);

        public static void Refresh(uint creature, StatType statType, float durationSeconds, StatType groupStatType) =>
            Instance.Refresh(creature, statType, durationSeconds, groupStatType);

        public static void Clear(uint creature) => Instance.Clear(creature);
    }
}
