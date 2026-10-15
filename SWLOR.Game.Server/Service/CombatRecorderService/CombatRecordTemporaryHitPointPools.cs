using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    /// <summary>
    /// Compares the temporary-HP pools captured before and after a damage application.
    /// </summary>
    public static class CombatRecordTemporaryHitPointPools
    {
        public static int Sum(CombatRecordTemporaryHitPointPool[] pools)
        {
            var total = 0;
            if (pools == null)
                return total;

            foreach (var pool in pools)
            {
                total += pool.Amount;
            }

            return total;
        }

        /// <summary>
        /// Temporary HP the hit took from one pre-hit pool.
        /// </summary>
        public static int GetAbsorbed(in CombatRecordTemporaryHitPointPool poolBefore, CombatRecordTemporaryHitPointPool[] poolsAfter)
        {
            return Math.Max(0, poolBefore.Amount - FindAmount(poolsAfter, poolBefore.EffectId));
        }

        /// <summary>
        /// Temporary HP the hit took from the pools that existed before it. A pool granted during
        /// the hit (a low-HP shield reacting to it) does not offset what the hit absorbed.
        /// </summary>
        public static int SumAbsorbed(CombatRecordTemporaryHitPointPool[] poolsBefore, CombatRecordTemporaryHitPointPool[] poolsAfter)
        {
            var total = 0;
            if (poolsBefore == null)
                return total;

            foreach (var pool in poolsBefore)
            {
                total += GetAbsorbed(pool, poolsAfter);
            }

            return total;
        }

        /// <summary>
        /// The pools of <paramref name="first"/>, plus each pool of <paramref name="second"/> it
        /// does not already hold. Merges the pools left after a hit with what removed or replaced
        /// pools had left when they went.
        /// </summary>
        public static CombatRecordTemporaryHitPointPool[] MergeKeepingFirst(
            CombatRecordTemporaryHitPointPool[] first, CombatRecordTemporaryHitPointPool[] second)
        {
            if (second == null || second.Length == 0)
                return first;
            if (first == null || first.Length == 0)
                return second;

            var merged = new List<CombatRecordTemporaryHitPointPool>(first);
            foreach (var pool in second)
            {
                if (!Contains(first, pool.EffectId))
                    merged.Add(pool);
            }

            return merged.ToArray();
        }

        private static bool Contains(CombatRecordTemporaryHitPointPool[] pools, ulong effectId)
        {
            foreach (var pool in pools)
            {
                if (pool.EffectId == effectId)
                    return true;
            }

            return false;
        }

        private static int FindAmount(CombatRecordTemporaryHitPointPool[] pools, ulong effectId)
        {
            if (pools == null)
                return 0;

            foreach (var pool in pools)
            {
                if (pool.EffectId == effectId)
                    return pool.Amount;
            }

            return 0;
        }
    }
}
