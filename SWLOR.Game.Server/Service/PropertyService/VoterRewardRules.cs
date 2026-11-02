using System;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.PropertyService
{
    /// <summary>
    /// The election voter reward: 10 RP points to the character that casts a CD key's vote, once per CD key every
    /// 28 days across all cities. The treasury pays nothing.
    /// </summary>
    public static class VoterRewardRules
    {
        public const int RewardPoints = 10;
        public const int IntervalDays = 28;

        /// <summary>
        /// True when a CD key can earn the reward: it never has, or its last reward is at least 28 days old.
        /// </summary>
        /// <param name="lastRewardUtc">The ledger's last reward time for the CD key, or null when it has no entry.</param>
        /// <param name="nowUtc">The time of the vote.</param>
        public static bool IsDue(DateTime? lastRewardUtc, DateTime nowUtc)
        {
            return !lastRewardUtc.HasValue || nowUtc >= lastRewardUtc.Value.AddDays(IntervalDays);
        }

        /// <summary>
        /// Claims the reward for a vote. Returns the ledger entry to save when the reward is due, stamped with this
        /// vote's time, or null when the CD key was paid in the last 28 days. The entry is written with the first vote
        /// that pays, so changing or withdrawing that vote and voting again never pays a second time.
        /// </summary>
        /// <param name="ledger">The CD key's ledger entry, or null when it has none.</param>
        /// <param name="cdKey">The voting character's CD key.</param>
        /// <param name="nowUtc">The time of the vote.</param>
        public static VoterRewardLedger Claim(VoterRewardLedger ledger, string cdKey, DateTime nowUtc)
        {
            if (!IsDue(ledger?.LastRewardUtc, nowUtc))
                return null;

            ledger ??= new VoterRewardLedger(cdKey);
            ledger.LastRewardUtc = nowUtc;
            return ledger;
        }
    }
}
