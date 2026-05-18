using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DampeningField1StatusEffect : StatusEffectBase
    {
        public override string Name => "Dampening Field I";
        public override EffectIconType Icon => EffectIconType.DampeningField1StatusEffect;

        public DampeningField1StatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment] = -10;
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = -10;
        }
    }
}
