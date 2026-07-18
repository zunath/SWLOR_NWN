using System;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ButchersTempoStatusEffect : StatusEffectBase
    {
        private readonly int _attackPercent;

        public override string Name => $"Butcher's Tempo (+{_attackPercent}% Attack)";
        public override EffectIconType Icon => EffectIconType.ButchersTempoStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public ButchersTempoStatusEffect()
            : this(1)
        {
        }

        public ButchersTempoStatusEffect(int attackPercent)
        {
            _attackPercent = Math.Max(1, attackPercent);
        }

        public override IStatusEffect Clone()
        {
            return new ButchersTempoStatusEffect(_attackPercent);
        }
    }
}
