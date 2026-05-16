using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class AlphaRhythm1BeastStatusEffect : StatusEffectBase
    {
        public override string Name => "Alpha Rhythm";
        public override EffectIconType Icon => EffectIconType.DamageReduction;
        public override bool PersistsOnLogout => false;

        public AlphaRhythm1BeastStatusEffect()
        {
            StatGroup.Stats[StatType.DamageDealtPercentAdjustment] = 10;
            StatGroup.Stats[StatType.DamageTakenPercentAdjustment] = -10;
        }
    }
}
