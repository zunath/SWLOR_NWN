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
    }
}
