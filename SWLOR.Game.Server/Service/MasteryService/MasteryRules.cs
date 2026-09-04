using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.MasteryService
{
    /// <summary>
    /// Pure business-rule logic for the Masteries system. Every method here takes plain
    /// profiles/DTOs (never a live NWN <c>uint</c> object) and every timestamp is passed
    /// in explicitly (never <see cref="DateTime.UtcNow"/> read internally), so this class
    /// is fully unit-testable without a running server. <see cref="Mastery"/> is the thin
    /// DB-touching orchestration layer that wraps these calls.
    /// </summary>
    public static class MasteryRules
    {
        /// <summary>
        /// The maximum number of tier-levels (summed across every mastery) a character
        /// may ever hold at once. A tier-4 mastery counts as 4 levels.
        /// </summary>
        public const int MaxTotalLevels = 17;

        /// <summary>
        /// The maximum number of training entries (active + queued) a character may
        /// have at once.
        /// </summary>
        public const int MaxQueueSize = 3;

        /// <summary>
        /// The minimum character age, in days, before masteries may be requested.
        /// </summary>
        public const int MinimumCharacterAgeDays = 14;

        /// <summary>
        /// The skill rank required before a mastery's associated skill unlocks it.
        /// </summary>
        public const int RequiredSkillRank = 50;

        private const int Tier5DurationDays = 152;
        private const int Tier5QuickSlotDurationDays = 131;
        private const int QuickSlotDurationDays = 7;
        private const int RetrainCredit7DurationDays = 7;
        private const int RetrainCredit14DurationDays = 14;
        private const int Standard14DurationDays = 14;
        private const int Standard21DurationDays = 21;
        private const int Standard28DurationDays = 28;

        /// <summary>
        /// Resolves both the duration and the source tag for a training entry about to
        /// be created, given which modifiers apply. Precedence: instant grant, then tier
        /// 5 (which cannot be discounted by a retrain credit - only by a Quick Slot),
        /// then a retrain credit, then a Quick Slot, then the character's standard
        /// 14/21/28-day bracket based on how many levels they've ever trained.
        /// </summary>
        private static (MasteryTrainingSource Source, int DurationDays) ResolveTraining(
            PlayerMasteryProfile profile,
            int targetTier,
            bool useQuickSlot,
            bool useRetrainCredit,
            bool isInstant)
        {
            if (isInstant)
                return (MasteryTrainingSource.Instant, 0);

            if (targetTier >= 5)
            {
                return useQuickSlot
                    ? (MasteryTrainingSource.QuickSlot, Tier5QuickSlotDurationDays)
                    : (MasteryTrainingSource.Tier5, Tier5DurationDays);
            }

            if (useRetrainCredit)
            {
                if (profile.RetrainCredits7 > 0)
                    return (MasteryTrainingSource.Retrain7, RetrainCredit7DurationDays);

                if (profile.RetrainCredits14 > 0)
                    return (MasteryTrainingSource.Retrain14, RetrainCredit14DurationDays);

                // No credit actually available - fall through to normal resolution below.
            }

            if (useQuickSlot)
                return (MasteryTrainingSource.QuickSlot, QuickSlotDurationDays);

            // LifetimeLevelsTrained only increments on completion (EvaluateTrainingQueue)
            // or an instant grant - a level that is merely queued/active never bumps it.
            // Counting only LifetimeLevelsTrained would let several approvals made back
            // to back, before any of them complete, all land in the same bracket (e.g.
            // three approvals for a brand-new character would all resolve to 14 days
            // instead of 14/21/28). Folding in the current queue length accounts for
            // those in-flight levels too. A queued entry that is later cancelled never
            // incremented LifetimeLevelsTrained, so simply leaving the queue frees its
            // bracket slot again with no extra bookkeeping.
            var effectivePriorLevels = profile.LifetimeLevelsTrained + profile.TrainingQueue.Count;
            return effectivePriorLevels switch
            {
                0 => (MasteryTrainingSource.Standard14, Standard14DurationDays),
                1 => (MasteryTrainingSource.Standard21, Standard21DurationDays),
                _ => (MasteryTrainingSource.Standard28, Standard28DurationDays)
            };
        }

        /// <summary>
        /// Whether a Quick Slot request has an actual Quick Slot available to spend.
        /// Always true for an instant grant, since an instant grant never spends a Quick
        /// Slot regardless of a stale <paramref name="useQuickSlot"/> flag (see
        /// <see cref="ResolveTraining"/>). Shared by <see cref="EnqueueTraining"/>'s own
        /// rejection and <see cref="Mastery.ApproveRequest"/>'s pre-check - the latter
        /// must know this BEFORE it materializes a Custom request's catalog row, so a
        /// doomed-to-fail Quick Slot approval never leaves an orphaned catalog entry
        /// behind.
        /// </summary>
        public static bool CanUseQuickSlot(PlayerMasteryProfile profile, bool useQuickSlot, bool isInstant)
        {
            return isInstant || !useQuickSlot || profile.QuickSlotsAvailable > 0;
        }

        /// <summary>
        /// Returns the number of days a training entry for <paramref name="targetTier"/>
        /// will take, given the character's history and any modifiers used.
        /// </summary>
        public static int GetTrainingDuration(
            PlayerMasteryProfile profile,
            int targetTier,
            bool useQuickSlot,
            bool useRetrainCredit,
            bool isInstant = false)
        {
            return ResolveTraining(profile, targetTier, useQuickSlot, useRetrainCredit, isInstant).DurationDays;
        }

        /// <summary>
        /// Returns the <see cref="MasteryTrainingSource"/> tag that will be recorded for a
        /// training entry for <paramref name="targetTier"/>, given the same inputs as
        /// <see cref="GetTrainingDuration"/>. Kept in lockstep with duration resolution
        /// via <see cref="ResolveTraining"/> so the two can never disagree.
        /// </summary>
        public static MasteryTrainingSource DetermineTrainingSource(
            PlayerMasteryProfile profile,
            int targetTier,
            bool useQuickSlot,
            bool useRetrainCredit,
            bool isInstant = false)
        {
            return ResolveTraining(profile, targetTier, useQuickSlot, useRetrainCredit, isInstant).Source;
        }

        /// <summary>
        /// Whether a pending approval decision should automatically spend a retrain
        /// credit: true only when the entry isn't itself Quick-Slotted or an instant
        /// grant, isn't targeting tier 5 (retrain credits never discount tier 5 - see
        /// <see cref="ResolveTraining"/>), and the character actually holds a credit to
        /// spend. Shared by <see cref="Mastery"/>'s ApproveRequest and the review
        /// window's live duration preview so the two can never disagree about whether a
        /// credit will be consumed.
        /// </summary>
        public static bool ShouldUseRetrainCredit(PlayerMasteryProfile profile, int targetTier, bool useQuickSlot, bool isInstant)
        {
            return !useQuickSlot && !isInstant && targetTier != 5 &&
                   (profile.RetrainCredits7 > 0 || profile.RetrainCredits14 > 0);
        }

        /// <summary>
        /// Whether a request in this status may still be approved, denied, or otherwise
        /// reviewed. Only Pending and InReview are reviewable: Approved/Denied are already
        /// decided, and Cancelled means the player withdrew it - a stale staff window (one
        /// reviewer's window racing another's decision, or a player who cancelled after
        /// staff opened the request) must never be able to resurrect any of those
        /// outcomes. Shared by <see cref="Mastery.ApproveRequest"/> and
        /// <see cref="Mastery.DenyRequest"/> so both re-check freshly fetched state against
        /// the exact same whitelist.
        /// </summary>
        public static bool CanReviewRequest(MasteryRequestStatus status)
        {
            return status == MasteryRequestStatus.Pending || status == MasteryRequestStatus.InReview;
        }

        /// <summary>
        /// Finds an already-in-flight (Pending or InReview) request among
        /// <paramref name="existingRequests"/> that would duplicate a new submission for
        /// the same mastery (or, for a Custom request, the same custom name) and target
        /// tier. Used by <see cref="Mastery.SubmitRequest"/> as defense-in-depth against a
        /// double submission racing past the Masteries window's own in-flight guard (see
        /// MasteriesViewModel.OnClickSubmitRequest) - the service must reject the
        /// duplicate regardless of what the client's UI state looked like.
        /// </summary>
        public static MasteryRequest FindDuplicatePendingRequest(
            IEnumerable<MasteryRequest> existingRequests,
            MasteryRequestType type,
            string masteryId,
            string customName,
            int targetTier)
        {
            if (existingRequests == null)
                return null;

            return existingRequests.FirstOrDefault(r =>
                CanReviewRequest(r.Status) &&
                r.Type == type &&
                r.TargetTier == targetTier &&
                (type == MasteryRequestType.Custom
                    ? string.Equals(r.CustomName, customName ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                    : r.MasteryId == (masteryId ?? string.Empty)));
        }

        /// <summary>
        /// The total number of tier-levels the character currently holds across every
        /// mastery they own (sum of each PlayerMasteryLevel.Tier).
        /// </summary>
        public static int GetEarnedLevelTotal(PlayerMasteryProfile profile)
        {
            return profile.Masteries.Values.Sum(level => level.Tier);
        }

        /// <summary>
        /// The projected total level count once every currently queued/active training
        /// entry completes - the highest resulting tier per mastery (earned tier vs. the
        /// max queued TargetTier for that mastery), summed across every mastery. Counting
        /// entries instead of tiers would undercount whenever a tier-progression override
        /// lets an entry jump more than one tier past the character's current tier.
        /// </summary>
        public static int GetProjectedLevelTotal(PlayerMasteryProfile profile)
        {
            return BuildProjectedTierByMastery(profile).Values.Sum();
        }

        /// <summary>
        /// Same as <see cref="GetProjectedLevelTotal(PlayerMasteryProfile)"/>, but also
        /// folds in a hypothetical additional request for <paramref name="masteryId"/> at
        /// <paramref name="targetTier"/> that has not yet been queued - used by
        /// <see cref="ValidateRequest"/> to evaluate whether approving a prospective
        /// request would push the character over the level cap.
        /// </summary>
        public static int GetProjectedLevelTotal(PlayerMasteryProfile profile, string masteryId, int targetTier)
        {
            var projectedTierByMastery = BuildProjectedTierByMastery(profile);

            projectedTierByMastery.TryGetValue(masteryId, out var currentProjected);
            if (targetTier > currentProjected)
                projectedTierByMastery[masteryId] = targetTier;

            return projectedTierByMastery.Values.Sum();
        }

        private static Dictionary<string, int> BuildProjectedTierByMastery(PlayerMasteryProfile profile)
        {
            var projectedTierByMastery = new Dictionary<string, int>();

            foreach (var (masteryId, level) in profile.Masteries)
                projectedTierByMastery[masteryId] = level.Tier;

            foreach (var entry in profile.TrainingQueue)
            {
                projectedTierByMastery.TryGetValue(entry.MasteryId, out var currentProjected);
                if (entry.TargetTier > currentProjected)
                    projectedTierByMastery[entry.MasteryId] = entry.TargetTier;
            }

            return projectedTierByMastery;
        }

        /// <summary>
        /// Validates a prospective mastery request against every business rule. Every
        /// violation except <see cref="MasteryRuleType.OffLimit"/> is a warning: staff
        /// may approve the request anyway provided they supply an override reason.
        /// </summary>
        /// <param name="profile">The requesting character's mastery profile.</param>
        /// <param name="ownedMasteryCatalog">
        /// The catalog entries (Rarity/etc.) for every mastery the character currently
        /// owns a tier in, keyed by Mastery.Id. Used to detect a conflicting Rare
        /// mastery. May be null or empty if the character owns nothing yet.
        /// </param>
        /// <param name="mastery">The catalog entry being requested.</param>
        /// <param name="targetTier">The tier being requested.</param>
        /// <param name="characterCreatedDate">The character's creation date (Player.DateCreated).</param>
        /// <param name="utcNow">The current UTC time.</param>
        /// <param name="skillRank">
        /// The character's rank in the mastery's associated skill, or null if the
        /// mastery has no associated skill requirement.
        /// </param>
        public static List<MasteryRuleViolation> ValidateRequest(
            PlayerMasteryProfile profile,
            IReadOnlyDictionary<string, Entity.Mastery> ownedMasteryCatalog,
            Entity.Mastery mastery,
            int targetTier,
            DateTime characterCreatedDate,
            DateTime utcNow,
            int? skillRank)
        {
            var violations = new List<MasteryRuleViolation>();

            if (mastery.Rarity == MasteryRarityType.OffLimit)
            {
                violations.Add(new MasteryRuleViolation(
                    MasteryRuleType.OffLimit,
                    $"'{mastery.Name}' is off-limits and cannot be requested.",
                    true));
            }

            if (mastery.Rarity == MasteryRarityType.Rare && ownedMasteryCatalog != null)
            {
                var alreadyHasOtherRare = ownedMasteryCatalog
                    .Where(kvp => kvp.Key != mastery.Id)
                    .Any(kvp => kvp.Value.Rarity == MasteryRarityType.Rare);

                if (alreadyHasOtherRare)
                {
                    violations.Add(new MasteryRuleViolation(
                        MasteryRuleType.RareConflict,
                        "This character already holds a different Rare mastery. Only one Rare mastery is allowed per character.",
                        false));
                }
            }

            if (targetTier >= 5)
            {
                var alreadyHasOtherTier5 = profile.Masteries
                    .Where(kvp => kvp.Key != mastery.Id)
                    .Any(kvp => kvp.Value.Tier >= 5);

                var alreadyTrainingOtherTier5 = profile.TrainingQueue
                    .Any(e => e.MasteryId != mastery.Id && e.TargetTier >= 5);

                if (alreadyHasOtherTier5 || alreadyTrainingOtherTier5)
                {
                    violations.Add(new MasteryRuleViolation(
                        MasteryRuleType.Tier5Conflict,
                        "This character already has a different mastery at tier 5. Only one mastery may ever reach tier 5.",
                        false));
                }
            }

            // Blocking: a tier outside 1-5 must never reach progression/timing logic at
            // all (e.g. a tier-6 request would otherwise pass progression from tier 5 and
            // receive tier-5 timing, then persist as an invalid tier 6 entry).
            if (targetTier < 1 || targetTier > 5)
            {
                violations.Add(new MasteryRuleViolation(
                    MasteryRuleType.InvalidTier,
                    $"Requested tier {targetTier} is out of the valid range (must be 1-5).",
                    true));
            }

            var currentTier = profile.Masteries.TryGetValue(mastery.Id, out var level) ? level.Tier : 0;
            if (targetTier != currentTier + 1)
            {
                violations.Add(new MasteryRuleViolation(
                    MasteryRuleType.TierProgression,
                    $"Requested tier {targetTier} is not the next tier for this mastery (current tier is {currentTier}).",
                    false));
            }

            var ageDays = (utcNow - characterCreatedDate).TotalDays;
            if (ageDays < MinimumCharacterAgeDays)
            {
                violations.Add(new MasteryRuleViolation(
                    MasteryRuleType.CharacterAge,
                    $"Character must be at least {MinimumCharacterAgeDays} days old to request masteries.",
                    false));
            }

            if (mastery.AssociatedSkill != null && (skillRank ?? 0) < RequiredSkillRank)
            {
                violations.Add(new MasteryRuleViolation(
                    MasteryRuleType.SkillRank,
                    $"Character must be rank {RequiredSkillRank} in {mastery.AssociatedSkill} to request this mastery.",
                    false));
            }

            if (profile.TrainingQueue.Count >= MaxQueueSize)
            {
                violations.Add(new MasteryRuleViolation(
                    MasteryRuleType.QueueFull,
                    $"This character's training queue is already at the maximum of {MaxQueueSize} entries.",
                    false));
            }

            // Project this specific request's resulting tier (not a blind +1) so an
            // overridden multi-tier jump for this mastery is weighed correctly against the
            // cap - see GetProjectedLevelTotal's 3-arg overload.
            if (GetProjectedLevelTotal(profile, mastery.Id, targetTier) > MaxTotalLevels)
            {
                violations.Add(new MasteryRuleViolation(
                    MasteryRuleType.LevelCap,
                    $"Granting this request would exceed the {MaxTotalLevels}-level total cap.",
                    false));
            }

            return violations;
        }

        /// <summary>
        /// Grants a tier to a mastery, creating the PlayerMasteryLevel entry if this is
        /// the character's first tier in it, and appending a TierHistory record.
        /// </summary>
        private static void GrantTier(PlayerMasteryProfile profile, string masteryId, int tier, DateTime dateEarned, MasteryTrainingSource source)
        {
            if (!profile.Masteries.TryGetValue(masteryId, out var level))
            {
                level = new PlayerMasteryLevel
                {
                    DateFirstEarned = dateEarned
                };
                profile.Masteries[masteryId] = level;
            }

            level.Tier = tier;
            level.TierHistory.Add(new MasteryTierRecord
            {
                Tier = tier,
                DateEarned = dateEarned,
                Source = source
            });
        }

        /// <summary>
        /// Evaluates a character's training queue against the current time, completing
        /// (and removing) every entry whose finish date has passed, granting the
        /// resulting tier, and starting the next entry at the completed entry's finish
        /// date (never at <paramref name="utcNow"/>, so no time is ever lost). Can
        /// complete multiple entries in a single pass. Mutates <paramref name="profile"/>.
        /// </summary>
        /// <returns>The training entries that completed during this evaluation, in the order they completed.</returns>
        public static List<MasteryTrainingEntry> EvaluateTrainingQueue(PlayerMasteryProfile profile, DateTime utcNow)
        {
            var completed = new List<MasteryTrainingEntry>();

            while (profile.TrainingQueue.Count > 0)
            {
                var active = profile.TrainingQueue[0];
                var finish = active.StartDate.AddDays(active.DurationDays - active.ReductionDays);

                if (utcNow < finish)
                    break;

                GrantTier(profile, active.MasteryId, active.TargetTier, finish, active.Source);
                profile.LifetimeLevelsTrained++;
                profile.TrainingQueue.RemoveAt(0);
                completed.Add(active);

                // The next entry's clock starts when the one before it actually finished,
                // never "now" - so no wall-clock time is ever lost between entries.
                if (profile.TrainingQueue.Count > 0)
                {
                    profile.TrainingQueue[0].StartDate = finish;
                }
            }

            return completed;
        }

        /// <summary>
        /// Abandons (or entirely removes, if tier 1) a mastery's current tier. Frees the
        /// level, refunds any Quick Slot spent on it, and grants the character a retrain
        /// credit for their next training entry - 7 days if the abandoned tier was
        /// trained via Quick Slot, was an instant grant, or was the character's 1st or
        /// 2nd ever mastery level; 14 days otherwise. Appends an audit entry.
        /// </summary>
        /// <param name="profile">The character's mastery profile.</param>
        /// <param name="masteryId">The mastery being abandoned.</param>
        /// <param name="tier">The tier to abandon. Must equal the mastery's current tier.</param>
        /// <param name="actor">Who performed this action (the player themselves, or staff for a Revoke).</param>
        /// <param name="reason">Why this action was taken. Required.</param>
        /// <param name="utcNow">The current UTC time.</param>
        /// <param name="actionLabel">The audit log action label - "Abandon" for player self-service, "Revoke" for a staff-initiated removal.</param>
        /// <returns>True if the tier was found and abandoned; false if the character did not hold that tier.</returns>
        public static bool Abandon(
            PlayerMasteryProfile profile,
            string masteryId,
            int tier,
            MasteryActor actor,
            string reason,
            DateTime utcNow,
            string actionLabel = "Abandon")
        {
            if (!profile.Masteries.TryGetValue(masteryId, out var level) || level.Tier != tier)
                return false;

            // Only the most recent record for this tier is un-earned - a character can
            // legitimately have re-trained the same tier more than once (e.g. abandon then
            // retrain), and RemoveAll would wipe every one of those duplicate-tier records
            // instead of just the one actually being abandoned now.
            var record = level.TierHistory.LastOrDefault(r => r.Tier == tier);
            if (record != null)
                level.TierHistory.Remove(record);
            level.Tier = tier - 1;

            if (level.Tier <= 0)
                profile.Masteries.Remove(masteryId);

            var grantsSevenDayCredit = record != null && (
                record.Source == MasteryTrainingSource.QuickSlot ||
                record.Source == MasteryTrainingSource.Standard14 ||
                record.Source == MasteryTrainingSource.Standard21 ||
                record.Source == MasteryTrainingSource.Instant);

            if (record?.Source == MasteryTrainingSource.QuickSlot)
                profile.QuickSlotsAvailable++;

            if (grantsSevenDayCredit)
                profile.RetrainCredits7++;
            else
                profile.RetrainCredits14++;

            AppendAudit(profile, utcNow, actor, actionLabel, reason);

            return true;
        }

        /// <summary>
        /// Directly sets a character's tier in a mastery to any value, bypassing the
        /// tier+1 progression rule. Staff-only. Appends a "Grant" audit entry.
        /// </summary>
        public static void GrantMastery(PlayerMasteryProfile profile, string masteryId, int tier, MasteryActor actor, string reason, DateTime utcNow)
        {
            GrantTier(profile, masteryId, tier, utcNow, MasteryTrainingSource.Instant);
            profile.LifetimeLevelsTrained++;
            AppendAudit(profile, utcNow, actor, "Grant", reason);
        }

        /// <summary>
        /// Applies a staff time reduction to the active (index 0) training entry.
        /// Rejects a non-positive <paramref name="days"/> (which would otherwise extend
        /// training instead of shortening it) and clamps the reduction to the entry's
        /// remaining duration so its finish date can never land before its StartDate -
        /// otherwise every later queued entry would cascade-rebase into the past the next
        /// time the queue is evaluated. Appends a "Reduce" audit entry only when a
        /// non-zero reduction is actually applied.
        /// </summary>
        public static bool ReduceActiveTrainingTime(PlayerMasteryProfile profile, int days, MasteryActor actor, string reason, DateTime utcNow)
        {
            if (days <= 0)
                return false;

            if (profile.TrainingQueue.Count == 0)
                return false;

            var active = profile.TrainingQueue[0];
            var remainingDays = active.DurationDays - active.ReductionDays;
            var appliedDays = Math.Min(days, Math.Max(0, remainingDays));

            if (appliedDays <= 0)
                return false;

            active.ReductionDays += appliedDays;
            AppendAudit(profile, utcNow, actor, "Reduce", reason);
            return true;
        }

        /// <summary>
        /// Awards the character a Quick Slot. Appends a "QuickSlotAward" audit entry.
        /// </summary>
        public static void AwardQuickSlot(PlayerMasteryProfile profile, MasteryActor actor, string reason, DateTime utcNow)
        {
            profile.QuickSlotsAvailable++;
            AppendAudit(profile, utcNow, actor, "QuickSlotAward", reason);
        }

        /// <summary>
        /// Enqueues a new training entry for an approved request - or, for an instant
        /// grant, completes it immediately without touching the queue at all. Consumes a
        /// Quick Slot or retrain credit if the resolved source used one. A non-instant
        /// entry's start date is <paramref name="utcNow"/> if the queue was empty, or the
        /// current last entry's finish date otherwise (strictly sequential). Appends an
        /// "Approve" audit entry, and a "QuickSlotSpend" entry if a Quick Slot was
        /// consumed.
        /// </summary>
        /// <returns>
        /// The created (or immediately-completed) training entry, or null if
        /// <paramref name="useQuickSlot"/> was requested with no Quick Slot actually
        /// available - rejected outright before any queue mutation, rather than silently
        /// granting the discounted duration for free. This check is skipped entirely when
        /// <paramref name="isInstant"/> is true, since an instant grant never spends a
        /// Quick Slot (see <see cref="ResolveTraining"/>) regardless of a stale
        /// <paramref name="useQuickSlot"/> flag. A stale or direct caller is the only way
        /// to hit the rejection; the review window already disables the Quick Slot option
        /// whenever <see cref="PlayerMasteryProfile.QuickSlotsAvailable"/> is 0.
        /// </returns>
        public static MasteryTrainingEntry EnqueueTraining(
            PlayerMasteryProfile profile,
            string masteryId,
            int targetTier,
            bool useQuickSlot,
            bool useRetrainCredit,
            bool isInstant,
            MasteryActor actor,
            string reason,
            string requestId,
            DateTime utcNow)
        {
            if (!CanUseQuickSlot(profile, useQuickSlot, isInstant))
                return null;

            var (source, duration) = ResolveTraining(profile, targetTier, useQuickSlot, useRetrainCredit, isInstant);

            // Instant grants bypass the queue entirely rather than being appended to the
            // back of it - appending would leave the grant stuck behind whatever is
            // already active/queued instead of completing "now" as an instant grant must.
            // The existing queue is left completely untouched.
            if (isInstant)
            {
                var instantEntry = new MasteryTrainingEntry
                {
                    MasteryId = masteryId,
                    TargetTier = targetTier,
                    StartDate = utcNow,
                    DurationDays = duration,
                    ReductionDays = 0,
                    Source = source,
                    RequestId = requestId ?? string.Empty
                };

                GrantTier(profile, masteryId, targetTier, utcNow, source);
                profile.LifetimeLevelsTrained++;
                AppendAudit(profile, utcNow, actor, "Approve", reason);

                return instantEntry;
            }

            var startDate = utcNow;
            if (profile.TrainingQueue.Count > 0)
            {
                var last = profile.TrainingQueue[^1];
                startDate = last.StartDate.AddDays(last.DurationDays - last.ReductionDays);
            }

            var entry = new MasteryTrainingEntry
            {
                MasteryId = masteryId,
                TargetTier = targetTier,
                StartDate = startDate,
                DurationDays = duration,
                ReductionDays = 0,
                Source = source,
                RequestId = requestId ?? string.Empty
            };

            profile.TrainingQueue.Add(entry);

            if (source == MasteryTrainingSource.QuickSlot && profile.QuickSlotsAvailable > 0)
            {
                profile.QuickSlotsAvailable--;
                AppendAudit(profile, utcNow, actor, "QuickSlotSpend", reason);
            }
            else if (source == MasteryTrainingSource.Retrain7 && profile.RetrainCredits7 > 0)
            {
                profile.RetrainCredits7--;
            }
            else if (source == MasteryTrainingSource.Retrain14 && profile.RetrainCredits14 > 0)
            {
                profile.RetrainCredits14--;
            }

            AppendAudit(profile, utcNow, actor, "Approve", reason);

            return entry;
        }

        /// <summary>
        /// Cancels a not-yet-completed training entry (active or queued) outright, with no
        /// tier ever granted. Distinct from <see cref="Abandon"/>, which un-earns an
        /// already-granted tier and awards a retrain credit - a cancelled entry never
        /// finished, so nothing was earned to un-earn and no credit is granted. Any Quick
        /// Slot or retrain credit spent on the entry is refunded. Every following entry's start date is
        /// recomputed so the queue stays strictly sequential with no time gained or lost;
        /// if the active (index 0) entry itself is cancelled, the next entry starts now.
        /// Appends an "AbandonTraining" audit entry.
        /// </summary>
        /// <param name="profile">The character's mastery profile.</param>
        /// <param name="index">The index within TrainingQueue to cancel.</param>
        /// <param name="actor">The staff member performing this action.</param>
        /// <param name="reason">Why this action was taken. Required.</param>
        /// <param name="utcNow">The current UTC time.</param>
        /// <returns>True if the entry existed and was cancelled; false otherwise.</returns>
        public static bool AbandonTrainingEntry(PlayerMasteryProfile profile, int index, MasteryActor actor, string reason, DateTime utcNow)
        {
            if (index < 0 || index >= profile.TrainingQueue.Count)
                return false;

            var entry = profile.TrainingQueue[index];
            profile.TrainingQueue.RemoveAt(index);

            if (entry.Source == MasteryTrainingSource.QuickSlot)
                profile.QuickSlotsAvailable++;
            else if (entry.Source == MasteryTrainingSource.Retrain7)
                profile.RetrainCredits7++;
            else if (entry.Source == MasteryTrainingSource.Retrain14)
                profile.RetrainCredits14++;

            if (profile.TrainingQueue.Count > 0)
            {
                // The entry that is now (or remains) active starts now if it was the one
                // just cancelled, rather than inheriting the cancelled entry's stale
                // timeline. Every entry after it cascades from there so the queue stays
                // strictly sequential.
                if (index == 0)
                    profile.TrainingQueue[0].StartDate = utcNow;

                for (var i = 1; i < profile.TrainingQueue.Count; i++)
                {
                    var previous = profile.TrainingQueue[i - 1];
                    profile.TrainingQueue[i].StartDate = previous.StartDate.AddDays(previous.DurationDays - previous.ReductionDays);
                }
            }

            AppendAudit(profile, utcNow, actor, "AbandonTraining", reason);

            return true;
        }

        /// <summary>
        /// Swaps a queued (non-active) training entry with its immediate neighbor in the
        /// given direction. The active entry (index 0) can never be moved, and an entry
        /// can never be moved into index 0 (that would silently make it active without
        /// going through <see cref="EvaluateTrainingQueue"/>). Every entry's start date is
        /// recomputed afterward so the queue stays strictly sequential. Appends a
        /// "Reorder" audit entry.
        /// </summary>
        /// <param name="profile">The character's mastery profile.</param>
        /// <param name="index">The index within TrainingQueue to move. Must be &gt;= 1.</param>
        /// <param name="direction">-1 to move the entry up (earlier), +1 to move it down (later).</param>
        /// <param name="actor">The staff member performing this action.</param>
        /// <param name="utcNow">The current UTC time.</param>
        /// <returns>True if the move was legal and applied; false otherwise.</returns>
        public static bool ReorderQueueEntry(PlayerMasteryProfile profile, int index, int direction, MasteryActor actor, DateTime utcNow)
        {
            // Only an adjacent-slot swap is a legal move. Without this, direction 0 would
            // pass both index checks below unchanged (newIndex == index) and swap the
            // entry with itself - returning true and appending a false "Reorder" audit
            // entry despite nothing actually moving.
            if (direction != -1 && direction != 1)
                return false;

            if (index <= 0 || index >= profile.TrainingQueue.Count)
                return false;

            var newIndex = index + direction;
            if (newIndex <= 0 || newIndex >= profile.TrainingQueue.Count)
                return false;

            var movingMasteryId = profile.TrainingQueue[index].MasteryId;

            (profile.TrainingQueue[index], profile.TrainingQueue[newIndex]) = (profile.TrainingQueue[newIndex], profile.TrainingQueue[index]);

            for (var i = 1; i < profile.TrainingQueue.Count; i++)
            {
                var previous = profile.TrainingQueue[i - 1];
                profile.TrainingQueue[i].StartDate = previous.StartDate.AddDays(previous.DurationDays - previous.ReductionDays);
            }

            AppendAudit(profile, utcNow, actor, "Reorder", $"Reordered queued training entry for mastery {movingMasteryId}.");

            return true;
        }

        private static void AppendAudit(PlayerMasteryProfile profile, DateTime utcNow, MasteryActor actor, string action, string reason)
        {
            profile.AuditLog.Add(new MasteryAuditEntry
            {
                Date = utcNow,
                ActorName = actor?.Name ?? string.Empty,
                ActorCDKey = actor?.CDKey ?? string.Empty,
                Action = action,
                Reason = reason ?? string.Empty
            });
        }

        /// <summary>
        /// Given the full set of currently seeded/staff-created masteries, returns the
        /// seed rows from <see cref="MasteryCatalogSeed.Entries"/> that still need to be
        /// inserted. Matches primarily by the immutable <see cref="Entity.Mastery.SeedKey"/>
        /// (set once at creation to the seed entry's Name) rather than the mutable Name
        /// field, so a seeded row staff later renamed is still recognized as already
        /// present and is never recreated as a duplicate. Rows with no SeedKey (created
        /// before that field existed) fall back to matching by Name. Never returns
        /// anything for an entry that already exists by either match, so re-running the
        /// seed never duplicates or overwrites an existing (possibly staff-edited) row.
        /// </summary>
        public static List<Entity.Mastery> BuildMissingCatalogEntries(IEnumerable<Entity.Mastery> existingCatalog)
        {
            var catalog = (existingCatalog ?? Enumerable.Empty<Entity.Mastery>()).ToList();

            var existingSeedKeys = new HashSet<string>(
                catalog.Where(m => !string.IsNullOrEmpty(m.SeedKey)).Select(m => m.SeedKey),
                StringComparer.OrdinalIgnoreCase);

            var existingNamesWithoutSeedKey = new HashSet<string>(
                catalog.Where(m => string.IsNullOrEmpty(m.SeedKey)).Select(m => m.Name),
                StringComparer.OrdinalIgnoreCase);

            return MasteryCatalogSeed.Entries
                .Where(seed => !existingSeedKeys.Contains(seed.Name) && !existingNamesWithoutSeedKey.Contains(seed.Name))
                .Select(seed => new Entity.Mastery
                {
                    Name = seed.Name,
                    SeedKey = seed.Name,
                    Category = seed.Category,
                    Description = seed.Description,
                    Rarity = seed.Rarity,
                    AssociatedSkill = seed.AssociatedSkill,
                    IsActive = true,
                    IsSeeded = true
                })
                .ToList();
        }
    }
}
