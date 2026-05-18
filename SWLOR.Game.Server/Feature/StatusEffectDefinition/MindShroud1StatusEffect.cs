using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class MindShroud1StatusEffect : StatusEffectBase
    {
        public override string Name => "Mind Shroud I";
        public override EffectIconType Icon => EffectIconType.MindShroud1StatusEffect;

        public MindShroud1StatusEffect()
        {
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = -5;
            StatGroup.Stats[StatType.MindResistance] = 10;
        }
    }
}
