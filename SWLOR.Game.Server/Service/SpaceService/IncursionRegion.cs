using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>An incursion region: an orbit with a dock (its centre) and its starlane neighbours.</summary>
    public class IncursionRegion
    {
        public string Center { get; set; }
        public string Name { get; set; }

        /// <summary>The centre's tier: the region reward's tier.</summary>
        public int Tier { get; set; }

        public IReadOnlyList<IncursionArea> Areas { get; set; } = Array.Empty<IncursionArea>();
    }
}
