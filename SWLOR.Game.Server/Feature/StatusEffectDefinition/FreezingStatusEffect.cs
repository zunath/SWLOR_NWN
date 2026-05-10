using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FreezingStatusEffect : StatusEffectBase
    {
        private readonly int _level;
        public override string Name => "Freezing";
        public override EffectIconType Icon => EffectIconType.DamageImmunityColdDecrease;
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
            var perception = GetAbilityModifier(AbilityType.Perception, Source);
            var damage = d2() + perception * _level;
            damage = Resistance.ApplyResistanceToDamage(creature, ResistanceType, damage);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, CombatDamageType.Ice.GetNWScriptDamageType()), creature);
            ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Cyan_Blue), creature, 5.9f);
            StatusEffect.ApplyStatusEffect(Source, creature, typeof(FreezingMightPenaltyStatusEffect), 6f);
        }

        protected override void Remove(uint creature)
        {
            StatusEffect.RemoveStatusEffect(creature, typeof(FreezingMightPenaltyStatusEffect), Source, false);
        }
    }
}
