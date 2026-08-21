using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CleanseOrder2StatusEffect : CleanseOrderTemporaryHitPointStatusEffectBase
    {
        public CleanseOrder2StatusEffect()
            : this(0)
        {
        }

        public CleanseOrder2StatusEffect(long temporaryHitPointApplicationId)
            : base(temporaryHitPointApplicationId)
        {
        }

        public override string Name => "Cleanse Order II";
        public override EffectIconType Icon => EffectIconType.CleanseOrder2StatusEffect;
        // The visible marker accompanies temporary HP tracked by TemporaryHitPointEffects.
        // It must not consume a beneficial-effect purge while leaving that temporary HP intact.
    }
}
