using SWLOR.Shared.Domain.StatusEffect.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Enums;
using SWLOR.Shared.Domain.StatusEffect.Events;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.StatusEffect.EventHandlers
{
    public class StatusEffectEventHandler
    {
        private readonly IStatusEffectService _statusEffectService;

        public StatusEffectEventHandler(
            IStatusEffectService statusEffectService,
            IEventAggregator eventAggregator)
        {
            _statusEffectService = statusEffectService;

            // Subscribe to events
            eventAggregator.Subscribe<OnModuleEnter>(e => OnPlayerEnter());
            eventAggregator.Subscribe<OnApplyStatusEffect>(e => OnApplyNWNEffect());
            eventAggregator.Subscribe<OnRemoveStatusEffect>(e => OnRemoveNWNEffect());
            eventAggregator.Subscribe<OnStatusEffectInterval>(e => OnNWNEffectInterval());
            eventAggregator.Subscribe<TestEvent>(e => Test());
        }
        public void OnPlayerEnter()
        {
            _statusEffectService.OnModuleEnter();
        }
        public void OnApplyNWNEffect()
        {
            _statusEffectService.OnApplyNWNStatusEffect();
        }
        public void OnRemoveNWNEffect()
        {
            _statusEffectService.OnRemoveNWNStatusEffect();
        }
        public void OnNWNEffectInterval()
        {
            _statusEffectService.OnNWNStatusEffectInterval();
        }
        public void Test()
        {
            var player = GetLastUsedBy();
            SendMessageToPC(player, "Applying haste");

            _statusEffectService.ApplyStatusEffect(OBJECT_SELF, player, StatusEffectType.Haste, 1);
        }
    }
}
