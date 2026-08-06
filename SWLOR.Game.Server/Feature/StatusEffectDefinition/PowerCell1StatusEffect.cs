using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PowerCell1StatusEffect : StatusEffectBase
    {
        public override string Name => "Power Cell I";
        public override EffectIconType Icon => EffectIconType.PowerCell1StatusEffect;

        public PowerCell1StatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment] = 4;
        }
    }
}
