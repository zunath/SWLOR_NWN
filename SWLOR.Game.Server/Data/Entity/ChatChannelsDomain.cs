using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("ChatChannelsDomain")]
    public class ChatChannelsDomain: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ChatChannelsDomain()
        {
            Name = "";
        }

        [ExplicitKey]
        public int ChatChannelID { get; set; }
        public string Name { get; set; }
    }
}
