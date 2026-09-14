using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PoisonResistancePenaltyStatusEffect : StatusEffectBase
    {
        public int Penalty { get; }

        public override string Name => "Poison Resistance";
        public override EffectIconType Icon => EffectIconType.PoisonResistancePenaltyStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override ResistanceType ResistanceType => ResistanceType.Poison;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;

        public PoisonResistancePenaltyStatusEffect()
            : this(-15)
        {
        }

        public PoisonResistancePenaltyStatusEffect(int penalty)
        {
            Penalty = -Math.Abs(penalty);
            StatGroup.Stats[StatType.PoisonDefense] = Penalty;
        }

        public override string CanApply(uint creature)
        {
            var existing = StatusEffect.GetStatusEffect(creature, GetType()) as PoisonResistancePenaltyStatusEffect;
            return existing != null && Math.Abs(existing.Penalty) >= Math.Abs(Penalty)
                ? "A more powerful effect is active."
                : string.Empty;
        }

        public override IStatusEffect Clone()
        {
            return new PoisonResistancePenaltyStatusEffect(Penalty);
        }
    }
}
