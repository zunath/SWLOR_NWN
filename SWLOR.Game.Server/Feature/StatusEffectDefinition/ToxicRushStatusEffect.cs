using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ToxicRushStatusEffect : StatusEffectBase
    {
        private readonly int _hastePercentPerStack;
        private readonly int _attackPercentPerStack;

        public override string Name => "Toxic Rush";
        public override EffectIconType Icon => EffectIconType.ToxicRushStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;
        public override bool PersistsOnLogout => false;
        public int Stacks { get; }

        public ToxicRushStatusEffect()
            : this(5, 4, 3)
        {
        }

        public ToxicRushStatusEffect(int stacks, int hastePercentPerStack, int attackPercentPerStack)
        {
            Stacks = Math.Max(0, stacks);
            _hastePercentPerStack = hastePercentPerStack;
            _attackPercentPerStack = attackPercentPerStack;

            StatGroup.Stats[StatType.AttackPercentAdjustment] = _attackPercentPerStack * Stacks;
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = _hastePercentPerStack * Stacks;
        }

        public override IStatusEffect Clone()
        {
            return new ToxicRushStatusEffect(Stacks, _hastePercentPerStack, _attackPercentPerStack);
        }
    }
}
