using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Shielding4StatusEffect : StaticStatStatusEffectBase
    {
        public override string Name => "Shielding IV";
        public override EffectIconType Icon => EffectIconType.DamageImmunityIncrease;
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(Shielding1StatusEffect),
            typeof(Shielding2StatusEffect),
            typeof(Shielding3StatusEffect)
        };

        public Shielding4StatusEffect()
            : base(StatType.PhysicalDefense, 20)
        {
        }
    }
}
