using SWLOR.Core.Enumeration;

namespace SWLOR.Core.Entity
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
