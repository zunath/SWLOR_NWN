using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Admin.Entity
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
