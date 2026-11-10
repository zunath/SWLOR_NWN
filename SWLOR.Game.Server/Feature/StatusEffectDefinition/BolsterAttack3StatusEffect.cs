using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BolsterAttack3StatusEffect : StatusEffectBase
    {
        public override string Name => "Bolster Attack III";
        public override EffectIconType Icon => EffectIconType.BolsterAttack3StatusEffect;

        public BolsterAttack3StatusEffect()
        {
            LessPowerfulEffectTypes.Add(typeof(BolsterAttack1StatusEffect));
            LessPowerfulEffectTypes.Add(typeof(BolsterAttack2StatusEffect));
            StatGroup.Stats[StatType.DamageDealtPercentAdjustment] = 12;
        }
    }
}
