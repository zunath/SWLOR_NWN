namespace SWLOR.Game.Server.Service.IndustryService
{
    /// <summary>
    /// Shadowport prices and bust chances. These are proposals awaiting the owner's approval, so every number sits here and nowhere
    /// else. A price share is a share of the lowest price a black market pays for the good; a bust multiple multiplies the chance
    /// one customs patrol scan would find the same cargo.
    /// </summary>
    public static class ShadowportTuning
    {
        /// <summary>Normal goods sell for this share of the usual price at a Shadowport.</summary>
        public const double NormalGoodsSaleFactor = 0.85;

        public const double ShadowportPriceShare = 0.90;
        public const double LawfulPriceShare = 0.60;

        public const double ShadowportBustMultiple = 0.5;
        public const double LawfulBustMultiple = 3.0;

        /// <summary>No sale is ever certain to be busted.</summary>
        public const double BustMaximum = 0.9;
    }
}
