using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.AbilityService
{
    public class AbilityBuilder
    {
        private readonly Dictionary<Feat, AbilityDetail> _abilities = new Dictionary<Feat, AbilityDetail>();
        private AbilityDetail _activeAbility;

        /// <summary>
        /// Creates a new ability.
        /// </summary>
        /// <param name="featType">The type of feat to link this ability to.</param>
        /// <param name="effectiveLevelPerkType">The type of perk used for determining effective level.</param>
        /// <returns>An ability builder with the configured options</returns>
        public AbilityBuilder Create(Feat featType, PerkType effectiveLevelPerkType)
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
        /// Assigns an activation type on the active ability we're building.
        /// Calling this more than once will replace the previous activation type.
        /// </summary>
        /// <param name="activationType">The activation type to set.</param>
        /// <returns>An ability builder with the configured options</returns>
        public AbilityBuilder UsesActivationType(AbilityActivationType activationType)
        {
            _activeAbility.ActivationType = activationType;

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
        /// For example, "Casted" abilitys fire the impact action at the end of the casting phase.
        /// While "Queued" abilitys fire the impact action on the next weapon hit.
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
        /// Adds an MP requirement to use the ability at this level.
        /// </summary>
        /// <param name="requiredMP">The amount of MP needed to use this ability at this level.</param>
        /// <returns>An ability builder with the configured options</returns>
        public AbilityBuilder RequirementMP(int requiredMP)
        {
            var requirement = new PerkMPRequirement(requiredMP);
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
        public Dictionary<Feat, AbilityDetail> Build()
        {
            return _abilities;
        }
    }
}
