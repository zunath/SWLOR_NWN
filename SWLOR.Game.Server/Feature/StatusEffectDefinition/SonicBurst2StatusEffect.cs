using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SonicBurst2StatusEffect : StatusEffectBase
    {
        public override string Name => "Sonic Burst II";
        public override EffectIconType Icon => EffectIconType.SonicBurst2StatusEffect;

        public SonicBurst2StatusEffect()
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = -6;
        }
    }
}
