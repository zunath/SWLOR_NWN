using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class LastStandOfTheLight1StatusEffect : StatusEffectBase
    {
        private const int TemporaryHPPercent = 15;
        private const int TemporaryHPDurationSeconds = 45;

        public override string Name => "Last Stand of the Light";
        public override EffectIconType Icon => EffectIconType.LastStandOfTheLight1StatusEffect;
        public override bool PersistsOnLogout => false;

        protected override void Apply(uint creature, int durationTicks)
        {
            var scalingSource = Source != 0 && Source != OBJECT_INVALID && GetIsObjectValid(Source)
                ? Source
                : creature;
            var scalingAbilityScore = scalingSource != 0 && scalingSource != OBJECT_INVALID && GetIsObjectValid(scalingSource)
                ? GetAbilityScore(scalingSource, AbilityType.Willpower)
                : 10;

            StatGroup.Stats[StatType.FatalDamageTemporaryHPPercent] =
                AbilityEffectScaling.ApplyActiveForceAffinityMagnitude(Source, TemporaryHPPercent);
            StatGroup.Stats[StatType.FatalDamageTemporaryHPDurationSeconds] = TemporaryHPDurationSeconds;
            StatGroup.Stats[StatType.FatalDamageTemporaryHPScalingAbilityScore] = scalingAbilityScore;
        }
    }
}
