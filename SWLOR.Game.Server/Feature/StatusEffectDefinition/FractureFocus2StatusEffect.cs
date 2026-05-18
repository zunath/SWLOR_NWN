using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FractureFocus2StatusEffect : StatusEffectBase
    {
        public override string Name => "Fracture Focus II";
        public override EffectIconType Icon => EffectIconType.FractureFocus2StatusEffect;

        public FractureFocus2StatusEffect()
        {
            StatGroup.Stats[StatType.FPCostPercentAdjustment] = 25;
            StatGroup.Stats[StatType.AbilityStaminaCostPercentAdjustment] = 25;
        }
    }
}
