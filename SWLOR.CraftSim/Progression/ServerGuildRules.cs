using SWLOR.Game.Server.Domain.Economy;

namespace SWLOR.CraftSim.Progression
{
    /// <summary>
    /// The guild rank chart (points needed per rank, the top rank), asked of the server's own <see cref="GuildService"/>. The two members
    /// read only its rank table, never the database, so the instance is built without one and the simulator does not call the
    /// <c>Guild</c> facade (which needs the service container).
    /// </summary>
    public static class ServerGuildRules
    {
        private static readonly Lazy<GuildService> Service = new(() =>
        {
            var service = new GuildService(null);
            service.LoadData();
            return service;
        });

        /// <summary>The highest guild rank.</summary>
        public static int MaxRank => Service.Value.MaxRank;

        /// <summary>The guild points a crafter needs to leave <paramref name="rank"/>.</summary>
        public static int GetGPRequiredForRank(int rank) => Service.Value.GetGPRequiredForRank(rank);
    }
}
