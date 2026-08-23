using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    // Icon identity anchor for the stat-configured CriticalRateStackTrackerStatusEffect.
    public sealed class DeadeyeReloadStatusEffect : StatusEffectBase
    {
        public override string Name => "Deadeye Reload";
        public override EffectIconType Icon => EffectIconType.DeadeyeReloadStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
    }
}
