using System.Collections.Generic;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service.AbilityService
{
    public class AbilityBuilder
    {
        private const Animation DefaultAnimationOverwriteCarrier = Animation.LoopingPause;
        private const string LoopingPauseSourceAnimationName = "pause1";

        private readonly Dictionary<FeatType, AbilityDetail> _abilities = new Dictionary<FeatType, AbilityDetail>();
        private AbilityDetail _activeAbility;

        /// <summary>
        /// The perk the ability currently being built belongs to. Used by shared configuration
        /// helpers that need a per-perk identity (e.g. temporary HP stacking keys).
        /// </summary>
        public PerkType ActiveEffectiveLevelPerkType => _activeAbility?.EffectiveLevelPerkType ?? PerkType.Invalid;

        /// <summary>
        /// Creates a new ability.
        /// </summary>
        /// <param name="featType">The type of feat to link this ability to.</param>
        /// <param name="effectiveLevelPerkType">The type of perk used for determining effective level.</param>
        /// <returns>An ability builder with the configured options</returns>
        public AbilityBuilder Create(FeatType featType, PerkType effectiveLevelPerkType)
        {
            _activeAbility = new AbilityDetail();
            _activeAbility.EffectiveLevelPerkType = effectiveLevelPerkType;
            _abilities[featType] = _activeAbility;

            return this;
        }

        /// <summary>
        /// Sets the name of the active ability we're building
        /// </summary>
        /// <param name="name">The name of the ability to set.</param>
        /// <returns>An ability builder with the configured options</returns>
        public AbilityBuilder Name(string name)
        {
            _activeAbility.Name = name;

            return this;
        }

        /// <summary>
        /// Indicates this ability is casted which fires once after the end of a configured delay (or instantly if no delay is assigned).
        /// </summary>
        /// <returns>An ability builder with the configured options.</returns>
        public AbilityBuilder IsCastedAbility()
        {
            _activeAbility.ActivationType = AbilityActivationType.Casted;

            return this;
        }

        /// <summary>
        /// Indicates this ability is executed on the next weapon hit.
        /// </summary>
        /// <returns>An ability builder with the configured options.</returns>
        public AbilityBuilder IsWeaponAbility()
        {
            _activeAbility.ActivationType = AbilityActivationType.Weapon;
            _activeAbility.RequiresTarget = false;

            return this;
        }

        /// <summary>
        /// Marks the activation delay as a channel: the ability's impact, costs, and recast delay all
        /// apply when the channel starts and the granted effects run for the channel itself.
        /// Interrupting the channel runs the interrupt action so the granted effects end early;
        /// the recast delay is not refunded.
        /// </summary>
        /// <param name="channelInterruptAction">Action run against the activator when the channel is interrupted.</param>
        /// <returns>An ability builder with the configured options.</returns>
        public AbilityBuilder IsChanneledAbility(Action<uint> channelInterruptAction)
        {
            _activeAbility.IsChanneled = true;
            _activeAbility.ChannelInterruptAction = channelInterruptAction;

            return this;
        }

        /// <summary>
        /// Indicates this ability can be used while in space.
        /// </summary>
        /// <returns>An ability builder with the configured options.</returns>
        public AbilityBuilder CanBeUsedInSpace()
        {
            _activeAbility.CanBeUsedInSpace = true;

            return this;
        }

        /// <summary>
        /// Assigns an animation to the caster of the ability. This will be played when the creature uses the ability.
        /// Calling this more than once will replace the previous animation.
        /// </summary>
        /// <param name="animation">The animation to set.</param>
        /// <returns>An ability builder with the configured options.</returns>
        public AbilityBuilder UsesAnimation(Animation animation)
        {
            _activeAbility.AnimationType = animation;
            _activeAbility.AnimationSourceAnimationName = string.Empty;
            _activeAbility.AnimationReplacementAnimationName = string.Empty;
            _activeAbility.AnimationRestoreDelaySeconds = 0f;

            return this;
        }

        /// <summary>
        /// Assigns an activation animation that temporarily overwrites a model animation key before playback.
        /// </summary>
        /// <param name="replacementAnimationName">The model animation key to play instead.</param>
        /// <param name="restoreDelaySeconds">The number of seconds before the source animation key is restored.</param>
        /// <returns>An ability builder with the configured options.</returns>
        public AbilityBuilder UsesAnimationOverwrite(
            string replacementAnimationName,
            float restoreDelaySeconds = 1.1f)
        {
            return UsesAnimationOverwrite(
                DefaultAnimationOverwriteCarrier,
                replacementAnimationName,
                restoreDelaySeconds);
        }

        /// <summary>
        /// Assigns an activation animation that temporarily overwrites the carrier animation's model key before playback.
        /// </summary>
        /// <param name="animation">The engine animation used to trigger the source animation key.</param>
        /// <param name="replacementAnimationName">The model animation key to play instead.</param>
        /// <param name="restoreDelaySeconds">The number of seconds before the source animation key is restored.</param>
        /// <returns>An ability builder with the configured options.</returns>
        public AbilityBuilder UsesAnimationOverwrite(
            Animation animation,
            string replacementAnimationName,
            float restoreDelaySeconds = 1.1f)
        {
            return UsesAnimationOverwrite(
                animation,
                GetAnimationSourceAnimationName(animation),
                replacementAnimationName,
                restoreDelaySeconds);
        }

        /// <summary>
        /// Assigns an activation animation that temporarily overwrites a specific model animation key before playback.
        /// </summary>
        /// <param name="animation">The engine animation used to trigger the source animation key.</param>
        /// <param name="sourceAnimationName">The existing model animation key to replace.</param>
        /// <param name="replacementAnimationName">The model animation key to play instead.</param>
        /// <param name="restoreDelaySeconds">The number of seconds before the source animation key is restored.</param>
        /// <returns>An ability builder with the configured options.</returns>
        public AbilityBuilder UsesAnimationOverwrite(
            Animation animation,
            string sourceAnimationName,
            string replacementAnimationName,
            float restoreDelaySeconds = 1.1f)
        {
            if (string.IsNullOrWhiteSpace(sourceAnimationName))
                throw new ArgumentException("Source animation name is required.", nameof(sourceAnimationName));
            if (string.IsNullOrWhiteSpace(replacementAnimationName))
                throw new ArgumentException("Replacement animation name is required.", nameof(replacementAnimationName));
            if (restoreDelaySeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(restoreDelaySeconds), restoreDelaySeconds, "Restore delay must be positive.");

            _activeAbility.AnimationType = animation;
            _activeAbility.AnimationSourceAnimationName = sourceAnimationName;
            _activeAbility.AnimationReplacementAnimationName = replacementAnimationName;
            _activeAbility.AnimationRestoreDelaySeconds = restoreDelaySeconds;

            return this;
        }

        /// <summary>
        /// Assigns an animation to the caster when this ability applies its combat impact.
        /// Calling this more than once will replace the previous animation.
        /// </summary>
        /// <param name="animation">The animation to set.</param>
        /// <returns>An ability builder with the configured options.</returns>
        public AbilityBuilder UsesImpactAnimation(Animation animation)
        {
            _activeAbility.ImpactAnimationType = animation;
            _activeAbility.ImpactAnimationSourceAnimationName = string.Empty;
            _activeAbility.ImpactAnimationReplacementAnimationName = string.Empty;
            _activeAbility.ImpactAnimationRestoreDelaySeconds = 0f;

            return this;
        }

        /// <summary>
        /// Assigns an impact animation that temporarily overwrites a model animation key before playback.
        /// </summary>
        /// <param name="replacementAnimationName">The model animation key to play instead.</param>
        /// <param name="restoreDelaySeconds">The number of seconds before the source animation key is restored.</param>
        /// <returns>An ability builder with the configured options.</returns>
        public AbilityBuilder UsesImpactAnimationOverwrite(
            string replacementAnimationName,
            float restoreDelaySeconds = 1.1f)
        {
            return UsesImpactAnimationOverwrite(
                DefaultAnimationOverwriteCarrier,
                replacementAnimationName,
                restoreDelaySeconds);
        }

        /// <summary>
        /// Assigns an impact animation that temporarily overwrites the carrier animation's model key before playback.
        /// </summary>
        /// <param name="animation">The engine animation used to trigger the source animation key.</param>
        /// <param name="replacementAnimationName">The model animation key to play instead.</param>
        /// <param name="restoreDelaySeconds">The number of seconds before the source animation key is restored.</param>
        /// <returns>An ability builder with the configured options.</returns>
        public AbilityBuilder UsesImpactAnimationOverwrite(
            Animation animation,
            string replacementAnimationName,
            float restoreDelaySeconds = 1.1f)
        {
            return UsesImpactAnimationOverwrite(
                animation,
                GetAnimationSourceAnimationName(animation),
                replacementAnimationName,
                restoreDelaySeconds);
        }

        /// <summary>
        /// Assigns an impact animation that temporarily overwrites a specific model animation key before playback.
        /// </summary>
        /// <param name="animation">The engine animation used to trigger the source animation key.</param>
        /// <param name="sourceAnimationName">The existing model animation key to replace.</param>
        /// <param name="replacementAnimationName">The model animation key to play instead.</param>
        /// <param name="restoreDelaySeconds">The number of seconds before the source animation key is restored.</param>
        /// <returns>An ability builder with the configured options.</returns>
        public AbilityBuilder UsesImpactAnimationOverwrite(
            Animation animation,
            string sourceAnimationName,
            string replacementAnimationName,
            float restoreDelaySeconds = 1.1f)
        {
            if (string.IsNullOrWhiteSpace(sourceAnimationName))
                throw new ArgumentException("Source animation name is required.", nameof(sourceAnimationName));
            if (string.IsNullOrWhiteSpace(replacementAnimationName))
                throw new ArgumentException("Replacement animation name is required.", nameof(replacementAnimationName));
            if (restoreDelaySeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(restoreDelaySeconds), restoreDelaySeconds, "Restore delay must be positive.");

            _activeAbility.ImpactAnimationType = animation;
            _activeAbility.ImpactAnimationSourceAnimationName = sourceAnimationName;
            _activeAbility.ImpactAnimationReplacementAnimationName = replacementAnimationName;
            _activeAbility.ImpactAnimationRestoreDelaySeconds = restoreDelaySeconds;

            return this;
        }

        private static string GetAnimationSourceAnimationName(Animation animation)
        {
            return animation switch
            {
                Animation.LoopingPause => LoopingPauseSourceAnimationName,
                _ => throw new ArgumentException(
                    $"No model animation source key is mapped for {animation}. Use the overload that accepts a source animation name.",
                    nameof(animation))
            };
        }

        /// <summary>
        /// The ability will not display an activation message to nearby players if this is set.
        /// </summary>
        /// <returns>An ability builder with the configured options.</returns>
        public AbilityBuilder HideActivationMessage()
        {
            _activeAbility.DisplaysActivationMessage = false;

            return this;
        }

        /// <summary>
        /// Assigns a visual effect to the activator of the ability. This will display while casting.
        /// Calling this more than once will replace the previous visual effect.
        /// </summary>
        /// <param name="vfx">The visual effect to display.</param>
        /// <returns>An ability builder with the configured options</returns>
        public AbilityBuilder DisplaysVisualEffectWhenActivating(VisualEffect vfx = VisualEffect.Vfx_Dur_Iounstone_Yellow)
        {
            _activeAbility.ActivationVisualEffect = vfx;

            return this;
        }

        public AbilityBuilder PlaysSoundWhenActivating(string soundResref)
        {
            _activeAbility.ActivationSound = soundResref;

            return this;
        }

        public AbilityBuilder PlaysSoundOnImpact(string soundResref)
        {
            _activeAbility.ImpactSound = soundResref;

            return this;
        }

        /// <summary>
        /// Indicates this ability runs an action immediately after validation but before any delays or impacts.
        /// This can be used to disable an active effect, like an aura, if a player uses the ability a second time.
        /// The result of the action can be true or false. If true, the delay and impact action will run when finished.
        /// If false, only this activation action will run and then the ability will exit.
        /// </summary>
        /// <param name="action">The action to fire when an ability passes validation but before the delay/impact process occurs.</param>
        /// <returns>An ability builder with the configured options</returns>
        public AbilityBuilder HasActivationAction(AbilityActivationAction action)
        {
            _activeAbility.ActivationAction = action;

            return this;
        }

        /// <summary>
        /// Assigns an impact action on the active ability we're building.
        /// Calling this more than once will replace the previous action.
        /// Impact actions are fired when a ability is used. The timing of when it fires depends on the activation type.
        /// "Casted" abilities fire the impact action at the end of the casting phase.
        /// "Queued" abilities fire the impact action on the next weapon hit.
        /// </summary>
        /// <param name="action">The action to fire when an ability impacts a target.</param>
        /// <returns>An ability builder with the configured options</returns>
        public AbilityBuilder HasImpactAction(AbilityImpactAction action)
        {
            _activeAbility.ImpactAction = action;

            return this;
        }

        /// <summary>
        /// Delays impact resolution after activation completes while keeping the activator busy.
        /// This is intended for effect choreography such as travel animations, not cast time.
        /// </summary>
        /// <param name="seconds">The delay between activation completion and impact.</param>
        /// <returns>An ability builder with the configured options.</returns>
        public AbilityBuilder HasImpactDelay(float seconds)
        {
            _activeAbility.ImpactDelay = seconds;

            return this;
        }

        public AbilityBuilder RemoveStatusEffectOnPerkRefund(Type statusEffectType)
        {
            if (statusEffectType == null)
                throw new ArgumentNullException(nameof(statusEffectType));

            if (!_activeAbility.StatusEffectTypesRemovedOnPerkRefund.Contains(statusEffectType))
                _activeAbility.StatusEffectTypesRemovedOnPerkRefund.Add(statusEffectType);

            return this;
        }

        public AbilityBuilder RemoveSourceOwnedStatusEffectOnPerkRefund(Type statusEffectType)
        {
            if (statusEffectType == null)
                throw new ArgumentNullException(nameof(statusEffectType));

            if (!_activeAbility.SourceOwnedStatusEffectTypesRemovedOnPerkRefund.Contains(statusEffectType))
                _activeAbility.SourceOwnedStatusEffectTypesRemovedOnPerkRefund.Add(statusEffectType);

            return this;
        }

        /// <summary>
        /// Assigns custom validation logic on the active ability we're building.
        /// Calling this more than once will replace the previous action.
        /// Custom validation runs twice: Once when a creature starts to use an ability and again when they finish.
        /// Returning a null or empty string will signify the validation passes.
        /// </summary>
        /// <param name="action">The action to fire when custom validation is run.</param>
        /// <returns>An ability builder with the configured options.</returns>
        public AbilityBuilder HasCustomValidation(AbilityCustomValidationAction action)
        {
            _activeAbility.CustomValidation = action;

            return this;
        }

        public AbilityBuilder HasAITarget(AITargetSelector selector)
        {
            _activeAbility.AITargetSelector = selector;

            return this;
        }

        public AbilityBuilder HasAIScore(AIScoreCalculation score)
        {
            _activeAbility.AIScore = score;

            return this;
        }

        /// <summary>
        /// Indicates this ability requires a concrete target object.
        /// </summary>
        /// <returns>An ability builder with the configured options.</returns>
        public AbilityBuilder RequiresTarget()
        {
            _activeAbility.RequiresTarget = true;

            return this;
        }

        /// <summary>
        /// Indicates this ability should resolve its target from the creature's current attack target.
        /// </summary>
        /// <returns>An ability builder with the configured options.</returns>
        public AbilityBuilder UsesActiveAttackTarget()
        {
            _activeAbility.UsesActiveAttackTarget = true;
            _activeAbility.RequiresTarget = false;

            return this;
        }

        /// <summary>
        /// Assigns an activation delay on the active ability we're building.
        /// This is typically used for casting times.
        /// Calling this more than once will replace the previous activation delay.
        /// </summary>
        /// <param name="delayAction">An action which calculates the delay.</param>
        /// <returns>An ability builder with the configured options</returns>
        public AbilityBuilder HasActivationDelay(AbilityActivationDelayAction delayAction)
        {
            _activeAbility.ActivationDelay = delayAction;

            return this;
        }

        /// <summary>
        /// Assigns an activation delay on the active ability we're building.
        /// This is typically used for casting times.
        /// Calling this more than once will replace the previous activation delay.
        /// </summary>
        /// <param name="seconds">The amount of time to delay, in seconds</param>
        /// <returns>An ability builder with the configured options</returns>
        public AbilityBuilder HasActivationDelay(float seconds)
        {
            _activeAbility.ActivationDelay = (activator, target, level) => seconds;

            return this;
        }

        /// <summary>
        /// Assigns a recast delay on the active ability we're building.
        /// This prevents the ability from being used again until the specified time has passed.
        /// Calling this more than once will replace the previous recast delay.
        /// </summary>
        /// <param name="recastGroup">The recast group this delay will fall under.</param>
        /// <param name="delay">An action which determines the recast delay.</param>
        /// <returns>An ability builder with the configured options.</returns>
        public AbilityBuilder HasRecastDelay(RecastGroup recastGroup, AbilityRecastDelayAction delay)
        {
            _activeAbility.RecastGroup = recastGroup;
            _activeAbility.RecastDelay = delay;

            return this;
        }

        /// <summary>
        /// Assigns a recast delay on the active ability we're building.
        /// This prevents the ability from being used again until the specified time has passed.
        /// Calling this more than once will replace the previous recast delay.
        /// </summary>
        /// <param name="recastGroup">The recast group this delay will fall under.</param>
        /// <param name="seconds">The number of seconds to delay.</param>
        /// <returns>An ability builder with the configured options.</returns>
        public AbilityBuilder HasRecastDelay(RecastGroup recastGroup, float seconds)
        {
            _activeAbility.RecastGroup = recastGroup;
            _activeAbility.RecastDelay = activator => seconds;

            return this;
        }

        /// <summary>
        /// Adds an FP requirement to use the ability at this level.
        /// </summary>
        /// <param name="requiredFP">The amount of FP needed to use this ability at this level.</param>
        /// <returns>An ability builder with the configured options</returns>
        public AbilityBuilder RequirementFP(int requiredFP)
        {
            var requirement = new AbilityRequirementFP(requiredFP);
            _activeAbility.Requirements.Add(requirement);

            return this;
        }

        /// <summary>
        /// Updates the max range of this ability (default is 5.0, i.e. melee range).
        /// </summary>
        /// <param name="maxRange">The maximum range of the ability.</param>
        /// <returns>An ability builder with the configured options</returns>
        public AbilityBuilder HasMaxRange(float maxRange)
        {
            _activeAbility.MaxRange = maxRange;
            _activeAbility.HasExplicitMaxRange = true;
            return this;
        }

        /// <summary>
        /// Adds a stamina requirement to use the ability at this level.
        /// </summary>
        /// <param name="requiredSTM">The amount of STM needed to use this ability at this level.</param>
        /// <returns>An ability builder with the configured options</returns>
        public AbilityBuilder RequirementStamina(int requiredSTM)
        {
            var requirement = new AbilityRequirementStamina(requiredSTM);
            _activeAbility.Requirements.Add(requirement);

            return this;
        }

        /// <summary>
        /// Adds an item requirement to use the ability at this level.
        /// </summary>
        /// <param name="itemResref">The resref of the required inventory item.</param>
        /// <param name="quantity">The number of items consumed on activation.</param>
        /// <param name="preserveChanceStatType">Optional stat containing the percent chance to preserve the item.</param>
        /// <returns>An ability builder with the configured options</returns>
        public AbilityBuilder RequirementItem(
            string itemResref,
            int quantity = 1,
            StatType preserveChanceStatType = StatType.Invalid)
        {
            var requirement = new AbilityRequirementItem(
                itemResref,
                quantity,
                preserveChanceStatType);
            _activeAbility.Requirements.Add(requirement);

            return this;
        }

        /// <summary>
        /// Indicates this ability is a hostile ability and should not target friendlies.
        /// </summary>
        /// <returns>An ability builder with the configured options</returns>
        public AbilityBuilder IsHostileAbility()
        {
            _activeAbility.IsHostileAbility = true;

            return this;
        }

        /// <summary>
        /// Marks this ability as a healing option for explicit companion Heal Me orders.
        /// </summary>
        /// <returns>An ability builder with the configured options</returns>
        public AbilityBuilder IsHealingAbility()
        {
            _activeAbility.IsHealingAbility = true;

            return this;
        }

        public AbilityBuilder SuppressesSourceStatusStackRiders()
        {
            _activeAbility.SuppressesSourceStatusStackRiders = true;

            return this;
        }

        /// <summary>
        /// Indicates this ability breaks stealth and invisibility when used.
        /// </summary>
        /// <returns>An ability builder with the configured options</returns>
        public AbilityBuilder BreaksStealth()
        {
            _activeAbility.BreaksStealth = true;

            return this;
        }

        /// <summary>
        /// Prevents the activation wind-up from clearing stealth before the ability's impact runs.
        /// Intended for abilities whose impact must inspect or toggle the current stealth state.
        /// </summary>
        public AbilityBuilder PreservesStealthDuringActivation()
        {
            _activeAbility.PreservesStealthDuringActivation = true;

            return this;
        }

        /// <summary>
        /// Saves the ability level of the ability to be pulled when used later.
        /// </summary>
        /// <param name="level">The level of the ability</param>
        /// <returns>An ability builder with the configured options</returns>
        public AbilityBuilder Level(int level)
        {
            _activeAbility.AbilityLevel = level;

            return this;
        }

        public AbilityBuilder SkillType(SkillType skillType)
        {
            _activeAbility.SkillType = skillType;

            return this;
        }

        public AbilityBuilder CombatImpactDamageAbility(AbilityType abilityType)
        {
            _activeAbility.CombatImpactDamageAbility = abilityType;

            return this;
        }

        public AbilityBuilder IsAreaAbility()
        {
            _activeAbility.IsAreaAbility = true;
            _activeAbility.IsSingleTargetAbility = false;

            return this;
        }

        public AbilityBuilder HasTargetingSphere(
            Spell spell,
            float radius,
            AbilityTargetingFlags flags,
            AbilityTargetingSizeResolver sizeResolver = null)
        {
            return HasTargeting(
                spell,
                AbilityTargetingShapeType.Sphere,
                radius,
                0f,
                flags,
                sizeResolver);
        }

        public AbilityBuilder HasActivationTargetingSphere(
            float radius,
            AbilityTargetingFlags flags,
            AbilityTargetingSizeResolver sizeResolver = null)
        {
            return HasTargeting(
                Spell.Invalid,
                AbilityTargetingShapeType.Sphere,
                radius,
                0f,
                flags,
                sizeResolver,
                false);
        }

        public AbilityBuilder AddActivationTargetingSphere(
            float radius,
            AbilityTargetingFlags flags,
            AbilityTargetingSizeResolver sizeResolver = null)
        {
            return AddActivationTargeting(
                AbilityTargetingShapeType.Sphere,
                radius,
                0f,
                flags,
                sizeResolver);
        }

        public AbilityBuilder HasTargetingLine(
            Spell spell,
            float length,
            float width,
            AbilityTargetingFlags flags,
            AbilityTargetingSizeResolver sizeResolver = null)
        {
            return HasTargeting(
                spell,
                AbilityTargetingShapeType.Rect,
                length,
                width,
                flags,
                sizeResolver);
        }

        public AbilityBuilder HasActivationTargetingLine(
            float length,
            float width,
            AbilityTargetingFlags flags,
            AbilityTargetingSizeResolver sizeResolver = null)
        {
            return HasTargeting(
                Spell.Invalid,
                AbilityTargetingShapeType.Rect,
                length,
                width,
                flags,
                sizeResolver,
                false);
        }

        public AbilityBuilder AddActivationTargetingLine(
            float length,
            float width,
            AbilityTargetingFlags flags,
            AbilityTargetingSizeResolver sizeResolver = null)
        {
            return AddActivationTargeting(
                AbilityTargetingShapeType.Rect,
                length,
                width,
                flags,
                sizeResolver);
        }

        public AbilityBuilder HasTargetingCone(
            Spell spell,
            float length,
            float width,
            AbilityTargetingFlags flags,
            AbilityTargetingSizeResolver sizeResolver = null)
        {
            return HasTargeting(
                spell,
                AbilityTargetingShapeType.Cone,
                length,
                width,
                flags,
                sizeResolver);
        }

        public AbilityBuilder HasActivationTargetingCone(
            float length,
            float width,
            AbilityTargetingFlags flags,
            AbilityTargetingSizeResolver sizeResolver = null)
        {
            return HasTargeting(
                Spell.Invalid,
                AbilityTargetingShapeType.Cone,
                length,
                width,
                flags,
                sizeResolver,
                false);
        }

        public AbilityBuilder AddActivationTargetingCone(
            float length,
            float width,
            AbilityTargetingFlags flags,
            AbilityTargetingSizeResolver sizeResolver = null)
        {
            return AddActivationTargeting(
                AbilityTargetingShapeType.Cone,
                length,
                width,
                flags,
                sizeResolver);
        }

        public AbilityBuilder HasTargeting(
            Spell spell,
            AbilityTargetingShapeType shape,
            float sizeX,
            float sizeY,
            AbilityTargetingFlags flags,
            AbilityTargetingSizeResolver sizeResolver = null,
            bool updatesClientTargeting = true)
        {
            _activeAbility.Targeting = new AbilityTargetingDetail(
                spell,
                shape,
                sizeX,
                sizeY,
                flags,
                sizeResolver,
                updatesClientTargeting);

            return this;
        }

        public AbilityBuilder AddActivationTargeting(
            AbilityTargetingShapeType shape,
            float sizeX,
            float sizeY,
            AbilityTargetingFlags flags,
            AbilityTargetingSizeResolver sizeResolver = null)
        {
            _activeAbility.AdditionalActivationTargeting.Add(
                new AbilityTargetingDetail(
                    Spell.Invalid,
                    shape,
                    sizeX,
                    sizeY,
                    flags,
                    sizeResolver,
                    false));

            return this;
        }

        public AbilityBuilder IsSingleTargetAbility()
        {
            _activeAbility.IsSingleTargetAbility = true;
            _activeAbility.IsAreaAbility = false;

            return this;
        }

        public AbilityBuilder TriggersDarkForceConversion()
        {
            _activeAbility.TriggersDarkForceConversion = true;

            return this;
        }

        /// <summary>
        /// Marks the active ability as a Mimicry technique learned from an enemy creature's ability.
        /// </summary>
        /// <param name="sourceCreatureFeat">The NPC feat this technique is copied from.</param>
        /// <param name="skillRequirement">The Mimicry rank required to learn and equip this technique.</param>
        /// <param name="slotCost">The number of technique slots this ability consumes when equipped.</param>
        /// <returns>An ability builder with the configured options</returns>
        public AbilityBuilder MimicryTechnique(FeatType sourceCreatureFeat, int skillRequirement, int slotCost)
        {
            if (sourceCreatureFeat == FeatType.Invalid)
                throw new ArgumentException($"{nameof(sourceCreatureFeat)} must be a real creature ability feat.");
            if (skillRequirement < 0 || skillRequirement > 50)
                throw new ArgumentException($"{nameof(skillRequirement)} must be between 0 and 50.");
            if (slotCost < 1)
                throw new ArgumentException($"{nameof(slotCost)} must be at least 1.");

            _activeAbility.IsMimicryTechnique = true;
            _activeAbility.MimicrySourceFeat = sourceCreatureFeat;
            _activeAbility.MimicrySkillRequirement = skillRequirement;
            _activeAbility.MimicrySlotCost = slotCost;

            // The NPC original keeps RequiresTarget so the AI only selects it with an enemy in
            // hand, but the player-facing technique aims with a cursor: a mandatory creature
            // target would break empty-ground casts of its line/cone/placed area.
            if (_activeAbility.IsAreaAbility)
            {
                _activeAbility.RequiresTarget = false;
            }

            return this;
        }

        /// <summary>
        /// Marks the active ability as a Mimicry trait: a passive technique learned from an enemy that
        /// contributes static stats for as long as it is equipped, instead of granting a hotbar action.
        /// Otherwise identical to a technique for learning, slot budgeting, and skill gating.
        ///
        /// Declare the trait's bonuses with <see cref="MimicryTraitStat"/> and
        /// <see cref="MimicryTraitResistance"/>. Equipping a trait deliberately applies no persistent
        /// status effect to the wearer: the bonus is static for the whole time it is slotted, so there
        /// is no state to show on the status icon bar and nothing to keep in sync across death or
        /// relog. That covers the trait's own lifecycle only — an on-hit proc trait still inflicts an
        /// ordinary status effect on its target when it fires.
        /// </summary>
        /// <param name="sourceCreatureFeat">The NPC feat this trait is copied from.</param>
        /// <param name="skillRequirement">The Mimicry rank required to learn and equip this trait.</param>
        /// <param name="slotCost">The number of technique slots this trait consumes when equipped.</param>
        /// <returns>An ability builder with the configured options</returns>
        public AbilityBuilder MimicryTrait(FeatType sourceCreatureFeat, int skillRequirement, int slotCost)
        {
            MimicryTechnique(sourceCreatureFeat, skillRequirement, slotCost);

            _activeAbility.IsMimicryTrait = true;

            return this;
        }

        /// <summary>
        /// Adds a flat stat adjustment granted while this Mimicry trait is equipped.
        /// </summary>
        /// <param name="stat">The stat to adjust.</param>
        /// <param name="amount">The amount to adjust it by.</param>
        /// <returns>An ability builder with the configured options</returns>
        public AbilityBuilder MimicryTraitStat(StatType stat, int amount)
        {
            if (!_activeAbility.IsMimicryTrait)
                throw new ArgumentException($"{nameof(MimicryTraitStat)} requires {nameof(MimicryTrait)} to be called first.");
            if (stat == StatType.Invalid)
                throw new ArgumentException($"{nameof(stat)} must be a real stat.");

            _activeAbility.MimicryTraitStats[stat] = amount;

            return this;
        }

        /// <summary>
        /// Adds a resistance adjustment granted while this Mimicry trait is equipped.
        /// </summary>
        /// <param name="resistance">The resistance to adjust.</param>
        /// <param name="amount">The amount to adjust it by.</param>
        /// <returns>An ability builder with the configured options</returns>
        public AbilityBuilder MimicryTraitResistance(ResistanceType resistance, int amount)
        {
            if (!_activeAbility.IsMimicryTrait)
                throw new ArgumentException($"{nameof(MimicryTraitResistance)} requires {nameof(MimicryTrait)} to be called first.");
            if (resistance == ResistanceType.Invalid)
                throw new ArgumentException($"{nameof(resistance)} must be a real resistance.");

            _activeAbility.MimicryTraitResistances[resistance] = amount;

            return this;
        }

        /// <summary>
        /// Marks a mimicked technique as a self-toggle stance: it activates/deactivates a stance status
        /// effect rather than casting a hostile ability, so the contract tests exempt it from the
        /// hostility / damage-element / combat-scaling assertions.
        /// </summary>
        public AbilityBuilder MimicryStance(FeatType sourceCreatureFeat, int skillRequirement, int slotCost)
        {
            MimicryTechnique(sourceCreatureFeat, skillRequirement, slotCost);
            _activeAbility.IsMimicryStance = true;
            return this;
        }

        /// <summary>
        /// Marks a mimicked technique as a non-damaging utility active (control, debuff, support, or
        /// zone) that declares no damage element or scaling attribute — for example an ally-targeting
        /// support cast. Exempts it from the damage-element / scaling / hostility contract assertions.
        /// </summary>
        public AbilityBuilder MimicryUtility()
        {
            _activeAbility.IsMimicryUtility = true;
            return this;
        }

        /// <summary>
        /// Declares the damage type a mimicked technique deals, used for damage-type loadout set
        /// bonuses (elemental resonance).
        /// </summary>
        public AbilityBuilder MimicryElement(CombatDamageType element)
        {
            _activeAbility.MimicryElement = element;
            return this;
        }

        /// <summary>
        /// Returns a built list of abilities.
        /// </summary>
        /// <returns>A list of built abilities.</returns>
        public Dictionary<FeatType, AbilityDetail> Build()
        {
            return _abilities;
        }
    }
}
