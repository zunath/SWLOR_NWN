
using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Enums;
using SWLOR.Shared.Domain.StatusEffect.Events;
using SWLOR.Shared.Events.Attributes;

namespace SWLOR.Component.StatusEffect.EventHandlers
{
    public class StatusEffectEventHandler
    {
        private readonly IStatusEffectService _statusEffectService;

        public StatusEffectEventHandler(IStatusEffectService statusEffectService)
        {
            _statusEffectService = statusEffectService;
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
