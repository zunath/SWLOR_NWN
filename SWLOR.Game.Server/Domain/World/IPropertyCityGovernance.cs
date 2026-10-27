using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// City governance: the boot-time processing of every city, the Discord broadcasts about it, and the city level rules.
    /// </summary>
    public interface IPropertyCityGovernance
    {
        /// <summary>
        /// Posts an event of a city to the property broadcast Discord channel. Nothing happens when no webhook is configured.
        /// </summary>
        /// <param name="city">The city the event is about.</param>
        /// <param name="title">The title of the post.</param>
        /// <param name="details">The text of the post.</param>
        /// <param name="color">The color of the post.</param>
        void BroadcastPropertyEvent(WorldProperty city, string title, string details, int color = PropertyCityGovernance.PropertyBroadcastColor);

        /// <summary>
        /// Formats a date the way the broadcasts show it, in UTC.
        /// </summary>
        /// <param name="date">The date to format.</param>
        /// <returns>The formatted date.</returns>
        string FormatUtcDate(DateTime date);

        void BroadcastCityUpkeepPaid(WorldProperty city);

        /// <summary>
        /// Handles the processing of each individual city. The following will occur on each boot:
        /// 1.) Checking for the required number of citizens.
        ///     If this is the first reboot where they're under the requirement, 18 hours will be given to them for correction.
        ///     Otherwise if the timer has expired, the city will be destroyed.
        /// 2.) Processing elections. Moves the election process between stages and calculates votes at the end of the process.
        ///     If a new mayor is elected, the previous mayor's permissions are removed and given to the new one.
        /// </summary>
        void ProcessCities();

        /// <summary>
        /// Retrieves the name of a particular city level.
        /// </summary>
        /// <param name="level">The level to retrieve</param>
        /// <returns>A string representing the city level.</returns>
        string GetCityLevelName(int level);

        /// <summary>
        /// Retrieves the number of citizens required for the next city level.
        /// If level isn't supported, -1 will be returned.
        /// </summary>
        /// <param name="level">The level to retrieve</param>
        /// <returns>The number of citizens required to level up the city.</returns>
        int GetCitizensRequiredForNextCityLevel(int level);

        /// <summary>
        /// Retrieves the effective upgrade level of a city, taking into account the city's overall level
        /// into the calculation.
        /// </summary>
        /// <param name="cityId">The property Id of the city.</param>
        /// <param name="upgradeType">The type of upgrade to check</param>
        /// <returns>A value ranging between 0 and 5.</returns>
        int GetEffectiveUpgradeLevel(string cityId, PropertyUpgradeType upgradeType);
    }
}
