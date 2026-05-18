using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DampeningField2StatusEffect : StatusEffectBase
    {
        public override string Name => "Dampening Field II";
        public override EffectIconType Icon => EffectIconType.DampeningField2StatusEffect;

        public DampeningField2StatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment] = -15;
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = -15;
        }
    }
}
