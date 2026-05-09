using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceDrain1StatusEffect : ForceDrainStatusEffectBase
    {
        public override string Name => "Force Drain I";
        protected override int BaseDamage => 0;
        protected override int DiceSize => 2;
        protected override int ApplyEnmity => 200;
        protected override int TickEnmity => 75;
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(ForceDrain2StatusEffect),
            typeof(ForceDrain3StatusEffect),
            typeof(ForceDrain4StatusEffect),
            typeof(ForceDrain5StatusEffect)
        };
    }
}
