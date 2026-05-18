using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Pacify2StatusEffect : StatusEffectBase
    {
        public override string Name => "Pacify II";
        public override EffectIconType Icon => EffectIconType.Pacify2StatusEffect;

        public Pacify2StatusEffect()
        {
            StatGroup.Stats[StatType.WeaponAndForceDamageDealtPercentAdjustment] = -8;
        }
    }
}
