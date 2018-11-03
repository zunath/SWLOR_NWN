
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("CraftDevices")]
    public class CraftDevice: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public CraftDevice()
        {
            Name = "";
        }

        [ExplicitKey]
        public int CraftDeviceID { get; set; }
        public string Name { get; set; }
    }
}
