using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BolsterAttack1StatusEffect : StatusEffectBase
    {
        public override string Name => "Bolster Attack I";
        public override EffectIconType Icon => EffectIconType.BolsterAttack1StatusEffect;

        public BolsterAttack1StatusEffect()
        {
            MorePowerfulEffectTypes.Add(typeof(BolsterAttack2StatusEffect));
            MorePowerfulEffectTypes.Add(typeof(BolsterAttack3StatusEffect));
            StatGroup.Stats[StatType.DamageDealtPercentAdjustment] = 5;
        }
    }
}
