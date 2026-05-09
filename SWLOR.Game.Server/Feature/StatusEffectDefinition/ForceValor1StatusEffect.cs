using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceValor1StatusEffect : StaticStatStatusEffectBase
    {
        public override string Name => "Force Valor I";
        public override EffectIconType Icon => EffectIconType.DamageImmunityIncrease;
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(ForceValor2StatusEffect)
        };

        public ForceValor1StatusEffect()
            : base(StatType.PhysicalDefense, 10)
        {
        }
    }
}
