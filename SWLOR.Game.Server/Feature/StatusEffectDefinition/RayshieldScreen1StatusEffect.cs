using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class RayshieldScreen1StatusEffect : StatusEffectBase
    {
        public override string Name => "Rayshield Screen I";
        public override EffectIconType Icon => EffectIconType.RayshieldScreen1StatusEffect;

        public RayshieldScreen1StatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 8;
        }
    }
}
