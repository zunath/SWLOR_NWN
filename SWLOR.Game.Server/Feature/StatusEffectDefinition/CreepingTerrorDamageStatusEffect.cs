namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CreepingTerrorDamageStatusEffect : ForceDamageOverTimeStatusEffectBase
    {
        public override string Name => "Creeping Terror";

        public CreepingTerrorDamageStatusEffect()
            : base(12)
        {
        }
    }
}
