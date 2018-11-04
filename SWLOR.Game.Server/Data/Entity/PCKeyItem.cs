
using System;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCKeyItems]")]
    public class PCKeyItem: IEntity
    {
        [Key]
        public int PCKeyItemID { get; set; }
        public string PlayerID { get; set; }
        public int KeyItemID { get; set; }
        public DateTime AcquiredDate { get; set; }
    }
}
