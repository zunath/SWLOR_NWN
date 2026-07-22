using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Rallying command buff granted to allies by the Final Mandate technique.
    /// </summary>
    public sealed class FinalMandateStatusEffect : StatusEffectBase
    {
        public override string Name => "Final Mandate";
        public override EffectIconType Icon => EffectIconType.FinalMandateStatusEffect;

        public FinalMandateStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 15;
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = 10;
        }
    }
}
