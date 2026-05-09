using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceValor2StatusEffect : StaticStatStatusEffectBase
    {
        public override string Name => "Force Valor II";
        public override EffectIconType Icon => EffectIconType.DamageImmunityIncrease;
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(ForceValor1StatusEffect)
        };

        public ForceValor2StatusEffect()
            : base(StatType.PhysicalDefense, 20)
        {
        }
    }
}
