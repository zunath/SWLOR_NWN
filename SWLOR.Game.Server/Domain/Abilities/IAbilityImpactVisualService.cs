using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// How an ability's visuals reach a recipient (a part of the ability split, Readmes/MaintainabilityRefactorAbilitySplit.md):
    /// the success visual plays once per recipient, and a concealed recipient sees it alone. <see cref="IAbilityService"/> and the
    /// hostile impact both play it, so it is a part below them.
    /// </summary>
    public interface IAbilityImpactVisualService
    {
        /// <summary>
        /// Shows an instant visual effect on <paramref name="target"/> to one player only, so feedback
        /// for a hidden creature never marks its position for anyone else.
        /// </summary>
        void ShowVisualEffectToPlayer(uint player, uint target, VisualEffect visualEffect);

        /// <summary>
        /// Plays an impact's success visual on <paramref name="recipient"/> the first time it reaches it: to the recipient alone
        /// while it is concealed, otherwise on it. Nothing when <paramref name="visuals"/> is null (no impact is tracked).
        /// </summary>
        void PlaySuccessfulImpactVisualEffect(AbilityImpactVisualEffects visuals, uint recipient);
    }
}
