
using Dapper.Contrib.Extensions;

namespace SWLOR.Game.Server.Data
{
    using System;
    using System.Collections.Generic;
    
    using SWLOR.Game.Server.Data.Contracts;
    
    public partial class AuthorizedDM: IEntity
    {
        [ExplicitKey]
        public int AuthorizedDMID { get; set; }
        public string Name { get; set; }
        public string CDKey { get; set; }
        public int DMRole { get; set; }
        public bool IsActive { get; set; }
    }
}
