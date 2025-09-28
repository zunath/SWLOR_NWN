using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.StatusEffect.Enums;

namespace SWLOR.Component.StatusEffect.Contracts
{
    /// <summary>
    /// Service responsible for applying status effects to creatures.
    /// </summary>
    public interface IStatusEffectApplicationService
    {
        /// <summary>
        /// Gives a status effect to a creature.
        /// If creature already has the status effect, and their timer is shorter than length,
        /// it will be extended to the length specified.
        /// </summary>
        /// <param name="source">The source of the status effect.</param>
        /// <param name="target">The creature receiving the status effect.</param>
        /// <param name="statusEffectType">The type of status effect to give.</param>
        /// <param name="length">The amount of time, in seconds, the status effect should last. Set to 0.0f to make it permanent.</param>
        /// <param name="effectData">Effect data used by the effect.</param>
        /// <param name="concentrationFeatType">If status effect is associated with a concentration ability, this will track the feat type used.</param>
        /// <param name="sendApplicationMessage">If true, a message will be sent to nearby players when the status effect is applied.</param>
        void Apply(
            uint source,
            uint target,
            StatusEffectType statusEffectType,
            float length,
            object effectData = null,
            FeatType concentrationFeatType = FeatType.Invalid,
            bool sendApplicationMessage = true);
    }
}
