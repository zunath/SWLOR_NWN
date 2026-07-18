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

            return profile.LifetimeLevelsTrained switch
            {
                0 => (MasteryTrainingSource.Standard14, Standard14DurationDays),
                1 => (MasteryTrainingSource.Standard21, Standard21DurationDays),
                _ => (MasteryTrainingSource.Standard28, Standard28DurationDays)
            };
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
        /// The total number of tier-levels the character currently holds across every
        /// mastery they own (sum of each PlayerMasteryLevel.Tier).
        /// </summary>
        public static int GetEarnedLevelTotal(PlayerMasteryProfile profile)
        {
            return profile.Masteries.Values.Sum(level => level.Tier);
        }

        /// <summary>
        /// The projected total level count once every currently queued/active training
        /// entry completes (earned levels + one per queued entry, since each entry
        /// advances its mastery by exactly one tier).
        /// </summary>
        public static int GetProjectedLevelTotal(PlayerMasteryProfile profile)
        {
            return GetEarnedLevelTotal(profile) + profile.TrainingQueue.Count;
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

                if (alreadyHasOtherTier5)
                {
                    violations.Add(new MasteryRuleViolation(
                        MasteryRuleType.Tier5Conflict,
                        "This character already has a different mastery at tier 5. Only one mastery may ever reach tier 5.",
                        false));
                }
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

            if (GetProjectedLevelTotal(profile) + 1 > MaxTotalLevels)
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

            var record = level.TierHistory.LastOrDefault(r => r.Tier == tier);
            level.TierHistory.RemoveAll(r => r.Tier == tier);
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
        /// Appends a "Reduce" audit entry.
        /// </summary>
        public static bool ReduceActiveTrainingTime(PlayerMasteryProfile profile, int days, MasteryActor actor, string reason, DateTime utcNow)
        {
            if (profile.TrainingQueue.Count == 0)
                return false;

            profile.TrainingQueue[0].ReductionDays += days;
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
        /// Enqueues a new training entry for an approved request. Consumes a Quick Slot
        /// or retrain credit if the resolved source used one. The new entry's start date
        /// is <paramref name="utcNow"/> if the queue was empty, or the current last
        /// entry's finish date otherwise (strictly sequential). Appends an "Approve"
        /// audit entry, and a "QuickSlotSpend" entry if a Quick Slot was consumed.
        /// </summary>
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
            var (source, duration) = ResolveTraining(profile, targetTier, useQuickSlot, useRetrainCredit, isInstant);

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

            // Instant grants complete immediately rather than sitting in the queue.
            if (isInstant)
            {
                EvaluateTrainingQueue(profile, utcNow);
            }

            return entry;
        }

        /// <summary>
        /// Cancels a not-yet-completed training entry (active or queued) outright, with no
        /// tier ever granted. Distinct from <see cref="Abandon"/>, which un-earns an
        /// already-granted tier and awards a retrain credit - a cancelled entry never
        /// finished, so nothing was earned to un-earn and no credit is granted. Any Quick
        /// Slot spent on the entry is refunded. Every following entry's start date is
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
        /// seed rows from <see cref="MasteryCatalogSeed.Entries"/> whose Name is not
        /// already present - i.e. the entries that still need to be inserted. Never
        /// returns anything for a Name that already exists, so re-running the seed never
        /// duplicates or overwrites an existing (possibly staff-edited) row.
        /// </summary>
        public static List<Entity.Mastery> BuildMissingCatalogEntries(IEnumerable<Entity.Mastery> existingCatalog)
        {
            var existingNames = new HashSet<string>(
                (existingCatalog ?? Enumerable.Empty<Entity.Mastery>()).Select(m => m.Name),
                StringComparer.OrdinalIgnoreCase);

            return MasteryCatalogSeed.Entries
                .Where(seed => !existingNames.Contains(seed.Name))
                .Select(seed => new Entity.Mastery
                {
                    Name = seed.Name,
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
