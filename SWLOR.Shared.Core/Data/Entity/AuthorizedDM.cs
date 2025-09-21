using SWLOR.Game.Server.Enumeration;
using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Core.Data.Entity
{
    public class AuthorizedDM: EntityBase
    {
        [Indexed]
        public string Name { get; set; }
        [Indexed]
        public string CDKey { get; set; }
        [Indexed]
        public AuthorizationLevel Authorization { get; set; }
    }
}
