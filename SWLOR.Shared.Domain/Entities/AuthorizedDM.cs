using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Domain.Admin.Enums;

namespace SWLOR.Shared.Domain.Entities
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
