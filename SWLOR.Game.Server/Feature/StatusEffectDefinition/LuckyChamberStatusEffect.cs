using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    // Icon identity anchor for the stat-configured AttackCycleTrackerStatusEffect.
    public sealed class LuckyChamberStatusEffect : StatusEffectBase
    {
        public override string Name => "Lucky Chamber";
        public override EffectIconType Icon => EffectIconType.LuckyChamberStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
    }
}
