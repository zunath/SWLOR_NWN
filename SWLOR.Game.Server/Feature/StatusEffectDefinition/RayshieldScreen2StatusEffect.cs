using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class RayshieldScreen2StatusEffect : StatusEffectBase
    {
        public override string Name => "Rayshield Screen II";
        public override EffectIconType Icon => EffectIconType.RayshieldScreen2StatusEffect;

        public RayshieldScreen2StatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 12;
        }
    }
}
