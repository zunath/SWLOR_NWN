using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Service.StatusEffectService
{
    public class StatusEffectBuilder
    {
        private readonly Dictionary<StatusEffectType, StatusEffectDetail> _statusEffects = new Dictionary<StatusEffectType, StatusEffectDetail>();
        private StatusEffectDetail _activeStatusEffect;

        /// <summary>
        /// Creates a new status effect.
        /// </summary>
        /// <param name="statusEffectType">The type of status effect to link this ability to.</param>
        /// <returns>A status effect builder with the configured options.</returns>
        public StatusEffectBuilder Create(StatusEffectType statusEffectType)
        {
            _activeStatusEffect = new StatusEffectDetail();
            _statusEffects[statusEffectType] = _activeStatusEffect;

            return this;
        }

        /// <summary>
        /// Sets the name of the active status effect we're building.
        /// </summary>
        /// <param name="name">The name of the status effect to set.</param>
        /// <returns>A status effect builder with the configured options.</returns>
        public StatusEffectBuilder Name(string name)
        {
            _activeStatusEffect.Name = name;

            return this;
        }

        /// <summary>
        /// Indicates the ability can stack. This means multiple of the same status effect can be applied on the same target.
        /// </summary>
        /// <returns>A status effect builder with the configured options.</returns>
        public StatusEffectBuilder CanStack()
        {
            _activeStatusEffect.CanStack = true;

            return this;
        }

        /// <summary>
        /// Indicates the status effects which will be replaced by this one.
        /// Generally used to ensure higher tier effects take priority over lower
        /// but can also be used for mutually exclusive effects.
        /// </summary>
        /// <param name="effectTypes">The effects which replace this one.</param>
        /// <returns>A status effect builder with the configured options.</returns>
        public StatusEffectBuilder Replaces(params StatusEffectType[] effectTypes)
        {
            _activeStatusEffect.ReplacesEffects = effectTypes;

            return this;
        }

        /// <summary>
        /// Indicates the status effects which take higher priority than this one.
        /// Can be used to ensure a tier 1 effect doesn't apply if a tier 2 one is already in place.
        /// </summary>
        /// <param name="effectTypes">The effects which this effect cannot replace.</param>
        /// <returns>A status effect builder with the configured options.</returns>
        public StatusEffectBuilder CannotReplace(params StatusEffectType[] effectTypes)
        {
            _activeStatusEffect.CannotReplaceEffects = effectTypes;

            return this;
        }

        /// <summary>
        /// Sets the effect icon of the active status effect we're building.
        /// </summary>
        /// <param name="effectIconId">The Id of the NWN effect icon</param>
        /// <returns>A status effect builder with the configured options.</returns>
        public StatusEffectBuilder EffectIcon(EffectIconType effectIconId)
        {
            _activeStatusEffect.EffectIconId = effectIconId;

            return this;
        }

        /// <summary>
        /// Sets the action to run when this status effect is applied to a creature.
        /// </summary>
        /// <param name="grantAction">The action to run when granted.</param>
        /// <returns>A status effect builder with the configured options.</returns>
        public StatusEffectBuilder GrantAction(StatusEffectAppliedDelegate grantAction)
        {
            _activeStatusEffect.AppliedAction = grantAction;

            return this;
        }

        /// <summary>
        /// Sets the action to run when this status effect is removed from a creature.
        /// </summary>
        /// <param name="removeAction">The action to run when removed.</param>
        /// <returns>A status effect builder with the configured options.</returns>
        public StatusEffectBuilder RemoveAction(StatusEffectRemovedDelegate removeAction)
        {
            _activeStatusEffect.RemoveAction = removeAction;

            return this;
        }

        /// <summary>
        /// Sets the action to run when this status effect ticks.
        /// </summary>
        /// <param name="tickAction">The action to run when the effect ticks.</param>
        /// <returns>A status effect builder with the configured options.</returns>
        public StatusEffectBuilder TickAction(StatusEffectTickDelegate tickAction)
        {
            _activeStatusEffect.TickAction = tickAction;

            return this;
        }

        public Dictionary<StatusEffectType, StatusEffectDetail> Build()
        {
            return _statusEffects;
        }
    }
}
