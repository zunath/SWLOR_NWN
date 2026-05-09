using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class AuraStatusEffectBase : StatusEffectBase
    {
        public override StatusEffectStackType StackingType => StatusEffectStackType.StackFromMultipleSources;
        public override bool PersistsOnLogout => false;
    }
}
