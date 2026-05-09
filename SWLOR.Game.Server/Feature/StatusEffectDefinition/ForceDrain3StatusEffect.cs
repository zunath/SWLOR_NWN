using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceDrain3StatusEffect : ForceDrainStatusEffectBase
    {
        public override string Name => "Force Drain III";
        protected override int BaseDamage => 15;
        protected override int DiceSize => 4;
        protected override int ApplyEnmity => 250;
        protected override int TickEnmity => 125;
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(ForceDrain4StatusEffect),
            typeof(ForceDrain5StatusEffect)
        };
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(ForceDrain1StatusEffect),
            typeof(ForceDrain2StatusEffect)
        };
    }
}
