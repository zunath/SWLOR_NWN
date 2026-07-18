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
    }
}
