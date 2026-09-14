using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Assault3StatusEffect : StatusEffectBase
    {
        public override string Name => "Assault III";
        public override EffectIconType Icon => EffectIconType.Assault3StatusEffect;

        public Assault3StatusEffect()
        {
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = 14;
        }
    }
}
