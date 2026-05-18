using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PredatorRush1StatusEffect : StatusEffectBase
    {
        public override string Name => "Predator Rush";
        public override EffectIconType Icon => EffectIconType.PredatorRush1StatusEffect;
        public override bool PersistsOnLogout => false;

        public PredatorRush1StatusEffect()
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = 8;
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = 20;
        }
    }
}
