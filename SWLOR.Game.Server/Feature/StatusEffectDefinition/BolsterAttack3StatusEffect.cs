using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BolsterAttack3StatusEffect : StatusEffectBase
    {
        public override string Name => "Bolster Attack III";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
        public override bool PersistsOnLogout => false;
        public override List<Type> LessPowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(BolsterAttack1StatusEffect),
            typeof(BolsterAttack2StatusEffect),
        };

        public BolsterAttack3StatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 12;
        }
    }
}
