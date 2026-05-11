using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Shielding2StatusEffect : StaticStatStatusEffectBase
    {
        public override string Name => "Shielding II";
        public override EffectIconType Icon => EffectIconType.DamageImmunityIncrease;
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(Shielding3StatusEffect),
        };
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(Shielding1StatusEffect)
        };

        public Shielding2StatusEffect()
            : base(StatType.PhysicalDefense, 10)
        {
        }
    }
}
