using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class AdrenalStimWillpowerPenaltyStatusEffectBase : StatusEffectBase
    {
        protected abstract int Penalty { get; }

        public override string Name => "Adrenal Stim Fatigue";
        public override EffectIconType Icon => EffectIconType.AbilityDecreaseWIS;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Abilities[AbilityType.Willpower] = -Penalty;
        }
    }
}
