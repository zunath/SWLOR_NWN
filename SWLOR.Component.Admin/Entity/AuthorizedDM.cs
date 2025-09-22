using SWLOR.Component.Admin.Enums;
using SWLOR.Shared.Abstractions;

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
