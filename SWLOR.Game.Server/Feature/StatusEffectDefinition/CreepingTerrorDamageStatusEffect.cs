using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CreepingTerrorDamageStatusEffect : ForceDamageOverTimeStatusEffectBase
    {
        public override string Name => "Creeping Terror";
        public override EffectIconType Icon => EffectIconType.CreepingTerrorDamageStatusEffect;

        public CreepingTerrorDamageStatusEffect()
            : base(12)
        {
        }
    }
}
