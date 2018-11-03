
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("ClientLogEventTypesDomain")]
    public class ClientLogEventTypesDomain: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ClientLogEventTypesDomain()
        {
        }

        [ExplicitKey]
        public int ClientLogEventTypeID { get; set; }
        public string Name { get; set; }
    
    }
}
