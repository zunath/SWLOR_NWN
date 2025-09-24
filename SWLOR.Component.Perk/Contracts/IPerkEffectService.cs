namespace SWLOR.Component.Perk.Contracts
{
    /// <summary>
    /// Interface for handling perk-specific effects and behaviors.
    /// </summary>
    public interface IPerkEffectService
    {
        /// <summary>
        /// When a weapon hits, apply Alacrity and Clarity effects for OneHanded perks.
        /// </summary>
        void ApplyAlacrityAndClarity();

        /// <summary>
        /// When a weapon hits, process Force Link for Beast Force perks.
        /// </summary>
        void OnForceLinkHit();

        /// <summary>
        /// When a weapon hits, process Endurance Link for Beast Bruiser perks.
        /// </summary>
        void OnEnduranceLinkHit();
    }
}
