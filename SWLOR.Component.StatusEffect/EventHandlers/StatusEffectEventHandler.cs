using SWLOR.Shared.Domain.StatusEffect.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Enums;
using SWLOR.Shared.Domain.StatusEffect.Events;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Contracts;
using SWLOR.Shared.Events.Events.Module;
namespace SWLOR.Component.StatusEffect.EventHandlers
{
    public class StatusEffectEventHandler : IEventHandler
    {
        private readonly IStatusEffectService _statusEffectService;

        public StatusEffectEventHandler(
            IStatusEffectService statusEffectService)
        {
            _statusEffectService = statusEffectService;
        }

        [ScriptHandler<OnModuleEnter>]
        public void OnPlayerEnter()
        {
            _statusEffectService.OnModuleEnter();
        }

        [ScriptHandler<OnApplyStatusEffect>]
        public void OnApplyNWNEffect()
        {
            _statusEffectService.OnApplyNWNStatusEffect();
        }

        [ScriptHandler<OnRemoveStatusEffect>]
        public void OnRemoveNWNEffect()
        {
            _statusEffectService.OnRemoveNWNStatusEffect();
        }

        [ScriptHandler<OnStatusEffectInterval>]
        public void OnNWNEffectInterval()
        {
            _statusEffectService.OnNWNStatusEffectInterval();
        }

        [ScriptHandler<TestEvent>]
        public void Test()
        {
            var player = GetLastUsedBy();
            SendMessageToPC(player, "Applying haste");

            _statusEffectService.ApplyStatusEffect(OBJECT_SELF, player, StatusEffectType.Haste, 1);
        }
    }
}


