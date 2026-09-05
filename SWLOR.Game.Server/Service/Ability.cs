using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.TelegraphService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using NumericsVector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Service
{
    public static class Ability
    {
        private static readonly Dictionary<FeatType, AbilityDetail> _abilities = new();
        private static readonly Dictionary<uint, PlayerAura> _playerAuras = new();
        private static readonly Dictionary<uint, TrackedAbilityImpact> _trackedAbilityImpacts = new();

        /// <summary>
        /// How long the visual-only impact flash lingers on an instant area ability, in seconds.
        /// Long enough to read the shape, short enough not to be mistaken for a pre-cast telegraph.
        /// </summary>
        public const float DefaultImpactFlashDuration = 0.3f;

        private const int MaxNumberOfAuras = 4;
        private const int HostileAbilityBaseEnmity = 100;
        private const int HostileAbilityMissEnmity = 1;
        private const int MinNPCAbilityScalingRank = 1;
        private const int MaxNPCAbilityScalingRank = 100;

        /// <summary>
        /// When the module caches, abilities will be cached and events will be scheduled.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            CacheAbilities();
        }

        private static void CacheAbilities()
        {
            _abilities.Clear();
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IAbilityListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IAbilityListDefinition)Activator.CreateInstance(type);
                var abilities = instance.BuildAbilities();

                foreach (var (feat, ability) in abilities)
                {
                    _abilities[feat] = ability;
                }
            }

            Console.WriteLine($"Loaded {_abilities.Count} abilities.");
            AbilityTargeting.CacheData(_abilities);
        }

        public static IReadOnlyDictionary<FeatType, AbilityDetail> GetAllAbilityDetails()
        {
            return _abilities;
        }

        /// <summary>
        /// Returns true if a feat is registered to an ability.
        /// Returns false otherwise.
        /// </summary>
        /// <param name="featType">The type of feat to check.</param>
        /// <returns>true if feat is registered to an ability. false otherwise.</returns>
        public static bool IsFeatRegistered(FeatType featType)
        {
            return _abilities.ContainsKey(featType);
        }

        /// <summary>
        /// Retrieves an ability's details by the specified feat type.
        /// If feat does not have an ability, an exception will be thrown.
        /// </summary>
        /// <param name="featType">The type of feat</param>
        /// <returns>The ability detail</returns>
        public static AbilityDetail GetAbilityDetail(FeatType featType)
        {
            if (!_abilities.ContainsKey(featType))
                throw new KeyNotFoundException($"Feat '{featType}' is not registered to an ability.");

            return _abilities[featType];
        }

        /// <summary>
        /// Consumes the activator's pending combat bonuses and starts impact tracking,
        /// retaining any activation-marker snapshots for the impact flash decision.
        /// </summary>
        public static void BeginAbilityImpact(
            uint activator,
            AbilityDetail ability,
            bool countsAsAttackAttempt = true,
            IReadOnlyList<TelegraphGeometry> activationAreaTelegraphs = null,
            AbilityImpactSequence sequence = null)
        {
            if (!GetIsObjectValid(activator) || ability == null)
                return;

            var abilitySkillType = Combat.GetAbilitySkillType(activator, ability);
            var nextAbilityDamageBonus = Combat.ConsumeNextAbilityDamageBonus(activator, ability.EffectiveLevelPerkType);
            var nextSkillAbilityBonuses = Combat.ConsumeNextSkillAbilityBonuses(activator, abilitySkillType);
            var persistentCriticalRate = Combat.GetPersistentNextSkillAbilityCriticalRateBonus(
                activator,
                abilitySkillType);
            var queuedWeaponBonuses = ability.ActivationType == AbilityActivationType.Weapon
                ? Combat.ConsumeQueuedWeaponAbilityBonuses(activator, abilitySkillType)
                : default;
            var guardedHitBonuses = ability.IsHostileAbility
                ? Combat.ConsumeNextAttackGuardedHitBonuses(activator)
                : (DMGBonus: 0, CriticalRatePercentAdjustment: 0, EnmityBonus: 0);
            var statusAppliedNextAttackDamageBonus = ability.IsHostileAbility
                ? Combat.GetStatusAppliedNextAttackDamageBonus(activator)
                : 0;
            BeginAbilityImpact(
                activator,
                ability,
                nextAbilityDamageBonus +
                nextSkillAbilityBonuses.DamageBonus +
                guardedHitBonuses.DMGBonus +
                statusAppliedNextAttackDamageBonus +
                queuedWeaponBonuses.DamageBonus,
                nextSkillAbilityBonuses.CriticalRatePercentAdjustment +
                persistentCriticalRate +
                queuedWeaponBonuses.CriticalRatePercentAdjustment +
                guardedHitBonuses.CriticalRatePercentAdjustment,
                nextSkillAbilityBonuses.DefenseIgnorePercentAdjustment,
                guardedHitBonuses.EnmityBonus,
                statusAppliedNextAttackDamageBonus,
                countsAsAttackAttempt,
                queuedWeaponBonuses.CriticalDamagePercentAdjustment,
                activationAreaTelegraphs,
                sequence);
        }

        /// <summary>
        /// Registers an impact using already-resolved bonuses and activation-marker geometry.
        /// </summary>
        private static void BeginAbilityImpact(
            uint activator,
            AbilityDetail ability,
            int nextAbilityDamageBonus,
            int nextAbilityCriticalRatePercentAdjustment,
            int nextAbilityDefenseIgnorePercentAdjustment = 0,
            int nextAttackEnmityBonus = 0,
            int statusAppliedNextAttackDamageBonus = 0,
            bool countsAsAttackAttempt = true,
            int nextAbilityCriticalDamagePercentAdjustment = 0,
            IReadOnlyList<TelegraphGeometry> activationAreaTelegraphs = null,
            AbilityImpactSequence sequence = null)
        {
            if (!GetIsObjectValid(activator) || ability == null)
                return;

            _trackedAbilityImpacts[activator] = new TrackedAbilityImpact(
                ability,
                nextAbilityDamageBonus,
                nextAbilityCriticalRatePercentAdjustment,
                nextAbilityDefenseIgnorePercentAdjustment,
                nextAttackEnmityBonus,
                statusAppliedNextAttackDamageBonus,
                countsAsAttackAttempt,
                nextAbilityCriticalDamagePercentAdjustment,
                activationAreaTelegraphs,
                sequence ?? new AbilityImpactSequence());
        }

        public static AbilityImpactSequence GetAbilityImpactSequence(uint activator)
        {
            return GetTrackedAbilityImpact(activator)?.Sequence;
        }

        public static bool TryTriggerAreaAbilityPulse(uint activator)
        {
            return GetAbilityImpactSequence(activator)?.TryTriggerAreaPulse() == true;
        }

        public static AbilityImpactSummary EndAbilityImpact(uint activator)
        {
            if (!_trackedAbilityImpacts.TryGetValue(activator, out var impact))
                return new AbilityImpactSummary();

            _trackedAbilityImpacts.Remove(activator);
            impact.FlushDamageEffects(activator);
            if (impact.Ability.IsHostileAbility && impact.Summary.CriticalHitCount > 0)
            {
                Combat.RefundCriticalRangedAbilityStaminaCost(
                    activator,
                    impact.Ability);
            }

            if (impact.Ability.IsHostileAbility && impact.CountsAsAttackAttempt)
            {
                if (impact.Summary.CriticalHitCount > 0)
                {
                    Combat.ConsumePersistentNextSkillAbilityCriticalRateBonus(
                        activator,
                        impact.Summary.SkillType);
                }
                else
                {
                    Combat.ApplyNonCriticalAbilityEffects(
                        activator,
                        OBJECT_INVALID,
                        impact.Summary.SkillType);
                }

                StatusEffect.NotifyAttackAttemptStatusEffects(
                    activator,
                    impact.Summary.SkillType,
                    impact.Summary);
            }
            _lastCompletedImpactSummaries[activator] = impact.Summary;
            return impact.Summary;
        }

        private static readonly Dictionary<uint, AbilityImpactSummary> _lastCompletedImpactSummaries = new();

        /// <summary>
        /// The summary of the activator's most recently COMPLETED ability impact, or null if
        /// none completed since the last clear. Observability seam for the engine test
        /// harness: a queued weapon ability's damage rides the same landed hit as the weapon
        /// swing, so an HP drop alone cannot attribute damage to the ability - a completed
        /// summary with impacted targets can.
        /// </summary>
        public static AbilityImpactSummary GetLastCompletedAbilityImpactSummary(uint activator)
        {
            return _lastCompletedImpactSummaries.TryGetValue(activator, out var summary)
                ? summary
                : null;
        }

        /// <summary>
        /// Clears the activator's last completed impact summary so a subsequent
        /// <see cref="GetLastCompletedAbilityImpactSummary"/> observation cannot match an
        /// earlier ability's impact.
        /// </summary>
        public static void ClearLastCompletedAbilityImpactSummary(uint activator)
        {
            _lastCompletedImpactSummaries.Remove(activator);
        }

        /// <summary>
        /// Discards an incomplete tracked impact without applying its queued damage effects.
        /// </summary>
        public static void AbortAbilityImpact(uint activator)
        {
            if (!_trackedAbilityImpacts.TryGetValue(activator, out var impact))
                return;

            _trackedAbilityImpacts.Remove(activator);
            Log.WriteStructured(
                LogGroup.Error,
                "Ability impact aborted for activator {Activator}. Ability: {Ability}.",
                activator,
                impact.Ability?.Name ?? "Unknown");
        }

        public static bool TryQueueTrackedDamageEffect(uint activator, uint target, int damage, DamageType damageType)
        {
            var trackedImpact = GetTrackedAbilityImpact(activator);
            if (trackedImpact == null)
                return false;

            trackedImpact.QueueDamageEffect(target, damage, damageType);
            return true;
        }

        public static AbilityImpactSummary GetActiveAbilityImpactSummary(uint activator)
        {
            return GetTrackedAbilityImpact(activator)?.Summary;
        }

        public static void AddActiveAbilityDefenseIgnorePercentAdjustment(uint activator, int adjustment)
        {
            if (adjustment == 0)
                return;

            GetTrackedAbilityImpact(activator)?.AddDefenseIgnorePercentAdjustment(adjustment);
        }

        private static TrackedAbilityImpact GetTrackedAbilityImpact(uint activator)
        {
            return _trackedAbilityImpacts.TryGetValue(activator, out var impact)
                ? impact
                : null;
        }

        public static float GetActiveForceAffinityMagnitudeMultiplier(uint activator)
        {
            var trackedImpact = GetTrackedAbilityImpact(activator);
            if (trackedImpact?.Ability?.SkillType != SkillType.Force)
                return 1f;

            return Perk.GetForceAffinityMagnitudeMultiplier(
                activator,
                trackedImpact.Ability.EffectiveLevelPerkType);
        }

        public static int ApplyActiveForceAffinityMagnitude(uint activator, int amount)
        {
            var trackedImpact = GetTrackedAbilityImpact(activator);
            if (trackedImpact?.Ability?.SkillType != SkillType.Force)
                return amount;

            return Perk.ApplyForceAffinityMagnitude(
                activator,
                trackedImpact.Ability.EffectiveLevelPerkType,
                amount);
        }

        public static float ApplyActiveForceAffinityMagnitude(uint activator, float amount)
        {
            var trackedImpact = GetTrackedAbilityImpact(activator);
            if (trackedImpact?.Ability?.SkillType != SkillType.Force)
                return amount;

            return Perk.ApplyForceAffinityMagnitude(
                activator,
                trackedImpact.Ability.EffectiveLevelPerkType,
                amount);
        }

        public static int ApplyCombatReadinessToActivatedAbilityMagnitude(uint activator, int amount)
        {
            var trackedImpact = GetTrackedAbilityImpact(activator);
            if (trackedImpact == null)
                return amount;

            return ApplyCombatReadinessMagnitude(activator, amount);
        }

        public static int ApplyCombatReadinessMagnitude(uint creature, int amount)
        {
            if (amount <= 0)
                return amount;

            var combatReadiness = Stat.GetCombatReadinessPercent(creature);
            if (combatReadiness > 0)
            {
                amount += (int)Math.Ceiling(amount * (combatReadiness / 100f));
            }

            return amount;
        }

        public static void ApplyHostileAbilityEnmity(uint activator, uint target, int damage = 0)
        {
            var amount = HostileAbilityBaseEnmity + Math.Max(0, damage);
            Enmity.ModifyEnmity(activator, target, amount);
        }

        private static void ApplyMissedHostileAbilityEnmity(uint activator, uint target)
        {
            Enmity.ModifyEnmity(activator, target, HostileAbilityMissEnmity);
        }

        private static void RecordAbilityImpactShape(uint activator, SkillType skillType, bool isArea)
        {
            var impact = GetTrackedAbilityImpact(activator);
            if (impact == null)
                return;

            impact.RecordShape(skillType, isArea);
        }

        private static void RecordAbilityImpactTarget(uint activator, uint target, SkillType skillType, bool isArea)
        {
            var impact = GetTrackedAbilityImpact(activator);
            if (impact == null || !GetIsObjectValid(target))
                return;

            impact.RecordShape(skillType, isArea);
            impact.RecordTarget(target);
        }

        private static bool IsTrackedAbilityArea(uint activator)
        {
            var impact = GetTrackedAbilityImpact(activator);
            return impact?.Summary.IsAreaAbility ?? false;
        }

        private static bool IsTrackedAbilitySingleTarget(uint activator)
        {
            var impact = GetTrackedAbilityImpact(activator);
            return impact?.Summary.IsSingleTargetAbility ?? false;
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
        private static string _lastActivationDenial = string.Empty;

        /// <summary>
        /// The reason the most recent CanUseAbility call returned false. Player-facing feedback
        /// goes through SendMessageToPC, which is invisible for NPC activators - the in-engine
        /// test harness reads this to report WHY an activation was rejected. Cleared at the
        /// start of every check.
        /// </summary>
        public static string GetLastActivationDenialReason()
        {
            return _lastActivationDenial;
        }

        public static bool CanUseAbility(
            uint activator,
            uint target,
            FeatType abilityType,
            int effectivePerkLevel,
            Location targetLocation)
        {
            var ability = GetAbilityDetail(abilityType);

            _lastActivationDenial = string.Empty;

            bool Deny(string reason)
            {
                _lastActivationDenial = reason;
                SendMessageToPC(activator, reason);
                return false;
            }

            // Cannot use this ability in space.
            if (Space.IsPlayerInSpaceMode(activator) &&
                !ability.CanBeUsedInSpace)
            {
                return Deny("This ability cannot be used in space.");
            }

            // Must have appropriate levels in the perk to use the ability.
            if (effectivePerkLevel <= 0 || ability.AbilityLevel > effectivePerkLevel)
            {
                return Deny("You do not meet the prerequisites to use this ability.");
            }

            if (Perk.ShouldEnforceActiveAbilityFeatReplacement(activator, ability.EffectiveLevelPerkType) &&
                !Perk.IsCurrentActiveAbilityFeat(abilityType, ability.EffectiveLevelPerkType, effectivePerkLevel))
            {
                return Deny("A newer rank has replaced this ability.");
            }

            // Mimicry techniques are granted through the equipped technique loadout rather than
            // directly by Combat Analyzer. Re-check the loadout at both activation gates so a stale
            // feat, or a technique unequipped while its cast is winding up, cannot still resolve.
            if (ability.IsMimicryTechnique &&
                !Mimicry.IsTechniqueEquipped(activator, abilityType))
            {
                return Deny("That technique is not equipped.");
            }

            // Activator is dead.
            if (GetCurrentHitPoints(activator) <= 0)
            {
                return Deny("You are dead.");
            }

            // Not commandable
            if (!GetCommandable(activator))
            {
                return Deny("You cannot take actions at this time.");
            }

            // Must be within line of sight.
            if (ability.RequiresTarget &&
                GetIsObjectValid(target) &&
                !HasAbilityLineOfSight(activator, target))
            {
                return Deny("You cannot see your target.");
            }

            // Must not be busy
            if (Activity.IsBusy(activator))
            {
                return Deny("You are busy.");
            }

            if (ability.ActivationType == AbilityActivationType.Weapon &&
                Combat.IsWeaponSkillType(ability.SkillType) &&
                !Combat.HasEquippedWeaponForAbilitySkill(activator, ability.SkillType))
            {
                var skillName = Skill.GetSkillDetails(ability.SkillType).Name;
                return Deny($"You must equip a {skillName} weapon to use this ability.");
            }

            if (Combat.GetAbilitySkillType(activator, ability) == SkillType.Force &&
                Stat.GetStatAdjustment(activator, StatType.ForceAbilityActivationDisabled) > 0)
            {
                return Deny("You cannot use Force abilities right now.");
            }

            // Target check.
            if (ability.RequiresTarget && !GetIsObjectValid(target))
            {
                return Deny("A target is required.");
            }

            // Aimed areas use the feat's location cursor. They must not use RequiresTarget,
            // because empty-ground casts have no target object and object range/hostility checks
            // do not describe a selected location or direction.
            if (ability.RequiresLocationTarget)
            {
                var targetArea = GetAreaFromLocation(targetLocation);
                if (!GetIsObjectValid(targetArea) || targetArea != GetArea(activator))
                {
                    return Deny("A target location in your current area is required.");
                }

                if (ability.HasExplicitMaxRange &&
                    GetDistanceBetweenLocations(GetLocation(activator), targetLocation) > ability.MaxRange)
                {
                    return Deny("You are out of range.  This ability has a range of " + ability.MaxRange + " meters.");
                }
            }

            // Object range check. Location-targeted areas are validated separately above.
            if (ability.RequiresTarget &&
                GetIsObjectValid(target) &&
                GetDistanceBetween(activator, target) > ability.MaxRange)
            {
                return Deny("You are out of range.  This ability has a range of " + ability.MaxRange + " meters.");
            }

            // Hostility check
            if (ability.RequiresTarget &&
                GetIsObjectValid(target) &&
                !GetIsReactionTypeHostile(target, activator) &&
                ability.IsHostileAbility)
            {
                return Deny("You may only use this ability on enemies.");
            }

            // Perk-specific requirement checks
            foreach (var req in ability.Requirements)
            {
                var requirementError = req.CheckRequirements(activator, ability);
                if (!string.IsNullOrWhiteSpace(requirementError))
                {
                    return Deny(requirementError);
                }
            }

            // Perk-specific custom validation logic.
            var customValidationResult = ability.CustomValidation == null ? string.Empty : ability.CustomValidation(activator, target, effectivePerkLevel, targetLocation);
            if (!string.IsNullOrWhiteSpace(customValidationResult))
            {
                return Deny(customValidationResult);
            }

            var areaLineOfSightError = ValidateHostileAreaLineOfSight(activator, target, targetLocation, ability);
            if (!string.IsNullOrWhiteSpace(areaLineOfSightError))
            {
                return Deny(areaLineOfSightError);
            }

            // Check if ability is on a recast timer still.
            var (isOnRecast, timeToWait) = Recast.IsOnRecastDelay(activator, ability.RecastGroup);
            if (isOnRecast)
            {
                return Deny($"This ability can be used in {timeToWait}.");
            }

            return true;
        }

        private static bool HasAbilityLineOfSight(uint activator, uint target)
        {
            return LineOfSightObject(activator, target) &&
                   LineOfSightVector(GetPosition(activator), GetPosition(target));
        }

        private static string ValidateHostileAreaLineOfSight(
            uint activator,
            uint target,
            Location targetLocation,
            AbilityDetail ability)
        {
            var targeting = ability.Targeting;
            if (targeting == null ||
                !targeting.Flags.HasFlag(AbilityTargetingFlags.HarmsEnemies) ||
                !TryGetCombatImpactShape(targeting.Shape, out var shape))
            {
                return string.Empty;
            }

            var creatures = GetHostileCreaturesInCombatImpactShape(
                    activator,
                    target,
                    targetLocation,
                    shape,
                    targeting.ResolveSizeX(activator, true),
                    targeting.ResolveSizeY(),
                    targeting.Flags.HasFlag(AbilityTargetingFlags.OriginOnSelf),
                    targeting.Flags.HasFlag(AbilityTargetingFlags.BackOffsetOrigin))
                .ToList();

            if (creatures.Count <= 0 ||
                creatures.Any(creature => HasAbilityLineOfSight(activator, creature)))
            {
                return string.Empty;
            }

            return "You cannot see any enemies in the target area.";
        }

        private static bool TryGetCombatImpactShape(AbilityTargetingShapeType targetingShape, out CombatImpactAreaShape shape)
        {
            switch (targetingShape)
            {
                case AbilityTargetingShapeType.Sphere:
                case AbilityTargetingShapeType.HSphere:
                    shape = CombatImpactAreaShape.Sphere;
                    return true;
                case AbilityTargetingShapeType.Rect:
                    shape = CombatImpactAreaShape.Line;
                    return true;
                case AbilityTargetingShapeType.Cone:
                    shape = CombatImpactAreaShape.Cone;
                    return true;
                case AbilityTargetingShapeType.None:
                default:
                    shape = default;
                    return false;
            }
        }

        /// <summary>
        /// Whenever a weapon's OnHit event is fired, add a Leadership combat point if an Aura is active.
        /// </summary>
        [NWNEventHandler(ScriptName.OnItemHit)]
        public static void AddLeadershipCombatPoint()
        {
            var player = OBJECT_SELF;
            var target = GetSpellTargetObject();
            if (!GetIsPC(player) || GetIsDM(player) || !GetIsObjectValid(player))
                return;

            if (GetIsPC(target) || GetIsDM(target))
                return;

            if (!_playerAuras.ContainsKey(player))
                return;

            var aura = _playerAuras[player];

            if (aura.Auras.Count <= 0)
                return;

            CombatPoint.AddCombatPoint(player, target, SkillType.Leadership);
        }

        private static int GetMaxNumberOfAuras(uint activator)
        {
            var social = GetAbilityScore(activator, AbilityType.Social);
            var count = 1 + (social - 10) / 5;

            if (count < 1)
                count = 1;

            if (count > MaxNumberOfAuras)
                count = MaxNumberOfAuras;

            return count;
        }

        private static void ApplyAuraEffect(uint source, uint recipient, Type type)
        {
            if (StatusEffect.HasStatusEffect(recipient, type, source) ||
                HasEqualOrStrongerAuraEffect(source, recipient, type))
            {
                return;
            }

            RemoveWeakerDuplicateAuraEffects(source, recipient, type);
            StatusEffect.ApplyStatusEffect(source, recipient, type, 0f);
        }

        private static void RemoveAuraEffect(uint source, uint recipient, Type type, bool sendsWornOffMessage = false)
        {
            StatusEffect.RemoveStatusEffect(recipient, type, source, sendsWornOffMessage);
        }

        private static bool HasEqualOrStrongerAuraEffect(uint source, uint recipient, Type type)
        {
            var sourceSocial = GetAuraSourceSocial(source);
            return StatusEffect.GetCreatureStatusEffects(recipient)
                .GetAllEffects()
                .Any(effect =>
                    effect.GetType() == type &&
                    effect.Source != source &&
                    GetAuraSourceSocial(effect.Source) >= sourceSocial);
        }

        private static void RemoveWeakerDuplicateAuraEffects(uint source, uint recipient, Type type)
        {
            var sourceSocial = GetAuraSourceSocial(source);
            var weakerEffects = StatusEffect.GetCreatureStatusEffects(recipient)
                .GetAllEffects()
                .Where(effect =>
                    effect.GetType() == type &&
                    effect.Source != source &&
                    GetAuraSourceSocial(effect.Source) < sourceSocial)
                .ToList();

            foreach (var weakerEffect in weakerEffects)
            {
                StatusEffect.RemoveStatusEffect(recipient, type, weakerEffect.Source, false);
            }
        }

        private static int GetAuraSourceSocial(uint source)
        {
            return GetIsObjectValid(source)
                ? GetAbilityScore(source, AbilityType.Social)
                : 0;
        }

        public static void ApplyAura(uint activator, Type type, bool targetsSelf, bool targetsParty, bool targetsEnemies)
        {
            if (!_playerAuras.ContainsKey(activator))
                _playerAuras.Add(activator, new PlayerAura());

            var aura = _playerAuras[activator];

            // Safety check - ensure the same aura never enters the cache more than once.
            if (aura.Auras.Exists(x => x.StatusEffect == type))
                return;

            var maxAuras = GetMaxNumberOfAuras(activator);
            var effectName = StatusEffect.GetStatusEffectName(type);

            while (aura.Auras.Count >= maxAuras)
            {
                var removeType = aura.Auras[0].StatusEffect;
                if (aura.Auras[0].TargetsSelf)
                {
                    RemoveAuraEffect(activator, activator, removeType);
                }

                if (aura.Auras[0].TargetsParty)
                {
                    foreach (var member in aura.PartyMembersInRange)
                    {
                        RemoveAuraEffect(activator, member, removeType);
                    }
                }

                if (aura.Auras[0].TargetsEnemies)
                {
                    foreach (var npc in aura.CreaturesInRange)
                    {
                        RemoveAuraEffect(activator, npc, removeType);
                    }
                }

                aura.Auras.RemoveAt(0);
            }

            aura.Auras.Add(new PlayerAuraDetail(type, targetsSelf, targetsParty, targetsEnemies));

            if (targetsSelf)
            {
                ApplyAuraEffect(activator, activator, type);
            }

            if (targetsParty)
            {
                foreach (var member in aura.PartyMembersInRange)
                {
                    if (Party.IsInParty(activator, member))
                        ApplyAuraEffect(activator, member, type);
                }
            }

            if (targetsEnemies)
            {
                foreach (var npc in aura.CreaturesInRange)
                {
                    if (!GetIsDMPossessed(npc) && !GetIsDM(npc) &&
                        (GetIsEnemy(activator, npc) || GetIsEnemy(npc, activator)))
                        ApplyAuraEffect(activator, npc, type);
                }
            }

            SendMessageToPC(activator, ColorToken.Green($"Aura '{effectName}' activated."));
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Sound_Burst), activator);
        }

        public static bool RemoveAura(uint activator, Type type, bool sendsWornOffMessage = false)
        {
            if (!_playerAuras.ContainsKey(activator))
                return false;

            var aura = _playerAuras[activator];
            var existing = aura.Auras.FirstOrDefault(x => x.StatusEffect == type);
            if (existing == null)
                return false;

            if (existing.TargetsSelf)
            {
                RemoveAuraEffect(activator, activator, type, sendsWornOffMessage);
            }

            if (existing.TargetsParty)
            {
                foreach (var member in aura.PartyMembersInRange)
                {
                    RemoveAuraEffect(activator, member, type, sendsWornOffMessage);
                }
            }

            if (existing.TargetsEnemies)
            {
                foreach (var npc in aura.CreaturesInRange)
                {
                    RemoveAuraEffect(activator, npc, type, sendsWornOffMessage);
                }
            }

            aura.Auras.Remove(existing);
            return true;
        }

        public static bool ToggleAura(uint activator, Type type)
        {
            if (!_playerAuras.ContainsKey(activator))
                return true;

            // Aura is active and player wants to deactivate it.
            // Remove it from the list and send a notification message.
            var effectName = StatusEffect.GetStatusEffectName(type);
            if (RemoveAura(activator, type))
            {
                SendMessageToPC(activator, ColorToken.Red($"Aura '{effectName}' deactivated."));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Removes all auras which are currently active on a creature.
        /// </summary>
        /// <param name="activator">The creature who originally activated the auras.</param>
        private static void RemoveAllAuras(uint activator)
        {
            if (!_playerAuras.ContainsKey(activator))
                return;

            var auraDetails = _playerAuras[activator];

            foreach (var aura in auraDetails.Auras)
            {
                if (aura.TargetsSelf)
                {
                    RemoveAuraEffect(activator, activator, aura.StatusEffect, true);
                }

                if (aura.TargetsParty)
                {
                    foreach (var member in auraDetails.PartyMembersInRange)
                    {
                        RemoveAuraEffect(activator, member, aura.StatusEffect);
                    }
                }

                if (aura.TargetsEnemies)
                {
                    foreach (var npc in auraDetails.CreaturesInRange)
                    {
                        RemoveAuraEffect(activator, npc, aura.StatusEffect);
                    }
                }
            }

            _playerAuras.Remove(activator);
        }

        /// <summary>
        /// Deactivates every aura the player is currently projecting and strips any aura effects they are
        /// receiving as a recipient. Used by flows that must return a player to a clean state, such as the
        /// character rebuilder before a reset.
        /// </summary>
        public static void ClearAllPlayerAuras(uint player)
        {
            RemoveAllAuras(player);
            RemoveCreatureFromAllAuraRanges(player);
        }

        /// <summary>
        /// Removes a creature from all active aura range lists and strips any aura effects they received
        /// as a recipient. Used when a creature leaves the game world in a way that bypasses the normal
        /// AOE exit event (e.g., entering space, being teleported).
        /// </summary>
        /// <param name="target">The creature to remove from all aura ranges.</param>
        public static void RemoveCreatureFromAllAuraRanges(uint target)
        {
            foreach (var (leader, playerAura) in _playerAuras)
            {
                if (playerAura.PartyMembersInRange.Remove(target))
                {
                    foreach (var aura in playerAura.Auras)
                    {
                        if (aura.TargetsParty)
                            RemoveAuraEffect(leader, target, aura.StatusEffect);
                    }
                }

                if (playerAura.CreaturesInRange.Remove(target))
                {
                    foreach (var aura in playerAura.Auras)
                    {
                        if (aura.TargetsEnemies)
                            RemoveAuraEffect(leader, target, aura.StatusEffect);
                    }
                }
            }
        }


        /// <summary>
        /// Refreshes party-aura eligibility for a creature against all active aura leaders.
        /// If the creature is still tracked in a party aura range but is no longer in the leader's party,
        /// remove the cached range entry and strip relevant aura effects.
        /// </summary>
        /// <param name="target">The creature whose party aura eligibility should be refreshed.</param>
        public static void RefreshPartyAuraEligibility(uint target)
        {
            foreach (var (leader, playerAura) in _playerAuras)
            {
                if (!playerAura.PartyMembersInRange.Contains(target))
                    continue;

                if (Party.IsInParty(leader, target))
                    continue;

                playerAura.PartyMembersInRange.Remove(target);

                foreach (var aura in playerAura.Auras)
                {
                    if (aura.TargetsParty)
                        RemoveAuraEffect(leader, target, aura.StatusEffect);
                }
            }
        }

        /// <summary>
        /// Re-applies any aura effects that a creature should be receiving based on their current position
        /// in active aura range lists. Used after in-place resurrection (subdual, revive) where the
        /// AOE enter event may not re-fire for a creature that never physically left the AOE.
        /// </summary>
        /// <param name="target">The creature to re-enroll in active auras.</param>
        public static void ReapplyAuraEffectsForCreature(uint target)
        {
            if (!GetIsObjectValid(target) || GetIsDead(target))
                return;

            foreach (var (leader, playerAura) in _playerAuras)
            {
                if (playerAura.PartyMembersInRange.Contains(target) && Party.IsInParty(leader, target))
                {
                    foreach (var aura in playerAura.Auras)
                    {
                        if (aura.TargetsParty)
                            ApplyAuraEffect(leader, target, aura.StatusEffect);
                    }
                }

                if (playerAura.CreaturesInRange.Contains(target) &&
                    !GetIsDMPossessed(target) && !GetIsDM(target) &&
                    (GetIsEnemy(leader, target) || GetIsEnemy(target, leader)))
                {
                    foreach (var aura in playerAura.Auras)
                    {
                        if (aura.TargetsEnemies)
                            ApplyAuraEffect(leader, target, aura.StatusEffect);
                    }
                }
            }
        }

        private static AreaOfEffect GetAuraAOE(int commandRadiusBonusMeters)
        {
            switch (commandRadiusBonusMeters)
            {
                case >= 4:
                    return AreaOfEffect.AuraUpgrade2;
                case >= 2:
                    return AreaOfEffect.AuraUpgrade1;
                default:
                    return AreaOfEffect.AuraDefault;
            }
        }

        public static void ReapplyPlayerAuraAOE(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            RemoveEffectByTag(player, "AURA_EFFECT");

            AssignCommand(player, () =>
            {
                var commandRadiusBonusMeters = Stat.GetStatAdjustment(player, StatType.LeadershipCommandRadiusBonusMeters);
                var auraAOE = GetAuraAOE(commandRadiusBonusMeters);
                var effect = SupernaturalEffect(EffectAreaOfEffect(auraAOE, "aura_enter", string.Empty, "aura_exit"));
                effect = TagEffect(effect, "AURA_EFFECT");
                ApplyEffectToObject(DurationType.Permanent, effect, player);
            });
        }

        /// <summary>
        /// When a player enters the server, apply the Aura AOE effect.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void ApplyAuraAOE()
        {
            var player = GetEnteringObject();
            ReapplyPlayerAuraAOE(player);
        }

        /// <summary>
        /// When a player exits the server, remove all of their Aura effects.
        /// Also removes the player from any aura ranges they are receiving as a recipient.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ClearAurasOnExit()
        {
            var player = GetExitingObject();
            RemoveAllAuras(player);
            RemoveCreatureFromAllAuraRanges(player);
        }

        /// <summary>
        /// When a player dies, remove all of their Aura effects.
        /// Also removes the player from any aura ranges they are receiving as a recipient.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void ClearAurasOnDeath()
        {
            var player = GetLastPlayerDied();
            RemoveAllAuras(player);
            RemoveCreatureFromAllAuraRanges(player);
        }

        /// <summary>
        /// When a player respawns, reapply the aura AOE effect
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleRespawn)]
        public static void ReapplyAuraOnRespawn()
        {
            var player = GetLastRespawnButtonPresser();
            ReapplyPlayerAuraAOE(player);
        }

        /// <summary>
        /// When a player enters space mode, remove all of their Aura effects.
        /// Also removes the player from any aura ranges they are receiving as a recipient.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSpaceEnter)]
        public static void ClearAurasOnSpaceEntry()
        {
            var player = OBJECT_SELF;
            RemoveAllAuras(player);
            RemoveCreatureFromAllAuraRanges(player);
        }

        /// <summary>
        /// Whenever a creature enters the aura, add them to the cache.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAuraEnter)]
        public static void AuraEnter()
        {
            var entering = GetEnteringObject();
            var self = GetAreaOfEffectCreator(OBJECT_SELF);

            if (!_playerAuras.ContainsKey(self))
                _playerAuras.Add(self, new PlayerAura());

            // Party Members
            if (Party.IsInParty(self, entering))
            {
                if (!_playerAuras[self].PartyMembersInRange.Contains(entering))
                    _playerAuras[self].PartyMembersInRange.Add(entering);

                foreach (var detail in _playerAuras[self].Auras)
                {
                    if (detail.TargetsParty)
                    {
                        ApplyAuraEffect(self, entering, detail.StatusEffect);
                    }
                }
            }

            // Enemies
            else if (!GetIsDMPossessed(entering) && !GetIsDM(entering) && (GetIsEnemy(self, entering) || GetIsEnemy(entering, self)))
            {
                if (!_playerAuras[self].CreaturesInRange.Contains(entering))
                    _playerAuras[self].CreaturesInRange.Add(entering);

                foreach (var detail in _playerAuras[self].Auras)
                {
                    if (detail.TargetsEnemies)
                    {
                        ApplyAuraEffect(self, entering, detail.StatusEffect);
                    }
                }
            }
        }

        /// <summary>
        /// Whenever a creature exits the aura, remove it from the cache.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAuraExit)]
        public static void AuraExit()
        {
            var exiting = GetExitingObject();
            var self = GetAreaOfEffectCreator(OBJECT_SELF);

            if (!_playerAuras.ContainsKey(self))
                _playerAuras.Add(self, new PlayerAura());

            if (Party.IsInParty(self, exiting))
            {
                if (!_playerAuras[self].PartyMembersInRange.Contains(exiting))
                    return;

                _playerAuras[self].PartyMembersInRange.Remove(exiting);

                foreach (var detail in _playerAuras[self].Auras)
                {
                    if (detail.TargetsParty)
                    {
                        RemoveAuraEffect(self, exiting, detail.StatusEffect);
                    }
                }
            }

            else if (!GetIsDMPossessed(exiting) && !GetIsDM(exiting) && (GetIsEnemy(self, exiting) || GetIsEnemy(exiting, self)))
            {
                if (!_playerAuras[self].CreaturesInRange.Contains(exiting))
                    return;

                _playerAuras[self].CreaturesInRange.Remove(exiting);

                foreach (var detail in _playerAuras[self].Auras)
                {
                    if (detail.TargetsEnemies)
                    {
                        RemoveAuraEffect(self, exiting, detail.StatusEffect);
                    }
                }
            }
        }

        /// <summary>
        /// Applies the standard hostile combat impact used by weapon and martial abilities.
        /// </summary>
        public static int ApplyCombatImpact(
            uint activator,
            uint target,
            Location targetLocation,
            SkillType skillType,
            int baseDamage,
            int duration,
            Type statusEffect,
            bool isArea,
            IEnumerable<Type> additionalStatusEffects = null,
            Func<IStatusEffect> statusEffectFactory = null,
            CombatDamageType damageType = CombatDamageType.Physical,
            ResistanceType statusResistanceType = ResistanceType.Invalid,
            VisualEffect targetVisualEffect = VisualEffect.None,
            VisualEffect areaVisualEffect = VisualEffect.None,
            Func<uint, int> damagePercentAdjustment = null,
            Func<uint, int> baseDamageAdjustment = null,
            Animation impactAnimation = Animation.Invalid,
            int enmityBonus = 0,
            Action<uint> afterSuccessfulHit = null,
            Action<uint> beforeSuccessfulImpactRiders = null,
            int hitChancePercentAdjustment = 0,
            int criticalRatePercentAdjustment = 0,
            bool useNPCStatScaling = false,
            bool awardsCombatPoints = true,
            DamageType? effectDamageType = null,
            bool playImpactAnimation = true,
            AbilityType combatImpactDamageAbility = AbilityType.Invalid,
            bool resolvesHit = true,
            bool canCritical = true,
            bool useUnscaledDamage = false,
            Action<uint> beforeImpact = null)
        {
            var totalDamage = 0;
            RecordAbilityImpactShape(activator, skillType, isArea);

            if (isArea)
            {
                var center = GetIsObjectValid(target) ? GetLocation(target) : targetLocation;
                var creature = GetFirstObjectInShape(Shape.Sphere, 5.0f, center, true);
                var creatures = new List<uint>();
                while (GetIsObjectValid(creature))
                {
                    if (HasAbilityLineOfSight(activator, creature))
                        creatures.Add(creature);

                    creature = GetNextObjectInShape(Shape.Sphere, 5.0f, center, true);
                }

                if (areaVisualEffect != VisualEffect.None &&
                    GetIsObjectValid(GetAreaFromLocation(center)))
                {
                    ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(areaVisualEffect), center);
                }

                totalDamage = ApplyCombatImpactToCreatures(
                    activator,
                    creatures,
                    skillType,
                    baseDamage,
                    statusEffect,
                    duration,
                    additionalStatusEffects,
                    statusEffectFactory,
                    damageType,
                    statusResistanceType,
                    targetVisualEffect,
                    damagePercentAdjustment,
                    baseDamageAdjustment,
                    enmityBonus: enmityBonus,
                    beforeImpact: beforeImpact,
                    afterSuccessfulHit: afterSuccessfulHit,
                    beforeSuccessfulImpactRiders: beforeSuccessfulImpactRiders,
                    hitChancePercentAdjustment: hitChancePercentAdjustment,
                    criticalRatePercentAdjustment: criticalRatePercentAdjustment,
                    useNPCStatScaling: useNPCStatScaling,
                    awardsCombatPoints: awardsCombatPoints,
                    effectDamageType: effectDamageType,
                    combatImpactDamageAbility: combatImpactDamageAbility,
                    resolvesHit: resolvesHit,
                    canCritical: canCritical,
                    useUnscaledDamage: useUnscaledDamage);
            }
            else if (GetIsObjectValid(target))
            {
                totalDamage = ApplyCombatImpactToCreatures(
                    activator,
                    new[] { target },
                    skillType,
                    baseDamage,
                    statusEffect,
                    duration,
                    additionalStatusEffects,
                    statusEffectFactory,
                    damageType,
                    statusResistanceType,
                    targetVisualEffect,
                    damagePercentAdjustment,
                    baseDamageAdjustment,
                    enmityBonus: enmityBonus,
                    beforeImpact: beforeImpact,
                    afterSuccessfulHit: afterSuccessfulHit,
                    beforeSuccessfulImpactRiders: beforeSuccessfulImpactRiders,
                    hitChancePercentAdjustment: hitChancePercentAdjustment,
                    criticalRatePercentAdjustment: criticalRatePercentAdjustment,
                    useNPCStatScaling: useNPCStatScaling,
                    awardsCombatPoints: awardsCombatPoints,
                    effectDamageType: effectDamageType,
                    combatImpactDamageAbility: combatImpactDamageAbility,
                    resolvesHit: resolvesHit,
                    canCritical: canCritical,
                    useUnscaledDamage: useUnscaledDamage);
            }

            if (playImpactAnimation)
                PlayCombatImpactAnimation(activator, impactAnimation);

            return totalDamage;
        }

        /// <summary>
        /// Applies a hostile combat impact after a visible telegraph resolves.
        /// </summary>
        public static int ApplyTelegraphedCombatImpact(
            uint activator,
            uint target,
            Location targetLocation,
            SkillType skillType,
            int baseDamage,
            int duration,
            Type statusEffect,
            CombatImpactAreaShape shape,
            float telegraphDuration,
            float lengthOrRadius,
            float width = 0f,
            IEnumerable<Type> additionalStatusEffects = null,
            bool centerOnActivator = false,
            Func<IStatusEffect> statusEffectFactory = null,
            CombatDamageType damageType = CombatDamageType.Physical,
            ResistanceType statusResistanceType = ResistanceType.Invalid,
            VisualEffect targetVisualEffect = VisualEffect.None,
            VisualEffect areaVisualEffect = VisualEffect.None,
            Func<uint, int> damagePercentAdjustment = null,
            Func<uint, int> baseDamageAdjustment = null,
            Action<AbilityImpactSummary> afterImpactAction = null,
            int maxTargets = 0,
            Animation impactAnimation = Animation.Invalid,
            int enmityBonus = 0,
            Action<uint> beforeImpact = null,
            Action<uint> afterSuccessfulHit = null,
            Action<uint> beforeSuccessfulImpactRiders = null,
            int hitChancePercentAdjustment = 0,
            int criticalRatePercentAdjustment = 0,
            bool useNPCStatScaling = false,
            bool awardsCombatPoints = true,
            DamageType? effectDamageType = null,
            bool playImpactAnimation = true,
            bool alwaysApplyAreaVisualEffect = true,
            AbilityType combatImpactDamageAbility = AbilityType.Invalid,
            bool sendsNoTargetMessage = true,
            bool resolvesHit = true,
            bool canCritical = true,
            float impactFlashDuration = DefaultImpactFlashDuration,
            bool useUnscaledDamage = false)
        {
            RecordAbilityImpactShape(activator, skillType, true);
            var trackedImpact = GetTrackedAbilityImpact(activator);
            var backOffsetOrigin = trackedImpact?.Ability.Targeting?.Flags
                .HasFlag(AbilityTargetingFlags.BackOffsetOrigin) == true;

            if (telegraphDuration <= 0f)
            {
                // Instant-cast area abilities cannot use a pre-cast telegraph without violating the
                // Bible's "Instant" activation time, so they flash their shape at impact instead.
                // The flash is purely visual: damage below is still applied immediately. Compare
                // the actual impact geometry with the activation marker before suppressing a redraw.
                ShowAreaImpactFlash(
                    activator,
                    target,
                    targetLocation,
                    shape,
                    lengthOrRadius,
                    width,
                    centerOnActivator,
                    impactFlashDuration,
                    backOffsetOrigin,
                    trackedImpact?.ActivationAreaTelegraphs);

                var totalDamage = ApplyCombatImpactInShape(
                    activator,
                    target,
                    targetLocation,
                    skillType,
                    baseDamage,
                    duration,
                    statusEffect,
                    shape,
                    lengthOrRadius,
                    width,
                    additionalStatusEffects,
                    centerOnActivator,
                    statusEffectFactory,
                    damageType,
                    statusResistanceType,
                    targetVisualEffect,
                    areaVisualEffect,
                    damagePercentAdjustment,
                    baseDamageAdjustment,
                    maxTargets,
                    enmityBonus,
                    beforeImpact,
                    afterSuccessfulHit,
                    beforeSuccessfulImpactRiders,
                    hitChancePercentAdjustment,
                    criticalRatePercentAdjustment,
                    useNPCStatScaling,
                    awardsCombatPoints,
                    effectDamageType,
                    alwaysApplyAreaVisualEffect,
                    combatImpactDamageAbility,
                    sendsNoTargetMessage,
                    resolvesHit,
                    canCritical,
                    useUnscaledDamage,
                    backOffsetOrigin);
                if (playImpactAnimation)
                    PlayCombatImpactAnimation(activator, impactAnimation);

                if (trackedImpact != null)
                    afterImpactAction?.Invoke(trackedImpact.Summary);

                return totalDamage;
            }

            var impactRotation = GetImpactRotationRadians(activator, target, targetLocation);
            var directionalOrigin = CombatImpactShapeGeometry.ResolveOrigin(
                GetPosition(activator),
                impactRotation,
                shape,
                backOffsetOrigin);
            var adjustedLength = CombatImpactShapeGeometry.ResolveLength(
                shape,
                lengthOrRadius,
                backOffsetOrigin);
            var areaVisualLocation = Location(
                GetArea(activator),
                shape == CombatImpactAreaShape.Sphere
                    ? GetAreaImpactPosition(activator, target, targetLocation, centerOnActivator)
                    : directionalOrigin,
                0f);
            var deferredNextAbilityDamageBonus =
                (trackedImpact?.NextAbilityDamageBonus ?? 0) -
                (trackedImpact?.StatusAppliedNextAttackDamageBonus ?? 0);
            var action = BuildTelegraphedCombatImpactAction(
                skillType,
                baseDamage,
                duration,
                statusEffect,
                additionalStatusEffects,
                statusEffectFactory,
                shape,
                areaVisualLocation,
                trackedImpact?.Ability,
                trackedImpact?.Sequence ?? new AbilityImpactSequence(),
                deferredNextAbilityDamageBonus,
                trackedImpact?.NextAbilityCriticalRatePercentAdjustment ?? 0,
                trackedImpact?.NextAbilityDefenseIgnorePercentAdjustment ?? 0,
                trackedImpact?.NextAttackEnmityBonus ?? 0,
                damageType,
                statusResistanceType,
                targetVisualEffect,
                areaVisualEffect,
                damagePercentAdjustment,
                baseDamageAdjustment,
                afterImpactAction,
                maxTargets,
                enmityBonus,
                beforeImpact,
                afterSuccessfulHit,
                beforeSuccessfulImpactRiders,
                hitChancePercentAdjustment,
                criticalRatePercentAdjustment,
                useNPCStatScaling,
                awardsCombatPoints,
                effectDamageType,
                alwaysApplyAreaVisualEffect,
                combatImpactDamageAbility,
                sendsNoTargetMessage,
                resolvesHit,
                canCritical,
                useUnscaledDamage);

            switch (shape)
            {
                case CombatImpactAreaShape.Sphere:
                    Telegraph.CreateSphereTelegraph(
                        activator,
                        GetPositionFromLocation(areaVisualLocation),
                        lengthOrRadius,
                        telegraphDuration,
                        true,
                        action);
                    break;
                case CombatImpactAreaShape.Cone:
                    Telegraph.CreateConeTelegraph(
                        activator,
                        directionalOrigin,
                        impactRotation,
                        adjustedLength,
                        width > 0f ? width : adjustedLength,
                        telegraphDuration,
                        true,
                        action);
                    break;
                case CombatImpactAreaShape.Line:
                    Telegraph.CreateLineTelegraph(
                        activator,
                        directionalOrigin,
                        impactRotation,
                        adjustedLength,
                        width > 0f ? width : 2.0f,
                        telegraphDuration,
                        true,
                        action);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
            }

            Combat.DeferAbilityStaminaCostContext(activator, trackedImpact?.Ability);

            if (playImpactAnimation)
                PlayCombatImpactAnimation(activator, impactAnimation);

            return 0;
        }

        /// <summary>
        /// Renders a short, purely visual telegraph showing the area an instant ability just struck.
        /// Unlike a pre-cast telegraph this carries no action and does not gate damage, so it gives
        /// positional feedback without changing an ability's Bible-mandated "Instant" activation time.
        /// </summary>
        private static void ShowAreaImpactFlash(
            uint activator,
            uint target,
            Location targetLocation,
            CombatImpactAreaShape shape,
            float lengthOrRadius,
            float width,
            bool centerOnActivator,
            float flashDuration,
            bool backOffsetOrigin,
            IReadOnlyList<TelegraphGeometry> activationAreaTelegraphs)
        {
            if (flashDuration <= 0f || lengthOrRadius <= 0f)
                return;

            var rotation = GetImpactRotationRadians(activator, target, targetLocation);
            var directionalOrigin = CombatImpactShapeGeometry.ResolveOrigin(
                GetPosition(activator),
                rotation,
                shape,
                backOffsetOrigin);
            var adjustedLength = CombatImpactShapeGeometry.ResolveLength(
                shape,
                lengthOrRadius,
                backOffsetOrigin);

            var telegraphType = shape switch
            {
                CombatImpactAreaShape.Sphere => TelegraphType.Sphere,
                CombatImpactAreaShape.Cone => TelegraphType.Cone,
                CombatImpactAreaShape.Line => TelegraphType.Line,
                _ => TelegraphType.None
            };
            if (telegraphType == TelegraphType.None)
                return;

            var position = shape == CombatImpactAreaShape.Sphere
                ? GetAreaImpactPosition(activator, target, targetLocation, centerOnActivator)
                : directionalOrigin;
            var size = shape == CombatImpactAreaShape.Sphere
                ? new System.Numerics.Vector2(lengthOrRadius, lengthOrRadius)
                : new System.Numerics.Vector2(adjustedLength,
                    width > 0f ? width : shape == CombatImpactAreaShape.Cone ? adjustedLength : 2.0f);
            var geometry = new TelegraphGeometry(GetArea(activator), telegraphType, position, size, rotation);
            if (!Telegraph.ShouldShowImpactFlash(geometry, activationAreaTelegraphs))
                return;

            Telegraph.CreateTelegraph(
                activator,
                position,
                shape == CombatImpactAreaShape.Sphere ? 0f : rotation,
                size,
                flashDuration,
                true,
                telegraphType,
                null);
        }

        private static int ApplyCombatImpactInShape(
            uint activator,
            uint target,
            Location targetLocation,
            SkillType skillType,
            int baseDamage,
            int duration,
            Type statusEffect,
            CombatImpactAreaShape shape,
            float lengthOrRadius,
            float width,
            IEnumerable<Type> additionalStatusEffects,
            bool centerOnActivator,
            Func<IStatusEffect> statusEffectFactory,
            CombatDamageType damageType,
            ResistanceType statusResistanceType,
            VisualEffect targetVisualEffect,
            VisualEffect areaVisualEffect,
            Func<uint, int> damagePercentAdjustment,
            Func<uint, int> baseDamageAdjustment,
            int maxTargets,
            int enmityBonus,
            Action<uint> beforeImpact,
            Action<uint> afterSuccessfulHit,
            Action<uint> beforeSuccessfulImpactRiders,
            int hitChancePercentAdjustment,
            int criticalRatePercentAdjustment,
            bool useNPCStatScaling,
            bool awardsCombatPoints,
            DamageType? effectDamageType,
            bool alwaysApplyAreaVisualEffect,
            AbilityType combatImpactDamageAbility,
            bool sendsNoTargetMessage,
            bool resolvesHit,
            bool canCritical,
            bool useUnscaledDamage,
            bool backOffsetOrigin)
        {
            RecordAbilityImpactShape(activator, skillType, true);

            var origin = GetCombatImpactShapeOrigin(activator, target, targetLocation, shape, centerOnActivator);
            var creatures = GetHostileCreaturesInCombatImpactShape(
                    activator,
                    target,
                    targetLocation,
                    shape,
                    lengthOrRadius,
                    width,
                    centerOnActivator,
                    backOffsetOrigin)
                .Where(creature => HasAbilityLineOfSight(activator, creature))
                .ToList();

            if (alwaysApplyAreaVisualEffect || creatures.Count > 0)
            {
                if (areaVisualEffect != VisualEffect.None)
                {
                    ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(areaVisualEffect), origin);
                }
            }

            return ApplyCombatImpactToCreatures(
                activator,
                creatures,
                skillType,
                baseDamage,
                statusEffect,
                duration,
                additionalStatusEffects,
                statusEffectFactory,
                damageType,
                statusResistanceType,
                targetVisualEffect,
                damagePercentAdjustment,
                baseDamageAdjustment,
                maxTargets,
                enmityBonus,
                beforeImpact,
                afterSuccessfulHit,
                beforeSuccessfulImpactRiders,
                hitChancePercentAdjustment: hitChancePercentAdjustment,
                criticalRatePercentAdjustment: criticalRatePercentAdjustment,
                useNPCStatScaling: useNPCStatScaling,
                awardsCombatPoints: awardsCombatPoints,
                effectDamageType: effectDamageType,
                combatImpactDamageAbility: combatImpactDamageAbility,
                sendsNoTargetMessage: sendsNoTargetMessage,
                resolvesHit: resolvesHit,
                canCritical: canCritical,
                useUnscaledDamage: useUnscaledDamage);
        }

        private static ApplyTelegraphEffect BuildTelegraphedCombatImpactAction(
            SkillType skillType,
            int baseDamage,
            int duration,
            Type statusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory,
            CombatImpactAreaShape shape,
            Location areaVisualLocation,
            AbilityDetail ability,
            AbilityImpactSequence sequence,
            int nextAbilityDamageBonus,
            int nextAbilityCriticalRatePercentAdjustment,
            int nextAbilityDefenseIgnorePercentAdjustment,
            int nextAttackEnmityBonus,
            CombatDamageType damageType,
            ResistanceType statusResistanceType,
            VisualEffect targetVisualEffect,
            VisualEffect areaVisualEffect,
            Func<uint, int> damagePercentAdjustment,
            Func<uint, int> baseDamageAdjustment,
            Action<AbilityImpactSummary> afterImpactAction,
            int maxTargets,
            int enmityBonus,
            Action<uint> beforeImpact,
            Action<uint> afterSuccessfulHit,
            Action<uint> beforeSuccessfulImpactRiders,
            int hitChancePercentAdjustment,
            int criticalRatePercentAdjustment,
            bool useNPCStatScaling,
            bool awardsCombatPoints,
            DamageType? effectDamageType,
            bool alwaysApplyAreaVisualEffect,
            AbilityType combatImpactDamageAbility,
            bool sendsNoTargetMessage,
            bool resolvesHit,
            bool canCritical,
            bool useUnscaledDamage)
        {
            return (creator, creatures) =>
            {
                var impactStarted = false;
                var impactEnded = false;
                try
                {
                    if (!GetIsObjectValid(creator) || GetCurrentHitPoints(creator) <= 0)
                        return;

                    var hostileCreatures = creatures
                        .Where(creature =>
                            GetIsObjectValid(creature) &&
                            GetIsReactionTypeHostile(creature, creator) &&
                            HasAbilityLineOfSight(creator, creature))
                        .ToList();

                    if (maxTargets > 0)
                    {
                        var impactPosition = GetPositionFromLocation(areaVisualLocation);
                        hostileCreatures = CombatImpactShapeGeometry
                            .TakeClosestToOrigin(hostileCreatures, impactPosition, GetPosition, maxTargets)
                            .ToList();
                    }

                    if (hostileCreatures.Count <= 0)
                    {
                        if (alwaysApplyAreaVisualEffect && areaVisualEffect != VisualEffect.None)
                        {
                            ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(areaVisualEffect), areaVisualLocation);
                        }

                        if (sendsNoTargetMessage)
                            SendCombatImpactNoTargetsMessage(creator, ability);
                        return;
                    }

                    if (ability != null)
                    {
                        var statusAppliedNextAttackDamageBonus = ability.IsHostileAbility
                            ? Combat.GetStatusAppliedNextAttackDamageBonus(creator)
                            : 0;
                        BeginAbilityImpact(
                            creator,
                            ability,
                            nextAbilityDamageBonus + statusAppliedNextAttackDamageBonus,
                            nextAbilityCriticalRatePercentAdjustment,
                            nextAbilityDefenseIgnorePercentAdjustment,
                            nextAttackEnmityBonus,
                            statusAppliedNextAttackDamageBonus,
                            countsAsAttackAttempt: false,
                            sequence: sequence);
                        impactStarted = true;
                        RecordAbilityImpactShape(creator, skillType, true);
                    }

                    if (areaVisualEffect != VisualEffect.None)
                    {
                        ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(areaVisualEffect), areaVisualLocation);
                    }

                    ApplyCombatImpactToCreatures(
                        creator,
                        hostileCreatures,
                        skillType,
                        baseDamage,
                        statusEffect,
                        duration,
                        additionalStatusEffects,
                        statusEffectFactory,
                        damageType,
                        statusResistanceType,
                        targetVisualEffect,
                        damagePercentAdjustment,
                        baseDamageAdjustment,
                        maxTargets,
                        enmityBonus,
                        beforeImpact,
                        afterSuccessfulHit,
                        beforeSuccessfulImpactRiders,
                        hitChancePercentAdjustment: hitChancePercentAdjustment,
                        criticalRatePercentAdjustment: criticalRatePercentAdjustment,
                        useNPCStatScaling: useNPCStatScaling,
                        awardsCombatPoints: awardsCombatPoints,
                        effectDamageType: effectDamageType,
                        combatImpactDamageAbility: combatImpactDamageAbility,
                        sendsNoTargetMessage: sendsNoTargetMessage,
                        resolvesHit: resolvesHit,
                        canCritical: canCritical,
                        useUnscaledDamage: useUnscaledDamage);

                    if (ability != null)
                    {
                        var summary = EndAbilityImpact(creator);
                        impactEnded = true;
                        Combat.ApplyAbilityImpactEffects(creator, summary);
                        afterImpactAction?.Invoke(summary);
                    }

                }
                finally
                {
                    if (impactStarted && !impactEnded)
                    {
                        AbortAbilityImpact(creator);
                    }

                    Combat.CompleteDeferredAbilityStaminaCostContext(creator, ability);
                }
            };
        }

        private static int ApplyCombatImpactToCreatures(
            uint activator,
            IEnumerable<uint> creatures,
            SkillType skillType,
            int baseDamage,
            Type statusEffect,
            int duration,
            IEnumerable<Type> additionalStatusEffects = null,
            Func<IStatusEffect> statusEffectFactory = null,
            CombatDamageType damageType = CombatDamageType.Physical,
            ResistanceType statusResistanceType = ResistanceType.Invalid,
            VisualEffect targetVisualEffect = VisualEffect.None,
            Func<uint, int> damagePercentAdjustment = null,
            Func<uint, int> baseDamageAdjustment = null,
            int maxTargets = 0,
            int enmityBonus = 0,
            Action<uint> beforeImpact = null,
            Action<uint> afterSuccessfulHit = null,
            Action<uint> beforeSuccessfulImpactRiders = null,
            int hitChancePercentAdjustment = 0,
            int criticalRatePercentAdjustment = 0,
            bool useNPCStatScaling = false,
            bool awardsCombatPoints = true,
            DamageType? effectDamageType = null,
            AbilityType combatImpactDamageAbility = AbilityType.Invalid,
            bool sendsNoTargetMessage = true,
            bool resolvesHit = true,
            bool canCritical = true,
            bool useUnscaledDamage = false)
        {
            var totalDamage = 0;
            var affectedCount = 0;
            var trackedAbility = GetTrackedAbilityImpact(activator)?.Ability;

            var impactTargets = creatures.Distinct().ToList();
            if (beforeImpact != null)
            {
                // Capture prerequisites for the entire target set before damage or riders
                // on any hit can change another target's eligibility during this impact.
                foreach (var creature in impactTargets)
                {
                    if (GetIsObjectValid(creature) && GetIsReactionTypeHostile(creature, activator))
                        beforeImpact(creature);
                }
            }

            foreach (var creature in impactTargets)
            {
                if (!GetIsObjectValid(creature) || !GetIsReactionTypeHostile(creature, activator))
                    continue;

                if (maxTargets > 0 && affectedCount >= maxTargets)
                    break;

                totalDamage += ApplyHostileCombatImpact(
                    activator,
                    creature,
                    skillType,
                    baseDamage,
                    statusEffect,
                    duration,
                    additionalStatusEffects,
                    statusEffectFactory,
                    damageType,
                    statusResistanceType,
                    targetVisualEffect,
                    damagePercentAdjustment,
                    baseDamageAdjustment,
                    enmityBonus,
                    afterSuccessfulHit,
                    beforeSuccessfulImpactRiders,
                    hitChancePercentAdjustment,
                    criticalRatePercentAdjustment,
                    useNPCStatScaling,
                    awardsCombatPoints,
                    effectDamageType,
                    combatImpactDamageAbility,
                    resolvesHit,
                    canCritical,
                    useUnscaledDamage);
                affectedCount++;
            }

            if (affectedCount <= 0 && sendsNoTargetMessage)
            {
                SendCombatImpactNoTargetsMessage(activator, trackedAbility);
            }

            return totalDamage;
        }

        private static NumericsVector3 GetAreaImpactPosition(uint activator, uint target, Location targetLocation, bool centerOnActivator = false)
        {
            if (centerOnActivator)
                return GetPosition(activator);

            if (GetIsObjectValid(target))
                return GetPosition(target);

            var targetArea = GetAreaFromLocation(targetLocation);
            return GetIsObjectValid(targetArea)
                ? GetPositionFromLocation(targetLocation)
                : GetPosition(activator);
        }

        private static float GetImpactRotationRadians(uint activator, uint target, Location targetLocation)
        {
            var origin = GetPosition(activator);
            var destination = GetIsObjectValid(target)
                ? GetPosition(target)
                : GetIsObjectValid(GetAreaFromLocation(targetLocation))
                    ? GetPositionFromLocation(targetLocation)
                    : origin;
            var delta = destination - origin;

            if (Math.Abs(delta.X) <= 0.01f && Math.Abs(delta.Y) <= 0.01f)
                return DegreesToRadians(GetFacing(activator));

            return (float)Math.Atan2(delta.Y, delta.X);
        }

        private static Location GetCombatImpactShapeOrigin(
            uint activator,
            uint target,
            Location targetLocation,
            CombatImpactAreaShape shape,
            bool centerOnActivator)
        {
            return shape == CombatImpactAreaShape.Sphere
                ? Location(GetArea(activator), GetAreaImpactPosition(activator, target, targetLocation, centerOnActivator), 0f)
                : GetLocation(activator);
        }

        private static IEnumerable<uint> GetHostileCreaturesInCombatImpactShape(
            uint activator,
            uint target,
            Location targetLocation,
            CombatImpactAreaShape shape,
            float lengthOrRadius,
            float width,
            bool centerOnActivator,
            bool backOffsetOrigin)
        {
            var origin = GetCombatImpactShapeOrigin(activator, target, targetLocation, shape, centerOnActivator);
            var rotation = GetImpactRotationRadians(activator, target, targetLocation);
            var originPosition = CombatImpactShapeGeometry.ResolveOrigin(
                GetPositionFromLocation(origin),
                rotation,
                shape,
                backOffsetOrigin);
            var adjustedLength = CombatImpactShapeGeometry.ResolveLength(
                shape,
                lengthOrRadius,
                backOffsetOrigin);
            var maxDistance = GetCombatImpactShapeSearchRadius(shape, adjustedLength, width);
            var candidates = GetAliveCreaturesInArea(GetAreaFromLocation(origin))
                .Select(creature => new
                {
                    Creature = creature,
                    Position = GetPosition(creature)
                })
                .Where(candidate => GetHorizontalDistance(candidate.Position, originPosition) <= maxDistance)
                .OrderBy(candidate => GetHorizontalDistance(candidate.Position, originPosition));

            foreach (var candidate in candidates)
            {
                if (GetIsReactionTypeHostile(candidate.Creature, activator) &&
                    IsPositionInCombatImpactShape(candidate.Position, originPosition, rotation, shape, adjustedLength, width))
                {
                    yield return candidate.Creature;
                }
            }
        }

        private static float DegreesToRadians(float degrees)
        {
            return degrees * ((float)Math.PI / 180f);
        }

        private static bool IsPositionInCombatImpactShape(
            NumericsVector3 position,
            NumericsVector3 origin,
            float rotation,
            CombatImpactAreaShape shape,
            float lengthOrRadius,
            float width)
        {
            switch (shape)
            {
                case CombatImpactAreaShape.Sphere:
                    return GetHorizontalDistance(position, origin) <= lengthOrRadius;
                case CombatImpactAreaShape.Cone:
                    return IsPositionInCone(position, origin, rotation, lengthOrRadius, width > 0f ? width : lengthOrRadius);
                case CombatImpactAreaShape.Line:
                    return IsPositionInLine(position, origin, rotation, lengthOrRadius, width > 0f ? width : 2.0f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
            }
        }

        private static bool IsPositionInCone(NumericsVector3 position, NumericsVector3 origin, float rotation, float length, float width)
        {
            var toPoint = position - origin;
            var distance = GetHorizontalDistance(position, origin);
            if (distance <= 0.01f)
                return true;

            var direction = new NumericsVector3((float)Math.Cos(rotation), (float)Math.Sin(rotation), 0f);
            var cosAngle = Math.Clamp(NumericsVector3.Dot(toPoint, direction) / distance, -1f, 1f);
            var angleBetween = (float)Math.Acos(cosAngle);
            var halfAngle = (float)Math.Atan(width * 0.5f / length);

            return distance <= length && angleBetween <= halfAngle;
        }

        private static float GetCombatImpactShapeSearchRadius(CombatImpactAreaShape shape, float lengthOrRadius, float width)
        {
            switch (shape)
            {
                case CombatImpactAreaShape.Sphere:
                case CombatImpactAreaShape.Cone:
                    return lengthOrRadius;
                case CombatImpactAreaShape.Line:
                    var effectiveWidth = width > 0f ? width : 2.0f;
                    var halfWidth = effectiveWidth * 0.5f;
                    return (float)Math.Sqrt(lengthOrRadius * lengthOrRadius + halfWidth * halfWidth);
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
            }
        }

        private static float GetHorizontalDistance(NumericsVector3 position, NumericsVector3 origin)
        {
            var x = position.X - origin.X;
            var y = position.Y - origin.Y;

            return (float)Math.Sqrt(x * x + y * y);
        }

        private static IEnumerable<uint> GetAliveCreaturesInArea(uint area)
        {
            if (!GetIsObjectValid(area))
                yield break;

            for (var creature = GetFirstObjectInArea(area, ObjectType.Creature);
                 GetIsObjectValid(creature);
                 creature = GetNextObjectInArea(area, ObjectType.Creature))
            {
                if (!GetIsDead(creature) && GetCurrentHitPoints(creature) > 0)
                    yield return creature;
            }
        }

        private static bool IsPositionInLine(NumericsVector3 position, NumericsVector3 origin, float rotation, float length, float width)
        {
            var toPoint = position - origin;
            var rotatedX = toPoint.X * (float)Math.Cos(-rotation) - toPoint.Y * (float)Math.Sin(-rotation);
            var rotatedY = toPoint.X * (float)Math.Sin(-rotation) + toPoint.Y * (float)Math.Cos(-rotation);

            return rotatedX >= 0f &&
                   rotatedX <= length &&
                   Math.Abs(rotatedY) <= width * 0.5f;
        }

        /// <summary>
        /// Plays a non-weapon combat impact animation while preserving explicit throw carriers.
        /// </summary>
        private static void PlayCombatImpactAnimation(uint activator, Animation impactAnimation)
        {
            var trackedAbility = GetTrackedAbilityImpact(activator)?.Ability;

            // Queued weapon abilities resolve inside the engine's landed auto-attack. Playing a
            // scripted impact animation here would enqueue a second swing after the real hit,
            // which becomes especially visible after a lethal attack has already killed its target.
            if (trackedAbility?.ActivationType == AbilityActivationType.Weapon)
                return;

            var animation = impactAnimation == Animation.Invalid
                ? trackedAbility?.ImpactAnimationType ?? Animation.Invalid
                : impactAnimation;

            if (animation == Animation.Invalid)
                return;

            var sourceAnimationName = string.Empty;
            var replacementAnimationName = string.Empty;
            var restoreDelaySeconds = 0f;

            if (impactAnimation == Animation.Invalid && trackedAbility != null)
            {
                sourceAnimationName = trackedAbility.ImpactAnimationSourceAnimationName;
                replacementAnimationName = trackedAbility.ImpactAnimationReplacementAnimationName;
                restoreDelaySeconds = trackedAbility.ImpactAnimationRestoreDelaySeconds;
            }

            if (!string.IsNullOrWhiteSpace(sourceAnimationName) &&
                !string.IsNullOrWhiteSpace(replacementAnimationName))
            {
                AssignCommand(activator, () =>
                {
                    PistolAnimationRemap.PlayAnimationWithTemporaryReplacementPreservingExplicitThrow(
                        activator,
                        animation,
                        1.0f,
                        restoreDelaySeconds,
                        sourceAnimationName,
                        replacementAnimationName,
                        restoreDelaySeconds);
                });
                return;
            }

            AssignCommand(
                activator,
                () => PistolAnimationRemap.PlayAnimationPreservingExplicitThrow(
                    activator,
                    animation));
        }

        public static int ApplyHostileCombatImpact(
            uint activator,
            uint target,
            SkillType skillType,
            int damage,
            CombatDamageType damageType,
            Type statusEffect = null,
            int duration = 0,
            IEnumerable<Type> additionalStatusEffects = null,
            Func<IStatusEffect> statusEffectFactory = null,
            ResistanceType statusResistanceType = ResistanceType.Invalid,
            VisualEffect targetVisualEffect = VisualEffect.None,
            int enmityBonus = 0,
            Action<uint> afterSuccessfulHit = null,
            Action<uint> beforeSuccessfulImpactRiders = null,
            bool awardsCombatPoints = true,
            DamageType? effectDamageType = null,
            bool firstHostileAbilityHitDamageBonusApplied = false)
        {
            using var damageDerivedHealing = Combat.BeginDamageDerivedHealing(activator);
            var trackedImpact = GetTrackedAbilityImpact(activator);

            // Register the combat point before applying damage. A lethal hit resolves the target's
            // death (and its skill XP distribution) synchronously during EffectDamage below, so
            // registering afterward would miss the payout entirely when an ability one-shots a target.
            if (awardsCombatPoints)
                CombatPoint.AddCombatPoint(activator, target, skillType, 3);

            if (damage > 0)
            {
                trackedImpact?.ConsumeStatusAppliedNextAttackDamageBonus(activator);
                if (trackedImpact == null)
                {
                    StatusEffect.NotifyPreDamageStatusEffects(activator, target, damage, damageType);
                    Combat.SendTemporaryHitPointDamageFeedback(activator, target, damage);
                    AssignCommand(
                        activator,
                        () => ApplyEffectToObject(
                            DurationType.Instant,
                            EffectDamage(damage, effectDamageType ?? damageType.GetNWScriptDamageType()),
                            target));
                }
                else
                {
                    trackedImpact.QueueDirectDamageEffect(
                        target,
                        damage,
                        effectDamageType ?? damageType.GetNWScriptDamageType(),
                        damageType);
                }

                ApplyDarkForceConversion(activator, target, damage);
                Combat.ConsumeSameTargetPressureWeaponAbilityDamageBonus(activator, target, skillType, damage);
                Combat.ApplyDamageDealtEffects(
                    activator,
                    target,
                    damage,
                    skillType,
                    damageType,
                    isAbilityDamage: true);
                StatusEffect.NotifyDamageStatusEffects(activator, target, damage, damageType);
                if (trackedImpact == null)
                    Combat.ApplyDamageReflectionEffects(activator, target, damage, damageType);
            }

            ApplyHostileAbilityEnmity(
                activator,
                target,
                damage + Math.Max(0, enmityBonus) + Math.Max(0, trackedImpact?.NextAttackEnmityBonus ?? 0));

            var statusApplied = ApplyCombatImpactStatusEffect(
                activator,
                target,
                skillType,
                statusEffect,
                duration,
                additionalStatusEffects,
                statusEffectFactory,
                statusResistanceType,
                damageType);
            if (statusApplied)
            {
                ApplyDarkForceCastConversion(activator, target);
            }

            beforeSuccessfulImpactRiders?.Invoke(target);

            Combat.ApplySuccessfulAbilityImpactRiders(
                activator,
                target,
                trackedImpact?.Ability,
                skillType,
                damageType,
                damage,
                statusApplied,
                statusEffect,
                additionalStatusEffects,
                firstHostileAbilityHitDamageBonusApplied,
                trackedImpact == null || trackedImpact.Summary.ImpactedTargetCount == 0);

            if ((damage > 0 || statusApplied) && targetVisualEffect != VisualEffect.None)
            {
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(targetVisualEffect), target);
            }

            afterSuccessfulHit?.Invoke(target);
            RecordAbilityImpactTarget(activator, target, skillType, false);
            return damage;
        }

        private static int ApplyHostileCombatImpact(
            uint activator,
            uint target,
            SkillType skillType,
            int baseDamage,
            Type statusEffect,
            int duration,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory,
            CombatDamageType damageType,
            ResistanceType statusResistanceType,
            VisualEffect targetVisualEffect,
            Func<uint, int> damagePercentAdjustment = null,
            Func<uint, int> baseDamageAdjustment = null,
            int enmityBonus = 0,
            Action<uint> afterSuccessfulHit = null,
            Action<uint> beforeSuccessfulImpactRiders = null,
            int hitChancePercentAdjustment = 0,
            int criticalRatePercentAdjustment = 0,
            bool useNPCStatScaling = false,
            bool awardsCombatPoints = true,
            DamageType? effectDamageType = null,
            AbilityType combatImpactDamageAbility = AbilityType.Invalid,
            bool resolvesHit = true,
            bool canCritical = true,
            bool useUnscaledDamage = false)
        {
            using var damageDerivedHealing = Combat.BeginDamageDerivedHealing(activator);
            var trackedImpact = GetTrackedAbilityImpact(activator);
            Combat.TrackHostileAbilityActivity(activator);
            Combat.TrackHostileDefensiveCombatEntryActivity(target, activator);
            var perkType = trackedImpact?.Ability?.EffectiveLevelPerkType ?? PerkType.Invalid;
            var usesNPCStatScaling = ShouldUseNPCStatScaling(activator, useNPCStatScaling);
            var damageAbility = combatImpactDamageAbility != AbilityType.Invalid
                ? combatImpactDamageAbility
                : trackedImpact?.Ability?.CombatImpactDamageAbility ?? AbilityType.Invalid;
            var appliedStatusCategories = GetCombatImpactStatusCategories(statusEffect, additionalStatusEffects, statusEffectFactory);
            var statusCategoryHitChanceAdjustment = Combat.GetAbilityStatusCategoryHitChancePercentAdjustment(
                activator,
                skillType,
                appliedStatusCategories);
            var skillLevelOverride = usesNPCStatScaling
                ? GetNPCAbilityScalingRank(activator, skillType, damageType, damageAbility)
                : -1;
            var shouldResolveHit = resolvesHit && ShouldResolveCombatImpactHit(trackedImpact);
            var hitRate = 100;
            if (shouldResolveHit &&
                !Combat.TryResolveAbilityHit(
                    activator,
                    target,
                    skillType,
                    perkType,
                    out hitRate,
                    hitChancePercentAdjustment + statusCategoryHitChanceAdjustment,
                    skillLevelOverride,
                    damageAbility))
            {
                SendCombatImpactResultMessage(activator, target, trackedImpact?.Ability, 4, hitRate);
                if (awardsCombatPoints)
                    CombatPoint.AddCombatPoint(activator, target, skillType, 1);
                ApplyMissedHostileAbilityEnmity(activator, target);
                return 0;
            }

            if (shouldResolveHit)
                SendCombatImpactResultMessage(activator, target, trackedImpact?.Ability, 1, hitRate);

            var adjustedBaseDamage = Math.Max(0, baseDamage + (baseDamageAdjustment?.Invoke(target) ?? 0));
            adjustedBaseDamage += Combat.GetAbilityImpactBaseDamageBonus(
                activator,
                target,
                trackedImpact?.Ability,
                skillType);
            adjustedBaseDamage += Combat.GetAbilityStatusCategoryDamageBonus(
                activator,
                skillType,
                appliedStatusCategories);
            var damage = useUnscaledDamage
                ? CalculateUnscaledCombatImpactDamage(activator, target, skillType, adjustedBaseDamage, damageType)
                : usesNPCStatScaling
                    ? CalculateNPCCombatImpactDamage(activator, target, skillType, adjustedBaseDamage, damageType, criticalRatePercentAdjustment, damageAbility, canCritical)
                    : CalculateCombatImpactDamage(activator, target, skillType, adjustedBaseDamage, damageType, criticalRatePercentAdjustment, damageAbility, canCritical);
            damage = ApplyDamagePercentAdjustment(target, damage, damagePercentAdjustment);
            damage = ApplyDarkForceTargetLowHPDamageModifier(activator, target, damage);
            return ApplyHostileCombatImpact(
                activator,
                target,
                skillType,
                damage,
                damageType,
                statusEffect,
                duration,
                additionalStatusEffects,
                statusEffectFactory,
                statusResistanceType,
                targetVisualEffect,
                enmityBonus,
                afterSuccessfulHit,
                beforeSuccessfulImpactRiders,
                awardsCombatPoints,
                effectDamageType,
                firstHostileAbilityHitDamageBonusApplied: true);
        }

        private static bool ShouldResolveCombatImpactHit(TrackedAbilityImpact trackedImpact)
        {
            return trackedImpact?.Ability?.ActivationType != AbilityActivationType.Weapon;
        }

        private static StatusEffectCategory GetCombatImpactStatusCategories(
            Type statusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory)
        {
            var categories = GetStatusEffectTypeCategories(statusEffect);

            if (additionalStatusEffects != null)
            {
                foreach (var additionalStatusEffect in additionalStatusEffects)
                {
                    categories |= GetStatusEffectTypeCategories(additionalStatusEffect);
                }
            }

            var factoryStatusEffect = statusEffectFactory?.Invoke();
            if (factoryStatusEffect != null)
            {
                categories |= factoryStatusEffect.Categories;
            }

            return categories;
        }

        private static StatusEffectCategory GetStatusEffectTypeCategories(Type statusEffect)
        {
            if (statusEffect == null)
                return StatusEffectCategory.None;

            var categories = StatusEffectCategory.None;
            foreach (StatusEffectCategory category in Enum.GetValues(typeof(StatusEffectCategory)))
            {
                if (category == StatusEffectCategory.None)
                    continue;

                if (StatusEffect.HasStatusEffectCategory(statusEffect, category))
                    categories |= category;
            }

            return categories;
        }

        private static void SendCombatImpactResultMessage(
            uint activator,
            uint target,
            AbilityDetail ability,
            int attackResultType,
            int hitRate)
        {
            if (!GetIsObjectValid(activator) || !GetIsObjectValid(target) || ability == null)
                return;

            Messaging.SendMessageNearbyToPlayers(
                target,
                receiver => Combat.BuildAbilityCombatLogMessage(
                    receiver,
                    activator,
                    target,
                    ability.Name,
                    attackResultType,
                    hitRate),
                60f);
        }

        private static void SendCombatImpactNoTargetsMessage(
            uint activator,
            AbilityDetail ability)
        {
            if (!GetIsObjectValid(activator) || ability == null)
                return;

            Messaging.SendMessageNearbyToPlayers(
                activator,
                receiver => Combat.BuildAbilityNoTargetCombatLogMessage(
                    receiver,
                    activator,
                    ability.Name),
                60f);
        }

        private static int ApplyDamagePercentAdjustment(
            uint target,
            int damage,
            Func<uint, int> damagePercentAdjustment)
        {
            if (damage <= 0 || damagePercentAdjustment == null)
                return damage;

            var adjustment = damagePercentAdjustment(target);
            if (adjustment == 0)
                return damage;

            return Math.Max(0, damage + (int)Math.Ceiling(damage * (adjustment / 100f)));
        }

        private static int ApplyDarkForceTargetLowHPDamageModifier(uint activator, uint target, int damage)
        {
            if (damage <= 0 || !GetIsObjectValid(target))
                return damage;

            var trackedImpact = GetTrackedAbilityImpact(activator);
            if (trackedImpact?.Ability?.TriggersDarkForceConversion != true)
                return damage;

            var threshold = Stat.GetStatAdjustment(activator, StatType.DarkForceTargetLowHPDamageThresholdPercent);
            var adjustment = Stat.GetStatAdjustment(activator, StatType.DarkForceTargetLowHPDamagePercentAdjustment);
            if (threshold <= 0 || adjustment == 0)
                return damage;

            var maxHP = GetMaxHitPoints(target);
            if (maxHP <= 0 || GetCurrentHitPoints(target) > maxHP * (threshold / 100f))
                return damage;

            return Math.Max(0, damage + (int)Math.Ceiling(damage * (adjustment / 100f)));
        }

        private static void ApplyDarkForceConversion(uint activator, uint target, int damage)
        {
            if (damage <= 0 || !GetIsObjectValid(activator))
                return;

            var trackedImpact = GetTrackedAbilityImpact(activator);
            if (trackedImpact == null ||
                trackedImpact.Ability?.TriggersDarkForceConversion != true)
            {
                return;
            }

            ApplyDarkForceDamageRestoration(activator, damage);
            ApplyDarkForceCastConversion(activator, target);
        }

        public static void ApplyDarkForceDamageRestoration(uint activator, int damage)
        {
            if (damage <= 0 || !GetIsObjectValid(activator))
                return;

            var hpRestorePercent = Stat.GetStatAdjustment(activator, StatType.DarkForceDamageHPPercentRestore);
            if (hpRestorePercent > 0)
            {
                Combat.ApplyDamageDerivedHealing(activator, damage, hpRestorePercent);
            }
        }

        private static void ApplyDarkForceCastConversion(uint activator, uint target)
        {
            var trackedImpact = GetTrackedAbilityImpact(activator);
            if (trackedImpact == null ||
                trackedImpact.Ability?.TriggersDarkForceConversion != true)
            {
                return;
            }

            var fpRestore = Stat.GetStatAdjustment(activator, StatType.DarkForceDamageFPRestore);
            var hpCostPercent = Stat.GetStatAdjustment(activator, StatType.DarkForceDamageHPCostPercent);
            if (trackedImpact.DarkForceConversionApplied)
                return;

            if (fpRestore <= 0 && hpCostPercent <= 0)
                return;

            trackedImpact.DarkForceConversionApplied = true;

            if (fpRestore > 0)
                Stat.RestoreFP(activator, fpRestore);

            var lowTargetThresholdPercent = Stat.GetStatAdjustment(activator, StatType.DarkForceDamageLowTargetHPThresholdPercent);
            var lowTargetHPCostPercent = Stat.GetStatAdjustment(activator, StatType.DarkForceDamageLowTargetHPCostPercent);
            if (lowTargetThresholdPercent > 0 &&
                lowTargetHPCostPercent > 0 &&
                GetIsObjectValid(target) &&
                GetMaxHitPoints(target) > 0 &&
                GetCurrentHitPoints(target) <= GetMaxHitPoints(target) * lowTargetThresholdPercent / 100)
            {
                hpCostPercent = lowTargetHPCostPercent;
            }

            if (hpCostPercent <= 0)
                return;

            var hpCost = Math.Max(1, GetMaxHitPoints(activator) * hpCostPercent / 100);
            hpCost = Math.Min(hpCost, Math.Max(0, GetCurrentHitPoints(activator) - 1));
            if (hpCost > 0)
                AssignCommand(activator, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(hpCost), activator));
        }

        private static int CalculateUnscaledCombatImpactDamage(
            uint activator,
            uint target,
            SkillType skillType,
            int baseDamage,
            CombatDamageType damageType)
        {
            if (baseDamage <= 0)
                return 0;

            var trackedImpact = GetTrackedAbilityImpact(activator);
            var damage = baseDamage + (trackedImpact?.NextAbilityDamageBonus ?? 0);
            damage = Combat.ApplyDamageDealtModifiers(
                activator,
                target,
                damage,
                skillType,
                damageType,
                isAbilityDamage: true,
                ability: trackedImpact?.Ability);
            damage = ApplyCombatReadinessToActivatedAbilityMagnitude(activator, damage);
            Combat.ApplyIncomingPhysicalToForceConversion(activator, target, damageType, ref damage);
            damage = Combat.ApplyTypedLeadershipDamageTakenModifier(target, damage, damageType);
            damage = Resistance.ApplyResistanceToDamage(target, damageType, damage);
            damage = Combat.ApplyDamageTakenModifiers(
                target,
                damage,
                activator,
                damageType,
                typedLeadershipReductionAlreadyApplied: true);

            return damage;
        }

        private static int CalculateCombatImpactDamage(
            uint activator,
            uint target,
            SkillType skillType,
            int baseDamage,
            CombatDamageType damageType,
            int criticalRatePercentAdjustment = 0,
            AbilityType combatImpactDamageAbility = AbilityType.Invalid,
            bool canCritical = true)
        {
            var trackedImpact = GetTrackedAbilityImpact(activator);
            var usesQueuedNaturalWeapon =
                trackedImpact?.Ability?.ActivationType == AbilityActivationType.Weapon &&
                skillType == SkillType.BeastMastery;
            if (baseDamage <= 0 &&
                !Combat.IsWeaponSkillType(skillType) &&
                !usesQueuedNaturalWeapon)
            {
                return 0;
            }

            var ability = GetCombatImpactDamageAbility(activator, combatImpactDamageAbility);
            var perkType = trackedImpact?.Ability?.EffectiveLevelPerkType ?? PerkType.Invalid;
            var idleBonuses = Combat.GetIdleSkillAbilityBonuses(activator, skillType);
            var damage = baseDamage +
                Combat.GetCombatImpactWeaponDamage(activator, skillType, usesQueuedNaturalWeapon) +
                Combat.GetAbilityDamageBonus(activator, skillType) +
                Combat.GetAbilityDamageFlatAdjustment(activator, perkType, skillType) +
                Combat.GetCostlyAbilityDamageBonus(activator, trackedImpact?.Ability, skillType) +
                Combat.GetSameTargetPressureWeaponAbilityDamageBonus(activator, target, skillType) +
                idleBonuses.DamageBonus;
            if (trackedImpact != null)
            {
                damage += trackedImpact.NextAbilityDamageBonus;
            }

            var attack = Stat.GetAttack(activator, ability, skillType);
            attack = Combat.ApplyTargetStatusAttackModifiers(activator, target, attack, skillType);
            var attackStat = GetAbilityScore(activator, ability);
            var defenseAbility = damageType.GetDefenseAbilityType();
            var defense = Stat.GetDefense(target, damageType, defenseAbility);
            defense = Combat.ApplyStatusSourceDefenseModifiers(activator, target, defense);
            defense = Combat.ApplyIncomingPhysicalToForceDefenseConversion(
                target,
                damageType,
                defense,
                () => Combat.ApplyStatusSourceDefenseModifiers(
                    activator,
                    target,
                    Stat.GetDefense(target, CombatDamageType.Force, CombatDamageType.Force.GetDefenseAbilityType())));
            var defenderStat = GetAbilityScore(target, defenseAbility);
            var defenseIgnorePercent =
                Combat.GetAbilityDefenseIgnorePercentAdjustment(activator, perkType, skillType, target) +
                (trackedImpact?.NextAbilityDefenseIgnorePercentAdjustment ?? 0);
            defense = Combat.ApplyDefenseIgnore(defense, defenseIgnorePercent);
            var criticalRating = canCritical
                ? Combat.CalculateAbilityCriticalRating(
                    activator,
                    skillType,
                    IsTrackedAbilityArea(activator),
                    (trackedImpact?.NextAbilityCriticalRatePercentAdjustment ?? 0) + criticalRatePercentAdjustment,
                    target)
                : 0;
            var damageRoll = Combat.CalculateDamageWithCriticalMitigation(
                target,
                attack,
                damage,
                attackStat,
                defense,
                defenderStat,
                criticalRating);
            var calculatedDamage = damageRoll.Damage;
            criticalRating = damageRoll.CriticalRating;
            if (damageRoll.WasCriticalDowngraded)
            {
                Combat.SendIncomingCriticalHitDowngradeFeedback(activator, target);
            }

            calculatedDamage = Combat.ApplyCriticalDamageModifier(
                activator,
                calculatedDamage,
                criticalRating,
                skillType,
                target,
                idleBonuses.CriticalDamagePercentAdjustment +
                (trackedImpact?.NextAbilityCriticalDamagePercentAdjustment ?? 0));
            calculatedDamage = Combat.ApplySideAttackDamageModifier(activator, target, skillType, calculatedDamage);
            calculatedDamage = Combat.ApplyTwinBladeAbilityShapeDamageModifier(
                activator,
                skillType,
                calculatedDamage,
                IsTrackedAbilitySingleTarget(activator),
                IsTrackedAbilityArea(activator));
            calculatedDamage = Combat.ApplyThrowingAbilityShapeDamageModifier(
                activator,
                skillType,
                calculatedDamage,
                IsTrackedAbilityArea(activator));
            calculatedDamage = Combat.ApplySkillAreaAbilityDamageModifier(
                activator,
                skillType,
                calculatedDamage,
                IsTrackedAbilityArea(activator));
            calculatedDamage = Combat.ApplyPhysicalAbilityShapeDamageModifier(
                activator,
                damageType,
                calculatedDamage,
                IsTrackedAbilitySingleTarget(activator));
            calculatedDamage = Combat.ApplyAreaAbilityAfterDeflectionDamageModifier(
                activator,
                skillType,
                calculatedDamage,
                IsTrackedAbilityArea(activator));
            if (skillType == SkillType.Force)
            {
                calculatedDamage = Perk.ApplyForceAffinityMagnitude(activator, perkType, calculatedDamage);
            }
            calculatedDamage = Combat.ApplyDamageDealtModifiers(
                activator,
                target,
                calculatedDamage,
                skillType,
                damageType,
                isAbilityDamage: true,
                ability: trackedImpact?.Ability);
            calculatedDamage = ApplyCombatReadinessToActivatedAbilityMagnitude(activator, calculatedDamage);
            // Saber Ward / Aegis Eternal: re-type a share of an incoming physical hit into a real Force
            // instance (mitigated by Force resistance, shown as Force) before physical resistance.
            Combat.ApplyIncomingPhysicalToForceConversion(activator, target, damageType, ref calculatedDamage);
            // Conversion must split first so each portion receives only its own typed Leadership channel.
            calculatedDamage = Combat.ApplyTypedLeadershipDamageTakenModifier(target, calculatedDamage, damageType);
            calculatedDamage = Resistance.ApplyResistanceToDamage(target, damageType, calculatedDamage);
            calculatedDamage = Combat.ApplyDamageTakenModifiers(
                target,
                calculatedDamage,
                activator,
                damageType,
                typedLeadershipReductionAlreadyApplied: true);

            if (criticalRating > 0)
            {
                trackedImpact?.RecordCriticalHit();
                Combat.SendAbilityCriticalHitFeedback(
                    activator,
                    target,
                    trackedImpact?.Ability?.Name);
                Combat.ApplyCriticalHitEffects(
                    activator,
                    target,
                    calculatedDamage,
                    criticalRating,
                    IsTrackedAbilitySingleTarget(activator),
                    skillType);
            }
            return calculatedDamage;
        }

        private static bool ShouldUseNPCStatScaling(uint activator, bool useNPCStatScaling)
        {
            return useNPCStatScaling &&
                   GetIsObjectValid(activator) &&
                   !GetIsPC(activator);
        }

        private static int GetNPCAbilityScalingRank(
            uint activator,
            SkillType skillType,
            CombatDamageType damageType,
            AbilityType combatImpactDamageAbility = AbilityType.Invalid)
        {
            var npcStats = Stat.GetNPCStats(activator);
            if (npcStats.Skills.TryGetValue(skillType, out var skillRank) && skillRank > 0)
            {
                return Math.Clamp(skillRank, MinNPCAbilityScalingRank, MaxNPCAbilityScalingRank);
            }

            var ability = GetCombatImpactDamageAbility(activator, combatImpactDamageAbility);
            var abilityScore = ability == AbilityType.Invalid
                ? 0
                : GetAbilityScore(activator, ability);
            var rankFromAbility = Math.Max(MinNPCAbilityScalingRank, abilityScore - 8);
            var rankFromOffense = Math.Max(0, GetNPCAbilityOffenseBonus(npcStats, skillType, damageType) / 2);
            var rankFromDefense = Math.Max(0, GetNPCAbilityRelevantDefense(npcStats, damageType) / 2);
            var rankFromResistance = Math.Max(0, GetNPCAbilityRelevantResistance(npcStats, damageType) / 2);
            var rank = new[]
            {
                rankFromAbility,
                rankFromOffense,
                rankFromDefense,
                rankFromResistance
            }.Max();

            return Math.Clamp(rank, MinNPCAbilityScalingRank, MaxNPCAbilityScalingRank);
        }

        private static int GetNPCAbilityOffenseBonus(NPCStats npcStats, SkillType skillType, CombatDamageType damageType)
        {
            return skillType == SkillType.Force || damageType == CombatDamageType.Force
                ? npcStats.ForceAttack
                : npcStats.Attack;
        }

        private static int GetNPCAbilityRelevantDefense(NPCStats npcStats, CombatDamageType damageType)
        {
            var defenseType = damageType.GetDefenseDamageType();
            if (npcStats.Defenses.TryGetValue(defenseType, out var defense))
            {
                return defense;
            }

            return npcStats.Defenses.Count > 0
                ? npcStats.Defenses.Values.Max()
                : 0;
        }

        private static int GetNPCAbilityRelevantResistance(NPCStats npcStats, CombatDamageType damageType)
        {
            if (damageType.TryGetElementalResistanceType(out var resistanceType) &&
                npcStats.Resistances.TryGetValue(resistanceType, out var resistance))
            {
                return resistance;
            }

            if (damageType.TryGetSourceResistanceType(out resistanceType) &&
                npcStats.Resistances.TryGetValue(resistanceType, out resistance))
            {
                return resistance;
            }

            return 0;
        }

        private static int CalculateNPCCombatImpactDamage(
            uint activator,
            uint target,
            SkillType skillType,
            int baseDamage,
            CombatDamageType damageType,
            int criticalRatePercentAdjustment = 0,
            AbilityType combatImpactDamageAbility = AbilityType.Invalid,
            bool canCritical = true)
        {
            var trackedImpact = GetTrackedAbilityImpact(activator);
            var usesQueuedNaturalWeapon =
                trackedImpact?.Ability?.ActivationType == AbilityActivationType.Weapon &&
                skillType == SkillType.BeastMastery;
            if (baseDamage <= 0 &&
                !Combat.IsWeaponSkillType(skillType) &&
                !usesQueuedNaturalWeapon)
            {
                return 0;
            }

            var ability = GetCombatImpactDamageAbility(activator, combatImpactDamageAbility);
            var perkType = trackedImpact?.Ability?.EffectiveLevelPerkType ?? PerkType.Invalid;
            var idleBonuses = Combat.GetIdleSkillAbilityBonuses(activator, skillType);
            var scalingRank = GetNPCAbilityScalingRank(activator, skillType, damageType, combatImpactDamageAbility);
            var damage = baseDamage +
                Combat.GetCombatImpactWeaponDamage(activator, skillType, usesQueuedNaturalWeapon) +
                (int)Math.Ceiling(scalingRank * 0.15f) +
                Combat.GetAbilityDamageFlatAdjustment(activator, perkType, skillType) +
                Combat.GetCostlyAbilityDamageBonus(activator, trackedImpact?.Ability, skillType) +
                Combat.GetSameTargetPressureWeaponAbilityDamageBonus(activator, target, skillType) +
                idleBonuses.DamageBonus;
            if (trackedImpact != null)
            {
                damage += trackedImpact.NextAbilityDamageBonus;
            }

            var npcStats = Stat.GetNPCStats(activator);
            var attackStat = ability == AbilityType.Invalid
                ? 0
                : GetAbilityScore(activator, ability);
            var attack = Stat.GetAttack(
                scalingRank,
                attackStat,
                GetNPCAbilityOffenseBonus(npcStats, skillType, damageType) + Stat.GetStatAdjustment(activator, StatType.Attack));
            attack = ApplyNPCAbilitySourceAttackModifiers(activator, skillType, attack);
            attack = Combat.ApplyTargetStatusAttackModifiers(activator, target, attack, skillType);
            var defenseAbility = damageType.GetDefenseAbilityType();
            var defense = Stat.GetDefense(target, damageType, defenseAbility);
            defense = Combat.ApplyStatusSourceDefenseModifiers(activator, target, defense);
            defense = Combat.ApplyIncomingPhysicalToForceDefenseConversion(
                target,
                damageType,
                defense,
                () => Combat.ApplyStatusSourceDefenseModifiers(
                    activator,
                    target,
                    Stat.GetDefense(target, CombatDamageType.Force, CombatDamageType.Force.GetDefenseAbilityType())));
            var defenderStat = GetAbilityScore(target, defenseAbility);
            var defenseIgnorePercent =
                Combat.GetAbilityDefenseIgnorePercentAdjustment(activator, perkType, skillType, target) +
                (trackedImpact?.NextAbilityDefenseIgnorePercentAdjustment ?? 0);
            defense = Combat.ApplyDefenseIgnore(defense, defenseIgnorePercent);
            var criticalRating = canCritical
                ? Combat.CalculateAbilityCriticalRating(
                    activator,
                    skillType,
                    IsTrackedAbilityArea(activator),
                    (trackedImpact?.NextAbilityCriticalRatePercentAdjustment ?? 0) + criticalRatePercentAdjustment,
                    target)
                : 0;
            var damageRoll = Combat.CalculateDamageWithCriticalMitigation(
                target,
                attack,
                damage,
                attackStat,
                defense,
                defenderStat,
                criticalRating);
            var calculatedDamage = damageRoll.Damage;
            criticalRating = damageRoll.CriticalRating;
            if (damageRoll.WasCriticalDowngraded)
            {
                Combat.SendIncomingCriticalHitDowngradeFeedback(activator, target);
            }

            calculatedDamage = Combat.ApplyCriticalDamageModifier(
                activator,
                calculatedDamage,
                criticalRating,
                skillType,
                target,
                idleBonuses.CriticalDamagePercentAdjustment +
                (trackedImpact?.NextAbilityCriticalDamagePercentAdjustment ?? 0));
            calculatedDamage = Combat.ApplySideAttackDamageModifier(activator, target, skillType, calculatedDamage);
            calculatedDamage = Combat.ApplyTwinBladeAbilityShapeDamageModifier(
                activator,
                skillType,
                calculatedDamage,
                IsTrackedAbilitySingleTarget(activator),
                IsTrackedAbilityArea(activator));
            calculatedDamage = Combat.ApplyThrowingAbilityShapeDamageModifier(
                activator,
                skillType,
                calculatedDamage,
                IsTrackedAbilityArea(activator));
            calculatedDamage = Combat.ApplySkillAreaAbilityDamageModifier(
                activator,
                skillType,
                calculatedDamage,
                IsTrackedAbilityArea(activator));
            calculatedDamage = Combat.ApplyPhysicalAbilityShapeDamageModifier(
                activator,
                damageType,
                calculatedDamage,
                IsTrackedAbilitySingleTarget(activator));
            calculatedDamage = Combat.ApplyAreaAbilityAfterDeflectionDamageModifier(
                activator,
                skillType,
                calculatedDamage,
                IsTrackedAbilityArea(activator));
            if (skillType == SkillType.Force)
            {
                calculatedDamage = Perk.ApplyForceAffinityMagnitude(activator, perkType, calculatedDamage);
            }
            calculatedDamage = Combat.ApplyDamageDealtModifiers(
                activator,
                target,
                calculatedDamage,
                skillType,
                damageType,
                isAbilityDamage: true,
                ability: trackedImpact?.Ability);
            calculatedDamage = ApplyCombatReadinessToActivatedAbilityMagnitude(activator, calculatedDamage);
            // Saber Ward / Aegis Eternal: re-type a share of an incoming physical hit into a real Force
            // instance (mitigated by Force resistance, shown as Force) before physical resistance.
            Combat.ApplyIncomingPhysicalToForceConversion(activator, target, damageType, ref calculatedDamage);
            // Conversion must split first so each portion receives only its own typed Leadership channel.
            calculatedDamage = Combat.ApplyTypedLeadershipDamageTakenModifier(target, calculatedDamage, damageType);
            calculatedDamage = Resistance.ApplyResistanceToDamage(target, damageType, calculatedDamage);
            calculatedDamage = Combat.ApplyDamageTakenModifiers(
                target,
                calculatedDamage,
                activator,
                damageType,
                typedLeadershipReductionAlreadyApplied: true);

            if (criticalRating > 0)
            {
                trackedImpact?.RecordCriticalHit();
                Combat.SendAbilityCriticalHitFeedback(
                    activator,
                    target,
                    trackedImpact?.Ability?.Name);
                Combat.ApplyCriticalHitEffects(
                    activator,
                    target,
                    calculatedDamage,
                    criticalRating,
                    IsTrackedAbilitySingleTarget(activator),
                    skillType);
            }
            return calculatedDamage;
        }

        private static int ApplyNPCAbilitySourceAttackModifiers(uint activator, SkillType skillType, int attack)
        {
            var adjustment = Stat.GetStatAdjustment(activator, StatType.AttackPercentAdjustment);
            if (skillType == SkillType.Force)
            {
                adjustment += Stat.GetStatAdjustment(activator, StatType.ForceAttackPercentAdjustment);
            }

            adjustment += GetHighFPAndStaminaAttackAdjustment(activator);
            adjustment += Combat.GetNearbyStatusTargetAttackAdjustment(activator);
            adjustment += Combat.GetLowHPAttackAdjustment(activator);

            return Math.Max(1, ApplyPercentAdjustment(attack, adjustment));
        }

        private static int GetHighFPAndStaminaAttackAdjustment(uint activator)
        {
            var threshold = Stat.GetStatAdjustment(activator, StatType.HighFPAndStaminaAttackThresholdPercent);
            var adjustment = Stat.GetStatAdjustment(activator, StatType.HighFPAndStaminaAttackPercentAdjustment);

            if (threshold <= 0 || adjustment == 0)
                return 0;

            var currentFP = Stat.GetCurrentFP(activator);
            var maxFP = Stat.GetMaxFP(activator);
            var currentStamina = Stat.GetCurrentStamina(activator);
            var maxStamina = Stat.GetMaxStamina(activator);

            if (maxFP <= 0 || maxStamina <= 0)
                return 0;

            return currentFP >= maxFP * (threshold / 100f) &&
                   currentStamina >= maxStamina * (threshold / 100f)
                ? adjustment
                : 0;
        }

        private static int ApplyPercentAdjustment(int value, int percentAdjustment)
        {
            if (percentAdjustment == 0)
                return value;

            var delta = (int)Math.Ceiling(value * (Math.Abs(percentAdjustment) / 100f));
            return percentAdjustment > 0
                ? value + delta
                : value - delta;
        }

        private static bool ApplyCombatImpactStatusEffect(
            uint activator,
            uint target,
            SkillType skillType,
            Type statusEffect,
            int duration,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory,
            ResistanceType statusResistanceType,
            CombatDamageType sourceDamageType)
        {
            var hasAdditionalStatusEffects = additionalStatusEffects?.Any(x => x != null) ?? false;
            if (duration <= 0 || (statusEffect == null && statusEffectFactory == null && !hasAdditionalStatusEffects))
                return false;

            duration = ApplyAbilityStatusDurationAdjustment(
                activator,
                duration,
                skillType,
                statusEffect,
                additionalStatusEffects,
                statusEffectFactory);

            var statusApplied = false;
            if (statusEffectFactory != null)
                statusApplied |= ApplyCombatImpactTrackedStatusEffect(activator, target, statusEffectFactory, duration, statusResistanceType, sourceDamageType);
            else if (statusEffect != null)
                statusApplied |= ApplyCombatImpactTrackedStatusEffect(activator, target, statusEffect, duration, statusResistanceType, sourceDamageType);

            if (additionalStatusEffects != null)
            {
                foreach (var additionalStatusEffect in additionalStatusEffects.Where(x => x != null && x != statusEffect).Distinct())
                {
                    statusApplied |= ApplyCombatImpactTrackedStatusEffect(activator, target, additionalStatusEffect, duration, statusResistanceType, sourceDamageType);
                }
            }

            return statusApplied;
        }

        private static int ApplyAbilityStatusDurationAdjustment(
            uint activator,
            int duration,
            SkillType skillType,
            Type statusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory)
        {
            if (duration <= 0)
                return duration;

            var trackedImpact = GetTrackedAbilityImpact(activator);
            var perkType = trackedImpact?.Ability?.EffectiveLevelPerkType ?? PerkType.Invalid;
            var adjustment = Combat.GetAbilityStatusDurationPercentAdjustment(
                activator,
                perkType,
                skillType,
                statusEffect,
                additionalStatusEffects,
                statusEffectFactory);
            if (adjustment == 0)
                return duration;

            return Math.Max(1, duration + (int)Math.Ceiling(duration * (adjustment / 100f)));
        }

        private static bool ApplyCombatImpactTrackedStatusEffect(
            uint activator,
            uint target,
            Type type,
            float duration,
            ResistanceType statusResistanceType,
            CombatDamageType sourceDamageType)
        {
            return Resistance.IsValidResistanceType(statusResistanceType)
                ? StatusEffect.ApplyStatusEffect(activator, target, type, duration, statusResistanceType)
                : StatusEffect.ApplyStatusEffect(activator, target, type, duration, sourceDamageType);
        }

        private static bool ApplyCombatImpactTrackedStatusEffect(
            uint activator,
            uint target,
            Func<IStatusEffect> statusEffectFactory,
            float duration,
            ResistanceType statusResistanceType,
            CombatDamageType sourceDamageType)
        {
            var statusEffect = statusEffectFactory?.Invoke();
            if (statusEffect == null)
                return false;

            return Resistance.IsValidResistanceType(statusResistanceType)
                ? StatusEffect.ApplyStatusEffect(activator, target, statusEffect, duration, statusResistanceType)
                : StatusEffect.ApplyStatusEffect(activator, target, statusEffect, duration, sourceDamageType);
        }

        private static AbilityType GetCombatImpactDamageAbility(
            uint activator,
            AbilityType combatImpactDamageAbility)
        {
            var ability = combatImpactDamageAbility != AbilityType.Invalid
                ? combatImpactDamageAbility
                : GetTrackedAbilityImpact(activator)?.Ability?.CombatImpactDamageAbility ?? AbilityType.Invalid;

            return ability == AbilityType.Invalid
                ? AbilityType.Might
                : ability;
        }

        /// <summary>
        /// The hard-CC immunity types which, in addition to their own same-type immunity,
        /// also grant and check immunity against every other type in this set. This lets a
        /// target who was just knocked down (for example) resist being chained into a daze,
        /// stun, immobilize, blind, sleep, or confusion for the same window.
        /// </summary>
        private static readonly HashSet<ImmunityType> HardCrowdControlImmunityTypes = new()
        {
            ImmunityType.Knockdown,
            ImmunityType.Dazed,
            ImmunityType.Stun,
            ImmunityType.Immobilized,
            ImmunityType.Blindness,
            ImmunityType.Sleep,
            ImmunityType.Confused
        };

        /// <summary>
        /// Applies a temporary immunity effect to a particular target.
        /// This will add 20 seconds on top of whatever the ability duration length is.
        /// An existing immunity of the same type is replaced so its timer restarts from this
        /// application without shortening a longer remaining immunity.
        /// If the immunity is one of the hard-CC types, this also grants temporary immunity
        /// to every other hard-CC type for the same duration.
        /// </summary>
        /// <param name="target">The target receiving the immunity</param>
        /// <param name="abilityDuration">The length of the ability's duration. This will be added on top of the 20 seconds.</param>
        /// <param name="immunity">The type of immunity to apply.</param>
        public static void ApplyTemporaryImmunity(uint target, float abilityDuration, ImmunityType immunity)
        {
            ApplyTemporaryImmunityForDuration(
                target,
                TemporaryImmunityBaseDurationSeconds + abilityDuration,
                immunity);
        }

        /// <summary>
        /// Applies the remaining post-control immunity after accounting for time that elapsed
        /// while the affected creature was logged out.
        /// </summary>
        public static void ApplyPostControlImmunity(
            uint target,
            float secondsSinceControlEnded,
            ImmunityType immunity)
        {
            var duration = Math.Max(
                0f,
                TemporaryImmunityBaseDurationSeconds - Math.Max(0f, secondsSinceControlEnded));
            if (duration <= 0f)
                return;

            ApplyTemporaryImmunityForDuration(target, duration, immunity);
        }

        private const float TemporaryImmunityBaseDurationSeconds = 20f;

        private static void ApplyTemporaryImmunityForDuration(
            uint target,
            float duration,
            ImmunityType immunity)
        {
            ApplyTemporaryImmunitySingle(target, duration, immunity);

            if (HardCrowdControlImmunityTypes.Contains(immunity))
            {
                ApplyTemporaryImmunitySingle(target, duration, ImmunityType.HardCrowdControl);
            }
        }

        private static void ApplyTemporaryImmunitySingle(uint target, float requestedDuration, ImmunityType immunity)
        {
            var effectTag = GetTemporaryImmunityEffectTag(immunity);
            var duration = Math.Max(
                requestedDuration,
                GetTemporaryImmunityDurationRemaining(target, effectTag));
            if (duration <= 0f)
                return;

            RemoveEffectByTag(target, effectTag);
            var effect = EffectImmunity(immunity);
            effect = TagEffect(effect, effectTag);
            ApplyEffectToObject(DurationType.Temporary, effect, target, duration);
        }

        private static int GetTemporaryImmunityDurationRemaining(uint target, string effectTag)
        {
            var remaining = 0;
            for (var effect = GetFirstEffect(target);
                 GetIsEffectValid(effect);
                 effect = GetNextEffect(target))
            {
                if (GetEffectTag(effect) == effectTag)
                {
                    remaining = Math.Max(remaining, GetEffectDurationRemaining(effect));
                }
            }

            return remaining;
        }

        public static bool HasTemporaryImmunity(uint target, ImmunityType immunity)
        {
            return HasEffectByTag(target, GetTemporaryImmunityEffectTag(immunity));
        }

        /// <summary>
        /// Checks whether the target is immune to a hard-CC type: it still has immunity to that
        /// specific type, it recently suffered a different hard-CC type and is still within the
        /// shared post-control immunity window, or a hard-CC status is active on it right now -
        /// controls do not stack, they queue behind the post-control window.
        /// </summary>
        public static bool HasHardCrowdControlImmunity(uint target, ImmunityType immunity)
        {
            return HasTemporaryImmunity(target, immunity) ||
                   HasTemporaryImmunity(target, ImmunityType.HardCrowdControl) ||
                   HasActiveHardCrowdControlStatus(target);
        }

        private static bool HasActiveHardCrowdControlStatus(uint target)
        {
            return StatusEffect.GetCreatureStatusEffects(target)
                .GetAllEffects()
                .Any(effect =>
                    (effect.Categories & StatusEffectCategory.HardCrowdControl) ==
                    StatusEffectCategory.HardCrowdControl);
        }

        private static string GetTemporaryImmunityEffectTag(ImmunityType immunity)
        {
            return $"ABILITY_TEMP_IMMUNITY_{immunity}";
        }

        private sealed class TrackedAbilityImpact
        {
            private readonly HashSet<uint> _impactedTargets = new();
            private readonly List<PendingDamageEffect> _pendingDamageEffects = new();

            public AbilityDetail Ability { get; }
            public AbilityImpactSequence Sequence { get; }
            public AbilityImpactSummary Summary { get; }
            public bool CountsAsAttackAttempt { get; }
            public IReadOnlyList<TelegraphGeometry> ActivationAreaTelegraphs { get; }
            public int NextAbilityDamageBonus { get; private set; }
            public int NextAbilityCriticalRatePercentAdjustment { get; }
            public int NextAbilityCriticalDamagePercentAdjustment { get; }
            public int NextAbilityDefenseIgnorePercentAdjustment { get; private set; }
            public int NextAttackEnmityBonus { get; }
            public int StatusAppliedNextAttackDamageBonus { get; }
            public bool DarkForceConversionApplied { get; set; }
            private bool _statusAppliedNextAttackDamageBonusConsumed;

            /// <summary>
            /// Initializes per-impact bonuses, summary classification, and the activation
            /// geometry that can suppress redundant impact markers.
            /// </summary>
            public TrackedAbilityImpact(
                AbilityDetail ability,
                int nextAbilityDamageBonus,
                int nextAbilityCriticalRatePercentAdjustment,
                int nextAbilityDefenseIgnorePercentAdjustment,
                int nextAttackEnmityBonus,
                int statusAppliedNextAttackDamageBonus,
                bool countsAsAttackAttempt,
                int nextAbilityCriticalDamagePercentAdjustment,
                IReadOnlyList<TelegraphGeometry> activationAreaTelegraphs,
                AbilityImpactSequence sequence)
            {
                Ability = ability;
                Sequence = sequence;
                NextAbilityDamageBonus = nextAbilityDamageBonus;
                NextAbilityCriticalRatePercentAdjustment = nextAbilityCriticalRatePercentAdjustment;
                NextAbilityCriticalDamagePercentAdjustment = nextAbilityCriticalDamagePercentAdjustment;
                NextAbilityDefenseIgnorePercentAdjustment = nextAbilityDefenseIgnorePercentAdjustment;
                NextAttackEnmityBonus = nextAttackEnmityBonus;
                StatusAppliedNextAttackDamageBonus = statusAppliedNextAttackDamageBonus;
                CountsAsAttackAttempt = countsAsAttackAttempt;
                ActivationAreaTelegraphs = activationAreaTelegraphs;
                Summary = new AbilityImpactSummary
                {
                    SkillType = ability.SkillType,
                    IsAreaAbility = ability.IsAreaAbility,
                    IsSingleTargetAbility = ability.IsSingleTargetAbility
                };
            }

            public void ConsumeStatusAppliedNextAttackDamageBonus(uint activator)
            {
                if (_statusAppliedNextAttackDamageBonusConsumed ||
                    StatusAppliedNextAttackDamageBonus <= 0)
                {
                    return;
                }

                Combat.ConsumeStatusAppliedNextAttackDamageBonus(activator);
                NextAbilityDamageBonus -= StatusAppliedNextAttackDamageBonus;
                _statusAppliedNextAttackDamageBonusConsumed = true;
            }

            public void AddDefenseIgnorePercentAdjustment(int adjustment)
            {
                NextAbilityDefenseIgnorePercentAdjustment += adjustment;
            }

            public void RecordShape(SkillType skillType, bool isArea)
            {
                if (Summary.SkillType == SkillType.Invalid && skillType != SkillType.Invalid)
                {
                    Summary.SkillType = skillType;
                }

                if (isArea)
                {
                    Summary.IsAreaAbility = true;
                    Summary.IsSingleTargetAbility = false;
                }
                else if (!Summary.IsAreaAbility)
                {
                    Summary.IsSingleTargetAbility = true;
                }
            }

            public void RecordTarget(uint target)
            {
                _impactedTargets.Add(target);
                Summary.ImpactedTargetCount = _impactedTargets.Count;

                if (_impactedTargets.Count > 1)
                {
                    Summary.IsAreaAbility = true;
                    Summary.IsSingleTargetAbility = false;
                }
            }

            public void RecordCriticalHit()
            {
                Summary.CriticalHitCount++;
            }

            public void QueueDamageEffect(uint target, int damage, DamageType damageType)
            {
                QueueDamageEffect(target, damage, damageType, CombatDamageType.Invalid);
            }

            public void QueueDirectDamageEffect(
                uint target,
                int damage,
                DamageType damageType,
                CombatDamageType combatDamageType)
            {
                QueueDamageEffect(target, damage, damageType, combatDamageType);
            }

            private void QueueDamageEffect(
                uint target,
                int damage,
                DamageType damageType,
                CombatDamageType combatDamageType)
            {
                if (!GetIsObjectValid(target) || damage <= 0)
                    return;

                Summary.AttributedDamage += damage;
                _pendingDamageEffects.Add(new PendingDamageEffect(
                    target,
                    damage,
                    damageType,
                    combatDamageType));
            }

            public void FlushDamageEffects(uint activator)
            {
                if (_pendingDamageEffects.Count <= 0)
                    return;

                var effects = _pendingDamageEffects.ToArray();
                _pendingDamageEffects.Clear();

                AssignCommand(activator, () =>
                {
                    foreach (var effect in effects)
                    {
                        if (!GetIsObjectValid(effect.Target))
                            continue;

                        var isDirectAbilityDamage = effect.CombatDamageType != CombatDamageType.Invalid;
                        if (isDirectAbilityDamage)
                        {
                            // Resolve the direct-hit lifecycle inside this loop so the next queued
                            // hit observes temporary HP and status effects consumed by this one.
                            StatusEffect.NotifyPreDamageStatusEffects(
                                activator,
                                effect.Target,
                                effect.Damage,
                                effect.CombatDamageType);
                            Combat.SendTemporaryHitPointDamageFeedback(
                                activator,
                                effect.Target,
                                effect.Damage);
                            Combat.ApplyDamageReflectionEffects(
                                activator,
                                effect.Target,
                                effect.Damage,
                                effect.CombatDamageType);
                        }

                        ApplyEffectToObject(
                            DurationType.Instant,
                            EffectDamage(effect.Damage, effect.DamageType),
                            effect.Target);
                    }
                });
            }
        }

        private sealed class PendingDamageEffect
        {
            public PendingDamageEffect(
                uint target,
                int damage,
                DamageType damageType,
                CombatDamageType combatDamageType)
            {
                Target = target;
                Damage = damage;
                DamageType = damageType;
                CombatDamageType = combatDamageType;
            }

            public uint Target { get; }
            public int Damage { get; }
            public DamageType DamageType { get; }
            public CombatDamageType CombatDamageType { get; }
        }
    }
}
