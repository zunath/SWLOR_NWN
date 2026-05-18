using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BerserkerStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Berserker Stance";
        public override EffectIconType Icon => EffectIconType.BerserkerStanceStatusEffect;

        protected override void Apply(uint creature, int durationTicks)
        {
            var level = Perk.GetPerkLevel(Source, PerkType.BerserkerStance);
            ApplyStatAdjustments(level);
        }

        private void ApplyStatAdjustments(int level)
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = level >= 2 ? 25 : 15;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = -20;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = -20;
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = level >= 2 ? 15 : 10;
        }

    }
}
