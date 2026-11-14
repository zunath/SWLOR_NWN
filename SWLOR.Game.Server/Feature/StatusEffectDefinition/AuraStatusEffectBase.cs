using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class AuraStatusEffectBase : SocialScalingStatusEffectBase
    {
        /// <summary>The scale of a value with no stated one: the 25% SOC 26 adds to a direct effect.</summary>
        public static readonly (int Base, int Maximum) StandardStrengthScale = (100, 125);

        /// <summary>
        /// The base and maximum of the value that decides which commander's copy of this aura is stronger, for a space aura
        /// competing with another commander's copy (<see cref="Service.SpaceAuras"/>).
        /// </summary>
        public virtual (int Base, int Maximum) StrengthScale => StandardStrengthScale;

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
