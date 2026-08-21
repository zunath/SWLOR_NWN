using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class CleanseOrderTemporaryHitPointStatusEffectBase : SocialScalingStatusEffectBase
    {
        public const string TemporaryHitPointEffectKey = "CLEANSE_ORDER";

        private readonly long _temporaryHitPointApplicationId;

        protected CleanseOrderTemporaryHitPointStatusEffectBase(long temporaryHitPointApplicationId)
        {
            _temporaryHitPointApplicationId = temporaryHitPointApplicationId;
        }

        public override StatusEffectCategory Categories => StatusEffectCategory.None;
        public override bool PersistsOnLogout => false;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Command;

        protected override void Remove(uint creature)
        {
            TemporaryHitPointEffects.RemoveIfCurrent(
                creature,
                TemporaryHitPointEffectKey,
                _temporaryHitPointApplicationId);
        }
    }
}
