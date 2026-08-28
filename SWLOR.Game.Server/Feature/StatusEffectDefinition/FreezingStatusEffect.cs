using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FreezingStatusEffect : StatusEffectBase
    {
        private readonly int _level;
        public override string Name => "Freezing";
        public override EffectIconType Icon => EffectIconType.FreezingStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override ResistanceType ResistanceType => ResistanceType.Ice;
        public override float Frequency => 6f;

        public FreezingStatusEffect()
            : this(1)
        {
        }

        public FreezingStatusEffect(int level)
        {
            _level = System.Math.Max(1, level);
        }

        public override IStatusEffect Clone()
        {
            return new FreezingStatusEffect(_level);
        }

        protected override void Tick(uint creature)
        {
            var source = GetIsObjectValid(Source) ? Source : creature;
            var perception = GetAbilityModifier(AbilityType.Perception, source);
            var damage = System.Math.Max(1, d4() + perception * 2 * _level);
            var mimicryPotency = Stat.GetStatAdjustment(source, StatType.MimicryPotencyPercent);
            if (mimicryPotency > 0)
            {
                damage += (int)System.Math.Ceiling(damage * (mimicryPotency / 100f));
            }

            damage = Resistance.ApplyResistanceToDamage(creature, ResistanceType, damage);
            damage = Combat.ApplyDamageOverTimeTakenModifiers(creature, damage, CombatDamageType.Ice);
            damage = Combat.ApplyDamageTakenModifiers(creature, damage, source, CombatDamageType.Ice);
            if (damage > 0)
            {
                AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, CombatDamageType.Ice.GetNWScriptDamageType()), creature));
            }

            ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Cyan_Blue), creature, 5.9f);
            StatusEffect.ApplyStatusEffect(source, creature, typeof(FreezingMightPenaltyStatusEffect), 30f);
        }

        protected override void Remove(uint creature)
        {
            var source = GetIsObjectValid(Source) ? Source : creature;
            StatusEffect.RemoveStatusEffect(creature, typeof(FreezingMightPenaltyStatusEffect), source, false);
        }
    }
}
