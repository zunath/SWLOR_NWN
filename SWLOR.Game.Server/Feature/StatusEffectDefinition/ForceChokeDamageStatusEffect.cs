namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceChokeDamageStatusEffect : ForceDamageOverTimeStatusEffectBase
    {
        public override string Name => "Force Choke";

        public ForceChokeDamageStatusEffect()
            : base(12)
        {
        }
    }
}
