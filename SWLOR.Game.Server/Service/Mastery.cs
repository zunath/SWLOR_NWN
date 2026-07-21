using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MasteryService;
using SWLOR.Game.Server.Service.SkillService;

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
        /// Retrieves (or lazily creates) a character's mastery profile. Only call this
        /// from a path where the character is genuinely engaging the system (opening the
        /// Masteries window, submitting/reviewing a request, or a staff mutation) - a
        /// read-only path with no such intent (e.g. a login hook) must check
        /// <see cref="HasProfile"/> first so uninvolved players never get a persisted row.
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
        /// Whether a mastery profile already exists for this character, without creating
        /// one. Read-only call sites that fire for every player regardless of intent
        /// (e.g. the login hook in MasteryNotifications) must check this before touching
        /// the profile at all, so a character who has never engaged with the Masteries
        /// system never gets a DB row written on their behalf.
        /// </summary>
        public static bool HasProfile(string playerId)
        {
            return DB.Get<PlayerMasteryProfile>(playerId) != null;
        }

        /// <summary>
        /// Resolves the catalog entries (keyed by Mastery.Id) for every mastery the
        /// character currently owns a tier in OR has a queued/active training entry for.
        /// Used to feed <see cref="MasteryRules.ValidateRequest"/>'s Rare/Tier5-conflict
        /// checks, so a Rare or Tier-5 slot reserved by in-flight training (not yet
        /// earned) still counts as taken.
        /// </summary>
        public static IReadOnlyDictionary<string, Entity.Mastery> GetOwnedMasteryCatalog(PlayerMasteryProfile profile)
        {
            var result = new Dictionary<string, Entity.Mastery>();

            var masteryIds = new HashSet<string>(profile.Masteries.Keys);
            foreach (var trainingEntry in profile.TrainingQueue)
                masteryIds.Add(trainingEntry.MasteryId);

            foreach (var masteryId in masteryIds)
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
            // Defense-in-depth against a double submission racing past the Masteries
            // window's own in-flight guard (MasteriesViewModel.OnClickSubmitRequest) - if
            // an identical request is already in flight, hand that one back instead of
            // persisting a second Pending row.
            var duplicate = MasteryRules.FindDuplicatePendingRequest(
                GetPlayerRequests(playerId), type, masteryId, customName, targetTier);
            if (duplicate != null)
                return duplicate;

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
        /// Approves a request: re-validates current state immediately before mutation,
        /// enqueues a training entry (or completes it immediately if isInstant), and marks
        /// the request Approved. Returns false if the request could not be found, is not
        /// in a reviewable state (only Pending/InReview - see the Status whitelist below),
        /// has a blocking rule violation (e.g. OffLimit or an out-of-range tier), has
        /// unjustified warnings (a non-empty <paramref name="overrideReason"/> is required
        /// whenever any non-blocking violation is present), or requests a Quick Slot with
        /// none available.
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
            // Re-fetch fresh rather than trusting anything the caller captured earlier - a
            // stale staff window (or a player who cancelled the request in the meantime)
            // must never be able to approve a request that is no longer Pending/InReview.
            // This single re-check is also what makes materializing a Custom request's
            // catalog entry below race-safe: it can only ever run once per request.
            var request = DB.Get<MasteryRequest>(requestId);
            if (request == null)
                return false;

            if (!MasteryRules.CanReviewRequest(request.Status))
                return false;

            // A Custom (unlisted) request has no catalog row yet - build the same
            // transient stand-in ValidateRequest/BuildEligibilityChecks use elsewhere so
            // the rules re-check below applies identically to catalog and unlisted requests.
            var existingMastery = string.IsNullOrWhiteSpace(request.MasteryId) ? null : GetMastery(request.MasteryId);

            // A non-Custom request must always resolve to a real catalog row. Falling
            // through to the Custom stand-in here would let a MasteryId whose row was
            // deleted (or was simply blank due to a data bug) get treated as an unlisted
            // request and queued/persisted under a nonexistent id. A retired-but-still-
            // present row (IsActive false) is intentionally still allowed through - it
            // only blocks NEW catalog selection (see MasteriesViewModel's catalog filter),
            // not approval of a request against a mastery the character may already be
            // part-way trained in.
            if (request.Type != MasteryRequestType.Custom && existingMastery == null)
                return false;

            var checkMastery = existingMastery ?? new Entity.Mastery
            {
                Name = request.CustomName,
                Rarity = MasteryRarityType.Standard
            };

            var dbPlayer = DB.Get<Player>(request.PlayerId);
            var characterCreatedDate = dbPlayer?.DateCreated ?? utcNow;
            int? skillRank = null;
            if (checkMastery.AssociatedSkill != null && dbPlayer != null && dbPlayer.Skills.TryGetValue(checkMastery.AssociatedSkill.Value, out var skill))
                skillRank = skill.Rank;

            // Re-run every rule check against CURRENT state immediately before mutation -
            // never trust whatever the reviewer's window last displayed, since the queue,
            // owned catalog, or level total may have changed since. Any blocking violation
            // (OffLimit, an out-of-range tier) rejects outright; any other violation
            // requires a non-empty override reason.
            var violations = ValidateRequest(request.PlayerId, checkMastery, request.TargetTier, characterCreatedDate, utcNow, skillRank);
            if (violations.Any(v => v.IsBlocking))
                return false;

            if (violations.Count > 0 && string.IsNullOrWhiteSpace(overrideReason))
                return false;

            var profile = GetOrCreateProfile(request.PlayerId);
            var actor = new MasteryActor(actorName, actorCDKey);

            // Reject a Quick Slot approval with none available BEFORE a Custom request's
            // catalog row is ever created below. CreateMastery persists immediately
            // (there is no single transaction spanning it and the EnqueueTraining
            // rejection further down), so checking only after would leave an orphaned
            // catalog row behind on every retry of a doomed zero-slot approval.
            if (!MasteryRules.CanUseQuickSlot(profile, useQuickSlot, isInstant))
                return false;

            // Approving a Custom (unlisted) request for the first time materializes its
            // catalog entry. This only ever runs once, because it happens after the fresh
            // Pending/InReview re-check above - a second reviewer racing a stale window
            // can never create a duplicate catalog row for the same request.
            if (request.Type == MasteryRequestType.Custom && string.IsNullOrWhiteSpace(request.MasteryId))
            {
                var created = CreateMastery(
                    request.CustomName,
                    MasteryCategoryType.General,
                    request.CustomDescription,
                    MasteryRarityType.Standard,
                    null,
                    actorName,
                    actorCDKey);

                request.MasteryId = created.Id;
            }

            // Retrain credits are spent automatically whenever one is available and
            // applicable - see MasteryRules.ShouldUseRetrainCredit for the exact
            // conditions (never on a Quick Slot or instant grant, never on tier 5).
            var useRetrainCredit = MasteryRules.ShouldUseRetrainCredit(profile, request.TargetTier, useQuickSlot, isInstant);

            var enqueued = MasteryRules.EnqueueTraining(
                profile,
                request.MasteryId,
                request.TargetTier,
                useQuickSlot,
                useRetrainCredit,
                isInstant,
                actor,
                reviewFeedback,
                request.Id,
                utcNow);

            // EnqueueTraining rejects a Quick Slot request with none available before
            // mutating anything - a stale or direct call must not be able to approve into
            // a free discounted duration.
            if (enqueued == null)
                return false;

            request.Status = MasteryRequestStatus.Approved;
            request.ReviewerName = actorName;
            request.ReviewerCDKey = actorCDKey;
            request.DateReviewed = utcNow;
            request.ReviewFeedback = reviewFeedback ?? string.Empty;
            request.OverrideReason = overrideReason ?? string.Empty;

            // Persist the request as Approved BEFORE the profile/training-queue mutation.
            // Redis has no multi-key transaction here, so if the profile write below were
            // to throw, this ordering guarantees a retry can never double-enqueue: the
            // retry re-fetches the request (now Approved) and is rejected by the
            // Pending/InReview check above. The worst case is a request stuck Approved with
            // no training actually queued, which staff can recover from with a direct grant.
            DB.Set(request);
            DB.Set(profile);

            Log.Write(LogGroup.Mastery, $"{actorName} [{actorCDKey}] approved mastery request {request.Id} for player {request.PlayerId}.");

            return true;
        }

        /// <summary>
        /// Denies a request. Returns false if the request could not be found or is not
        /// in a reviewable state (only Pending/InReview may be denied - a stale staff
        /// window can no longer deny a request the player already cancelled, or that
        /// another reviewer already decided).
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

            if (!MasteryRules.CanReviewRequest(request.Status))
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
        /// a player's profile in either staff window. Every completion queues a
        /// <see cref="PlayerMasteryProfile.PendingCompletionNotices"/> entry regardless of
        /// whether the character is online right now (e.g. a DM evaluating the queue
        /// while the character is offline) - this is the single place completion notices
        /// are produced, so <see cref="PeekPendingCompletionNotices"/> and
        /// <see cref="AcknowledgeCompletionNotices"/> are the single place they're
        /// delivered from.
        /// </summary>
        public static List<MasteryTrainingEntry> EvaluateTrainingQueue(string playerId, DateTime utcNow)
        {
            var profile = GetOrCreateProfile(playerId);
            var completed = MasteryRules.EvaluateTrainingQueue(profile, utcNow);

            if (completed.Count > 0)
            {
                foreach (var entry in completed)
                {
                    var mastery = GetMastery(entry.MasteryId);
                    var name = string.IsNullOrWhiteSpace(mastery?.Name) ? "a mastery" : mastery.Name;

                    profile.PendingCompletionNotices.Add(
                        $"Your training in {name} is complete - you are now Tier {entry.TargetTier}! Open Masteries on your character sheet for details.");
                }

                DB.Set(profile);
            }

            return completed;
        }

        /// <summary>
        /// Reads (without clearing) every pending completion-toast notice queued for a
        /// character. Call at login and at Masteries window open - the two points where a
        /// character can actually receive an in-game toast - and only clear them via
        /// <see cref="AcknowledgeCompletionNotices"/> once they have actually been
        /// delivered. Splitting peek from acknowledge means a UI exception between reading
        /// and displaying the notices can never permanently lose them - clearing first
        /// would make delivery at-most-once instead of exactly-once.
        /// </summary>
        public static List<string> PeekPendingCompletionNotices(string playerId)
        {
            var profile = GetOrCreateProfile(playerId);
            return new List<string>(profile.PendingCompletionNotices);
        }

        /// <summary>
        /// Clears and persists every pending completion-toast notice for a character.
        /// Call only after the notices returned by <see cref="PeekPendingCompletionNotices"/>
        /// have actually been delivered (e.g. the toast messages were sent to the player).
        /// </summary>
        public static void AcknowledgeCompletionNotices(string playerId)
        {
            var profile = GetOrCreateProfile(playerId);

            if (profile.PendingCompletionNotices.Count == 0)
                return;

            profile.PendingCompletionNotices.Clear();
            DB.Set(profile);
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
                $"Levels {MasteryRules.GetProjectedLevelTotal(profile, mastery.Id, targetTier)} of {MasteryRules.MaxTotalLevels}"));

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

        /// <summary>
        /// Retrieves every request whose Status matches one of the given values. Status is
        /// the only indexed field relevant to the review queue's filter combo - name search
        /// is filtered in memory by the caller since CharacterName is not indexed.
        /// </summary>
        public static List<MasteryRequest> GetRequestsByStatus(IEnumerable<MasteryRequestStatus> statuses)
        {
            var query = new DBQuery<MasteryRequest>()
                .AddFieldSearch(nameof(MasteryRequest.Status), statuses.Select(s => (int)s));
            var count = (int)DB.SearchCount(query);

            return count <= 0
                ? new List<MasteryRequest>()
                : DB.Search(query.AddPaging(count, 0)).ToList();
        }

        /// <summary>
        /// Transitions a Pending request to InReview once a staff member opens it in the
        /// review queue. A no-op for requests already InReview or decided.
        /// </summary>
        public static void MarkInReview(string requestId)
        {
            var request = DB.Get<MasteryRequest>(requestId);
            if (request == null || request.Status != MasteryRequestStatus.Pending)
                return;

            request.Status = MasteryRequestStatus.InReview;
            DB.Set(request);
        }

        /// <summary>
        /// Cancels a not-yet-completed training entry (active or queued), refunding any
        /// Quick Slot spent on it. See <see cref="MasteryRules.AbandonTrainingEntry"/> -
        /// distinct from <see cref="RevokeMastery"/>, which un-earns an already-granted tier.
        /// </summary>
        public static bool AbandonTrainingEntry(string playerId, int index, string actorName, string actorCDKey, string reason, DateTime utcNow)
        {
            var profile = GetOrCreateProfile(playerId);
            var result = MasteryRules.AbandonTrainingEntry(profile, index, new MasteryActor(actorName, actorCDKey), reason, utcNow);

            if (result)
            {
                DB.Set(profile);
                Log.Write(LogGroup.Mastery, $"{actorName} [{actorCDKey}] cancelled a queued training entry (index {index}) for player {playerId}. Reason: {reason}");
            }

            return result;
        }

        /// <summary>
        /// Moves a queued (non-active) training entry up or down within the queue.
        /// See <see cref="MasteryRules.ReorderQueueEntry"/>.
        /// </summary>
        public static bool ReorderTrainingQueueEntry(string playerId, int index, int direction, string actorName, string actorCDKey, DateTime utcNow)
        {
            var profile = GetOrCreateProfile(playerId);
            var result = MasteryRules.ReorderQueueEntry(profile, index, direction, new MasteryActor(actorName, actorCDKey), utcNow);

            if (result)
            {
                DB.Set(profile);
                Log.Write(LogGroup.Mastery, $"{actorName} [{actorCDKey}] reordered player {playerId}'s training queue (index {index}, direction {direction}).");
            }

            return result;
        }

        /// <summary>
        /// Creates a new catalog entry from the staff catalog-management screen (or from
        /// approving a Custom/unlisted request - see MasteryReviewViewModel). Never called
        /// by the seed pipeline, which builds <see cref="Entity.Mastery"/> rows directly.
        /// </summary>
        public static Entity.Mastery CreateMastery(
            string name,
            MasteryCategoryType category,
            string description,
            MasteryRarityType rarity,
            SkillType? associatedSkill,
            string actorName,
            string actorCDKey)
        {
            var mastery = new Entity.Mastery
            {
                Name = name ?? string.Empty,
                Category = category,
                Description = description ?? string.Empty,
                Rarity = rarity,
                AssociatedSkill = associatedSkill,
                IsActive = true,
                IsSeeded = false
            };

            DB.Set(mastery);

            Log.Write(LogGroup.Mastery, $"{actorName} [{actorCDKey}] created mastery catalog entry '{mastery.Name}' ({mastery.Id}).");

            return mastery;
        }

        /// <summary>
        /// Updates an existing catalog entry's editable fields from the staff
        /// catalog-management screen. Never overwrites Id/IsSeeded/SeedKey - renaming a
        /// seeded row here is exactly the case <see cref="Entity.Mastery.SeedKey"/> exists
        /// to survive, so the seed-matching logic in
        /// <see cref="MasteryRules.BuildMissingCatalogEntries"/> never mistakes the renamed
        /// row for missing and recreates it.
        /// </summary>
        public static bool UpdateMastery(
            string masteryId,
            string name,
            MasteryCategoryType category,
            string description,
            MasteryRarityType rarity,
            SkillType? associatedSkill,
            bool isActive,
            string actorName,
            string actorCDKey)
        {
            var mastery = DB.Get<Entity.Mastery>(masteryId);
            if (mastery == null)
                return false;

            mastery.Name = name ?? mastery.Name;
            mastery.Category = category;
            mastery.Description = description ?? string.Empty;
            mastery.Rarity = rarity;
            mastery.AssociatedSkill = associatedSkill;
            mastery.IsActive = isActive;

            DB.Set(mastery);

            Log.Write(LogGroup.Mastery, $"{actorName} [{actorCDKey}] updated mastery catalog entry '{mastery.Name}' ({mastery.Id}).");

            return true;
        }
    }
}
