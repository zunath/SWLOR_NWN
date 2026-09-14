using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PetFoodStatusEffect : StatusEffectBase
    {
        private readonly int _xpBonusPercent;
        public override string Name => "Pet Food";
        public override EffectIconType Icon => EffectIconType.PetFoodStatusEffect;

        public PetFoodStatusEffect()
            : this(0)
        {
        }

        public PetFoodStatusEffect(int xpBonusPercent)
        {
            _xpBonusPercent = xpBonusPercent;
            StatGroup.Stats[StatType.ExperiencePercentAdjustment] = xpBonusPercent;
        }

        public override IStatusEffect Clone()
        {
            return new PetFoodStatusEffect(_xpBonusPercent);
        }
    }
}
