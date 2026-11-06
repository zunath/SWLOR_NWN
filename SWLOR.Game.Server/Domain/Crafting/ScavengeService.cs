using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Service.ScavengeService;

namespace SWLOR.Game.Server.Domain.Crafting
{
    /// <inheritdoc cref="IScavengeService" />
    public class ScavengeService : IScavengeService
    {
        public ScavengeService(IDBService db)
        {
            World = new NwnScavengeWorld(db);
        }

        /// <inheritdoc />
        public NwnScavengeWorld World { get; }
    }
}
