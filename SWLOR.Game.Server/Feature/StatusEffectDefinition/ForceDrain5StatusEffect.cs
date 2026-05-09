using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceDrain5StatusEffect : ForceDrainStatusEffectBase
    {
        public override string Name => "Force Drain V";
        protected override int BaseDamage => 25;
        protected override int DiceSize => 8;
        protected override int ApplyEnmity => 350;
        protected override int TickEnmity => 175;
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(ForceDrain1StatusEffect),
            typeof(ForceDrain2StatusEffect),
            typeof(ForceDrain3StatusEffect),
            typeof(ForceDrain4StatusEffect)
        };
    }
}
