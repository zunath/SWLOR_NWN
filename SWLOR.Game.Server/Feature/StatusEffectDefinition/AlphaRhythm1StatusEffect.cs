using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class AlphaRhythm1StatusEffect : StatusEffectBase
    {
        public override string Name => "Alpha Rhythm";
        public override EffectIconType Icon => EffectIconType.AlphaRhythm1StatusEffect;
        public override bool PersistsOnLogout => false;

        public AlphaRhythm1StatusEffect()
        {
            StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment] = 8;
        }
    }
}
