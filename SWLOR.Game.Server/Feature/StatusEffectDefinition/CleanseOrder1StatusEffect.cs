using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CleanseOrder1StatusEffect : CleanseOrderTemporaryHitPointStatusEffectBase
    {
        public CleanseOrder1StatusEffect()
            : this(0)
        {
        }

        public CleanseOrder1StatusEffect(long temporaryHitPointApplicationId)
            : base(temporaryHitPointApplicationId)
        {
        }

        public override string Name => "Cleanse Order I";
        public override EffectIconType Icon => EffectIconType.Invalid;
    }
}
