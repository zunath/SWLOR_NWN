using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// The target a companion may engage: the one its master ordered, the threats it defends against and the target its
    /// master attacks, whether it can reach them, and whether a hostile ability or an attack command may go to a target.
    /// Moved here unchanged from <see cref="CompanionControlService"/>. It depends on no AI service, so the creature AI
    /// and the enmity tables read it without a cycle.
    /// </summary>
    public class CompanionTargetAuthorityService : ICompanionTargetAuthorityService
    {
        private const float AttackRangeToleranceMeters = 0.25f;

        private readonly ICompanionControlStoreService _store;
        private readonly ICompanionPositioningService _positioning;
        private readonly IAbilityActivationService _activation;
        private readonly IAbilityCatalog _catalog;
        private readonly IPerkService _perk;
        private readonly ICombatActivityService _combatActivity;
        private readonly ICombatQueriesService _queries;

        public CompanionTargetAuthorityService(
            ICompanionControlStoreService store,
            ICompanionPositioningService positioning,
            IAbilityActivationService activation,
            IAbilityCatalog catalog,
            IPerkService perk,
            ICombatActivityService combatActivity,
            ICombatQueriesService queries)
        {
            _store = store;
            _positioning = positioning;
            _activation = activation;
            _catalog = catalog;
            _perk = perk;
            _combatActivity = combatActivity;
            _queries = queries;
        }

        public uint AdvanceAuthorizedTarget(uint companion)
        {
            if (!_store.IsControlledCompanion(companion))
                return OBJECT_INVALID;

            var state = _store.GetState(companion);

            if (GetIsObjectValid(state.AttackNearestTarget))
            {
                if (ValidateAuthorizedTarget(
                        companion,
                        state.AttackNearestTarget,
                        CompanionEngagementType.AttackNearest) &&
                    TrackProgress(
                        companion,
                        state.AttackNearestTarget,
                        CompanionEngagementType.AttackNearest))
                {
                    return state.AttackNearestTarget;
                }

                CompleteAttackNearest(companion);
                return AdvanceAuthorizedTarget(companion);
            }

            var defensiveTarget = GetDefensiveTarget(companion, state);
            if (GetIsObjectValid(defensiveTarget))
                return defensiveTarget;

            if (GetIsObjectValid(state.OwnerAssistTarget))
            {
                if (ValidateAuthorizedTarget(
                        companion,
                        state.OwnerAssistTarget,
                        CompanionEngagementType.OwnerAssist) &&
                    TrackProgress(
                        companion,
                        state.OwnerAssistTarget,
                        CompanionEngagementType.OwnerAssist))
                {
                    return state.OwnerAssistTarget;
                }

                state.OwnerAssistTarget = OBJECT_INVALID;
                _positioning.ResetProgress(state);
            }

            return OBJECT_INVALID;
        }

        public uint PeekAuthorizedTarget(uint companion)
        {
            if (!_store.IsControlledCompanion(companion))
                return OBJECT_INVALID;

            var state = _store.GetState(companion);
            if (GetIsObjectValid(state.AttackNearestTarget) &&
                ValidateAuthorizedTarget(
                    companion,
                    state.AttackNearestTarget,
                    CompanionEngagementType.AttackNearest))
            {
                return state.AttackNearestTarget;
            }

            var defensiveTarget = SelectDefensiveTarget(companion, state, false);
            if (GetIsObjectValid(defensiveTarget))
                return defensiveTarget;

            return GetIsObjectValid(state.OwnerAssistTarget) &&
                   ValidateAuthorizedTarget(
                       companion,
                       state.OwnerAssistTarget,
                       CompanionEngagementType.OwnerAssist)
                ? state.OwnerAssistTarget
                : OBJECT_INVALID;
        }

        public uint ResolveHostileAbilityTarget(
            uint companion,
            AbilityDetail ability,
            uint authorizedTarget,
            uint selectedTarget)
        {
            if (!GetIsObjectValid(authorizedTarget))
                return OBJECT_INVALID;

            if (ability.IsAreaAbility &&
                ability.Targeting != null &&
                ability.Targeting.Flags.HasFlag(AbilityTargetingFlags.OriginOnSelf))
            {
                if (GetArea(companion) != GetArea(authorizedTarget))
                    return OBJECT_INVALID;

                var distance = GetDistanceBetween(companion, authorizedTarget);
                var sizeX = ability.Targeting.ResolveSizeX(companion, true);
                if (!CompanionControlPolicy.IsWithinSelfOriginAreaReach(
                        ability.Targeting.Shape,
                        true,
                        distance,
                        sizeX))
                {
                    return OBJECT_INVALID;
                }
            }

            if (ability.IsAreaAbility && selectedTarget == companion)
            {
                if (GetArea(companion) != GetArea(authorizedTarget))
                    return OBJECT_INVALID;

                var distance = GetDistanceBetween(companion, authorizedTarget);
                if (ability.Targeting == null)
                {
                    return CompanionControlPolicy.IsWithinAreaReach(distance, ability.MaxRange)
                        ? companion
                        : OBJECT_INVALID;
                }

                if (ability.Targeting.Shape is not AbilityTargetingShapeType.Sphere and
                    not AbilityTargetingShapeType.HSphere)
                {
                    return OBJECT_INVALID;
                }

                return ability.Targeting.Flags.HasFlag(AbilityTargetingFlags.OriginOnSelf)
                    ? companion
                    : OBJECT_INVALID;
            }

            return selectedTarget == authorizedTarget
                ? selectedTarget
                : OBJECT_INVALID;
        }

        public bool IsHostileAbilityTargetAuthorized(
            uint companion,
            AbilityDetail ability,
            uint selectedTarget)
        {
            var authorizedTarget = PeekAuthorizedTarget(companion);
            return GetIsObjectValid(ResolveHostileAbilityTarget(
                companion,
                ability,
                authorizedTarget,
                selectedTarget));
        }

        public bool CanIssueAttackCommand(uint companion, uint target)
        {
            if (!_store.IsRegisteredCompanion(companion))
                return true;

            var authorizedTarget = PeekAuthorizedTarget(companion);
            if (authorizedTarget != target)
                return false;

            var state = _store.GetState(companion);
            return state.Mode != CompanionMode.StandGround || IsWithinWeaponRange(companion, target);
        }

        public bool IsRegisteredCompanion(uint creature)
        {
            return _store.IsRegisteredCompanion(creature);
        }

        private uint GetDefensiveTarget(uint companion, CompanionControlState state)
        {
            var selected = SelectDefensiveTarget(companion, state, true);

            if (GetIsObjectValid(selected) &&
                !TrackProgress(companion, selected, CompanionEngagementType.Defensive))
            {
                RemoveDefensiveThreat(state, selected);
                _positioning.MaintainModePosition(companion);
                return OBJECT_INVALID;
            }

            return selected;
        }

        private uint SelectDefensiveTarget(
            uint companion,
            CompanionControlState state,
            bool updateState)
        {
            var master = GetMaster(companion);
            var now = UtcNow();
            var candidates = new List<uint>();

            foreach (var (threat, _) in state.DefensiveThreats.ToList())
            {
                var attackTarget = GetIsObjectValid(threat)
                    ? GetAttackTarget(threat)
                    : OBJECT_INVALID;
                var activelyThreatening = attackTarget == master || attackTarget == companion;
                if (updateState && activelyThreatening)
                    state.DefensiveThreats[threat] = now;

                var recentlyThreatened = (now - state.DefensiveThreats[threat]).TotalSeconds <
                                         CompanionControlPolicy.PathingTimeoutSeconds;
                var isAuthorized = (activelyThreatening || recentlyThreatened) &&
                                   ValidateAuthorizedTarget(
                                       companion,
                                       threat,
                                       CompanionEngagementType.Defensive);
                if (!isAuthorized)
                {
                    if (updateState)
                        RemoveDefensiveThreat(state, threat);

                    continue;
                }

                candidates.Add(threat);
            }

            return candidates
                .OrderByDescending(x => GetAttackTarget(x) == master)
                .ThenBy(x => GetDistanceBetween(companion, x))
                .FirstOrDefault(OBJECT_INVALID);
        }

        private void RemoveDefensiveThreat(CompanionControlState state, uint threat)
        {
            state.DefensiveThreats.Remove(threat);
            if (state.TrackedTarget == threat)
                _positioning.ResetProgress(state);
        }

        private bool ValidateAuthorizedTarget(
            uint companion,
            uint target,
            CompanionEngagementType engagementType)
        {
            if (!IsValidHostileTarget(companion, target))
                return false;

            var master = GetMaster(companion);
            var state = _store.GetState(companion);
            if (state.Mode == CompanionMode.StandGround &&
                engagementType != CompanionEngagementType.AttackNearest)
            {
                return CanEngageWithoutMoving(companion, target);
            }

            var tether = CompanionControlPolicy.GetTetherMeters(state.Mode, engagementType);
            if (GetArea(master) != GetArea(target) || GetDistanceBetween(master, target) > tether)
                return false;

            return true;
        }

        private bool TrackProgress(
            uint companion,
            uint target,
            CompanionEngagementType engagementType)
        {
            var state = _store.GetState(companion);
            if (state.TrackedTarget != target)
                return true;

            var distance = GetDistanceBetween(companion, target);
            var offensiveActivityAt = GetLastOffensiveActivityAt(companion);
            var hasNewOffensiveActivity = offensiveActivityAt > state.LastOffensiveActivityAt;

            if (CompanionControlPolicy.HasCombatProgress(
                    hasNewOffensiveActivity,
                    state.LastDistanceToTarget,
                    distance))
            {
                state.LastProgressAt = UtcNow();
                state.LastDistanceToTarget = distance;
                state.LastOffensiveActivityAt = offensiveActivityAt;
                return true;
            }

            if (!CompanionControlPolicy.HasPathingTimedOut(state.LastProgressAt, UtcNow()))
                return true;

            var returnsToFollow = CompanionControlPolicy.ReturnsToFollowWhenComplete(engagementType);
            Log.Write(
                LogGroup.AI,
                returnsToFollow
                    ? $"{GetName(companion)} timed out pathing to {GetName(target)} and returned to Follow."
                    : $"{GetName(companion)} timed out pathing to {GetName(target)} and retained {state.Mode} mode.");
            _positioning.SendResponse(
                companion,
                returnsToFollow
                    ? "Unable to reach the target; returning to Follow."
                    : $"Unable to reach the target; remaining in {state.Mode}.");
            return false;
        }

        private void CompleteAttackNearest(uint companion)
        {
            var state = _store.GetState(companion);
            state.Mode = CompanionMode.Follow;
            state.AttackNearestTarget = OBJECT_INVALID;
            state.OwnerAssistTarget = OBJECT_INVALID;
            _positioning.ResetProgress(state);
            Log.Write(LogGroup.AI, $"{GetName(companion)} completed Attack Nearest and returned to Follow.");
            _positioning.MaintainModePosition(companion);
        }

        public void StartTracking(CompanionControlState state, uint companion, uint target)
        {
            state.TrackedTarget = target;
            state.LastDistanceToTarget = GetDistanceBetween(companion, target);
            state.LastProgressAt = UtcNow();
            state.LastOffensiveActivityAt = GetLastOffensiveActivityAt(companion);
        }

        private bool CanEngageWithoutMoving(uint companion, uint target)
        {
            return IsWithinWeaponRange(companion, target) ||
                   CanUseHostileAbilityWithoutMoving(companion, target);
        }

        private bool CanUseHostileAbilityWithoutMoving(uint companion, uint target)
        {
            if (!_store.AreAbilitiesEnabled(companion))
                return false;

            foreach (var (feat, ability) in GetCachedHostileAbilities(companion))
            {
                if (ability.ActivationType == AbilityActivationType.Weapon)
                    continue;

                if (ability.IsAreaAbility && ability.Targeting == null)
                    continue;

                var isSelfCenteredArea = ability.IsAreaAbility &&
                                         ability.Targeting?.Shape == AbilityTargetingShapeType.Sphere &&
                                         ability.Targeting.Flags.HasFlag(AbilityTargetingFlags.OriginOnSelf);
                var isSelfOriginArea = ability.IsAreaAbility &&
                                       ability.Targeting.Flags.HasFlag(AbilityTargetingFlags.OriginOnSelf);
                var targetDistance = GetDistanceBetween(companion, target);
                if (ability.IsAreaAbility &&
                    !isSelfOriginArea &&
                    !CompanionControlPolicy.IsWithinDeclaredPlacementRange(
                        ability.HasExplicitMaxRange,
                        targetDistance,
                        ability.MaxRange))
                {
                    continue;
                }

                if (isSelfOriginArea &&
                    !CompanionControlPolicy.IsWithinSelfOriginAreaReach(
                        ability.Targeting.Shape,
                        true,
                        targetDistance,
                        ability.Targeting.ResolveSizeX(companion, true)))
                {
                    continue;
                }

                var abilityTarget = ResolveHostileAbilityTarget(
                    companion,
                    ability,
                    target,
                    isSelfCenteredArea ? companion : target);
                var targetLocation = GetLocation(
                    isSelfCenteredArea
                        ? companion
                        : target);
                var effectiveLevel = ability.EffectiveLevelPerkType == PerkType.Invalid
                    ? 1
                    : GetPerkLevel(companion, ability.EffectiveLevelPerkType);

                if (CanUseAbility(companion, abilityTarget, feat, effectiveLevel, targetLocation))
                    return true;
            }

            return false;
        }

        private IReadOnlyList<(FeatType Feat, AbilityDetail Ability)> GetCachedHostileAbilities(uint companion)
        {
            var state = _store.GetState(companion);
            var featCount = CreaturePlugin.GetFeatCount(companion);
            var featChecksum = 17;
            var knownFeats = new List<FeatType>(featCount);

            unchecked
            {
                for (var index = 0; index < featCount; index++)
                {
                    var feat = CreaturePlugin.GetFeatByIndex(companion, index);
                    knownFeats.Add(feat);
                    featChecksum = featChecksum * 31 + (int)feat;
                }
            }

            if (state.HostileAbilityFeatCount == featCount &&
                state.HostileAbilityFeatChecksum == featChecksum)
            {
                return state.HostileAbilities;
            }

            state.HostileAbilityFeatCount = featCount;
            state.HostileAbilityFeatChecksum = featChecksum;
            state.HostileAbilities.Clear();

            foreach (var feat in knownFeats)
            {
                if (!IsFeatRegistered(feat))
                    continue;

                var ability = GetAbilityDetail(feat);
                if (ability.IsHostileAbility)
                    state.HostileAbilities.Add((feat, ability));
            }

            return state.HostileAbilities;
        }

        private bool IsWithinWeaponRange(uint companion, uint target)
        {
            var skill = GetEquippedWeaponSkillType(companion);
            return GetDistanceBetween(companion, target) <=
                   GetWeaponEngagementRange(skill) + AttackRangeToleranceMeters;
        }

        public bool IsValidHostileTarget(uint companion, uint target)
        {
            return GetIsObjectValid(target) &&
                   target != companion &&
                   GetCurrentHitPoints(target) > 0 &&
                   GetArea(companion) == GetArea(target) &&
                   GetIsReactionTypeHostile(target, companion);
        }

        // Each protected virtual method below has the original call as its body, so the server behaves as it did.
        // They name the static services that are not converted yet (and the clock), so a test can answer them.

        protected virtual DateTime UtcNow() => DateTime.UtcNow;

        protected virtual DateTime GetLastOffensiveActivityAt(uint creature) =>
            _combatActivity.GetLastOffensiveActivityAt(creature);

        protected virtual SkillType GetEquippedWeaponSkillType(uint creature) =>
            _queries.GetEquippedWeaponSkillType(creature);

        protected virtual float GetWeaponEngagementRange(SkillType skill) =>
            CombatRules.GetWeaponEngagementRange(skill);

        protected virtual int GetPerkLevel(uint creature, PerkType perkType) =>
            _perk.GetPerkLevel(creature, perkType);

        protected virtual bool IsFeatRegistered(FeatType feat) => _catalog.IsFeatRegistered(feat);

        protected virtual AbilityDetail GetAbilityDetail(FeatType feat) => _catalog.GetAbilityDetail(feat);

        protected virtual bool CanUseAbility(
            uint activator,
            uint target,
            FeatType feat,
            int effectiveLevel,
            Location targetLocation) =>
            _activation.CanUseAbility(activator, target, feat, effectiveLevel, targetLocation);
    }
}
