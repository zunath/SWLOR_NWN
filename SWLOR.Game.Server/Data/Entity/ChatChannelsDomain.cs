using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("ChatChannelsDomain")]
    public partial class ChatChannelsDomain: IEntity, ICacheable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ChatChannelsDomain()
        {
            this.Name = "";
            this.ChatLogs = new HashSet<ChatLog>();
        }

        [ExplicitKey]
        public int ChatChannelID { get; set; }
        public string Name { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ChatLog> ChatLogs { get; set; }
    }
}
