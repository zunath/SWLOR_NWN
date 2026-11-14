using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service.SlicingService
{
    /// <summary>
    /// The space slicing rules (plan section 24, "Slicing in space") as pure functions: which sources are space
    /// targets, their tiers, who aboard slices, the Data Analyzer's range, data cores, locked caches and space lockbox
    /// drops. Numbers come from <see cref="SpaceSlicingTuning"/>.
    /// </summary>
    public static class SpaceSlicingRules
    {
        /// <summary>
        /// The items of a reward the slicer's full inventory refused, as ShipHold.KeepUnplacedYield keeps them: one
        /// entry per item, a component with its trait. Credits and agent intel are never items.
        /// </summary>
        public static List<(string Resref, ShipComponentTraitType? Trait)> UnplacedItems(SpaceSlicingReward reward)
        {
            if (reward == null || reward.Kind is SpaceSlicingRewardKind.Credits or SpaceSlicingRewardKind.AgentIntel ||
                string.IsNullOrWhiteSpace(reward.Resref))
            {
                return new List<(string Resref, ShipComponentTraitType? Trait)>();
            }

            ShipComponentTraitType? trait = reward.Trait == ShipComponentTraitType.None ? null : reward.Trait;
            return Enumerable.Repeat((reward.Resref, trait), Math.Max(0, reward.Quantity)).ToList();
        }

        /// <summary>Whether a source is one of the space targets.</summary>
        public static bool IsSpaceSource(SlicingSourceType source) =>
            source is SlicingSourceType.DataCan or SlicingSourceType.LockedCache or SlicingSourceType.WreckDataCore
                or SlicingSourceType.CustomsScan or SlicingSourceType.SpaceLockbox;

        /// <summary>Whether a source is started by the Data Analyzer on a target within its range.</summary>
        public static bool UsesDataAnalyzer(SlicingSourceType source) =>
            source is SlicingSourceType.DataCan or SlicingSourceType.LockedCache or SlicingSourceType.WreckDataCore;

        /// <summary>Whether a source is an item its owner carries and slices (and the window shows the lockbox art).</summary>
        public static bool IsPortable(SlicingSourceType source) =>
            source is SlicingSourceType.Lockbox or SlicingSourceType.SpaceLockbox;

        /// <summary>A space target's slicing tier: its area's tier, at most the highest board tier; Wild Space targets are tier 5.</summary>
        public static int TargetTier(int areaTier, bool wildSpace) =>
            wildSpace ? SpaceSlicingTuning.WildSpaceTier : Math.Clamp(areaTier, 1, SpaceSlicingTuning.MaximumTier);

        /// <summary>Whether a target this far from the ship (meters, same area) is within the Data Analyzer's range.</summary>
        public static bool IsInAnalyzerRange(float distance) => distance >= 0f && distance <= SpaceSlicingTuning.AnalyzerRange;

        /// <summary>
        /// Who aboard slices a target of a tier: the pilot or the Sensors crew member, whoever has the Slicing rank for
        /// it (the higher rank when both do, the pilot on a tie). When neither has it, the pilot, who is then refused.
        /// </summary>
        public static ShipCrewSeat PickSlicer(int pilotSlicingRank, bool hasSensorsCrew, int sensorsSlicingRank, int tier)
        {
            var pilotCan = pilotSlicingRank >= tier;
            var crewCan = hasSensorsCrew && sensorsSlicingRank >= tier;
            if (crewCan && (!pilotCan || sensorsSlicingRank > pilotSlicingRank))
                return ShipCrewSeat.Sensors;

            return ShipCrewSeat.Pilot;
        }

        /// <summary>Whether a destroyed NPC ship's wreck carries a data core: elite and boss ships and aces.</summary>
        public static bool HasDataCore(WreckDifficulty difficulty, bool isAce) =>
            isAce || SpaceSlicingTuning.DataCoreDifficulties.Contains(difficulty);

        /// <summary>
        /// Whether a ship may slice a wreck's data core now: the killing party's first claim covers it, as it covers
        /// salvage (<see cref="ShipSalvageRules.CanSalvage"/>). Any of the given players (the pilot, the slicer) counts.
        /// </summary>
        public static bool CanSliceDataCore(IReadOnlyCollection<string> claimants, IEnumerable<string> playerIds, DateTime destroyedAt, DateTime now) =>
            playerIds.Any(id => ShipSalvageRules.CanSalvage(claimants, id, destroyedAt, now));

        /// <summary>The percent chance (0-100) a destroyed preset NPC ship of a difficulty drops a space lockbox.</summary>
        public static int LockboxDropPercent(WreckDifficulty difficulty) =>
            SpaceSlicingTuning.LockboxDropPercent.TryGetValue(difficulty, out var percent) ? percent : 0;

        /// <summary>Whether a roll (0-99) drops a space lockbox.</summary>
        public static bool RollsLockbox(WreckDifficulty difficulty, int roll) => roll < LockboxDropPercent(difficulty);

        /// <summary>The space lockbox a ship of a faction tier (1-6) drops: its tier, at most 5.</summary>
        public static string LockboxResref(int shipTier) =>
            SpaceSlicingItems.Lockboxes[Math.Clamp(shipTier, 1, SpaceSlicingTuning.MaximumTier) - 1];

        /// <summary>The slicing tier of a space lockbox resref, or 0 when it is not one.</summary>
        public static int LockboxTier(string resref)
        {
            for (var index = 0; index < SpaceSlicingItems.Lockboxes.Count; index++)
            {
                if (SpaceSlicingItems.Lockboxes[index] == resref)
                    return index + 1;
            }

            return 0;
        }

        /// <summary>Whether a new cache site's roll (0-99) makes it a locked cache.</summary>
        public static bool RollsLockedCache(int roll) => roll < SpaceSlicingTuning.LockedCachePercent;

        /// <summary>
        /// The end of a customs spoof. Success: a clean scan, with no fine, no confiscation and no standing lost.
        /// Failure plays out as being caught: the scan's fine (or what the pilot carries, when that is less), the
        /// banned goods confiscated and the caught-smuggling standing loss.
        /// </summary>
        public static CustomsSpoofOutcome SpoofOutcome(bool success, int fine, int gold)
        {
            if (success)
                return new CustomsSpoofOutcome { Clean = true };

            return new CustomsSpoofOutcome
            {
                Fine = Math.Max(0, Math.Min(fine, gold)),
                ConfiscatesGoods = true,
                Standing = TradeStandingEvent.CaughtSmuggling,
            };
        }
    }
}
