using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SlugShakePenaltyStatusEffect : StatusEffectBase
    {
        private readonly AbilityType _ability;

        public override string Name => "Slug Shake";
        public override EffectIconType Icon => EffectIconType.SlugShakePenaltyStatusEffect;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        public SlugShakePenaltyStatusEffect()
            : this(AbilityType.Invalid)
        {
        }

        public SlugShakePenaltyStatusEffect(AbilityType ability)
        {
            _ability = ability;

            if (_ability != AbilityType.Invalid)
            {
                StatGroup.Abilities[_ability] = -50;
            }
        }

        public override IStatusEffect Clone()
        {
            return new SlugShakePenaltyStatusEffect(_ability);
        }
    }
}
