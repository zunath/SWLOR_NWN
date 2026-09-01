using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CleanseOrder1StatusEffect : SocialScalingStatusEffectBase
    {
        public const string TemporaryHitPointEffectKey = "CLEANSE_ORDER";

        public override string Name => "Cleanse Order I";
        public override EffectIconType Icon => EffectIconType.CleanseOrder1StatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Command;

        protected override void Remove(uint creature)
        {
            TemporaryHitPointEffects.RemoveIfCurrent(creature, TemporaryHitPointEffectKey, Id);
        }
    }
}
