using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Combat-stim buff granted to allies by the Stim Canister technique.
    /// </summary>
    public sealed class StimCanisterStatusEffect : StatusEffectBase
    {
        public override string Name => "Stim Canister";
        public override EffectIconType Icon => EffectIconType.Invalid;

        public StimCanisterStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 15;
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = 15;
        }
    }
}
