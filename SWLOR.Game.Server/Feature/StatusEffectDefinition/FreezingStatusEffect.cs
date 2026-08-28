using System;
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
            var mimicryPotency = Stat.GetStatAdjustment(source, StatType.MimicryPotencyPercent);
            var damage = CalculateTickDamage(
                d4(),
                perception,
                _level,
                mimicryPotency,
                amount => Resistance.ApplyResistanceToDamage(creature, ResistanceType, amount),
                amount => Combat.ApplyDamageOverTimeTakenModifiers(creature, amount, CombatDamageType.Ice),
                amount => Combat.ApplyDamageTakenModifiers(creature, amount, source, CombatDamageType.Ice));
            if (damage > 0)
            {
                AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, CombatDamageType.Ice.GetNWScriptDamageType()), creature));
            }

            ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Cyan_Blue), creature, 5.9f);
            StatusEffect.ApplyStatusEffect(source, creature, typeof(FreezingMightPenaltyStatusEffect), 30f);
        }

        /// <summary>
        /// Calculates one Freezing damage tick and applies each defensive stage in gameplay order.
        /// The delegates keep the formula deterministic and independently testable without an NWN runtime.
        /// </summary>
        public static int CalculateTickDamage(
            int dieRoll,
            int perceptionModifier,
            int level,
            int mimicryPotencyPercent,
            Func<int, int> applyResistance,
            Func<int, int> applyDamageOverTimeTaken,
            Func<int, int> applyDamageTaken)
        {
            var damage = Math.Max(1, dieRoll + perceptionModifier * 2 * Math.Max(1, level));
            if (mimicryPotencyPercent > 0)
            {
                damage += (int)Math.Ceiling(damage * (mimicryPotencyPercent / 100f));
            }

            damage = applyResistance(damage);
            damage = applyDamageOverTimeTaken(damage);
            return applyDamageTaken(damage);
        }

        protected override void Remove(uint creature)
        {
            var source = GetIsObjectValid(Source) ? Source : creature;
            StatusEffect.RemoveStatusEffect(creature, typeof(FreezingMightPenaltyStatusEffect), source, false);
        }
    }
}
