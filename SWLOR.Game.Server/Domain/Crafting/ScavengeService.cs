using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Crafting;

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
