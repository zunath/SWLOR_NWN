using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DiseaseStatusEffect : StatusEffectBase
    {
        private readonly int _level;
        public override string Name => "Disease";
        public override EffectIconType Icon => EffectIconType.DiseaseStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override ResistanceType ResistanceType => ResistanceType.Poison;
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
            var source = GetIsObjectValid(Source) ? Source : creature;
            var perception = GetAbilityModifier(AbilityType.Perception, source);
            var damage = System.Math.Max(1, d2() + perception * _level);
            damage = Resistance.ApplyResistanceToDamage(creature, ResistanceType, damage);
            if (damage > 0)
            {
                AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, CombatDamageType.Poison.GetNWScriptDamageType()), creature));
            }

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Disease_S), creature);
            StatusEffect.ApplyStatusEffect(source, creature, typeof(DiseaseVitalityPenaltyStatusEffect), 30f);
        }

        protected override void Remove(uint creature)
        {
            var source = GetIsObjectValid(Source) ? Source : creature;
            StatusEffect.RemoveStatusEffect(creature, typeof(DiseaseVitalityPenaltyStatusEffect), source, false);
        }
    }
}
