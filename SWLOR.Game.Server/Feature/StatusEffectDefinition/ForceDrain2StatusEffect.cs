using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceDrain2StatusEffect : ForceDrainStatusEffectBase
    {
        public override string Name => "Force Drain II";
        protected override int BaseDamage => 10;
        protected override int DiceSize => 3;
        protected override int ApplyEnmity => 250;
        protected override int TickEnmity => 100;
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(ForceDrain3StatusEffect),
            typeof(ForceDrain4StatusEffect),
            typeof(ForceDrain5StatusEffect)
        };
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(ForceDrain1StatusEffect)
        };
    }
}
