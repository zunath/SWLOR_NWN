using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[AuthorizedDMs]")]
    public class AuthorizedDM: IEntity
    {
        [ExplicitKey]
        public int AuthorizedDMID { get; set; }
        public string Name { get; set; }
        public string CDKey { get; set; }
        public int DMRole { get; set; }
        public bool IsActive { get; set; }
    }
}
