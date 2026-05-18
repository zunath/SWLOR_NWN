using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Clarity2StatusEffect : StatusEffectBase
    {
        public override string Name => "Clarity II";
        public override EffectIconType Icon => EffectIconType.Clarity2StatusEffect;

        public Clarity2StatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment] = 6;
        }
    }
}
