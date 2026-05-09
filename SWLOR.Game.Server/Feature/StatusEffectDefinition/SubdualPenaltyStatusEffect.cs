using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SubdualPenaltyStatusEffect : StatusEffectBase
    {
        public override string Name => "Subdual Penalty";
        public override EffectIconType Icon => EffectIconType.AttackDecrease;

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.Accuracy] = -50;
            StatGroup.Stats[StatType.Evasion] = -50;
            StatGroup.Stats[StatType.MovementSpeedPercentAdjustment] = -50;
        }
    }
}
