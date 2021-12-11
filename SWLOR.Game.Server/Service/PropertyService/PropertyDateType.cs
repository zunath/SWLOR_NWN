namespace SWLOR.Game.Server.Service.PropertyService
{
    public enum PropertyDateType
    {
        Invalid = 0,
        /// <summary>
        /// Used for apartments to indicate the date a lease is owed.
        /// </summary>
        Lease = 1,

        /// <summary>
        /// Used for cities to indicate when weekly upkeep is owed.
        /// </summary>
        Upkeep = 2,

        /// <summary>
        /// Used for cities to indicate when the next election starts
        /// or when the current election started.
        /// </summary>
        ElectionStart = 3,

        /// <summary>
        /// Used for cities to indicate when a city should be destroyed due
        /// to falling under the required number of citizens.
        /// </summary>
        BelowRequiredCitizens = 4,

        /// <summary>
        /// Used for cities to indicate when a city should be destroyed due
        /// to failing to pay upkeep.
        /// </summary>
        DisrepairDestruction = 5,
    }
}
