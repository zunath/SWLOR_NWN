using SWLOR.Game.Server.Service.DMToolService;

namespace SWLOR.Game.Server.Domain.Administration
{
    /// <summary>
    /// Builds the icons, one-line summaries and details the DM chooser and creator show.
    /// Blueprints are read from their templates; items and stat skins are briefly created in the
    /// no-access temporary storage container so the same code that reads live objects can read them.
    /// The static facade <c>Service.DMObjectDetails</c> forwards to the container's instance of this interface.
    /// </summary>
    public interface IDMObjectDetailsService
    {
        /// <summary>
        /// Returns a blueprint's list icon and one-line summary. Results are kept for the rest of the server's uptime.
        /// </summary>
        (string Icon, string Summary) GetBlueprintRow(DMBlueprint blueprint);

        /// <summary>
        /// Describes a blueprint from its template.
        /// </summary>
        DMDetails DescribeBlueprint(DMBlueprint blueprint);

        /// <summary>
        /// Returns a live object's list icon and one-line summary.
        /// </summary>
        (string Icon, string Summary) GetObjectRow(uint obj);

        /// <summary>
        /// Describes a live area or object.
        /// </summary>
        DMDetails DescribeObject(uint obj);

        /// <summary>
        /// Counts the players in an area. DMs and creatures DMs possess are staff, not players.
        /// </summary>
        int CountPlayersInArea(uint area);

        /// <summary>
        /// Describes an area: who is in it and how big it is.
        /// </summary>
        DMDetails DescribeArea(uint area);
    }
}
