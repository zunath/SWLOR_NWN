using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Pacify3StatusEffect : StatusEffectBase
    {
        public override string Name => "Pacify III";
        public override EffectIconType Icon => EffectIconType.Pacify3StatusEffect;

        public Pacify3StatusEffect()
        {
            StatGroup.Stats[StatType.WeaponAndForceDamageDealtPercentAdjustment] = -12;
        }
    }
}
