using System;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class LuckyChamberStatusEffect : StatusEffectBase
    {
        public int AttackCount { get; }
        public override string Name => $"Lucky Chamber ({AttackCount}/4)";
        public override EffectIconType Icon => EffectIconType.LuckyChamberStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        public LuckyChamberStatusEffect()
            : this(1)
        {
        }

        public LuckyChamberStatusEffect(int attackCount)
        {
            AttackCount = Math.Clamp(attackCount, 1, 4);
        }

        public override IStatusEffect Clone()
        {
            return new LuckyChamberStatusEffect(AttackCount);
        }
    }
}
