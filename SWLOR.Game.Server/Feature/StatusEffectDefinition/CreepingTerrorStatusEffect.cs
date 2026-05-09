using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CreepingTerrorStatusEffect : StatusEffectBase
    {
        private readonly int _level;
        public override string Name => "Creeping Terror";
        public override EffectIconType Icon => EffectIconType.Curse;
        public override float Frequency => 6f;

        public CreepingTerrorStatusEffect()
            : this(1)
        {
        }

        public CreepingTerrorStatusEffect(int level)
        {
            _level = System.Math.Clamp(level, 1, 3);
        }

        public override IStatusEffect Clone()
        {
            return new CreepingTerrorStatusEffect(_level);
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            const float Duration = 6f;
            var dc = _level switch
            {
                1 => 8,
                2 => 12,
                _ => 14
            };

            dc = Combat.CalculateSavingThrowDC(Source, SavingThrow.Will, dc);
            if (WillSave(creature, dc, SavingThrowType.None, Source) == SavingThrowResultType.Failed)
            {
                var effect = TagEffect(EffectEntangle(), Id);
                ApplyEffectToObject(DurationType.Temporary, effect, creature, Duration);
                Ability.ApplyTemporaryImmunity(creature, Duration, ImmunityType.Entangle);
            }

            ApplyDamage(creature);
            Enmity.ModifyEnmity(Source, creature, 350);
        }

        protected override void Tick(uint creature)
        {
            ApplyDamage(creature);
        }

        private void ApplyDamage(uint creature)
        {
            var willpower = GetAbilityScore(Source, AbilityType.Willpower);
            var dmg = _level switch
            {
                1 => willpower / 2,
                2 => willpower,
                _ => willpower * 3 / 2
            };

            var attackerStat = GetAbilityScore(Source, AbilityType.Willpower);
            var defenderStat = GetAbilityScore(creature, AbilityType.Willpower);
            var attack = Stat.GetAttack(Source, AbilityType.Willpower, SkillType.Force);
            var defense = Stat.GetDefense(creature, CombatDamageType.Force, AbilityType.Willpower);
            var damage = Combat.CalculateDamage(attack, dmg, attackerStat, defense, defenderStat, 0);

            AssignCommand(Source, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), creature));
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Poison_S), creature);

            Enmity.ModifyEnmity(Source, creature, _level * 50 + damage + 6);
            CombatPoint.AddCombatPoint(Source, creature, SkillType.Force, 3);
        }
    }
}
