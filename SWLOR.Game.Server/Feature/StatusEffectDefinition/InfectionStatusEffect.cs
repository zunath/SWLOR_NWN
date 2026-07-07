using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class InfectionStatusEffect : StatusEffectBase
    {
        private const int DamagePerStack = 2;

        public int Stacks { get; private set; }

        public override string Name => Stacks > 1
            ? $"Infection ({Stacks})"
            : "Infection";
        public override EffectIconType Icon => EffectIconType.InfectionStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff | StatusEffectCategory.Infection;
        public override StatusEffectStackType StackingType => StatusEffectStackType.StackFromMultipleSources;
        public override ResistanceType ResistanceType => ResistanceType.Poison;
        public override StatusEffectCleanseType CleanseTypes =>
            StatusEffectCleanseType.Purify |
            StatusEffectCleanseType.TreatmentKit2 |
            StatusEffectCleanseType.SoothePet;
        public override float Frequency => 6f;

        public InfectionStatusEffect()
            : this(1)
        {
        }

        public InfectionStatusEffect(int stacks)
        {
            Stacks = Math.Max(1, stacks);
        }

        public void AddStack(int maximumStacks)
        {
            Stacks = Math.Min(Math.Max(1, maximumStacks), Stacks + 1);
        }

        public override IStatusEffect Clone()
        {
            return new InfectionStatusEffect(Stacks);
        }

        protected override void Tick(uint creature)
        {
            var source = GetIsObjectValid(Source) ? Source : creature;
            var damageAmount = Combat.ApplyDamageTypeDealtModifiers(
                source,
                DamagePerStack * Math.Max(1, Stacks),
                CombatDamageType.Poison);
            damageAmount = Resistance.ApplyResistanceToDamage(creature, ResistanceType, damageAmount);
            damageAmount = Combat.ApplyDamageOverTimeTakenModifiers(creature, damageAmount, CombatDamageType.Poison);
            damageAmount = Combat.ApplyDamageTakenModifiers(
                creature,
                damageAmount,
                source,
                CombatDamageType.Poison,
                CombatDamageDeliveryType.DamageOverTime);
            if (damageAmount <= 0)
                return;

            AssignCommand(
                source,
                () => ApplyEffectToObject(
                    DurationType.Instant,
                    EffectDamage(damageAmount, CombatDamageType.Poison.GetNWScriptDamageType()),
                    creature));
        }
    }
}
