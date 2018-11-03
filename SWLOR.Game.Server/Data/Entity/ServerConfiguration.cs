
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("ServerConfiguration")]
    public partial class ServerConfiguration: IEntity, ICacheable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ServerConfiguration()
        {
            this.ServerName = "";
            this.MessageOfTheDay = "";
        }

        [ExplicitKey]
        public int ServerConfigurationID { get; set; }
        public string ServerName { get; set; }
        public string MessageOfTheDay { get; set; }
        public int AreaBakeStep { get; set; }
    }
}
