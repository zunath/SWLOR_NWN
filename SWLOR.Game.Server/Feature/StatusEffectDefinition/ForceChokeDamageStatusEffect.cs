using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceChokeDamageStatusEffect : ForceDamageOverTimeStatusEffectBase
    {
        public override string Name => "Force Choke";
        public override EffectIconType Icon => EffectIconType.ForceChokeDamageStatusEffect;

        public ForceChokeDamageStatusEffect()
            : base(12)
        {
        }
    }
}
