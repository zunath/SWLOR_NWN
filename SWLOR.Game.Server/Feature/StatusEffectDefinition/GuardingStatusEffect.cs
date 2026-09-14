using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class GuardingStatusEffect : StatusEffectBase
    {
        public override string Name => "Guarding";
        public override EffectIconType Icon => EffectIconType.GuardingStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override float Frequency => 1f;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        protected override void Tick(uint creature)
        {
            if (!GuardedStatusEffect.HasGuardedTarget(creature))
                IsFlaggedForRemoval = true;
        }
    }
}
