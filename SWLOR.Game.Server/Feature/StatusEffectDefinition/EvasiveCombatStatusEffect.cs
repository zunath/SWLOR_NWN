using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class EvasiveCombatStatusEffect : StatusEffectBase
    {
        public override string Name => "Evasive Combat";
        public override EffectIconType Icon => EffectIconType.EvasiveCombatStatusEffect;

        protected override void Apply(uint creature, int durationTicks)
        {
            var level = Perk.GetPerkLevel(Source, PerkType.EvasiveCombat);

            StatGroup.Stats[StatType.AttackPercentAdjustment] = level >= 2 ? -25 : -15;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = level >= 2 ? 20 : 10;
            StatGroup.Stats[StatType.EnmityPercentAdjustment] = level >= 2 ? -25 : -15;
        }

    }
}
