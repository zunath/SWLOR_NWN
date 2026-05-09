using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceSpark3StatusEffect : ForceSparkStatusEffectBase
    {
        public ForceSpark3StatusEffect()
            : base(6)
        {
        }

        public override string Name => "Force Spark III";

        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(ForceSpark1StatusEffect),
            typeof(ForceSpark2StatusEffect)
        };
    }
}
