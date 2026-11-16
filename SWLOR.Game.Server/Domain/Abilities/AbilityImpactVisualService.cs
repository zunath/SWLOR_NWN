using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// How an ability's visuals reach a recipient (a part of the ability split, Readmes/MaintainabilityRefactorAbilitySplit.md),
    /// moved from <c>AbilityService</c> unchanged: <c>AbilityService</c> plays the success visual for content and the hostile
    /// impact plays it for each hit, and the hostile impact sits below <c>AbilityService</c>, so the playback is a part below
    /// both.
    /// </summary>
    public class AbilityImpactVisualService : IAbilityImpactVisualService
    {
        private readonly IStealthService _stealth;

        public AbilityImpactVisualService(IStealthService stealth)
        {
            _stealth = stealth;
        }

        /// <summary>
        /// Shows an instant visual effect on <paramref name="target"/> to one player only, so feedback
        /// for a hidden creature never marks its position for anyone else.
        /// </summary>
        public void ShowVisualEffectToPlayer(uint player, uint target, VisualEffect visualEffect)
        {
            if (visualEffect == VisualEffect.None || !GetIsObjectValid(target) || !GetIsPC(player) || GetIsDM(player))
                return;

            PlayerPlugin.ApplyInstantVisualEffectToObject(player, target, visualEffect);
        }

        public void PlaySuccessfulImpactVisualEffect(AbilityImpactVisualEffects visuals, uint recipient)
        {
            if (visuals == null || !GetIsObjectValid(recipient) || !visuals.TryRecordRecipient(recipient))
                return;

            if (_stealth.IsConcealed(recipient))
            {
                ShowVisualEffectToPlayer(recipient, recipient, visuals.Effect);
                return;
            }

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(visuals.Effect), recipient);
        }
    }
}
