using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FocusStim1StatusEffect : StatusEffectBase
    {
        public override string Name => "Focus Stim I";
        public override EffectIconType Icon => EffectIconType.FocusStim1StatusEffect;

        public FocusStim1StatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment] = 5;
        }
    }
}
