using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Shielding3StatusEffect : StatusEffectBase
    {
        public override string Name => "Shielding III";
        public override EffectIconType Icon => EffectIconType.Shielding3StatusEffect;

        public Shielding3StatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment] = -11;
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = -11;
        }
    }
}
