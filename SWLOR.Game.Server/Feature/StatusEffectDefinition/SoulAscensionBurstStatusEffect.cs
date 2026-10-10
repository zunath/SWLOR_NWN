using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SoulAscensionBurstStatusEffect : StatusEffectBase
    {
        public override string Name => "Soul Ascension Burst";
        public override EffectIconType Icon => EffectIconType.SoulAscensionBurstStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public SoulAscensionBurstStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 20;
            StatGroup.Stats[StatType.DamageDealtHPPercentRestore] = 8;
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            var effect = TagNativeEffect(EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Red_White));
            ApplyEffectToObject(DurationType.Temporary, effect, creature, GetDurationSeconds(durationTicks));
        }
    }
}
