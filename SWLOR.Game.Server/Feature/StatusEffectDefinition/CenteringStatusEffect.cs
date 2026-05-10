using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CenteringStatusEffect : StatusEffectBase
    {
        public override string Name => "Centering";
        public override EffectIconType Icon => EffectIconType.AttackIncrease;

        protected override void Apply(uint creature, int durationTicks)
        {
            var level = Perk.GetPerkLevel(Source, PerkType.Centering);
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = level >= 2 ? 20 : 10;
            StatGroup.Stats[StatType.EnmityPercentAdjustment] = level >= 2 ? -50 : -25;
        }

    }
}
