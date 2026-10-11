using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BloodFrenzyStatusEffect : StatusEffectBase
    {
        public override string Name => "Blood Frenzy";
        public override EffectIconType Icon => EffectIconType.BloodFrenzyStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public BloodFrenzyStatusEffect()
        {
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = 15;
            StatGroup.Stats[StatType.AutoAttackHitStaminaRestore] = 1;
            StatGroup.Stats[StatType.DefeatedEnemyStaminaRestore] = 8;
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            var effect = TagNativeEffect(EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Red_Orange));
            ApplyEffectToObject(DurationType.Temporary, effect, creature, GetDurationSeconds(durationTicks));
        }
    }
}
