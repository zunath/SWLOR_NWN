using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MasteryService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Thin DB-touching orchestration for the Masteries system. Every rule/math decision
    /// lives in <see cref="MasteryRules"/> (pure, unit-tested without a server); this
    /// class is only responsible for loading/saving entities around those calls.
    /// </summary>
    public static class Mastery
    {
        /// <summary>
        /// Idempotently inserts any catalog entry from <see cref="MasteryCatalogSeed"/>
        /// that isn't already present (matched by Name). Never overwrites an existing
        /// row, so staff edits to a seeded entry are preserved across restarts.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void SeedCatalog()
        {
            var existing = GetAllMasteries();
            var missing = MasteryRules.BuildMissingCatalogEntries(existing);

            foreach (var mastery in missing)
            {
                DB.Set(mastery);
            }

            if (missing.Count > 0)
            {
                Log.Write(LogGroup.Mastery, $"Seeded {missing.Count} new mastery catalog entries.");
            }
        }

        /// <summary>
        /// Retrieves every mastery catalog entry, active or retired.
        /// </summary>
        public static List<Entity.Mastery> GetAllMasteries()
        {
            var query = new DBQuery<Entity.Mastery>();
            var count = (int)DB.SearchCount(query);

            return count <= 0
                ? new List<Entity.Mastery>()
                : DB.Search(query.AddPaging(count, 0)).ToList();
        }

        /// <summary>
        /// Retrieves a single mastery catalog entry, or null if it does not exist.
        /// </summary>
        public static Entity.Mastery GetMastery(string masteryId)
        {
            if (string.IsNullOrWhiteSpace(masteryId))
                return null;

            return DB.Get<Entity.Mastery>(masteryId);
        }

        /// <summary>
        /// Retrieves (or lazily creates) a character's mastery profile.
        /// </summary>
        public static PlayerMasteryProfile GetOrCreateProfile(string playerId)
        {
            var profile = DB.Get<PlayerMasteryProfile>(playerId);

            if (profile == null)
            {
                profile = new PlayerMasteryProfile(playerId);
                DB.Set(profile);
            }

            return profile;
        }

        /// <summary>
        /// Resolves the catalog entries (keyed by Mastery.Id) for every mastery the
        /// character currently owns a tier in. Used to feed
        /// <see cref="MasteryRules.ValidateRequest"/>'s Rare-conflict check.
        /// </summary>
        public static IReadOnlyDictionary<string, Entity.Mastery> GetOwnedMasteryCatalog(PlayerMasteryProfile profile)
        {
            var result = new Dictionary<string, Entity.Mastery>();

            foreach (var masteryId in profile.Masteries.Keys)
            {
                var mastery = GetMastery(masteryId);
                if (mastery != null)
                    result[masteryId] = mastery;
            }

            return result;
        }

        /// <summary>
        /// Validates a prospective request for a character, pulling the character's
        /// profile and owned-catalog lookup automatically.
        /// </summary>
        public static List<MasteryRuleViolation> ValidateRequest(
            string playerId,
            Entity.Mastery mastery,
            int targetTier,
            DateTime characterCreatedDate,
            DateTime utcNow,
            int? skillRank)
        {
            var profile = GetOrCreateProfile(playerId);
            var ownedCatalog = GetOwnedMasteryCatalog(profile);

            return MasteryRules.ValidateRequest(profile, ownedCatalog, mastery, targetTier, characterCreatedDate, utcNow, skillRank);
        }

        /// <summary>
        /// Submits a new mastery request in Pending status. Does not enqueue training or
        /// validate rules - that happens at approval time so staff can review warnings.
        /// </summary>
        public static MasteryRequest SubmitRequest(
            string playerId,
            string characterName,
            MasteryRequestType type,
            string masteryId,
            string customName,
            string customDescription,
            int targetTier,
            string justification)
        {
            var request = new MasteryRequest
            {
                PlayerId = playerId,
                CharacterName = characterName,
                Type = type,
                MasteryId = masteryId ?? string.Empty,
                CustomName = customName ?? string.Empty,
                CustomDescription = customDescription ?? string.Empty,
                TargetTier = targetTier,
                Justification = justification ?? string.Empty,
                Status = MasteryRequestStatus.Pending
            };

            DB.Set(request);

            Log.Write(LogGroup.Mastery, $"{characterName} [{playerId}] submitted a mastery request ({type}, tier {targetTier}, id {request.Id}).");

            return request;
        }

        /// <summary>
        /// Approves a request: enqueues a training entry (or completes it immediately if
        /// isInstant) and marks the request Approved. Returns false if the request could
        /// not be found or is not in a reviewable state.
        /// </summary>
        public static bool ApproveRequest(
            string requestId,
            string actorName,
            string actorCDKey,
            string reviewFeedback,
            string overrideReason,
            bool useQuickSlot,
            bool isInstant,
            DateTime utcNow)
        {
            var request = DB.Get<MasteryRequest>(requestId);
            if (request == null)
                return false;

            if (request.Status == MasteryRequestStatus.Approved || request.Status == MasteryRequestStatus.Denied)
                return false;

            var profile = GetOrCreateProfile(request.PlayerId);
            var actor = new MasteryActor(actorName, actorCDKey);

            MasteryRules.EnqueueTraining(
                profile,
                request.MasteryId,
                request.TargetTier,
                useQuickSlot,
                false,
                isInstant,
                actor,
                reviewFeedback,
                request.Id,
                utcNow);

            request.Status = MasteryRequestStatus.Approved;
            request.ReviewerName = actorName;
            request.ReviewerCDKey = actorCDKey;
            request.DateReviewed = utcNow;
            request.ReviewFeedback = reviewFeedback ?? string.Empty;
            request.OverrideReason = overrideReason ?? string.Empty;

            DB.Set(profile);
            DB.Set(request);

            Log.Write(LogGroup.Mastery, $"{actorName} [{actorCDKey}] approved mastery request {request.Id} for player {request.PlayerId}.");

            return true;
        }

        /// <summary>
        /// Denies a request. Returns false if the request could not be found or is not
        /// in a reviewable state.
        /// </summary>
        public static bool DenyRequest(
            string requestId,
            string actorName,
            string actorCDKey,
            string reviewFeedback,
            DateTime utcNow)
        {
            var request = DB.Get<MasteryRequest>(requestId);
            if (request == null)
                return false;

            if (request.Status == MasteryRequestStatus.Approved || request.Status == MasteryRequestStatus.Denied)
                return false;

            request.Status = MasteryRequestStatus.Denied;
            request.ReviewerName = actorName;
            request.ReviewerCDKey = actorCDKey;
            request.DateReviewed = utcNow;
            request.ReviewFeedback = reviewFeedback ?? string.Empty;

            DB.Set(request);

            var profile = GetOrCreateProfile(request.PlayerId);
            profile.AuditLog.Add(new MasteryAuditEntry
            {
                Date = utcNow,
                ActorName = actorName ?? string.Empty,
                ActorCDKey = actorCDKey ?? string.Empty,
                Action = "Deny",
                Reason = reviewFeedback ?? string.Empty
            });
            DB.Set(profile);

            Log.Write(LogGroup.Mastery, $"{actorName} [{actorCDKey}] denied mastery request {request.Id} for player {request.PlayerId}.");

            return true;
        }

        /// <summary>
        /// Staff direct-grant of any tier of a mastery, bypassing the tier+1 progression
        /// rule and the request pipeline entirely (e.g. for OffLimit exceptions).
        /// </summary>
        public static void GrantMastery(string playerId, string masteryId, int tier, string actorName, string actorCDKey, string reason, DateTime utcNow)
        {
            var profile = GetOrCreateProfile(playerId);
            MasteryRules.GrantMastery(profile, masteryId, tier, new MasteryActor(actorName, actorCDKey), reason, utcNow);
            DB.Set(profile);

            Log.Write(LogGroup.Mastery, $"{actorName} [{actorCDKey}] granted mastery {masteryId} tier {tier} to player {playerId}. Reason: {reason}");
        }

        /// <summary>
        /// Staff-initiated removal of a character's current tier in a mastery.
        /// </summary>
        public static bool RevokeMastery(string playerId, string masteryId, int tier, string actorName, string actorCDKey, string reason, DateTime utcNow)
        {
            var profile = GetOrCreateProfile(playerId);
            var result = MasteryRules.Abandon(profile, masteryId, tier, new MasteryActor(actorName, actorCDKey), reason, utcNow, "Revoke");

            if (result)
            {
                DB.Set(profile);
                Log.Write(LogGroup.Mastery, $"{actorName} [{actorCDKey}] revoked mastery {masteryId} tier {tier} from player {playerId}. Reason: {reason}");
            }

            return result;
        }

        /// <summary>
        /// Player self-service abandon of their own current tier in a mastery.
        /// </summary>
        public static bool AbandonMastery(string playerId, string masteryId, int tier, string actorName, string actorCDKey, string reason, DateTime utcNow)
        {
            var profile = GetOrCreateProfile(playerId);
            var result = MasteryRules.Abandon(profile, masteryId, tier, new MasteryActor(actorName, actorCDKey), reason, utcNow, "Abandon");

            if (result)
            {
                DB.Set(profile);
            }

            return result;
        }

        /// <summary>
        /// Applies a staff time reduction (in days) to a character's active training entry.
        /// </summary>
        public static bool ReduceTrainingTime(string playerId, int days, string actorName, string actorCDKey, string reason, DateTime utcNow)
        {
            var profile = GetOrCreateProfile(playerId);
            var result = MasteryRules.ReduceActiveTrainingTime(profile, days, new MasteryActor(actorName, actorCDKey), reason, utcNow);

            if (result)
            {
                DB.Set(profile);
                Log.Write(LogGroup.Mastery, $"{actorName} [{actorCDKey}] reduced player {playerId}'s active training entry by {days} day(s). Reason: {reason}");
            }

            return result;
        }

        /// <summary>
        /// Awards a character a Quick Slot.
        /// </summary>
        public static void AwardQuickSlot(string playerId, string actorName, string actorCDKey, string reason, DateTime utcNow)
        {
            var profile = GetOrCreateProfile(playerId);
            MasteryRules.AwardQuickSlot(profile, new MasteryActor(actorName, actorCDKey), reason, utcNow);
            DB.Set(profile);

            Log.Write(LogGroup.Mastery, $"{actorName} [{actorCDKey}] awarded player {playerId} a Quick Slot. Reason: {reason}");
        }

        /// <summary>
        /// Evaluates a character's training queue, completing any entries whose finish
        /// date has passed. Call on login, on Masteries window open, and when staff open
        /// a player's profile in either staff window.
        /// </summary>
        public static List<MasteryTrainingEntry> EvaluateTrainingQueue(string playerId, DateTime utcNow)
        {
            var profile = GetOrCreateProfile(playerId);
            var completed = MasteryRules.EvaluateTrainingQueue(profile, utcNow);

            if (completed.Count > 0)
            {
                DB.Set(profile);
            }

            return completed;
        }

        /// <summary>
        /// Retrieves every request a character has ever submitted, most recently
        /// submitted first.
        /// </summary>
        public static List<MasteryRequest> GetPlayerRequests(string playerId)
        {
            var query = new DBQuery<MasteryRequest>()
                .AddFieldSearch(nameof(MasteryRequest.PlayerId), playerId, false);
            var count = (int)DB.SearchCount(query);

            return count <= 0
                ? new List<MasteryRequest>()
                : DB.Search(query.AddPaging(count, 0)).OrderByDescending(r => r.DateCreated).ToList();
        }

        /// <summary>
        /// Player self-service cancellation of one of their own requests. Only requests
        /// still Pending or InReview may be cancelled, and only by the player who
        /// submitted them.
        /// </summary>
        public static bool CancelRequest(string requestId, string playerId)
        {
            var request = DB.Get<MasteryRequest>(requestId);
            if (request == null || request.PlayerId != playerId)
                return false;

            if (request.Status != MasteryRequestStatus.Pending && request.Status != MasteryRequestStatus.InReview)
                return false;

            request.Status = MasteryRequestStatus.Cancelled;
            DB.Set(request);

            return true;
        }

        /// <summary>
        /// Appends a comment to a request's negotiation thread - used by both the
        /// requesting player's reply box and (in a later phase) staff review comments.
        /// </summary>
        public static bool AddComment(string requestId, string authorName, bool isStaff, string text, DateTime utcNow)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var request = DB.Get<MasteryRequest>(requestId);
            if (request == null)
                return false;

            request.Comments.Add(new MasteryRequestComment
            {
                Date = utcNow,
                AuthorName = authorName ?? string.Empty,
                IsStaff = isStaff,
                Text = text
            });

            DB.Set(request);

            return true;
        }

        /// <summary>
        /// Builds pass/fail display lines for the subset of <see cref="MasteryRules.ValidateRequest"/>'s
        /// checks that are relevant to this specific request. Reused by both the player's
        /// live eligibility panel and the Discord submission embed, so the two surfaces
        /// can never disagree about which checks passed. Every pass/fail decision comes
        /// from <see cref="MasteryRules"/> - this only decides which lines are worth
        /// showing and how to word them.
        /// </summary>
        public static List<(bool Passed, string Label)> BuildEligibilityChecks(
            string playerId,
            Entity.Mastery mastery,
            int targetTier,
            DateTime characterCreatedDate,
            DateTime utcNow,
            int? skillRank)
        {
            var profile = GetOrCreateProfile(playerId);
            var ownedCatalog = GetOwnedMasteryCatalog(profile);
            var violations = MasteryRules.ValidateRequest(profile, ownedCatalog, mastery, targetTier, characterCreatedDate, utcNow, skillRank);
            var violationTypes = new HashSet<MasteryRuleType>(violations.Select(v => v.RuleType));

            var checks = new List<(bool, string)>();

            var ageDays = (int)(utcNow - characterCreatedDate).TotalDays;
            checks.Add((!violationTypes.Contains(MasteryRuleType.CharacterAge),
                $"Character age {ageDays}d ({MasteryRules.MinimumCharacterAgeDays} required)"));

            if (mastery.AssociatedSkill != null)
            {
                checks.Add((!violationTypes.Contains(MasteryRuleType.SkillRank),
                    $"{mastery.AssociatedSkill} rank {skillRank ?? 0} / {MasteryRules.RequiredSkillRank}"));
            }

            checks.Add((!violationTypes.Contains(MasteryRuleType.QueueFull),
                $"Queue {profile.TrainingQueue.Count} of {MasteryRules.MaxQueueSize}"));

            checks.Add((!violationTypes.Contains(MasteryRuleType.LevelCap),
                $"Levels {MasteryRules.GetProjectedLevelTotal(profile)} of {MasteryRules.MaxTotalLevels}"));

            if (mastery.Rarity == MasteryRarityType.Rare)
            {
                checks.Add((!violationTypes.Contains(MasteryRuleType.RareConflict), "Rare mastery slot available"));
            }

            if (targetTier >= 5)
            {
                checks.Add((!violationTypes.Contains(MasteryRuleType.Tier5Conflict), "Tier 5 slot available"));
            }

            if (violationTypes.Contains(MasteryRuleType.TierProgression))
            {
                var currentTier = profile.Masteries.TryGetValue(mastery.Id, out var level) ? level.Tier : 0;
                checks.Add((false, $"Requested tier {targetTier} is not the next tier (current tier {currentTier})"));
            }

            if (mastery.Rarity == MasteryRarityType.OffLimit)
            {
                checks.Add((false, $"'{mastery.Name}' is off-limits and cannot be requested"));
            }

            return checks;
        }

        /// <summary>
        /// The number of a character's requests which were reviewed since they were last
        /// notified in-game. Used to decide whether to show the one-line login toast.
        /// </summary>
        public static int CountUnnotifiedReviewedRequests(string playerId)
        {
            var profile = GetOrCreateProfile(playerId);
            var lastNotified = profile.DateLastNotified ?? DateTime.MinValue;

            return GetPlayerRequests(playerId)
                .Count(r => r.DateReviewed.HasValue && r.DateReviewed.Value > lastNotified);
        }

        /// <summary>
        /// Marks a character as notified as of the given time, so already-seen reviewed
        /// requests and completions don't toast again on a later login.
        /// </summary>
        public static void MarkNotified(string playerId, DateTime utcNow)
        {
            var profile = GetOrCreateProfile(playerId);
            profile.DateLastNotified = utcNow;
            DB.Set(profile);
        }
    }
}
