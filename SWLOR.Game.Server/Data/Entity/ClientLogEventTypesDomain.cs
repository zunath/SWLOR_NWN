
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("ClientLogEventTypesDomain")]
    public partial class ClientLogEventTypesDomain: IEntity, ICacheable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ClientLogEventTypesDomain()
        {
            this.ClientLogEvents = new HashSet<ClientLogEvent>();
        }

        [ExplicitKey]
        public int ClientLogEventTypeID { get; set; }
        public string Name { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ClientLogEvent> ClientLogEvents { get; set; }
    }
}
