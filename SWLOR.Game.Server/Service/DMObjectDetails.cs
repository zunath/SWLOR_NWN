using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Service.DMToolService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API for the icons, one-line summaries and details the DM chooser and creator show.
    /// Blueprints are read from their templates; items and stat skins are briefly created in the
    /// no-access temporary storage container so the same code that reads live objects can read them.
    /// The logic and state live in <see cref="IDMObjectDetailsService"/>; every member here forwards to the
    /// container's instance.
    /// </summary>
    public static class DMObjectDetails
    {
        /// <summary>
        /// Shown in the details panel when nothing is selected.
        /// </summary>
        public const string NoSelectionIcon = Domain.Administration.DMObjectDetailsService.NoSelectionIcon;

        /// <summary>
        /// Shown for areas, which have no portrait.
        /// </summary>
        public const string AreaIcon = Domain.Administration.DMObjectDetailsService.AreaIcon;

        private static IDMObjectDetailsService Instance => FacadeAccessor.Resolve<IDMObjectDetailsService>();

        /// <summary>
        /// Returns a blueprint's list icon and one-line summary. Results are kept for the rest of the server's uptime.
        /// </summary>
        public static (string Icon, string Summary) GetBlueprintRow(DMBlueprint blueprint)
        {
            return Instance.GetBlueprintRow(blueprint);
        }

        /// <summary>
        /// Describes a blueprint from its template.
        /// </summary>
        public static DMDetails DescribeBlueprint(DMBlueprint blueprint)
        {
            return Instance.DescribeBlueprint(blueprint);
        }

        /// <summary>
        /// Returns a live object's list icon and one-line summary.
        /// </summary>
        public static (string Icon, string Summary) GetObjectRow(uint obj)
        {
            return Instance.GetObjectRow(obj);
        }

        /// <summary>
        /// Describes a live area or object.
        /// </summary>
        public static DMDetails DescribeObject(uint obj)
        {
            return Instance.DescribeObject(obj);
        }

        /// <summary>
        /// Counts the players in an area. DMs and creatures DMs possess are staff, not players.
        /// </summary>
        public static int CountPlayersInArea(uint area)
        {
            return Instance.CountPlayersInArea(area);
        }

        /// <summary>
        /// Describes an area: who is in it and how big it is.
        /// </summary>
        public static DMDetails DescribeArea(uint area)
        {
            return Instance.DescribeArea(area);
        }
    }
}
