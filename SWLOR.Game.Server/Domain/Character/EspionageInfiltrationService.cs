using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Tracks one infiltration attempt per player/hostile pair. A player succeeds by evading at
    /// least one Detection check, moving meaningfully through the hostile's aggro aura, and leaving
    /// that aura while still stealthed. Detection resolves the pair for a smaller failure award.
    /// The logic and state moved here unchanged from the static <c>Service.EspionageInfiltration</c>; the facade keeps
    /// the event handlers.
    /// </summary>
    public class EspionageInfiltrationService : IEspionageInfiltrationService
    {
        public const float RequiredTravelDistanceMeters = 4f;
        public const float DetectionFailureXpPercent = 0.15f;
        public const int HostileFactionId = 1;
        private const float MovementSampleIntervalSeconds = 1f;

        private sealed class InfiltrationAttempt
        {
            public Location EntryLocation { get; init; }
            public float MaximumTravelDistance { get; set; }
            public bool EvadedDetection { get; set; }
            public bool PlayerInitiatedCombat { get; set; }
        }

        private readonly Dictionary<(uint Player, uint Npc), InfiltrationAttempt> _activeAttempts = new();
        private readonly Dictionary<uint, HashSet<string>> _resolvedPlayerIdsByNpc = new();
        private readonly Dictionary<uint, long> _movementSamplerIdsByPlayer = new();
        private long _nextMovementSamplerId;

        private readonly IDBService _db;
        private readonly IAIService _ai;
        private readonly IStealthService _stealth;
        private readonly IEnmityService _enmity;
        private readonly IStatService _stat;
        private readonly ISkillService _skill;

        public EspionageInfiltrationService(IDBService db, IAIService ai, IStealthService stealth, IEnmityService enmity, IStatService stat, ISkillService skill)
        {
            _db = db;
            _ai = ai;
            _stealth = stealth;
            _enmity = enmity;
            _stat = stat;
            _skill = skill;
        }

        /// <summary>
        /// Starts an attempt when a stealthed player enters an eligible hostile's real aggro range.
        /// The same method is also safe to call from Spot detection, which covers gaining line of
        /// sight after entering the aura behind an obstruction.
        /// </summary>
        public bool TryBegin(uint player, uint npc)
        {
            var key = (player, npc);
            if (_activeAttempts.ContainsKey(key))
                return true;

            if (!CanStartAttempt(player, npc) || HasResolved(player, npc))
                return false;

            _activeAttempts[key] = new InfiltrationAttempt
            {
                EntryLocation = GetLocation(player)
            };
            StartMovementSampler(player);
            return true;
        }

        /// <summary>
        /// Records the engine's resolved Spot verdict. Detection pays the small failure award
        /// immediately; an undetected verdict qualifies the attempt for a later successful exit.
        /// </summary>
        public void RecordDetection(uint observer, uint target, bool detected)
        {
            var key = (target, observer);
            if (!_activeAttempts.TryGetValue(key, out var attempt))
            {
                if (!TryBegin(target, observer) || !_activeAttempts.TryGetValue(key, out attempt))
                    return;
            }

            if (IsStealthDetectionBlocked(observer))
            {
                // A blinded observer supplied this traversal, so it pays no bypass XP even if the
                // cloud lifts before the player leaves.
                _activeAttempts.Remove(key);
                TryMarkResolved(target, observer);
                return;
            }

            if (!_ai.IsInAggroRange(observer, target))
                return;

            UpdateMaximumTravelDistance(target, attempt);

            // Detection can establish combat enmity between this player and observer before the
            // callback finishes. That pair-specific enmity is the expected failure outcome only
            // when the player did not initiate combat and neither participant is fighting elsewhere.
            var hasPairCombatEnmity = HasNonProximityEnmity(target, observer);
            var hasUnrelatedCombatEnmity = HasNonProximityEnmityOutsidePair(target, observer);
            if (ShouldRejectDetectionOutcome(
                    detected,
                    attempt.PlayerInitiatedCombat,
                    hasPairCombatEnmity,
                    hasUnrelatedCombatEnmity))
            {
                _activeAttempts.Remove(key);
                return;
            }

            if (!detected)
            {
                attempt.EvadedDetection = true;
                return;
            }

            _activeAttempts.Remove(key);
            if (!TryMarkResolved(target, observer))
                return;

            GrantXp(target, observer, wasDetected: true);
        }

        /// <summary>
        /// Completes an attempt when NWN reports that the player left the hostile's aggro aura.
        /// </summary>
        public void Complete(uint player, uint npc)
        {
            var key = (player, npc);
            if (!_activeAttempts.Remove(key, out var attempt))
                return;

            if (!GetIsObjectValid(player) ||
                !GetIsObjectValid(npc) ||
                GetIsDead(player) ||
                GetIsDead(npc) ||
                GetCurrentHitPoints(player) <= 0 ||
                GetCurrentHitPoints(npc) <= 0)
            {
                return;
            }

            UpdateMaximumTravelDistance(player, attempt);
            var isStealthed = GetActionMode(player, ActionMode.Stealth);
            var hasCombatEnmity = attempt.PlayerInitiatedCombat || HasCombatEnmity(player, npc);
            if (!MeetsSuccessRequirements(
                    attempt.EvadedDetection,
                    attempt.MaximumTravelDistance,
                    isStealthed,
                    hasCombatEnmity) ||
                !TryMarkResolved(player, npc))
            {
                return;
            }

            GrantXp(player, npc, wasDetected: false);
        }

        /// <summary>
        /// Samples movement for active attempts. The dedicated one-second sampler catches quick
        /// traversals, while stealth ticks, Spot events, and aura exit provide additional samples.
        /// </summary>
        public void UpdateMovement(uint player)
        {
            var attempts = _activeAttempts
                .Where(x => x.Key.Player == player)
                .ToArray();

            foreach (var (key, attempt) in attempts)
            {
                if (!GetIsObjectValid(key.Npc) ||
                    GetIsDead(key.Npc) ||
                    GetArea(player) != GetArea(key.Npc))
                {
                    _activeAttempts.Remove(key);
                    continue;
                }

                UpdateMaximumTravelDistance(player, attempt);
            }
        }

        public bool MeetsSuccessRequirements(
            bool evadedDetection,
            float maximumTravelDistance,
            bool isStealthed,
            bool hasCombatEnmity)
        {
            return evadedDetection &&
                   maximumTravelDistance >= RequiredTravelDistanceMeters &&
                   isStealthed &&
                   !hasCombatEnmity;
        }

        public int CalculateXp(int npcLevel, int espionageRank, bool wasDetected)
        {
            var baseXp = GetDeltaXP(npcLevel - espionageRank);
            return wasDetected
                ? (int)(baseXp * DetectionFailureXpPercent)
                : baseXp;
        }

        public bool ShouldRejectDetectionOutcome(
            bool detected,
            bool playerInitiatedCombat,
            bool hasPairCombatEnmity,
            bool hasUnrelatedCombatEnmity)
        {
            return playerInitiatedCombat ||
                   hasUnrelatedCombatEnmity ||
                   (!detected && hasPairCombatEnmity);
        }

        /// <summary>
        /// Marks every active attempt for a player before their attack can create pair enmity.
        /// The later Spot callback can then distinguish that hostile action from detection aggro.
        /// </summary>
        public void RecordPlayerCombatInitiation(uint player)
        {
            foreach (var (key, attempt) in _activeAttempts)
            {
                if (key.Player == player)
                    attempt.PlayerInitiatedCombat = true;
            }
        }

        public void CancelPlayer(uint player)
        {
            var keys = _activeAttempts.Keys
                .Where(x => x.Player == player)
                .ToArray();

            foreach (var key in keys)
            {
                _activeAttempts.Remove(key);
            }

            _movementSamplerIdsByPlayer.Remove(player);
        }

        public void ClearNpc(uint npc)
        {
            var keys = _activeAttempts.Keys
                .Where(x => x.Npc == npc)
                .ToArray();

            foreach (var key in keys)
            {
                _activeAttempts.Remove(key);
            }

            _resolvedPlayerIdsByNpc.Remove(npc);
        }

        public void OnPlayerExit()
        {
            var exiting = GetExitingObject();
            if (GetIsPC(exiting))
            {
                CancelPlayer(exiting);
            }
        }

        public void OnPlayerDeath()
        {
            CancelPlayer(GetLastPlayerDied());
        }

        public void OnNpcRemoved()
        {
            ClearNpc(OBJECT_SELF);
        }

        private bool CanStartAttempt(uint player, uint npc)
        {
            if (!GetIsObjectValid(player) ||
                !GetIsObjectValid(npc) ||
                !GetIsPC(player) ||
                GetIsDM(player) ||
                GetIsPC(npc) ||
                GetIsDM(npc) ||
                GetIsDead(player) ||
                GetIsDead(npc) ||
                GetCurrentHitPoints(player) <= 0 ||
                GetCurrentHitPoints(npc) <= 0 ||
                !GetActionMode(player, ActionMode.Stealth) ||
                GetLocalInt(player, Stealth.CombatEntryWindowVariable) != 0 ||
                !IsBypassXpEligible(HasGrantedStealth(player), IsStealthDetectionBlocked(npc)) ||
                HasNonProximityEnmityForCreature(player) ||
                !_ai.IsCreatureAIEnabled(npc) ||
                CreaturePlugin.GetFaction(npc) != HostileFactionId ||
                !GetIsEnemy(player, npc) ||
                !_ai.IsInAggroRange(npc, player) ||
                HasNonProximityEnmity(npc) ||
                GetNPCStats(npc).Level <= 0)
            {
                return false;
            }

            var master = GetMaster(npc);
            return !GetIsObjectValid(master) || (!GetIsPC(master) && !GetIsDM(master));
        }

        private void UpdateMaximumTravelDistance(uint player, InfiltrationAttempt attempt)
        {
            var currentLocation = GetLocation(player);
            if (GetAreaFromLocation(currentLocation) != GetAreaFromLocation(attempt.EntryLocation))
                return;

            var distance = GetDistanceBetweenLocations(attempt.EntryLocation, currentLocation);
            attempt.MaximumTravelDistance = Math.Max(attempt.MaximumTravelDistance, distance);
        }

        private void StartMovementSampler(uint player)
        {
            if (_movementSamplerIdsByPlayer.ContainsKey(player))
                return;

            var samplerId = ++_nextMovementSamplerId;
            _movementSamplerIdsByPlayer[player] = samplerId;
            DelayCommand(MovementSampleIntervalSeconds, () => SampleMovement(player, samplerId));
        }

        private bool HasCombatEnmity(uint player, uint npc)
        {
            return HasNonProximityEnmityForCreature(player) ||
                   HasNonProximityEnmity(npc);
        }

        private void SampleMovement(uint player, long samplerId)
        {
            if (!_movementSamplerIdsByPlayer.TryGetValue(player, out var activeSamplerId) ||
                activeSamplerId != samplerId)
            {
                return;
            }

            if (!GetIsObjectValid(player) || !_activeAttempts.Keys.Any(x => x.Player == player))
            {
                _movementSamplerIdsByPlayer.Remove(player);
                return;
            }

            UpdateMovement(player);
            DelayCommand(MovementSampleIntervalSeconds, () => SampleMovement(player, samplerId));
        }

        private bool HasResolved(uint player, uint npc)
        {
            var playerId = GetObjectUUID(player);
            return !string.IsNullOrWhiteSpace(playerId) &&
                   _resolvedPlayerIdsByNpc.TryGetValue(npc, out var playerIds) &&
                   playerIds.Contains(playerId);
        }

        private bool TryMarkResolved(uint player, uint npc)
        {
            var playerId = GetObjectUUID(player);
            if (string.IsNullOrWhiteSpace(playerId))
                return false;

            if (!_resolvedPlayerIdsByNpc.TryGetValue(npc, out var playerIds))
            {
                playerIds = new HashSet<string>();
                _resolvedPlayerIdsByNpc[npc] = playerIds;
            }

            return playerIds.Add(playerId);
        }

        /// <summary>
        /// Stealth granted by Shared Presence and enemies blinded by Cloud Mind award no bypass XP.
        /// </summary>
        public bool IsBypassXpEligible(bool playerHasGrantedStealth, bool npcIsClouded)
        {
            return !playerHasGrantedStealth && !npcIsClouded;
        }

        private void GrantXp(uint player, uint npc, bool wasDetected)
        {
            // Cloud Mind can land after the attempt started; the bypass still pays nothing.
            if (!IsBypassXpEligible(HasGrantedStealth(player), IsStealthDetectionBlocked(npc)))
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            var npcLevel = GetNPCStats(npc).Level;
            var espionageRank = dbPlayer.Skills[SkillType.Espionage].Rank;
            var xp = CalculateXp(npcLevel, espionageRank, wasDetected);

            GiveSkillXP(player, SkillType.Espionage, xp, false, false);
        }

        /// <summary>Whether the player's stealth was granted by another source: <c>Stealth.HasGrantedStealth</c>.</summary>
        protected virtual bool HasGrantedStealth(uint creature)
        {
            return _stealth.HasGrantedStealth(creature);
        }

        /// <summary>Whether detection by the observer is blocked: <c>Stealth.IsStealthDetectionBlocked</c>.</summary>
        protected virtual bool IsStealthDetectionBlocked(uint observer)
        {
            return _stealth.IsStealthDetectionBlocked(observer);
        }

        /// <summary>Enmity between the pair: <c>Enmity.HasNonProximityEnmity</c>.</summary>
        protected virtual bool HasNonProximityEnmity(uint creature, uint enemy)
        {
            return _enmity.HasNonProximityEnmity(creature, enemy);
        }

        /// <summary>Enmity of the enemy: <c>Enmity.HasNonProximityEnmity</c>.</summary>
        protected virtual bool HasNonProximityEnmity(uint enemy)
        {
            return _enmity.HasNonProximityEnmity(enemy);
        }

        /// <summary>Enmity of the creature: <c>Enmity.HasNonProximityEnmityForCreature</c>.</summary>
        protected virtual bool HasNonProximityEnmityForCreature(uint creature)
        {
            return _enmity.HasNonProximityEnmityForCreature(creature);
        }

        /// <summary>Enmity outside the pair: <c>Enmity.HasNonProximityEnmityOutsidePair</c>.</summary>
        protected virtual bool HasNonProximityEnmityOutsidePair(uint first, uint second)
        {
            return _enmity.HasNonProximityEnmityOutsidePair(first, second);
        }

        /// <summary>The stats of an NPC: <c>Stat.GetNPCStats</c>.</summary>
        protected virtual NPCStats GetNPCStats(uint npc)
        {
            return _stat.GetNPCStats(npc);
        }

        /// <summary>Awards skill XP: <c>Skill.GiveSkillXP</c>.</summary>
        protected virtual int GiveSkillXP(uint player, SkillType skill, int xp, bool ignoreBonuses, bool applyHenchmanPenalty)
        {
            return _skill.GiveSkillXP(player, skill, xp, ignoreBonuses, applyHenchmanPenalty);
        }

        /// <summary>The XP for a level difference: <c>Skill.GetDeltaXP</c>.</summary>
        protected virtual int GetDeltaXP(int delta)
        {
            return _skill.GetDeltaXP(delta);
        }
    }
}
