namespace SWLOR.Game.Server.Service.PropertyService
{
    public enum PropertyTaxType
    {
        Invalid = 0,

        /// <summary>
        /// This is a flat fee which is charged to all citizens once a week.
        /// </summary>
        Citizenship = 1,

        /// <summary>
        /// This is a percentage-base fee which is charged when transportation fees are paid.
        /// </summary>
        Transportation = 2,

        /// <summary>
        /// This is a whole percent (0 to 10) added to purchases and taken from sales at the city starport's Trade Exchange.
        /// Cities created before the exchange have no entry, which counts as 0.
        /// </summary>
        Exchange = 3,

        /// <summary>
        /// Flat credits (0 to the cap) charged for each Capital hull built at a player station's Shipyard, paid into the
        /// treasury. Cities without an entry charge none.
        /// </summary>
        Shipyard = 4,
    }
}
