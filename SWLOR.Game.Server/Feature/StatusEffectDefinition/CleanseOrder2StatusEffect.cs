using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CleanseOrder2StatusEffect : SocialScalingStatusEffectBase
    {
        public override string Name => "Cleanse Order II";
        public override EffectIconType Icon => EffectIconType.CleanseOrder2StatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Command;
    }
}
