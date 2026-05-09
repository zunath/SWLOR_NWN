using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceRage1StatusEffect : StaticStatStatusEffectBase
    {
        public override string Name => "Force Rage I";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(ForceRage2StatusEffect)
        };

        public ForceRage1StatusEffect()
            : base(StatType.Attack, 10)
        {
        }
    }
}
