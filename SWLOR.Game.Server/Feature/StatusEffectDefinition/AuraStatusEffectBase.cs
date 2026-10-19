using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class AuraStatusEffectBase : SocialScalingStatusEffectBase
    {
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        /// <summary>
        /// False when the aura is re-applied to an ally who was just told about it, so edge-of-range
        /// movement does not repeat the application notice.
        /// </summary>
        public bool AnnouncesApplication { get; set; } = true;
        public override bool SendsApplicationMessage => AnnouncesApplication;
    }
}
