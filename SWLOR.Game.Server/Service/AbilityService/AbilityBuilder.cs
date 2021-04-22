using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.AbilityService
{
    public class AbilityBuilder
    {
        private readonly Dictionary<FeatType, AbilityDetail> _abilities = new Dictionary<FeatType, AbilityDetail>();
        private AbilityDetail _activeAbility;

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

            return this;
        }
        
        /// <summary>
        /// Indicates this is a concentration ability which stays active and drains resources until turned off or player runs out of required resources.
        /// A corresponding status effect must also be defined and this will be applied when the concentration ability is activated and removed when it ends.
        /// </summary>
        /// <param name="concentrationStatusEffectType">The status effect to use for this concentration ability.</param>
        /// <returns>An ability builder with the configured options.</returns>
        public AbilityBuilder IsConcentrationAbility(StatusEffectType concentrationStatusEffectType)
        {
            _activeAbility.ActivationType = AbilityActivationType.Concentration;
            _activeAbility.ConcentrationStatusEffectType = concentrationStatusEffectType;

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

            return this;
        }

        /// <summary>
        /// Assigns a visual effect to the caster of the spell. This will display while casting.
        /// Calling this more than once will replace the previous visual effect.
        /// </summary>
        /// <param name="vfx">The visual effect to display.</param>
        /// <returns>An ability builder with the configured options</returns>
        public AbilityBuilder DisplaysVisualEffectWhenActivating(VisualEffect vfx = VisualEffect.Vfx_Dur_Elemental_Shield)
        {
            _activeAbility.ActivationVisualEffect = vfx;

            return this;
        }

        /// <summary>
        /// Assigns an impact action on the active ability we're building.
        /// Calling this more than once will replace the previous action.
        /// Impact actions are fired when a ability is used. The timing of when it fires depends on the activation type.
        /// "Casted" abilities fire the impact action at the end of the casting phase.
        /// "Queued" abilities fire the impact action on the next weapon hit.
        /// "Concentration" abilities fire the impact action on each concentration cycle.
        /// </summary>
        /// <param name="action">The action to fire when a ability impacts a target.</param>
        /// <returns>An ability builder with the configured options</returns>
        public AbilityBuilder HasImpactAction(AbilityImpactAction action)
        {
            _activeAbility.ImpactAction = action;

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
            var requirement = new PerkFPRequirement(requiredFP);
            _activeAbility.Requirements.Add(requirement);

            return this;
        }

        /// <summary>
        /// Adds a stamina requirement to use the ability at this level.
        /// </summary>
        /// <param name="requiredSTM">The amount of STM needed to use this ability at this level.</param>
        /// <returns>An ability builder with the configured options</returns>
        public AbilityBuilder RequirementStamina(int requiredSTM)
        {
            var requirement = new AbilityStaminaRequirement(requiredSTM);
            _activeAbility.Requirements.Add(requirement);

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
