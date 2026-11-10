using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FocusStim2StatusEffect : StatusEffectBase
    {
        public override string Name => "Focus Stim II";
        public override EffectIconType Icon => EffectIconType.FocusStim2StatusEffect;

        public FocusStim2StatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment] = 8;
        }
    }
}
