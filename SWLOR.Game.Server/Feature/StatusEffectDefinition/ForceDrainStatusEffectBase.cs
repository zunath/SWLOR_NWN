using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class ForceDrainStatusEffectBase : StatusEffectBase
    {
        public override EffectIconType Icon => EffectIconType.LevelDrain;
        public override float Frequency => 6f;
        public override bool PersistsOnLogout => false;

        protected abstract int BaseDamage { get; }
        protected abstract int DiceSize { get; }
        protected abstract int ApplyEnmity { get; }
        protected abstract int TickEnmity { get; }

        protected override void Apply(uint creature, int durationTicks)
        {
            Drain(creature, ApplyEnmity);
        }

        protected override void Tick(uint creature)
        {
            Drain(creature, TickEnmity);
        }

        private void Drain(uint target, int enmity)
        {
            var willBonus = GetAbilityScore(Source, AbilityType.Willpower);
            var damage = BaseDamage + willBonus + Roll(willBonus / 3);
            ProcessForceDrainTick(VisualEffect.Vfx_Beam_Drain, damage, damage, target);
            Enmity.ModifyEnmityOnAll(Source, enmity);

            CombatPoint.AddCombatPoint(Source, target, SkillType.Force, 3);
        }

        private int Roll(int amount)
        {
            return DiceSize switch
            {
                2 => Random.D2(amount),
                3 => Random.D3(amount),
                4 => Random.D4(amount),
                6 => Random.D6(amount),
                8 => Random.D8(amount),
                _ => 0
            };
        }

        private void ProcessForceDrainTick(VisualEffect vfx, int damage, int heal, uint target)
        {
            var distance = GetDistanceBetween(Source, target);
            if (distance > 20.0f)
            {
                Ability.EndConcentrationAbility(Source);
                SendMessageToPC(Source, "Your Force Drain connection has been broken due to distance.");
                return;
            }

            var dc = Combat.CalculateSavingThrowDC(Source, SavingThrow.Will, 14);
            var checkResult = WillSave(target, dc, SavingThrowType.None, Source);

            if (checkResult != SavingThrowResultType.Failed)
                return;

            PlaySound("plr_force_absorb");

            AssignCommand(Source, () =>
            {
                ApplyEffectToObject(DurationType.Temporary, EffectBeam(vfx, target, BodyNode.Hand), Source, 2.0f);
                ApplyEffectToObject(DurationType.Temporary, EffectBeam(vfx, Source, BodyNode.Hand), target, 2.0f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Negative_Energy), target);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Reduce_Ability_Score), target);
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
                ApplyEffectToObject(DurationType.Instant, EffectHeal(heal), Source);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Pulse_Negative), Source);
            });
        }
    }
}
