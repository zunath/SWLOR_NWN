using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CleanseOrder2StatusEffect : SocialScalingStatusEffectBase
    {
        public override string Name => "Cleanse Order II";
        public override EffectIconType Icon => EffectIconType.CleanseOrder2StatusEffect;
        // The visible marker accompanies temporary HP tracked by TemporaryHitPointEffects.
        // It must not consume a beneficial-effect purge while leaving that temporary HP intact.
        public override StatusEffectCategory Categories => StatusEffectCategory.None;
        public override bool PersistsOnLogout => false;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Command;
    }
}
