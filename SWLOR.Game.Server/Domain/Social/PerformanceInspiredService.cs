using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerformanceService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// Puts an Inspired status effect on a creature: the logic that used to be static in
    /// <c>Service.PerformanceService.PerformanceInspiredApplier</c>, moved here unchanged. The effect is rebuilt only when
    /// its composition changed; otherwise only its remaining time is refreshed, which is all a tick usually needs.
    /// </summary>
    public class PerformanceInspiredService : IPerformanceInspiredService
    {
        private readonly IStatusEffectService _statusEffect;

        public PerformanceInspiredService(IStatusEffectService statusEffect)
        {
            _statusEffect = statusEffect;
        }

        public bool Apply(uint creature, InspiredEffectData data, double remainingSeconds, bool bankedAgain)
        {
            var seconds = (float)Math.Max(1.0, remainingSeconds);
            var existing = GetInspiredEffect(creature);
            if (existing != null && existing.Data.Equals(data))
            {
                RefreshDuration(creature, seconds);
                if (bankedAgain)
                    existing.ResetRequiem();

                return false;
            }

            var effect = new InspiredStatusEffect(data);
            if (existing != null)
                effect.CarryStateFrom(existing, bankedAgain);

            return ApplyEffect(creature, effect, seconds);
        }

        public double GetRemainingSeconds(uint creature, DateTime nowUtc)
        {
            var existing = GetInspiredEffect(creature);
            return existing == null ? 0.0 : existing.GetRemainingDurationSeconds(nowUtc);
        }

        public void SetRemaining(uint creature, double remainingSeconds, bool bankedAgain = false)
        {
            RefreshDuration(creature, (float)Math.Max(1.0, remainingSeconds));
            if (bankedAgain)
                GetInspiredEffect(creature)?.ResetRequiem();
        }

        protected virtual InspiredStatusEffect GetInspiredEffect(uint creature) =>
            _statusEffect.GetStatusEffect<InspiredStatusEffect>(creature);

        protected virtual void RefreshDuration(uint creature, float seconds) =>
            _statusEffect.RefreshStatusEffectDuration(creature, typeof(InspiredStatusEffect), creature, seconds);

        protected virtual bool ApplyEffect(uint creature, InspiredStatusEffect effect, float seconds) =>
            _statusEffect.ApplyStatusEffect(creature, creature, effect, seconds);
    }
}
