using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PowerCell2StatusEffect : StatusEffectBase
    {
        public override string Name => "Power Cell II";
        public override EffectIconType Icon => EffectIconType.PowerCell2StatusEffect;

        public PowerCell2StatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment] = 6;
        }
    }
}
