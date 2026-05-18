using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class MindShroud2StatusEffect : StatusEffectBase
    {
        public override string Name => "Mind Shroud II";
        public override EffectIconType Icon => EffectIconType.MindShroud2StatusEffect;

        public MindShroud2StatusEffect()
        {
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = -10;
            StatGroup.Stats[StatType.MindResistance] = 15;
        }
    }
}
