using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class LastStandOfTheLight1StatusEffect : StatusEffectBase
    {
        private const int TemporaryHPPercent = 20;
        private const int TemporaryHPDurationSeconds = 12;

        public override string Name => "Last Stand of the Light";
        public override EffectIconType Icon => EffectIconType.TemporaryHitpoints;
        public override bool PersistsOnLogout => false;

        protected override void Apply(uint creature, int durationTicks)
        {
            var scalingSource = GetIsObjectValid(Source) ? Source : creature;
            StatGroup.Stats[StatType.FatalDamageTemporaryHPPercent] =
                AbilityEffectScaling.ApplyActiveForceAffinityMagnitude(Source, TemporaryHPPercent);
            StatGroup.Stats[StatType.FatalDamageTemporaryHPDurationSeconds] = TemporaryHPDurationSeconds;
            StatGroup.Stats[StatType.FatalDamageTemporaryHPScalingAbilityScore] = GetAbilityScore(
                scalingSource,
                AbilityType.Willpower);
        }
    }
}
