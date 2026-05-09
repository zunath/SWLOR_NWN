using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceRage2StatusEffect : StaticStatStatusEffectBase
    {
        public override string Name => "Force Rage II";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(ForceRage1StatusEffect)
        };

        public ForceRage2StatusEffect()
            : base(StatType.Attack, 20)
        {
        }
    }
}
