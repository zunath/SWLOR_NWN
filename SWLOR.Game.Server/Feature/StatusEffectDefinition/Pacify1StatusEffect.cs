using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Pacify1StatusEffect : StatusEffectBase
    {
        public override string Name => "Pacify I";
        public override EffectIconType Icon => EffectIconType.Pacify1StatusEffect;

        public Pacify1StatusEffect()
        {
            StatGroup.Stats[StatType.WeaponAndForceDamageDealtPercentAdjustment] = -5;
        }
    }
}
