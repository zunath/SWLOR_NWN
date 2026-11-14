using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BolsterAttack2StatusEffect : StatusEffectBase
    {
        public override string Name => "Bolster Attack II";
        public override EffectIconType Icon => EffectIconType.BolsterAttack2StatusEffect;

        public BolsterAttack2StatusEffect()
        {
            LessPowerfulEffectTypes.Add(typeof(BolsterAttack1StatusEffect));
            MorePowerfulEffectTypes.Add(typeof(BolsterAttack3StatusEffect));
            StatGroup.Stats[StatType.DamageDealtPercentAdjustment] = 8;
        }
    }
}
