using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Premonition2StatusEffect : PremonitionStatusEffectBase
    {
        public override string Name => "Premonition II";
        protected override int Concealment => 25;
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(Premonition1StatusEffect)
        };
    }
}
