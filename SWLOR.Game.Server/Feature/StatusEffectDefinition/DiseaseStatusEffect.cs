using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DiseaseStatusEffect : StatusEffectBase
    {
        private readonly int _level;
        public override string Name => "Disease";
        public override EffectIconType Icon => EffectIconType.Disease;
        public override StatusEffectCleanseType CleanseTypes =>
            StatusEffectCleanseType.Purify |
            StatusEffectCleanseType.TreatmentKit2 |
            StatusEffectCleanseType.SoothePet;
        public override float Frequency => 6f;

        public DiseaseStatusEffect()
            : this(1)
        {
        }

        public DiseaseStatusEffect(int level)
        {
            _level = System.Math.Max(1, level);
        }

        public override IStatusEffect Clone()
        {
            return new DiseaseStatusEffect(_level);
        }

        protected override void Tick(uint creature)
        {
            var perception = GetAbilityModifier(AbilityType.Perception, Source);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(d2() + perception * _level), creature);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Disease_S), creature);
            StatusEffect.ApplyStatusEffect(Source, creature, typeof(DiseaseVitalityPenaltyStatusEffect), 6f);
        }

        protected override void Remove(uint creature)
        {
            StatusEffect.RemoveStatusEffect(creature, typeof(DiseaseVitalityPenaltyStatusEffect), Source, false);
        }
    }
}
