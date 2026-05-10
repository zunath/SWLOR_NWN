using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DefensiveStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Defensive Stance";
        public override EffectIconType Icon => EffectIconType.DamageReduction;

        protected override void Apply(uint creature, int durationTicks)
        {
            var level = Perk.GetPerkLevel(Source, PerkType.DefensiveStance);
            var defense = level >= 2 ? 20 : 15;

            StatGroup.Stats[StatType.AttackPercentAdjustment] = -20;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = defense;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = defense;
            StatGroup.Stats[StatType.EnmityPercentAdjustment] = level >= 2 ? 30 : 20;
        }

    }
}
