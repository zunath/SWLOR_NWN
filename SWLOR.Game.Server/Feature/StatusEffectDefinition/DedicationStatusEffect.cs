using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DedicationStatusEffect : AuraStatusEffectBase
    {
        public override string Name => "Dedication";
        public override EffectIconType Icon => EffectIconType.Dedication;

        protected override void Apply(uint creature, int durationTicks)
        {
            if (!GetIsObjectValid(Source))
                return;

            var perkLevel = Perk.GetPerkLevel(Source, PerkType.Dedication);
            var social = GetAbilityScore(Source, AbilityType.Social);

            StatGroup.Stats[StatType.ExperiencePercentAdjustment] = 10 + perkLevel * social;
        }
    }
}
