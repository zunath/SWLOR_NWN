using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
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
