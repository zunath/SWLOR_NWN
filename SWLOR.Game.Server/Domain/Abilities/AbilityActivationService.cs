using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The activation checks (part 6 of the ability split, Readmes/MaintainabilityRefactorAbilitySplit.md), moved from
    /// <c>AbilityService</c> unchanged apart from one split: <see cref="CanUseAbility"/> was one ordered chain of denial checks
    /// over the method size limit, so the checks of the activator's state and readiness are methods here that return the
    /// denial (or empty), the checks of the target are part 7 (<see cref="IAbilityTargetCheckService"/>), and
    /// <see cref="CanUseAbility"/> runs them in the original order, so the first failing check and its message do not change.
    /// The space mode read goes through <see cref="ISpaceModeQuery"/>, which Space implements. Calls into other services and
    /// into <c>TwinPistolEquipmentValidation</c> sit behind <c>protected virtual</c> methods whose bodies are the original
    /// calls, so a unit test can answer them.
    /// </summary>
    public class AbilityActivationService : IAbilityActivationService
    {
        private readonly IAbilityCatalog _catalog;
        private readonly ISpaceModeQuery _spaceMode;
        private readonly IPerkGrantedFeatService _perkGrantedFeat;
        private readonly IMimicryService _mimicry;
        private readonly IActivityService _activity;
        private readonly IStatService _stat;
        private readonly ICombatQueriesService _queries;
        private readonly IAbilityTargetCheckService _targetCheck;

        public AbilityActivationService(
            IAbilityCatalog catalog,
            ISpaceModeQuery spaceMode,
            IPerkGrantedFeatService perkGrantedFeat,
            IMimicryService mimicry,
            IActivityService activity,
            IStatService stat,
            ICombatQueriesService queries,
            IAbilityTargetCheckService targetCheck)
        {
            _catalog = catalog;
            _spaceMode = spaceMode;
            _perkGrantedFeat = perkGrantedFeat;
            _mimicry = mimicry;
            _activity = activity;
            _stat = stat;
            _queries = queries;
            _targetCheck = targetCheck;
        }

        [ReloadState(ReloadStateKind.Runtime)]
        private string _lastActivationDenial = string.Empty;

        /// <summary>
        /// The reason the most recent CanUseAbility call returned false. Player-facing feedback
        /// goes through SendMessageToPC, which is invisible for NPC activators - the in-engine
        /// test harness reads this to report WHY an activation was rejected. Cleared at the
        /// start of every check.
        /// </summary>
        public string GetLastActivationDenialReason()
        {
            return _lastActivationDenial;
        }

        /// <summary>
        /// Checks whether a creature can activate the perk feat.
        /// </summary>
        /// <param name="activator">The activator of the perk feat.</param>
        /// <param name="target">The target of the perk feat.</param>
        /// <param name="abilityType">The type of ability to use.</param>
        /// <param name="effectivePerkLevel">The activator's effective perk level.</param>
        /// <param name="targetLocation">The target location of the perk feat.</param>
        /// <returns>true if successful, false otherwise</returns>
        public bool CanUseAbility(
            uint activator,
            uint target,
            FeatType abilityType,
            int effectivePerkLevel,
            Location targetLocation)
        {
            var ability = _catalog.GetAbilityDetail(abilityType);

            _lastActivationDenial = string.Empty;

            bool Deny(string reason)
            {
                _lastActivationDenial = reason;
                SendMessageToPC(activator, reason);
                return false;
            }

            // The checks in their original order: each returns its denial, or empty when the activation passes it.
            var denial = ValidateActivatorState(activator, abilityType, effectivePerkLevel, ability);
            if (string.IsNullOrEmpty(denial))
                denial = _targetCheck.ValidateLineOfSight(activator, target, ability);
            if (string.IsNullOrEmpty(denial))
                denial = ValidateActivatorReadiness(activator, ability);
            if (string.IsNullOrEmpty(denial))
                denial = _targetCheck.ValidateTarget(activator, target, ability, effectivePerkLevel, targetLocation);

            return string.IsNullOrEmpty(denial) || Deny(denial);
        }

        /// <summary>
        /// The checks of the activator that come before line of sight: space, the perk level, a replaced rank, the Mimicry
        /// loadout, death and whether it can act. Returns the denial of the first that fails, or empty.
        /// </summary>
        private string ValidateActivatorState(
            uint activator,
            FeatType abilityType,
            int effectivePerkLevel,
            AbilityDetail ability)
        {
            // Cannot use this ability in space.
            if (IsPlayerInSpaceMode(activator) &&
                !ability.CanBeUsedInSpace)
            {
                return "This ability cannot be used in space.";
            }

            // Must have appropriate levels in the perk to use the ability.
            if (effectivePerkLevel <= 0 || ability.AbilityLevel > effectivePerkLevel)
            {
                return "You do not meet the prerequisites to use this ability.";
            }

            if (ShouldEnforceActiveAbilityFeatReplacement(activator, ability.EffectiveLevelPerkType) &&
                !IsCurrentActiveAbilityFeat(abilityType, ability.EffectiveLevelPerkType, effectivePerkLevel))
            {
                return "A newer rank has replaced this ability.";
            }

            // Mimicry techniques are granted through the equipped technique loadout rather than
            // directly by Combat Analyzer. Re-check the loadout at both activation gates so a stale
            // feat, or a technique unequipped while its cast is winding up, cannot still resolve.
            if (ability.IsMimicryTechnique &&
                !_mimicry.IsTechniqueEquipped(activator, abilityType))
            {
                return "That technique is not equipped.";
            }

            // Activator is dead.
            if (GetCurrentHitPoints(activator) <= 0)
            {
                return "You are dead.";
            }

            // Not commandable
            if (!GetCommandable(activator))
            {
                return "You cannot take actions at this time.";
            }

            return string.Empty;
        }

        /// <summary>
        /// The checks of the activator that come after line of sight: busy, an equipped weapon, usable equipment and whether
        /// Force abilities are disabled. Returns the denial of the first that fails, or empty.
        /// </summary>
        private string ValidateActivatorReadiness(uint activator, AbilityDetail ability)
        {
            // Must not be busy
            if (_activity.IsBusy(activator))
            {
                return "You are busy.";
            }

            if (ability.ActivationType == AbilityActivationType.Weapon &&
                IsWeaponSkillType(ability.SkillType) &&
                !HasEquippedWeaponForAbility(activator))
            {
                return "You must equip a weapon to use this ability.";
            }

            if (ability.ActivationType == AbilityActivationType.Weapon ||
                ability.IsHostileAbility && IsWeaponSkillType(ability.SkillType))
            {
                var equipmentError = GetUnusableEquipmentError(activator);
                if (!string.IsNullOrEmpty(equipmentError)) return equipmentError;
            }

            if (IsForcePower(activator, ability) &&
                GetStatAdjustment(activator, StatType.ForceAbilityActivationDisabled) > 0)
            {
                return "You cannot use Force abilities right now.";
            }

            return string.Empty;
        }

        protected virtual string GetUnusableEquipmentError(uint creature) => TwinPistolEquipmentValidation.GetUnusableEquipmentError(creature);

        protected virtual bool IsPlayerInSpaceMode(uint player) => _spaceMode.IsPlayerInSpaceMode(player);

        protected virtual int GetStatAdjustment(uint creature, StatType stat) => _stat.GetStatAdjustment(creature, stat);

        protected virtual bool ShouldEnforceActiveAbilityFeatReplacement(uint creature, PerkType perkType) => _perkGrantedFeat.ShouldEnforceActiveAbilityFeatReplacement(creature, perkType);

        protected virtual bool IsCurrentActiveAbilityFeat(FeatType feat, PerkType perkType, int perkLevel) => _perkGrantedFeat.IsCurrentActiveAbilityFeat(feat, perkType, perkLevel);

        protected virtual bool IsForcePower(uint creature, AbilityDetail ability) => _queries.IsForcePower(creature, ability);

        protected virtual bool IsWeaponSkillType(SkillType skillType) => CombatRules.IsWeaponSkillType(skillType);

        protected virtual bool HasEquippedWeaponForAbility(uint creature) => _queries.HasEquippedWeaponForAbility(creature);
    }
}
