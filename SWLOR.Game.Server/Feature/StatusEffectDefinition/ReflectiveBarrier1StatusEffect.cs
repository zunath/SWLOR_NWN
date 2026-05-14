using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ReflectiveBarrier1StatusEffect : StatusEffectBase
    {
        private const int BaseReflectionPercent = 15;

        public override string Name => "Reflective Barrier";
        public override EffectIconType Icon => EffectIconType.ElementalShield;
        public override bool PersistsOnLogout => false;

        protected override void Apply(uint creature, int durationTicks)
        {
            var reflection = AbilityEffectScaling.ScaleDirectEffect(
                BaseReflectionPercent,
                GetAbilityScore(Source, AbilityType.Willpower));

            StatGroup.Stats[StatType.ForceDamageReflectionPercentAdjustment] = reflection;
            StatGroup.Stats[StatType.ElementalDamageReflectionPercentAdjustment] = reflection;
        }
    }
}
