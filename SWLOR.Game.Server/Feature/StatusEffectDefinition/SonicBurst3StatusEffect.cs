using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SonicBurst3StatusEffect : StatusEffectBase
    {
        public override string Name => "Sonic Burst III";
        public override EffectIconType Icon => EffectIconType.SonicBurst3StatusEffect;

        public SonicBurst3StatusEffect()
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = -10;
        }
    }
}
