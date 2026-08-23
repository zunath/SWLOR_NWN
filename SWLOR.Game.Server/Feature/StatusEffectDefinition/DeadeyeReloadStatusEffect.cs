using System;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DeadeyeReloadStatusEffect : StatusEffectBase
    {
        public int CriticalRatePercent { get; }
        public override string Name => $"Deadeye Reload (+{CriticalRatePercent}% Critical Rate)";
        public override EffectIconType Icon => EffectIconType.DeadeyeReloadStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        public DeadeyeReloadStatusEffect()
            : this(5)
        {
        }

        public DeadeyeReloadStatusEffect(int criticalRatePercent)
        {
            CriticalRatePercent = Math.Clamp(criticalRatePercent, 5, 15);
        }

        public override IStatusEffect Clone()
        {
            return new DeadeyeReloadStatusEffect(CriticalRatePercent);
        }
    }
}
