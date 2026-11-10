using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Crafting;

namespace SWLOR.Game.Server.Domain.Crafting
{
    /// <inheritdoc cref="IProspectingService" />
    public class ProspectingService : IProspectingService
    {
        public ProspectingService(IDBService db)
        {
            World = new NwnProspectingWorld(db);
        }

        /// <inheritdoc />
        public NwnProspectingWorld World { get; }
    }
}
