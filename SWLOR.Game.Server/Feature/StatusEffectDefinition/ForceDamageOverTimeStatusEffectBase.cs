using System;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class ForceDamageOverTimeStatusEffectBase : StatusEffectBase
    {
        private readonly int _baseTotalDamage;
        private int _remainingDamage;
        private int _remainingTicks;

        public override EffectIconType Icon => EffectIconType.DamageImmunityMagicDecrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Disruption;
        public override bool PersistsOnLogout => false;
        public override float Frequency => 3f;

        protected ForceDamageOverTimeStatusEffectBase(int baseTotalDamage)
        {
            _baseTotalDamage = Math.Max(1, baseTotalDamage);
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            _remainingTicks = Math.Max(1, durationTicks);
            _remainingDamage = GetIsObjectValid(Source)
                ? AbilityEffectScaling.ScaleDirectEffect(_baseTotalDamage, GetAbilityScore(Source, AbilityType.Willpower), source: Source)
                : _baseTotalDamage;
        }

        protected override void Tick(uint creature)
        {
            if (_remainingDamage <= 0 || _remainingTicks <= 0)
                return;

            var damage = Math.Max(1, (int)Math.Ceiling(_remainingDamage / (float)_remainingTicks));
            _remainingDamage = Math.Max(0, _remainingDamage - damage);
            _remainingTicks = Math.Max(0, _remainingTicks - 1);

            var source = GetIsObjectValid(Source) ? Source : creature;
            damage = Resistance.ApplyResistanceToDamage(creature, ResistanceType, damage);
            damage = Combat.ApplyDamageOverTimeTakenModifiers(creature, damage, CombatDamageType.Force);
            damage = Combat.ApplyDamageTakenModifiers(creature, damage, source, CombatDamageType.Force);
            if (damage <= 0)
                return;

            AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, CombatDamageType.Force.GetNWScriptDamageType()), creature));

            if (!GetIsObjectValid(Source))
                return;

            Combat.ApplyDamageDealtEffects(
                Source,
                creature,
                damage,
                SkillType.Force,
                CombatDamageType.Force,
                CombatDamageDeliveryType.DamageOverTime);
            Ability.ApplyDarkForceDamageRestoration(Source, damage);
            StatusEffect.NotifyDamageStatusEffects(Source, creature, damage, CombatDamageType.Force, CombatDamageDeliveryType.DamageOverTime);
        }
    }
}
