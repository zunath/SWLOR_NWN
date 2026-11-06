using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Service.ProspectingService;

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
