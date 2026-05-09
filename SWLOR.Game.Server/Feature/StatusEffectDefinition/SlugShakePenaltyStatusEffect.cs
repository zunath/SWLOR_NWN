using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SlugShakePenaltyStatusEffect : StatusEffectBase
    {
        private readonly AbilityType _ability;

        public override string Name => "Slug Shake";
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        public override EffectIconType Icon => _ability switch
        {
            AbilityType.Might => EffectIconType.AbilityDecreaseSTR,
            AbilityType.Perception => EffectIconType.AbilityDecreaseDEX,
            AbilityType.Vitality => EffectIconType.AbilityDecreaseCON,
            AbilityType.Agility => EffectIconType.AbilityDecreaseINT,
            AbilityType.Willpower => EffectIconType.AbilityDecreaseWIS,
            AbilityType.Social => EffectIconType.AbilityDecreaseCHA,
            _ => EffectIconType.Invalid
        };

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
