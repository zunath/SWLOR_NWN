using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceDrain4StatusEffect : ForceDrainStatusEffectBase
    {
        public override string Name => "Force Drain IV";
        protected override int BaseDamage => 20;
        protected override int DiceSize => 6;
        protected override int ApplyEnmity => 300;
        protected override int TickEnmity => 150;
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(ForceDrain5StatusEffect)
        };
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(ForceDrain1StatusEffect),
            typeof(ForceDrain2StatusEffect),
            typeof(ForceDrain3StatusEffect)
        };
    }
}
