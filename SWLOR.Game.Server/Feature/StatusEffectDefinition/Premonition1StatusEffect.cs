using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Premonition1StatusEffect : PremonitionStatusEffectBase
    {
        public override string Name => "Premonition I";
        protected override int Concealment => 15;
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(Premonition2StatusEffect)
        };
    }
}
