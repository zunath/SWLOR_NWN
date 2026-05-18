using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Assault1StatusEffect : StatusEffectBase
    {
        public override string Name => "Assault I";
        public override EffectIconType Icon => EffectIconType.Assault1StatusEffect;

        public Assault1StatusEffect()
        {
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = 6;
        }
    }
}
